using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The painted bushes (painted-2): the forest-floor bush Terrain scatters and the leafy
// clumps of the berry bush, drawn with Painted.Bush. Each clump is a dark heart wrapped
// in pointed leaf lobes that lie down its slope, like the painted broadleaf's crown, so
// it reads as painted leaves instead of a blob. The light falls on the clump as a whole:
// every vertex takes mostly the clump's own outward normal, so the stepped bands make a
// few big painted shapes, the ink runs round the silhouette and the lobes show by their
// outline and by being lighter at the tip than at the root.
public static class PaintedBushes
{
    static Mesh? bush;
    static readonly Dictionary<Color, Mesh> Clumps = new();

    // About a metre across, in five clumps; vertex shades near white, so the instance
    // colour sets the green. Under 1500 triangles.
    public static Mesh Bush
    {
        get { lock (Clumps) return bush ??= BuildBush(); }
    }

    // One clump for the berry bush, the heart's radii as its old lobe sphere
    // (0.42 by 0.3), in the given leaf colour (a MeshInstance3D has no instance colour).
    public static Mesh Clump(Color leaf)
    {
        lock (Clumps)
        {
            if (!Clumps.TryGetValue(leaf, out var mesh))
            {
                var st = Begin();
                AddClump(st, Vector3.Zero, new Vector3(0.42f, 0.3f, 0.42f), 7, leaf, 30);
                Clumps[leaf] = mesh = st.Commit();
            }
            return mesh;
        }
    }

    // A world bush colour pulled to the painted palette: muted olive and grey-green,
    // keeping a little of the source's own variation.
    public static Color Tint(Color color)
    {
        float lum = color.R * 0.3f + color.G * 0.59f + color.B * 0.11f;
        var olive = new Color(0.3f, 0.36f, 0.28f) * (0.85f + lum * 0.6f);
        return olive.Lerp(color, 0.2f) with { A = 1f };
    }

    static Mesh BuildBush()
    {
        var st = Begin();
        AddClump(st, new Vector3(0, 0.36f, 0), new Vector3(0.4f, 0.3f, 0.4f), 1, Colors.White, 32);
        for (int k = 0; k < 4; k++)
        {
            float a = k * Mathf.Tau / 4f + 0.7f;
            float r = 0.27f + 0.04f * (k % 2);
            AddClump(st, new Vector3(Mathf.Cos(a) * 0.34f, 0.2f, Mathf.Sin(a) * 0.34f), new Vector3(r, r * 0.8f, r), 2 + k,
                new Color(0.92f, 0.94f, 0.92f), 22);
        }
        return st.Commit();
    }

