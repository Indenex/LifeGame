using System;
using Godot;

namespace LifeGame;

// A small toolkit for building models in code: tubes lofted through a list of
// cross-sections (limbs, bodies, tails), ellipsoids, and flat blades (leaves).
// Every vertex is bound wholly to one bone, so a figure is one skinned mesh and
// one draw call per surface. Faceted models share one normal per face; smooth
// ones use per-vertex normals. Colors ride in the vertices for the painted shader.
public sealed class MeshKit
{
    // One cross-section of a loft, at T (0 to 1) along the axis. Width runs across
    // the axis sideways (x for an upright part), depth front to back (z). Shift
    // and Lift move the section's center forward and sideways.
    public readonly record struct Ring(float T, float Width, float Depth, float Shift = 0f, float Side = 0f);

    readonly SurfaceTool st = new();
    public readonly bool Flat;
    int bone;
    Color color = Colors.White;
    public int Triangles { get; private set; }
    // Applied to every vertex as it is written, e.g. to lay a held item on the ground.
    // Must be a rotation plus a move, so faces keep their winding.
    public Transform3D Xform = Transform3D.Identity;
    public bool Empty => Triangles == 0;

    public MeshKit(bool flat)
    {
        Flat = flat;
        st.Begin(Mesh.PrimitiveType.Triangles);
    }

    public MeshKit Bone(int index)
    {
        bone = index;
        return this;
    }

    // The shader raises vertex colors to 2.2, so a palette color is written so that
    // it comes out as the sRGB color it was authored as, darkened by shade.
    public MeshKit Paint(Color part, float shade = 1f)
    {
        var lin = part.SrgbToLinear();
        const float g = 1f / 2.2f;
        color = new Color(Mathf.Pow(lin.R, g) * shade, Mathf.Pow(lin.G, g) * shade, Mathf.Pow(lin.B, g) * shade);
        return this;
    }

