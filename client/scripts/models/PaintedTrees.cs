using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The painted trees (model-10; Justin's picks D-282, D-285 and the broadleaf repaint):
// pines and broadleaf trees after the Golden Skull cards, built from drooping boughs and
// leaf lobes that a painted shader colours in flat bands. Each comes in seeded variants
// (D-166) and in a detailed and a far (coarse) version, drawn near and far by Terrain.
// Meshes stand Height tall at scale 1, the size of the old tree meshes, so Terrain's
// placement, the see-through fade and the sway keep working as before.
//
// The shader is Look's painted shader with PAINTED_FOLIAGE defined: everything Look does
// for trees (seasons, snow, wetness, falling when cut, the far fog rim, the see-through
// ghost) still applies, and on top the boughs are painted from their UVs (UV.x across,
// UV.y root to tip; UV2.x 0 trunk, 1 dark heart, 2 bough; UV2.y a per-bough random)
// and lit in stepped bands round the whole crown.
public static class PaintedTrees
{
    // How tall a tree stands at scale 1 (the old meshes' height), and how many variants of
    // each kind stand in the world.
    public const float Height = 4.4f;
    public const int Variants = 4;
    // How far from the camera (to a chunk's middle) the detailed trees show; the far
    // version takes over beyond.
    public const float NearRange = 40f;
    // Where the broadleaf crown's heart sits, as a fraction of the height.
    const float CrownY = 0.58f;

    static readonly Dictionary<(bool, int, bool, bool), Mesh> Meshes = new();

    public static Mesh Pine(int variant, bool coarse) => Get(true, variant, coarse, false);
    public static Mesh Broadleaf(int variant, bool coarse) => Get(false, variant, coarse, false);

    // The same tree cut down (Environment's TreeFall): no root flare, the trunk sawn flat
    // at its base. Detailed, as a falling tree is always close by.
    public static Mesh Felled(bool pine, int variant) => Get(pine, variant, false, true);

    // The crown alone, with no trunk, for trees that bring their own: birches on their white
    // trunks and the landmark giants on theirs. Same height and frame as the whole tree.
    public static Mesh PineCrown(int variant, bool coarse) => Crown(true, variant, coarse);
    public static Mesh BroadleafCrown(int variant, bool coarse) => Crown(false, variant, coarse);
    public static bool IsCrown(Mesh mesh) { lock (Crowns) return Crowns.ContainsValue(mesh); }
    static readonly Dictionary<(bool, int, bool), Mesh> Crowns = new();

    static Mesh Crown(bool pine, int variant, bool coarse)
    {
        int v = ((variant % Variants) + Variants) % Variants;
        var whole = (ArrayMesh)Get(pine, v, coarse, false);
        lock (Crowns)
        {
            if (!Crowns.TryGetValue((pine, v, coarse), out var mesh))
            {
                // Surface 0 is the trunk and roots, surface 1 the foliage.
                var crown = new ArrayMesh();
                crown.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, whole.SurfaceGetArrays(1));
                Crowns[(pine, v, coarse)] = mesh = crown;
            }
            return mesh;
        }
    }

    static Mesh Get(bool pine, int variant, bool coarse, bool felled)
    {
        int v = ((variant % Variants) + Variants) % Variants;
        lock (Meshes)
        {
            if (!Meshes.TryGetValue((pine, v, coarse, felled), out var mesh))
                Meshes[(pine, v, coarse, felled)] = mesh = pine ? BuildPine(v, coarse, felled) : BuildBroadleaf(v, coarse, felled);
            return mesh;
        }
    }

    // Makes every mesh and material up front, on the main thread, so chunk workers only read them.
    public static void Prewarm()
    {
        for (int v = 0; v < Variants; v++)
        {
            foreach (bool coarse in new[] { false, true })
            {
                Pine(v, coarse);
                Broadleaf(v, coarse);
                PineCrown(v, coarse);
                BroadleafCrown(v, coarse);
            }
            Felled(true, v);
            Felled(false, v);
        }
        GhostOf(PineMaterial);
        GhostOf(BroadleafMaterial);
    }

    static float H(int a, int b) => MeshKit.Hash(a, b + 90);

    static MeshKit Kit() => new(true) { Skinned = false };

    // The mesh grown from unit height to Height.
    static ArrayMesh Scaled(ArrayMesh unit)
    {
        var mesh = new ArrayMesh();
        for (int s = 0; s < unit.GetSurfaceCount(); s++)
        {
            var arrays = unit.SurfaceGetArrays(s);
            var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            for (int i = 0; i < verts.Length; i++) verts[i] *= Height;
            arrays[(int)Mesh.ArrayType.Vertex] = verts;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        }
        return mesh;
    }

    static ArrayMesh BuildPine(int v, bool coarse, bool felled = false)
    {
        float R(int salt) => H(v * 97 + 5, salt + 400);
        float trunkTop = 0.16f + R(1) * 0.06f;
        int tiers = 5 + (int)(R(2) * 2.99f);
        float width = 0.85f + R(3) * 0.3f;
        float leanAmount = R(4) * 0.05f, leanAngle = R(5) * Mathf.Tau;
        var leanDir = new Vector3(Mathf.Sin(leanAngle), 0, Mathf.Cos(leanAngle));
        float fullAngle = R(6) * Mathf.Tau, fullAmount = 0.08f + R(7) * 0.18f;
        int gapTier = R(8) < 0.65f ? 1 + (int)(R(9) * (tiers - 2)) : -1;
        float gapAngle = R(10) * Mathf.Tau;
        // A slight shift in value and hue, bluer or yellower.
        float value = 0.9f + R(11) * 0.18f, warm = (R(12) - 0.5f) * 0.12f;
        var tint = new Color(value * (1f + warm), value, value * (1f - warm));
        Vector3 Lean(Vector3 p) => p + leanDir * leanAmount * p.Y * p.Y;

        var k = Kit();
        // A felled tree is cut through above its root flare, flat at the base.
        k.Paint(Colors.White).Loft(new Vector3(0, felled ? 0.03f : 0f, 0), Lean(new Vector3(0, trunkTop + 0.25f, 0)), new MeshKit.Ring[]
        {
            new(0f, felled ? 0.028f : 0.036f, felled ? 0.028f : 0.036f), new(0.1f, 0.027f, 0.027f), new(1f, 0.018f, 0.018f),
        }, 7, felled ? 0f : 0.2f);
        for (int i = 0; i < (felled ? 0 : 4); i++)
        {
            float a = i * Mathf.Tau / 4 + 0.4f + R(13 + i) * 0.6f;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            k.Paint(Colors.White).Tube(new[] { dir * 0.012f + Vector3.Up * 0.05f, dir * 0.065f + Vector3.Up * 0.004f }, 0.012f, 4, 0.004f);
        }

        var f = new FoliageKit(tint, Lean) { Segs = coarse ? 2 : 4 };

        float foliage = trunkTop + 0.13f, span = 1f - foliage, step = span / (tiers + 0.3f);
        for (int t = 0; t < tiers; t++)
        {
            float rootY = foliage + step * t;
            float frac = (rootY - foliage) / span;
            float length = (0.015f + 0.215f * Mathf.Pow(1f - frac, 1.05f)) * width;
            float droop = Mathf.Max(length * 0.72f, step * 1.2f);
            f.Core(rootY - droop * 0.75f, Mathf.Min(rootY + step * 1.3f, 1f), length * 0.5f);
            int count = Mathf.Max(5, Mathf.RoundToInt(Mathf.Tau * length / ((coarse ? 0.13f : 0.07f) * width)));
            float wide = Mathf.Tau * length * 0.5f / count * 1.15f;
            float turn = R(30 + t) * Mathf.Tau;
            for (int ring = 0; ring < (coarse ? 1 : 2); ring++)
            {
                int n = ring == 0 ? count : Mathf.Max(4, count * 2 / 3);
                for (int i = 0; i < n; i++)
                {
                    // The bottom ring stays even (Justin, D-285): spacing, length and droop
                    // vary far less there than in the tiers above.
                    float calm = t == 0 ? 0.2f : 1f;
                    float rnd = 0.5f + (H(v * 131 + t * 17 + ring * 7, i + 500) - 0.5f) * calm;
                    float a = (i + (ring == 0 ? 0f : 0.5f) + (rnd - 0.5f) * 0.4f) * Mathf.Tau / n + turn;
                    var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    float full = 1f + fullAmount * calm * Mathf.Cos(a - fullAngle);
                    if (t == gapTier && ring == 0 && Mathf.Abs(Mathf.AngleDifference(a, gapAngle)) < 0.55f) continue;
                    // Hem lobes of uneven length, some hanging well below the rest, as on the card.
                    float hang = t == 0 ? 0f : H(v * 53 + t * 11, i + 700);
                    float len = length * full * (ring == 0 ? 0.8f + rnd * 0.3f + (hang > 0.75f ? 0.18f : 0f) : 0.55f);
                    float y = rootY + (ring == 0 ? 0f : step * 0.5f) + (rnd - 0.5f) * step * 0.25f;
                    f.Bough(new Vector3(0, y, 0) + dir * length * 0.08f, dir, len, droop * (ring == 0 ? 0.85f + rnd * 0.3f + (hang > 0.75f ? 0.25f : 0f) : 0.7f), wide * (ring == 0 ? 1f : 0.85f), rnd);
                }
            }
        }
        // The leader: a small upright spike at the top.
        f.Core(1f - step * 0.6f, 1.01f, 0.014f * width);

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, k.CommitToArrays());
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, f.Commit());
        return Scaled(mesh);
    }



    static ArrayMesh BuildBroadleaf(int v, bool coarse, bool felled = false)
    {
        float R(int salt) => H(v * 89 + 11, salt + 800);
        float width = 0.86f + R(1) * 0.28f;
        int rows = coarse ? 7 : 12 + (int)(R(2) * 4f);
        float squash = 0.92f + R(3) * 0.16f;
        float leanAmount = R(4) * 0.04f, leanAngle = R(5) * Mathf.Tau;
        var leanDir = new Vector3(Mathf.Sin(leanAngle), 0, Mathf.Cos(leanAngle));
        float fullAngle = R(6) * Mathf.Tau, fullAmount = 0.06f + R(7) * 0.14f;
        float value = 0.9f + R(8) * 0.18f, warm = (R(9) - 0.5f) * 0.14f;
        var tint = new Color(value * (1f + warm), value, value * (1f - warm));
        Vector3 Lean(Vector3 p) => p + leanDir * leanAmount * p.Y * p.Y;
        var center = new Vector3(0, CrownY, 0);
        var radii = new Vector3(0.25f * width, 0.4f * squash, 0.25f * width);

        var k = Kit();
        k.Paint(Colors.White).Loft(new Vector3(0, felled ? 0.04f : 0f, 0), Lean(center), new MeshKit.Ring[]
        {
            new(0f, felled ? 0.046f : 0.058f, felled ? 0.046f : 0.058f), new(0.12f, 0.045f, 0.045f), new(1f, 0.032f, 0.032f),
        }, 8, felled ? 0f : 0.2f);
        for (int i = 0; i < (felled ? 0 : 5); i++)
        {
            float a = i * Mathf.Tau / 5 + R(13 + i) * 0.6f;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            k.Paint(Colors.White).Tube(new[] { dir * 0.02f + Vector3.Up * 0.07f, dir * 0.1f + Vector3.Up * 0.005f }, 0.018f, 4, 0.005f);
        }

        var f = new FoliageKit(tint, Lean) { Segs = coarse ? 2 : 3 };
        f.Blob(center, radii * 0.84f, coarse ? 8 : 12, coarse ? 5 : 8);
        // Rows of lobes from the crown's top down to its underside.
        for (int r = 0; r < rows; r++)
        {
            float lat0 = 0.22f + r * 2.3f / (rows - 1);
            float ringR0 = radii.X * Mathf.Sin(lat0);
            // Lobes on the crown's top are short, so the top stays round instead of fringed.
            float length = (0.04f + 0.035f * Mathf.Sin(lat0)) * width * (coarse ? 1.9f : 1.2f) * Mathf.Lerp(0.55f, 1f, Mathf.Clamp(lat0 / 1.1f, 0f, 1f));
            int count = Mathf.Max(5, Mathf.RoundToInt(Mathf.Tau * ringR0 / ((coarse ? 0.1f : 0.045f) * width)));
            // Narrow lobes, so each reads as a small pointed leaf rather than a round scale.
            float wide = Mathf.Tau * ringR0 / count * 0.55f + 0.008f;
            float turn = R(30 + r) * Mathf.Tau;
            for (int i = 0; i < count; i++)
            {
                float rnd = H(v * 151 + r * 19, i + 900);
                float a = (i + (rnd - 0.5f) * 0.45f) * Mathf.Tau / count + turn;
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                float full = 1f + fullAmount * Mathf.Cos(a - fullAngle);
                // Each lobe sits a little above or below its row, so rows don't read as plates.
                float lat = lat0 + (H(v * 157 + r * 23, i + 950) - 0.5f) * 2.3f / (rows - 1) * 1.1f;
                float ringR = radii.X * Mathf.Sin(lat), y = center.Y + radii.Y * Mathf.Cos(lat);
                // A few broad bulges, so the crown's outline is lumpy like the card's.
                float lump = 1f + 0.11f * Mathf.Sin(a * 3f + lat * 2.3f + R(61) * 6f) + 0.06f * Mathf.Sin(a * 5f - lat * 3.1f + R(62) * 6f);
                var root = new Vector3(0, y, 0) + dir * ringR * full * lump * 0.85f;
                // Lobes near the top reach out more; lower ones hang down the crown's side.
                float droop = length * (0.35f + 0.75f * Mathf.Clamp(lat / 1.6f, 0f, 1.2f)) * (0.85f + rnd * 0.3f);
                f.Bough(root, dir, length * (0.85f + rnd * 0.35f), droop, wide * (0.9f + rnd * 0.2f), rnd);
            }
        }
        // A tuft of short lobes on top, broad and meeting at the middle so no dark heart
        // shows through when the crown is seen from above (Justin's "hole in the top").
        int tuft = coarse ? 6 : 9;
        for (int i = 0; i < tuft; i++)
        {
            float a = i * Mathf.Tau / tuft + R(60) * Mathf.Tau;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            f.Bough(center + new Vector3(0, radii.Y * 1.03f, 0), dir, 0.085f * width, 0.035f, 0.085f, H(v, i + 990));
        }
        // And a few lying across the very top, rooted just past the middle, so their lighter
        // middles (not their dark roots) cover where the tuft meets.
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.Tau / 3 + R(63) * Mathf.Tau;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            f.Bough(center + new Vector3(0, radii.Y * 1.06f, 0) - dir * 0.03f, dir, 0.08f * width, 0.03f, 0.08f, H(v, i + 980));
        }

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, k.CommitToArrays());
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, f.Commit());
        return Scaled(mesh);
    }


    sealed class FoliageKit
    {
        readonly SurfaceTool st = new();
        readonly Color tint;
        readonly System.Func<Vector3, Vector3> warp;

        public FoliageKit(Color tint, System.Func<Vector3, Vector3> warp)
        {
            this.tint = tint;
            this.warp = warp;
            st.Begin(Mesh.PrimitiveType.Triangles);
        }

        // Segments along each bough: fewer for the far version.
        public int Segs = 5;

        public Godot.Collections.Array Commit() => st.CommitToArrays();

        // A dark ellipsoid heart for a round crown.
        public void Blob(Vector3 center, Vector3 radii, int segments = 12, int rings = 8)
        {
            var uv2 = new Vector2(1f, 0f);
            Vector3 P(int i, int j)
            {
                float lat = Mathf.Pi * j / rings, lon = Mathf.Tau * i / segments;
                return new Vector3(Mathf.Sin(lat) * Mathf.Sin(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Cos(lon));
            }
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < segments; i++)
                {
                    Vector3 a = P(i, j), b = P(i + 1, j), c = P(i + 1, j + 1), d = P(i, j + 1);
                    var n = (a + b + c + d).Normalized();
                    Tri(center + a * radii, center + b * radii, center + c * radii, default, default, default, n, uv2);
                    Tri(center + a * radii, center + c * radii, center + d * radii, default, default, default, n, uv2);
                }
        }

            void Vert(Vector3 p, Vector3 n, Vector2 uv, Vector2 uv2)
            {
                st.SetColor(tint);
                st.SetNormal(n);
                st.SetUV(uv);
                st.SetUV2(uv2);
                st.AddVertex(warp(p));
            }
            // One triangle, wound so its front faces along the hint.
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 hint, Vector2 uv2)
            {
                var face = (c - a).Cross(b - a);
                if (face.LengthSquared() < 1e-12f) return;
                if (face.Dot(hint) < 0f) { (b, c) = (c, b); (ub, uc) = (uc, ub); }
                var n = hint.Normalized();
                Vert(a, n, ua, uv2);
                Vert(b, n, ub, uv2);
                Vert(c, n, uc, uv2);
            }
            // A bough: a strip from root out and down to its tip, folded along a raised
            // ridge, widest past halfway. Its tip splits into three points, the side ones
            // shorter, like the card's lobes.
            public void Bough(Vector3 root, Vector3 dir, float length, float droop, float wide, float rnd)
            {
                var side = new Vector3(dir.Z, 0, -dir.X);
                float bend = (rnd - 0.5f) * 0.3f;
                Vector3 At(float t) => root + (dir + side * bend * t) * length * t + Vector3.Down * droop * Mathf.Pow(t, 1.7f);
                int segs = Segs;
                var uv2 = new Vector2(2f, rnd);
                for (int i = 0; i < segs; i++)
                {
                    float t0 = (float)i / segs, t1 = (float)(i + 1) / segs;
                    Vector3 p0 = At(t0), p1 = At(t1);
                    float w0 = wide * Profile(t0), w1 = wide * Profile(t1);
                    var up0 = (Vector3.Up + dir * 0.6f).Normalized();
                    var ridge = up0 * wide * 0.12f;
                    // Left, ridge, right at both ends of the segment.
                    Vector3 l0 = p0 - side * w0, l1 = p1 - side * w1, r0 = p0 + side * w0, r1 = p1 + side * w1;
                    Vector3 m0 = p0 + ridge * Profile(t0), m1 = p1 + ridge * Profile(t1);
                    Tri(l0, m0, m1, new(-1, t0), new(0, t0), new(0, t1), up0, uv2);
                    Tri(l0, m1, l1, new(-1, t0), new(0, t1), new(-1, t1), up0, uv2);
                    Tri(m0, r0, r1, new(0, t0), new(1, t0), new(1, t1), up0, uv2);
                    Tri(m0, r1, m1, new(0, t0), new(1, t1), new(0, t1), up0, uv2);
                }
                // The split tip: a long middle point and two short side points.
                var end = At(1f);
                var nUp = (Vector3.Up + dir * 0.6f).Normalized();
                float tipW = wide * Profile(1f);
                var tipC = end + (dir * 0.6f + Vector3.Down * 0.8f).Normalized() * length * 0.22f;
                Tri(end - side * tipW, end + side * tipW, tipC, new(-1, 0.9f), new(1, 0.9f), new(0, 1f), nUp, uv2);
                foreach (float s in new[] { -1f, 1f })
                {
                    var baseA = At(0.6f) + side * s * wide * Profile(0.6f);
                    var baseB = end + side * s * tipW;
                    var tip = baseA.Lerp(baseB, 0.4f) + (side * s * 0.7f + dir * 0.4f + Vector3.Down * 0.7f).Normalized() * length * (0.2f + rnd * 0.12f);
                    Tri(baseA, baseB, tip, new(s, 0.8f), new(s, 0.9f), new(s * 0.2f, 1f), nUp, uv2);
                }
            }
            // A dark cone at each tier's heart, so no light shows between the boughs.
            public void Core(float bottom, float top, float radius)
            {
                const int sides = 10;
                var uv2 = new Vector2(1f, 0f);
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.Tau / sides, a1 = (i + 1) * Mathf.Tau / sides;
                    var d0 = new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0));
                    var d1 = new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1));
                    var apex = new Vector3(0, top, 0);
                    Vector3 b0 = d0 * radius + Vector3.Up * bottom, b1 = d1 * radius + Vector3.Up * bottom;
                    Tri(apex, b0, b1, new(0, 0), new(0, 0), new(0, 0), (d0 + d1) * 0.5f + Vector3.Up * 0.4f, uv2);
                    Tri(new Vector3(0, bottom, 0), b1, b0, new(0, 0), new(0, 0), new(0, 0), Vector3.Down, uv2);
                }
            }
    }


    // A bough's half-width along its length: narrow at the trunk, broadest past
    // halfway, then narrowing to the split tip.
    static float Profile(float t) => 0.25f + 0.75f * Mathf.Sin(Mathf.Min(t * 1.25f, 1f) * Mathf.Pi * 0.5f) * (1f - 0.35f * Mathf.Max(0f, t - 0.8f) / 0.2f);

    // --- The painted shader --------------------------------------------------------

    public static ShaderMaterial PineMaterial => pineMaterial ??= Material(-1f);
    public static ShaderMaterial BroadleafMaterial => broadleafMaterial ??= Material(CrownY);
    static ShaderMaterial? pineMaterial, broadleafMaterial, pineGhost, broadleafGhost;

    // The see-through copy Terrain draws for a tree in the way, like Look's tree ghosts.
    public static ShaderMaterial? GhostOf(Material material) =>
        material == PineMaterial ? pineGhost ??= Look.Ghost(PineMaterial, GhostShader) :
        material == BroadleafMaterial ? broadleafGhost ??= Look.Ghost(BroadleafMaterial, GhostShader) : Painted.GhostOf(material);

    static Shader Shader => shader ??= Look.MakeShader(Code(ghost: false));
    static Shader GhostShader => ghostShader ??= Look.MakeShader(Code(ghost: true));
    static Shader? shader, ghostShader;

    // A material on the painted shader with Look's foliage settings (brush, detail, sway),
    // the see-through fade on, and no relief: the painting is the bough's own.
    static ShaderMaterial Material(float crownY)
    {
        var material = new ShaderMaterial { Shader = Shader };
        var source = Look.Foliage;
        foreach (var uniform in source.Shader.GetShaderUniformList())
        {
            var name = new StringName(uniform.AsGodotDictionary()["name"].AsString());
            material.SetShaderParameter(name, source.GetShaderParameter(name));
        }
        material.SetShaderParameter("see_through", true);
        material.SetShaderParameter("bump", 0f);
        material.SetShaderParameter("cavity", 0f);
        material.SetShaderParameter("sway", 0.05f);
        material.SetShaderParameter("sway_height", 3f);
        material.SetShaderParameter("crown_y", crownY < 0f ? -1f : crownY * Height);
        material.SetShaderParameter("tree_height", Height);
        material.SetShaderParameter("dabs", Dabs);
        if (crownY >= 0f)
        {
            // The card broadleaf's dark grey-green.
            material.SetShaderParameter("shade_col", new Color(0.09f, 0.12f, 0.11f));
            material.SetShaderParameter("base_col", new Color(0.15f, 0.2f, 0.17f));
            material.SetShaderParameter("lit_col", new Color(0.22f, 0.28f, 0.22f));
        }
        return material;
    }

    // A tiling noise of soft blobs, read in world space as brush dabs.
    static NoiseTexture2D Dabs => dabs ??= new NoiseTexture2D
    {
        Width = 256, Height = 256, Seamless = true, GenerateMipmaps = true,
        Noise = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 0.02f, FractalOctaves = 3, FractalGain = 0.45f, Seed = 7 },
    };
    static NoiseTexture2D? dabs;

    // Look's painted shader with PAINTED_FOLIAGE switched on and the painted parts put in
    // at the places named below; each anchor must be found exactly once.
    static string Code(bool ghost)
    {
        string code = Look.PaintedCode;
        string Put(string text, string anchor, string replacement)
        {
            int at = text.IndexOf(anchor, System.StringComparison.Ordinal);
            if (at < 0 || text.IndexOf(anchor, at + anchor.Length, System.StringComparison.Ordinal) >= 0)
                throw new System.InvalidOperationException($"PaintedTrees: Look's shader changed near '{anchor}'");
            return text[..at] + replacement + text[(at + anchor.Length)..];
        }
        code = Put(code, "shader_type spatial;", "shader_type spatial;\n#define PAINTED_FOLIAGE" + (ghost ? "\n#define GHOST" : ""));
        if (ghost) code = Put(code, "render_mode cull_disabled;", "render_mode cull_disabled, depth_draw_always;");
        code = Put(code, "varying float season_kind;", "varying float season_kind;\n" + Declarations);
        code = Put(code, "VERTEX *= 1.0 - 0.85 * leaf_drop();", """
            {
            #ifdef PAINTED_FOLIAGE
                // Only the leaves go: they shrink into the crown's heart, the trunk stays.
                if (UV2.x > 0.5) { vec3 heart = vec3(0.0, max(crown_y, 0.5 * tree_height), 0.0); VERTEX = heart + (VERTEX - heart) * (1.0 - 0.85 * leaf_drop()); }
            #else
                VERTEX *= 1.0 - 0.85 * leaf_drop();
            #endif
            }
            """);
        code = Put(code, "world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);", """
            world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
                obj = VERTEX;
                // The crown's own normal: out from the trunk and up (a pine's cone), or out
                // from the heart of an egg-shaped crown (a broadleaf).
                vec3 out_dir = length(VERTEX.xz) > 1e-4 ? normalize(vec3(VERTEX.x, 0.0, VERTEX.z)) : vec3(0.0, 0.0, 1.0);
                vec3 crown = crown_y < 0.0 ? out_dir + vec3(0.0, 0.45, 0.0) : (VERTEX - vec3(0.0, crown_y, 0.0)) * vec3(1.0, 0.75, 1.0);
                cone_n = normalize((MODELVIEW_MATRIX * vec4(crown, 0.0)).xyz);
            """);
        code = Put(code, "vec3 color = tint * (1.0 + paint * 2.0);", """
            #ifdef PAINTED_FOLIAGE
                dab = dab_at(world_pos);
                vec3 painted = painted_color(UV, UV2, FRONT_FACING, world_normal.y);
                float paint_value = dot(painted, vec3(0.3, 0.59, 0.11)) / 0.25;
                vec3 color = painted * tint;
            #else
                vec3 color = tint * (1.0 + paint * 2.0);
            #endif
            """);
        code = Put(code, "color = mix(color, turned * (1.0 + paint * 2.0), smoothstep(0.0, 0.6, season_autumn));", """
            #ifdef PAINTED_FOLIAGE
                    if (UV2.x > 0.5) color = mix(color, turned * paint_value * 0.8, smoothstep(0.0, 0.6, season_autumn));
            #else
                    color = mix(color, turned * (1.0 + paint * 2.0), smoothstep(0.0, 0.6, season_autumn));
            #endif
            """);
        code = Put(code, "NORMAL = normalize(abs(det) * NORMAL - grad * bump * 0.035);", """
            NORMAL = normalize(abs(det) * NORMAL - grad * bump * 0.035);
            #ifdef PAINTED_FOLIAGE
                // Light falls on the crown as a whole, in big painted shapes.
                if (UV2.x > 0.5) NORMAL = normalize(mix(FRONT_FACING ? NORMAL : -NORMAL, cone_n, cone_light));
            #endif
            """);
        code = Put(code, "void light() {", "void light() {\n#ifdef PAINTED_FOLIAGE\n" + Light + "\n#else\n");
        code = code.TrimEnd();
        if (!code.EndsWith("}")) throw new System.InvalidOperationException("PaintedTrees: Look's shader no longer ends with light()");
        return code[..^1] + "#endif\n}\n";
    }

    // D-319 on the trees: the boughs keep their one lighter patch each, but its edge is
    // soft, the small dab spots are gone and the light bands blend instead of stepping.
    // These are the one set to tune; the old values are in brackets.
    internal static class Smooth
    {
        public const float PatchSoft = 0.12f;  // lit patch edge width (0.04)
        public const float Spots = 0f;         // the small lit spots across boughs (1)
        public const float DabScale = 1.8f;    // brush patches this many times larger (1)
        public const float PatchBreak = 0.1f;  // how much the brush wobbles the patch edge (0.25)
        public const float BandSoft = 0.1f;    // light-band edge width (0.025)
        public const float BandBreak = 0.08f;  // how much the brush breaks the bands (0.22)
    }

    static string F(float v) => v.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture);

    static readonly string Declarations = $$"""
        uniform sampler2D dabs : filter_linear_mipmap, repeat_enable;
        // Colours picked from the card pine; the broadleaf sets its own.
        uniform vec3 shade_col : source_color = vec3(0.11, 0.155, 0.145);
        uniform vec3 base_col : source_color = vec3(0.16, 0.22, 0.2);
        uniform vec3 lit_col : source_color = vec3(0.22, 0.29, 0.24);
        uniform vec3 ink : source_color = vec3(0.1, 0.11, 0.08);
        uniform vec3 bark_dark : source_color = vec3(0.24, 0.16, 0.19);
        uniform vec3 bark_lit : source_color = vec3(0.39, 0.31, 0.27);
        // Shade leans violet, as a painter's would; lit tips lean warm.
        uniform vec3 shade_tint : source_color = vec3(0.72, 0.7, 0.9);
        uniform vec3 warm_tint : source_color = vec3(1.0, 0.98, 0.92);
        uniform float dab_size = 3.5;
        const float DAB_SCALE = {{F(Smooth.DabScale)}};
        uniform float cone_light = 0.75;
        uniform float crown_y = -1.0;
        uniform float tree_height = 4.4;
        varying vec3 obj;
        varying vec3 cone_n;
        varying float dab;

        // The brush noise at a world point, read on two planes so it sticks to the tree
        // from every side; stretched a little sideways, like short strokes.
        float dab_at(vec3 p) {
            vec2 a = vec2(p.x * 0.8, p.y * 1.3) / (dab_size * DAB_SCALE);
            vec2 b = vec2(p.z * 0.8, p.y * 1.3) / (dab_size * DAB_SCALE) + 0.37;
            return mix(texture(dabs, a).r, texture(dabs, b).r, 0.5);
        }

        // The painted colour of a point: the bark in vertical strokes; the dark heart;
        // a bough's deep green with one lighter patch along its upper middle, joined by
        // dabs across neighbouring boughs, darker toward the trunk and sides, ink on the tips.
        vec3 painted_color(vec2 uv, vec2 uv2, bool front, float up) {
            if (uv2.x < 0.5) {
                vec3 o = obj / tree_height;
                float ang = atan(o.x, o.z);
                float stroke = fract(ang * 1.4 + sin(o.y * 18.0 + ang * 3.0) * 0.12);
                return mix(bark_dark, bark_lit, 0.7 * smoothstep(0.55, 0.62, stroke) * (1.0 - smoothstep(0.78, 0.85, stroke)));
            }
            if (uv2.x < 1.5) return shade_col;
            float across = abs(uv.x), along = uv.y, rnd = uv2.y;
            float edge = 0.55 + {{F(Smooth.PatchBreak)}} * (dab - 0.5) + 0.12 * rnd;
            float patch = (1.0 - smoothstep(edge - {{F(Smooth.PatchSoft)}}, edge + {{F(Smooth.PatchSoft)}}, across))
                * smoothstep(0.2 + 0.12 * rnd, 0.28 + 0.12 * rnd + {{F(Smooth.PatchSoft)}}, along)
                * (1.0 - smoothstep(0.8 - {{F(Smooth.PatchSoft)}}, 0.86, along));
            float spots = {{F(Smooth.Spots)}} * smoothstep(0.6, 0.63, dab) * smoothstep(0.1, 0.3, along);
            patch = max(patch * 0.85, spots);
            patch *= front ? smoothstep(0.05, 0.3, up) : 0.0;
            vec3 col = mix(base_col, lit_col, patch);
            col *= mix(vec3(1.0), warm_tint, smoothstep(0.7, 0.9, along) * patch);
            col = mix(col, shade_col, 0.65 * smoothstep(0.72, 0.98, across) * (1.0 - patch));
            col = mix(shade_col, col, smoothstep(0.16, 0.4, along + (dab - 0.5) * 0.15));
            if (!front) col = mix(col, shade_col, 0.6);
            float line = smoothstep(0.9, 1.0, across) * smoothstep(0.7, 0.9, along) * 0.5 + smoothstep(0.9, 1.0, along);
            return mix(col, ink, 0.7 * clamp(line, 0.0, 1.0));
        }
        """;

    // Light in painted steps (Atmosphere's rules): the wrapped light times the shadow and
    // the cloud shadow is quantised into shade, mid and lit, edges broken by the brush;
    // the light's own colour stays smooth and ambient is left to the scene. At low sun
    // the light wraps further and the shade lifts, and the needles let light through when
    // a tree stands against the sun (D-285). Fires and doorways fade with their falloff.
    static readonly string Light = $$"""
            float sky = LIGHT_IS_DIRECTIONAL ? shade : 1.0;
            float elevation = (INV_VIEW_MATRIX * vec4(LIGHT, 0.0)).y;
            float low_sun = LIGHT_IS_DIRECTIONAL ? 1.0 - smoothstep(0.2, 0.55, elevation) : 0.0;
            float wrap = mix(0.4, 0.85, low_sun);
            float wrapped = clamp(dot(NORMAL, LIGHT) * (1.0 - wrap) + wrap, 0.0, 1.0);
            float l = wrapped * ATTENUATION * sky + (dab - 0.5) * {{F(Smooth.BandBreak)}};
            float soft = {{F(Smooth.BandSoft)}};
            float mid = smoothstep(0.3 - soft, 0.3 + soft, l);
            float lit = smoothstep(0.62 - soft, 0.62 + soft, l);
            float band = 0.1 + 0.3 * low_sun + 0.36 * mid + 0.3 * lit;
            vec3 tone = mix(shade_tint, vec3(1.0), mid);
            float reach = LIGHT_IS_DIRECTIONAL ? 1.0 : smoothstep(0.0, 0.25, ATTENUATION);
            DIFFUSE_LIGHT += LIGHT_COLOR * tone * band * reach / PI;
            if (LIGHT_IS_DIRECTIONAL) {
                float back = max(0.0, dot(VIEW, -LIGHT));
                float edge = 1.0 - abs(dot(NORMAL, VIEW));
                DIFFUSE_LIGHT += LIGHT_COLOR * sky * (back * back * (0.35 + 0.5 * edge)) * mix(0.25, 1.0, low_sun) / PI;
            }
        """;
}