    // A clump: a dark heart at 0.82 of the radii, and `lobes` pointed leaves spread over
    // its top and sides, each rooted just inside the heart and lying out and down.
    static void AddClump(SurfaceTool st, Vector3 center, Vector3 radii, int seed, Color shade, int lobes)
    {
        Vector3 Out(Vector3 p) => ((p - center) / (radii * radii)).Normalized();

        // The heart: a low ellipsoid in the darkest tone, so no light shows between leaves.
        const int segs = 8, rings = 5;
        var heart = radii * 0.82f;
        var dark = shade * 0.72f;
        Vector3 H(int i, int j)
        {
            float lat = Mathf.Pi * j / rings, lon = Mathf.Tau * i / segs;
            return center + new Vector3(Mathf.Sin(lat) * Mathf.Sin(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Cos(lon)) * heart;
        }
        for (int j = 0; j < rings; j++)
            for (int i = 0; i < segs; i++)
            {
                Vector3 a = H(i, j), b = H(i + 1, j), c = H(i + 1, j + 1), d = H(i, j + 1);
                Tri(st, a, b, c, dark, dark, dark, Out(a), Out(b), Out(c));
                Tri(st, a, c, d, dark, dark, dark, Out(a), Out(c), Out(d));
            }

        // The leaves, on a golden-angle spiral from the crown down to just below the middle.
        for (int i = 0; i < lobes; i++)
        {
            float rnd = Terrain.Hash01(seed, i, 610);
            float y = 0.97f - (i + 0.5f) / lobes * 1.12f;
            float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float a = i * 2.39996f + seed * 1.3f;
            var n = new Vector3(Mathf.Cos(a) * ring, y, Mathf.Sin(a) * ring);
            var root = center + n * radii * 0.84f;
            // Down the clump's slope (any way round on the very top), tipped outward.
            var slope = Vector3.Down - n * Vector3.Down.Dot(n);
            if (slope.LengthSquared() < 0.02f) slope = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var outward = Out(root);
            var dir = (slope.Normalized() * 0.75f + outward * 0.6f).Normalized();
            float size = (radii.X + radii.Y) * 0.5f;
            float length = size * (0.5f + rnd * 0.16f) * Mathf.Lerp(0.75f, 1f, ring);
            float wide = size * (0.26f + rnd * 0.06f);
            float bright = 0.9f + Terrain.Hash01(seed, i, 611) * 0.22f;
            Leaf(st, root, dir, outward, length, wide, shade * bright, Out);
        }
    }

    // A pointed leaf lobe: a strip folded along a raised ridge, broadest past halfway,
    // its tip split in three like the painted broadleaf's lobes. 8 triangles.
    static void Leaf(SurfaceTool st, Vector3 root, Vector3 dir, Vector3 up, float length, float wide, Color shade, System.Func<Vector3, Vector3> outOf)
    {
        var side = dir.Cross(up).Normalized();
        var lift = dir.Cross(side).Normalized();
        if (lift.Dot(up) < 0f) lift = -lift;
        Vector3 At(float t) => root + dir * length * t + Vector3.Down * length * 0.18f * t * t;
        Vector3 p1 = At(0.55f), end = At(0.92f);
        Vector3 l1 = p1 - side * wide, r1 = p1 + side * wide;
        Vector3 m1 = p1 + lift * wide * 0.3f;
        Vector3 le = end - side * wide * 0.55f, re = end + side * wide * 0.55f;
        var tip = At(1.18f);
        Vector3 lt = le + (dir * 0.3f - side).Normalized() * wide * 0.45f + Vector3.Down * wide * 0.15f;
        Vector3 rt = re + (dir * 0.3f + side).Normalized() * wide * 0.45f + Vector3.Down * wide * 0.15f;

        // Darker at the root, lighter toward the tip, a touch lighter on the ridge.
        Color c0 = shade * 0.8f, c1 = shade * 0.95f, cm = shade * 1.0f, ce = shade * 1.05f, ct = shade * 1.08f;
        Vector3 N(Vector3 p) => (outOf(p) * 0.8f + lift * 0.2f).Normalized();
        Tri(st, root, l1, m1, c0, c1, cm, N(root), N(l1), N(m1));
        Tri(st, root, m1, r1, c0, cm, c1, N(root), N(m1), N(r1));
        Tri(st, l1, le, m1, c1, ce, cm, N(l1), N(le), N(m1));
        Tri(st, m1, re, r1, cm, ce, c1, N(m1), N(re), N(r1));
        Tri(st, m1, le, re, cm, ce, ce, N(m1), N(le), N(re));
        Tri(st, le, tip, re, ce, ct, ce, N(le), N(tip), N(re));
        Tri(st, le, lt, tip, ce, ct, ct, N(le), N(lt), N(tip));
        Tri(st, re, tip, rt, ce, ct, ct, N(re), N(tip), N(rt));
    }

    static SurfaceTool Begin()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        return st;
    }

    // One triangle, wound so its front faces out along its normals (Godot fronts are
    // clockwise), or the two-sided shading would flip half the leaves into the dark.
    static void Tri(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc, Vector3 na, Vector3 nb, Vector3 nc)
    {
        if ((c - a).Cross(b - a).Dot(na + nb + nc) < 0f) { (b, c) = (c, b); (cb, cc) = (cc, cb); (nb, nc) = (nc, nb); }
        st.SetNormal(na); st.SetColor(ca); st.AddVertex(a);
        st.SetNormal(nb); st.SetColor(cb); st.AddVertex(b);
        st.SetNormal(nc); st.SetColor(cc); st.AddVertex(c);
    }
}