    public void Commit(ArrayMesh mesh, Material material)
    {
        st.Commit(mesh);
        mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, material);
        st.Clear();
        st.Begin(Mesh.PrimitiveType.Triangles);
    }

    // Godot's front faces wind clockwise. The outward hint picks the winding, so
    // callers never have to think about it.
    public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
    {
        var face = (c - a).Cross(b - a);
        if (face.LengthSquared() < 1e-12f) return;
        if (face.Dot(na + nb + nc) < 0f)
        {
            (b, c) = (c, b);
            (nb, nc) = (nc, nb);
            face = -face;
        }
        if (Flat) na = nb = nc = face.Normalized();
        Vertex(a, na);
        Vertex(b, nb);
        Vertex(c, nc);
        Triangles++;
    }

    // Bones and weights only when the mesh will be skinned; static items skip them.
    public bool Skinned = true;

    // For meshes drawn with the grass shader: UV.y runs from 0 at y = 0 to 1 at
    // this height, and the shader sways vertices by it. Zero writes no UVs.
    public float SwayHeight;

    void Vertex(Vector3 p, Vector3 n)
    {
        st.SetColor(color);
        if (Skinned)
        {
            st.SetBones(new[] { bone, 0, 0, 0 });
            st.SetWeights(new[] { 1f, 0f, 0f, 0f });
        }
        st.SetNormal(Xform.Basis * n);
        var at = Xform * p;
        if (SwayHeight > 0f) st.SetUV(new Vector2(0f, Mathf.Clamp(at.Y / SwayHeight, 0f, 1f)));
        st.AddVertex(at);
    }

    public Godot.Collections.Array CommitToArrays()
    {
        var arrays = st.CommitToArrays();
        st.Clear();
        st.Begin(Mesh.PrimitiveType.Triangles);
        return arrays;
    }

    // Two axes across a direction: side (x for an upright part) and front (z).
    public static (Vector3 side, Vector3 front) Frame(Vector3 dir)
    {
        dir = dir.Normalized();
        var reference = Mathf.Abs(dir.Z) > 0.9f ? Vector3.Up : Vector3.Back;
        var side = dir.Cross(reference).Normalized();
        var front = side.Cross(dir).Normalized();
        // Keep side pointing to +x and front to +z (or up) whichever way the axis runs.
        if (side.X < -1e-4f || (Mathf.Abs(side.X) <= 1e-4f && side.Y < 0f)) side = -side;
        if (front.Z < -1e-4f || (Mathf.Abs(front.Z) <= 1e-4f && front.Y < 0f)) front = -front;
        return (side, front);
    }

    // A tube from a to b through the given sections, capped at both ends. Caps bulge
    // out by capBulge times the end radius so limb ends look rounded, not cut.
    // twist turns the polygon so a flat face (or an edge) points forward.
    public void Loft(Vector3 a, Vector3 b, Ring[] rings, int sides, float capBulge = 0.5f, float twist = -1f)
    {
        var axis = b - a;
        var dir = axis.Normalized();
        var (side, front) = Frame(dir);
        if (twist < 0f) twist = Mathf.Pi / sides;
        var pts = new Vector3[rings.Length, sides];
        var nrm = new Vector3[rings.Length, sides];
        for (int r = 0; r < rings.Length; r++)
        {
            var ring = rings[r];
            var center = a + axis * ring.T + front * ring.Shift + side * ring.Side;
            for (int k = 0; k < sides; k++)
            {
                float ang = twist + k * Mathf.Tau / sides;
                float s = Mathf.Sin(ang), c = Mathf.Cos(ang);
                pts[r, k] = center + side * s * ring.Width + front * c * ring.Depth;
                nrm[r, k] = (side * s / Mathf.Max(ring.Width, 1e-3f) + front * c / Mathf.Max(ring.Depth, 1e-3f)).Normalized();
            }
        }
        for (int r = 0; r + 1 < rings.Length; r++)
            for (int k = 0; k < sides; k++)
            {
                int k2 = (k + 1) % sides;
                Tri(pts[r, k], pts[r + 1, k], pts[r + 1, k2], nrm[r, k], nrm[r + 1, k], nrm[r + 1, k2]);
                Tri(pts[r, k], pts[r + 1, k2], pts[r, k2], nrm[r, k], nrm[r + 1, k2], nrm[r, k2]);
            }
        Cap(pts, nrm, 0, a + axis * rings[0].T + front * rings[0].Shift + side * rings[0].Side, -dir, rings[0], sides, capBulge);
        int last = rings.Length - 1;
        Cap(pts, nrm, last, a + axis * rings[last].T + front * rings[last].Shift + side * rings[last].Side, dir, rings[last], sides, capBulge);
    }

    void Cap(Vector3[,] pts, Vector3[,] nrm, int r, Vector3 center, Vector3 outward, Ring ring, int sides, float bulge)
    {
        var tip = center + outward * Mathf.Min(ring.Width, ring.Depth) * bulge;
        for (int k = 0; k < sides; k++)
        {
            int k2 = (k + 1) % sides;
            var n1 = (nrm[r, k] + outward).Normalized();
            var n2 = (nrm[r, k2] + outward).Normalized();
            Tri(tip, pts[r, k], pts[r, k2], outward, n1, n2);
        }
    }

    // An ellipsoid with its own rotation. Low ring and segment counts make gems
    // when faceted and pebbles when smooth.
    public void Ellipsoid(Vector3 center, Vector3 radii, int segments, int rings, Basis? rotation = null, float squashBottom = 1f)
    {
        var basis = rotation ?? Basis.Identity;
        var pts = new Vector3[rings + 1, segments];
        var nrm = new Vector3[rings + 1, segments];
        for (int r = 0; r <= rings; r++)
        {
            float lat = Mathf.Pi * r / rings - Mathf.Pi / 2f;
            float y = Mathf.Sin(lat), rr = Mathf.Cos(lat);
            if (y < 0f) y *= squashBottom;
            for (int k = 0; k < segments; k++)
            {
                float lon = Mathf.Tau * (k + (r % 2) * 0.5f * (Flat ? 1f : 0f)) / segments;
                var unit = new Vector3(Mathf.Sin(lon) * rr, y, Mathf.Cos(lon) * rr);
                pts[r, k] = center + basis * (unit * radii);
                nrm[r, k] = (basis * (unit / radii)).Normalized();
            }
        }
        for (int r = 0; r < rings; r++)
            for (int k = 0; k < segments; k++)
            {
                int k2 = (k + 1) % segments;
                Tri(pts[r, k], pts[r + 1, k], pts[r + 1, k2], nrm[r, k], nrm[r + 1, k], nrm[r + 1, k2]);
                Tri(pts[r, k], pts[r + 1, k2], pts[r, k2], nrm[r, k], nrm[r + 1, k2], nrm[r, k2]);
            }
    }

    // A leaf or flat strip: a pointed blade from root to tip, width at its widest,
    // bent outward by curl along normal, with a little thickness so both sides light.
    public void Blade(Vector3 root, Vector3 tip, Vector3 normal, float width, float curl = 0f)
    {
        var axis = tip - root;
        var across = axis.Cross(normal).Normalized() * width * 0.5f;
        var mid = root + axis * 0.45f + normal * curl;
        var l = mid - across;
        var rgt = mid + across;
        var tipBent = tip + normal * curl * 1.6f;
        var n = normal.Normalized();
        Tri(root, l, rgt, n, n, n);
        Tri(l, tipBent, rgt, n, n, n);
        // The back side, nudged in so it never fights the front.
        var back = -n;
        var o = -n * 0.004f;
        Tri(root + o, rgt + o, l + o, back, back, back);
        Tri(l + o, rgt + o, tipBent + o, back, back, back);
    }

    // A round tube along a path (cord, a bent bow, a coil), its frame carried
    // along so it never twists. Radius may taper from start to end.
    public void Tube(Vector3[] path, float radius, int sides, float endRadius = -1f, bool caps = true)
    {
        if (path.Length < 2) return;
        if (endRadius < 0f) endRadius = radius;
        int n = path.Length;
        var tangent = new Vector3[n];
        for (int i = 0; i < n; i++)
            tangent[i] = (path[Math.Min(i + 1, n - 1)] - path[Math.Max(i - 1, 0)]).Normalized();
        var (side, _) = Frame(tangent[0]);
        var pts = new Vector3[n, sides];
        var nrm = new Vector3[n, sides];
        for (int i = 0; i < n; i++)
        {
            if (i > 0)
            {
                // Parallel transport: remove the part of the old side along the new tangent.
                side = (side - tangent[i] * side.Dot(tangent[i])).Normalized();
            }
            var up = tangent[i].Cross(side).Normalized();
            float r = radius + (endRadius - radius) * i / (n - 1f);
            for (int k = 0; k < sides; k++)
            {
                float a = k * Mathf.Tau / sides;
                var dir = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                pts[i, k] = path[i] + dir * r;
                nrm[i, k] = dir;
            }
        }
        for (int i = 0; i + 1 < n; i++)
            for (int k = 0; k < sides; k++)
            {
                int k2 = (k + 1) % sides;
                Tri(pts[i, k], pts[i + 1, k], pts[i + 1, k2], nrm[i, k], nrm[i + 1, k], nrm[i + 1, k2]);
                Tri(pts[i, k], pts[i + 1, k2], pts[i, k2], nrm[i, k], nrm[i + 1, k2], nrm[i, k2]);
            }
        if (!caps) return;
        for (int end = 0; end < 2; end++)
        {
            int i = end == 0 ? 0 : n - 1;
            var outward = end == 0 ? -tangent[0] : tangent[n - 1];
            var tip = path[i] + outward * (end == 0 ? radius : endRadius) * 0.4f;
            for (int k = 0; k < sides; k++)
            {
                int k2 = (k + 1) % sides;
                Tri(tip, pts[i, k], pts[i, k2], outward, (nrm[i, k] + outward).Normalized(), (nrm[i, k2] + outward).Normalized());
            }
        }
    }

    // A ring of points around a center, for coils and lashings.
    public static Vector3[] Circle(Vector3 center, Vector3 axis, float radius, int points, float turns = 1f, float rise = 0f, float startAngle = 0f)
    {
        var (u, v) = Frame(axis);
        axis = axis.Normalized();
        var path = new Vector3[points];
        for (int i = 0; i < points; i++)
        {
            float t = i / (points - 1f);
            float a = startAngle + t * turns * Mathf.Tau;
            path[i] = center + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * radius + axis * rise * t;
        }
        return path;
    }

    public static float Hash(int a, int b) => Terrain.Hash01(a, b, 911);

    public static Basis Aim(Vector3 forward)
    {
        var f = forward.Normalized();
        var up = Mathf.Abs(f.Y) > 0.95f ? Vector3.Back : Vector3.Up;
        var x = up.Cross(f).Normalized();
        var y = f.Cross(x);
        return new Basis(x, y, f);
    }

    public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);
}
