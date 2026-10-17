using Godot;

namespace LifeGame;

// World props in the model-1 style, for the Environment lane (model-2). Each is a
// plain mesh with near-white vertex colors, made to drop into Terrain's
// MultiMeshes: the instance color tints it, the caller's material (Look.TreeBark,
// Look.RockSurface...) shades it, and it keeps the origin and size of the mesh it
// replaces (trunk: base at y = 0, 2.8 m tall; rock: about 1 m across).
public static class PropMeshes
{
    static Mesh? trunk, stump, cattails;

    static MeshKit Kit() => new(true) { Skinned = false };

    static float H(int a, int b) => MeshKit.Hash(a, b + 90);

    static ArrayMesh Done(MeshKit k)
    {
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, k.CommitToArrays());
        return mesh;
    }

    // A broadleaf trunk: flared roots gripping the ground, a slight bend, and two
    // broken-off branch stubs. Same base and top (0, 2.8, 0) as Terrain's trunk, so
    // the canopies sit where they did.
    public static Mesh Trunk => trunk ??= BuildTrunk();

    static ArrayMesh BuildTrunk()
    {
        var k = Kit();
        k.Paint(Colors.White).Loft(new Vector3(0, -0.15f, 0), new Vector3(0, 2.8f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.3f, 0.28f), new(0.06f, 0.24f, 0.23f), new(0.2f, 0.19f, 0.18f, 0.01f), new(0.55f, 0.15f, 0.145f, -0.02f, 0.025f), new(1f, 0.1f, 0.1f),
        }, 7, 0.2f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5f + H(i, 1) * 0.5f;
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            k.Paint(Colors.White, 0.9f + H(i, 2) * 0.1f).Tube(new[] { dir * 0.08f + Vector3.Up * 0.35f, dir * 0.28f + Vector3.Up * 0.08f, dir * (0.45f + H(i, 3) * 0.15f) - Vector3.Up * 0.04f }, 0.09f, 5, 0.03f);
        }
        k.Paint(Colors.White, 0.92f).Tube(new[] { new Vector3(0.1f, 1.7f, 0), new Vector3(0.32f, 1.95f, 0.04f) }, 0.055f, 5, 0.04f);
        k.Paint(Colors.White, 0.92f).Tube(new[] { new Vector3(-0.06f, 2.2f, 0.06f), new Vector3(-0.24f, 2.4f, 0.2f) }, 0.045f, 5, 0.03f);
        return Done(k);
    }

    // A weathered boulder about 1 m across, settled into the ground with moss on
    // top. Four shapes, since boulders are everywhere: worn lumps, a tall split
    // stone, a low slab, and a round stone cracked in two.
    public const int RockVariants = 4, FallenLogVariants = 2;

    public static Mesh Rock(int variant) => Variant("rock", variant, RockVariants, BuildRock);

    static ArrayMesh BuildRock(int v)
    {
        var k = Kit();
        var moss = new Color(0.62f, 0.78f, 0.45f);
        switch (v)
        {
            case 0:
                k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.1f, 0), new Vector3(0.48f, 0.34f, 0.42f), 9, 5, new Basis(Vector3.Up, 0.3f), 0.4f);
                k.Paint(Colors.White, 0.95f).Ellipsoid(new Vector3(0.36f, 0.04f, 0.16f), new Vector3(0.28f, 0.22f, 0.25f), 7, 4, new Basis(Vector3.Up, 1.1f), 0.4f);
                k.Paint(Colors.White, 0.97f).Ellipsoid(new Vector3(-0.22f, 0.02f, -0.24f), new Vector3(0.22f, 0.16f, 0.2f), 7, 4, new Basis(Vector3.Up, 2.0f), 0.4f);
                k.Paint(moss).Ellipsoid(new Vector3(-0.04f, 0.38f, -0.02f), new Vector3(0.3f, 0.07f, 0.26f), 8, 3, new Basis(Vector3.Up, 0.6f), 0.3f);
                break;
            case 1:
                // Tall and split down the middle, the halves leaning apart.
                k.Paint(Colors.White).Ellipsoid(new Vector3(-0.13f, 0.32f, 0), new Vector3(0.24f, 0.58f, 0.36f), 7, 5, new Basis(Vector3.Forward, -0.1f), 0.4f);
                k.Paint(Colors.White, 0.93f).Ellipsoid(new Vector3(0.17f, 0.26f, 0.02f), new Vector3(0.22f, 0.5f, 0.32f), 7, 5, new Basis(Vector3.Forward, 0.14f), 0.4f);
                k.Paint(moss, 0.95f).Ellipsoid(new Vector3(-0.17f, 0.82f, 0), new Vector3(0.14f, 0.05f, 0.2f), 6, 2, new Basis(Vector3.Forward, -0.1f));
                break;
            case 2:
                // A low, broad slab with a smaller one leaning on it.
                k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.08f, 0), new Vector3(0.6f, 0.2f, 0.45f), 8, 3, new Basis(Vector3.Up, 0.5f) * new Basis(Vector3.Forward, 0.06f), 0.5f);
                k.Paint(Colors.White, 0.92f).Ellipsoid(new Vector3(0.38f, 0.16f, -0.2f), new Vector3(0.28f, 0.14f, 0.22f), 7, 3, new Basis(Vector3.Up, 1.4f) * new Basis(Vector3.Forward, 0.4f), 0.5f);
                k.Paint(moss).Ellipsoid(new Vector3(-0.15f, 0.27f, 0.05f), new Vector3(0.35f, 0.04f, 0.25f), 8, 2, new Basis(Vector3.Up, 0.5f));
                break;
            default:
                // A round stone cracked in two, a fern of moss in the crack.
                k.Paint(Colors.White).Ellipsoid(new Vector3(-0.2f, 0.18f, 0), new Vector3(0.3f, 0.42f, 0.4f), 8, 5, new Basis(Vector3.Forward, -0.05f), 0.5f);
                k.Paint(Colors.White, 0.95f).Ellipsoid(new Vector3(0.22f, 0.15f, 0.03f), new Vector3(0.28f, 0.38f, 0.38f), 8, 5, new Basis(Vector3.Forward, 0.08f), 0.5f);
                k.Paint(moss, 0.9f).Ellipsoid(new Vector3(0.0f, 0.3f, 0.0f), new Vector3(0.06f, 0.18f, 0.3f), 6, 3);
                k.Paint(moss).Ellipsoid(new Vector3(0.25f, 0.5f, 0.02f), new Vector3(0.18f, 0.05f, 0.2f), 6, 2);
                break;
        }
        return Done(k);
    }

    // A fallen trunk lying in the grass, moss along the top: straight with a
    // splintered end, or broken in two with a forked crown and a stub of root.
    public static Mesh FallenLog(int variant) => Variant("fallenlog", variant, FallenLogVariants, BuildFallenLog);

    static ArrayMesh BuildFallenLog(int v)
    {
        var k = Kit();
        var moss = new Color(0.62f, 0.8f, 0.45f);
        if (v == 0)
        {
            k.Paint(Colors.White, 0.75f).Loft(new Vector3(-1.5f, 0.2f, 0), new Vector3(1.5f, 0.15f, 0.05f), new MeshKit.Ring[]
            {
                new(0f, 0.22f, 0.21f), new(0.5f, 0.2f, 0.19f, 0f, 0.01f), new(1f, 0.16f, 0.15f),
            }, 8, 0.05f);
            Splinters(k, new Vector3(1.5f, 0.15f, 0.05f), Vector3.Right, 0.09f);
            k.Paint(Colors.White, 0.97f).Ellipsoid(new Vector3(-1.505f, 0.2f, 0), new Vector3(0.006f, 0.19f, 0.18f), 8, 2);
            k.Paint(moss).Loft(new Vector3(-1.2f, 0.37f, 0.0f), new Vector3(0.6f, 0.34f, 0.02f), new MeshKit.Ring[]
            {
                new(0f, 0.02f, 0.08f), new(0.5f, 0.03f, 0.12f), new(1f, 0.015f, 0.06f),
            }, 6, 0.3f);
        }
        else
        {
            // The butt end with roots torn out of the ground, broken in the middle;
            // the crown half lies a little apart, forking into two limbs.
            k.Paint(Colors.White, 0.75f).Loft(new Vector3(-1.6f, 0.24f, 0), new Vector3(-0.1f, 0.2f, 0.05f), new MeshKit.Ring[]
            {
                new(0f, 0.26f, 0.25f), new(0.15f, 0.21f, 0.2f), new(1f, 0.18f, 0.17f),
            }, 8, 0.05f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.Tau / 5f + 0.4f;
                var dir = new Vector3(0, Mathf.Sin(a), Mathf.Cos(a));
                k.Paint(Colors.White, 0.65f).Tube(new[] { new Vector3(-1.55f, 0.24f, 0) + dir * 0.15f, new Vector3(-1.85f - H(i, 50) * 0.2f, 0.24f, 0) + dir * (0.4f + H(i, 51) * 0.2f) }, 0.07f, 5, 0.02f);
            }
            Splinters(k, new Vector3(-0.1f, 0.2f, 0.05f), Vector3.Right, 0.11f);
            var crown = new Vector3(0.15f, 0.17f, 0.18f);
            k.Paint(Colors.White, 0.75f).Loft(crown, crown + new Vector3(1.1f, -0.02f, 0.25f), new MeshKit.Ring[]
            {
                new(0f, 0.17f, 0.16f), new(1f, 0.13f, 0.12f),
            }, 8, 0.05f);
            Splinters(k, crown, Vector3.Left, 0.1f);
            var fork = crown + new Vector3(1.1f, -0.02f, 0.25f);
            k.Paint(Colors.White, 0.72f).Tube(new[] { fork, fork + new Vector3(0.5f, -0.04f, 0.35f), fork + new Vector3(0.9f, -0.08f, 0.45f) }, 0.1f, 6, 0.04f);
            k.Paint(Colors.White, 0.72f).Tube(new[] { fork, fork + new Vector3(0.55f, 0.0f, -0.12f), fork + new Vector3(0.95f, -0.06f, -0.3f) }, 0.09f, 6, 0.035f);
            k.Paint(moss).Loft(new Vector3(-1.3f, 0.44f, 0.0f), new Vector3(-0.3f, 0.38f, 0.04f), new MeshKit.Ring[]
            {
                new(0f, 0.02f, 0.1f), new(0.5f, 0.03f, 0.13f), new(1f, 0.015f, 0.06f),
            }, 6, 0.3f);
        }
        return Done(k);
    }

    // Pale splinters sticking out of a broken end.
    static void Splinters(MeshKit k, Vector3 at, Vector3 outward, float radius)
    {
        var (side, front) = MeshKit.Frame(outward);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5f + H(i, 52);
            var root = at + (side * Mathf.Sin(a) + front * Mathf.Cos(a)) * radius;
            k.Paint(Colors.White).Tube(new[] { root, root + outward * (0.12f + H(i, 1) * 0.1f) + (side * (H(i, 2) - 0.5f) + front * (H(i, 3) - 0.5f)) * 0.04f }, 0.03f, 4, 0.004f);
        }
    }

    // A pine of stacked tiers with jagged, drooping edges, like the reference art
    // (docs/art-reference, D-232): each tier a ring of points hanging down, a
    // notch between each, and the underside in shade. Same trunk and height as
    // Terrain's pine, so it can sit on PropMeshes.Trunk. Prototype for model-4.
    public static Mesh ScallopPine => scallopPine ??= BuildScallopPine();
    static Mesh? scallopPine, leafClumps;

    static ArrayMesh BuildScallopPine()
    {
        var k = Kit();
        float[] radii = { 1.35f, 1.1f, 0.85f, 0.55f };
        for (int t = 0; t < radii.Length; t++)
        {
            float bottom = 1.5f + t * 0.72f, top = bottom + 1.3f - t * 0.1f, r = radii[t];
            int points = 11 - t;
            var apex = new Vector3(0, top, 0);
            var under = new Vector3(0, bottom + 0.35f, 0);
            var rim = new Vector3[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float a = (i + H(t, i) * 0.3f) * Mathf.Pi / points + t * 0.7f;
                bool tip = i % 2 == 0;
                float rr = tip ? r * (0.95f + H(t, i + 20) * 0.12f) : r * 0.72f;
                float y = tip ? bottom - 0.12f - H(t, i + 40) * 0.08f : bottom + 0.14f;
                rim[i] = new Vector3(Mathf.Sin(a) * rr, y, Mathf.Cos(a) * rr);
            }
            for (int i = 0; i < rim.Length; i++)
            {
                var a = rim[i];
                var b = rim[(i + 1) % rim.Length];
                var outward = (((a + b) * 0.5f) with { Y = 0 }).Normalized();
                k.Paint(Colors.White, 0.96f + 0.04f * t).Tri(apex, a, b, outward + Vector3.Up * 0.6f, outward + Vector3.Up * 0.6f, outward + Vector3.Up * 0.6f);
                k.Paint(Colors.White, 0.6f).Tri(under, b, a, Vector3.Down, Vector3.Down, Vector3.Down);
            }
        }
        return Done(k);
    }

    // A pine matched to the Golden Skull card pine (Forest_big_tree_needle_04), one
    // unit tall (scale it to the tree's height): a bare trunk to about a fifth of
    // the height, then five to seven close tiers of broad drooping boughs, each
    // tier's tips hanging over the tier below so the crown reads full. Every variant
    // differs (D-166): tier count, width, trunk height, lean, a fuller side, now and
    // then a missing bough, and a slight colour shift. Drawn with MaterialLook.CardPine,
    // which paints the boughs in the card's flat bands from their UVs: UV.x runs
    // across a bough (-1 edge, 0 ridge, 1 edge), UV.y from root to tip; UV2.x tells
    // the parts apart (0 trunk, 1 core, 2 bough) and UV2.y is a per-bough random;
    // the vertex colour carries the variant's tint.
    public const int PineVariants = 16;
    public static Mesh PaintedPine(int variant) => Variant("paintedpine", variant, PineVariants, BuildPaintedPine);

    static ArrayMesh BuildPaintedPine(int v)
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
        k.Paint(Colors.White).Loft(Vector3.Zero, Lean(new Vector3(0, trunkTop + 0.25f, 0)), new MeshKit.Ring[]
        {
            new(0f, 0.036f, 0.036f), new(0.1f, 0.027f, 0.027f), new(1f, 0.018f, 0.018f),
        }, 7, 0.2f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.Tau / 4 + 0.4f + R(13 + i) * 0.6f;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            k.Paint(Colors.White).Tube(new[] { dir * 0.012f + Vector3.Up * 0.05f, dir * 0.065f + Vector3.Up * 0.004f }, 0.012f, 4, 0.004f);
        }

        var f = new FoliageKit(tint, Lean);

        float foliage = trunkTop + 0.13f, span = 1f - foliage, step = span / (tiers + 0.3f);
        for (int t = 0; t < tiers; t++)
        {
            float rootY = foliage + step * t;
            float frac = (rootY - foliage) / span;
            float length = (0.015f + 0.215f * Mathf.Pow(1f - frac, 1.05f)) * width;
            float droop = Mathf.Max(length * 0.72f, step * 1.2f);
            f.Core(rootY - droop * 0.75f, Mathf.Min(rootY + step * 1.3f, 1f), length * 0.5f);
            int count = Mathf.Max(5, Mathf.RoundToInt(Mathf.Tau * length / (0.07f * width)));
            float wide = Mathf.Tau * length * 0.5f / count * 1.15f;
            float turn = R(30 + t) * Mathf.Tau;
            for (int ring = 0; ring < 2; ring++)
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
        return mesh;
    }


    // A broadleaf tree matched to the Golden Skull card (Forest_big_tree_leaf_01), one
    // unit tall: a short stout trunk under a tall egg-shaped crown of drooping leaf
    // lobes laid in rows, each row hanging over the one below, round a dark heart.
    // Seeded per variant (D-166): row count, width, crown height and squash, lean, a
    // fuller side and a slight colour shift. Drawn with MaterialLook.CardBroadleaf.
    public const int BroadleafVariants = 16;
    public static Mesh PaintedBroadleaf(int variant) => Variant("paintedbroadleaf", variant, BroadleafVariants, BuildPaintedBroadleaf);

    // Where the crown's heart sits, for the shader's crown-wide light.
    public const float BroadleafCrownY = 0.58f;

    static ArrayMesh BuildPaintedBroadleaf(int v)
    {
        float R(int salt) => H(v * 89 + 11, salt + 800);
        float width = 0.86f + R(1) * 0.28f;
        int rows = 12 + (int)(R(2) * 4f);
        float squash = 0.92f + R(3) * 0.16f;
        float leanAmount = R(4) * 0.04f, leanAngle = R(5) * Mathf.Tau;
        var leanDir = new Vector3(Mathf.Sin(leanAngle), 0, Mathf.Cos(leanAngle));
        float fullAngle = R(6) * Mathf.Tau, fullAmount = 0.06f + R(7) * 0.14f;
        float value = 0.9f + R(8) * 0.18f, warm = (R(9) - 0.5f) * 0.14f;
        var tint = new Color(value * (1f + warm), value, value * (1f - warm));
        Vector3 Lean(Vector3 p) => p + leanDir * leanAmount * p.Y * p.Y;
        var center = new Vector3(0, BroadleafCrownY, 0);
        var radii = new Vector3(0.25f * width, 0.4f * squash, 0.25f * width);

        var k = Kit();
        k.Paint(Colors.White).Loft(Vector3.Zero, Lean(center), new MeshKit.Ring[]
        {
            new(0f, 0.058f, 0.058f), new(0.12f, 0.045f, 0.045f), new(1f, 0.032f, 0.032f),
        }, 8, 0.2f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5 + R(13 + i) * 0.6f;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            k.Paint(Colors.White).Tube(new[] { dir * 0.02f + Vector3.Up * 0.07f, dir * 0.1f + Vector3.Up * 0.005f }, 0.018f, 4, 0.005f);
        }

        var f = new FoliageKit(tint, Lean);
        f.Blob(center, radii * 0.84f);
        // Rows of lobes from the crown's top down to its underside.
        for (int r = 0; r < rows; r++)
        {
            float lat0 = 0.22f + r * 2.3f / (rows - 1);
            float ringR0 = radii.X * Mathf.Sin(lat0);
            float length = (0.05f + 0.045f * Mathf.Sin(lat0)) * width;
            int count = Mathf.Max(4, Mathf.RoundToInt(Mathf.Tau * ringR0 / (0.05f * width)));
            float wide = Mathf.Tau * ringR0 / count * 0.7f + 0.015f;
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
                var root = new Vector3(0, y, 0) + dir * ringR * full * 0.85f;
                // Lobes near the top reach out more; lower ones hang down the crown's side.
                float droop = length * (0.35f + 0.75f * Mathf.Clamp(lat / 1.6f, 0f, 1.2f)) * (0.85f + rnd * 0.3f);
                f.Bough(root, dir, length * (0.85f + rnd * 0.35f), droop, wide * (0.9f + rnd * 0.2f), rnd);
            }
        }
        // A tuft of short lobes on top.
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5 + R(60) * Mathf.Tau;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            f.Bough(center + new Vector3(0, radii.Y * 1.0f, 0) + dir * 0.02f, dir, 0.07f * width, 0.03f, 0.06f, H(v, i + 990));
        }

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, k.CommitToArrays());
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, f.Commit());
        return mesh;
    }

    // Painted foliage built for MaterialLook.CardPine: boughs (UV across and along,
    // UV2.x 2) and dark hearts (UV2.x 1), the vertex colour carrying a variant's tint,
    // each point passed through warp (a lean) as it is written.
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
                const int segs = 5;
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

    // A broadleaf crown of rounded leaf clumps, each with a jagged lower edge, for
    // the reference look's leafy trees. Sits on PropMeshes.Trunk (top at 2.8 m).
    public static Mesh LeafClumps => leafClumps ??= BuildLeafClumps();

    static ArrayMesh BuildLeafClumps()
    {
        var k = Kit();
        void Clump(Vector3 at, float r, int seed)
        {
            k.Paint(Colors.White, 0.92f + H(seed, 1) * 0.1f).Ellipsoid(at, new Vector3(r, r * 0.78f, r), 9, 5, new Basis(Vector3.Up, H(seed, 2) * 3f), 0.85f);
            // A skirt of points round the lower edge.
            int points = 9;
            for (int i = 0; i < points; i++)
            {
                float a = (i + H(seed, i + 3) * 0.4f) * Mathf.Tau / points;
                var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var root = at + dir * r * 0.82f + Vector3.Down * r * 0.15f;
                var tip = at + dir * r * 1.08f + Vector3.Down * r * (0.42f + H(seed, i + 13) * 0.12f);
                k.Paint(Colors.White, 0.8f).Blade(root, tip, dir, r * 0.5f, 0f);
            }
        }
        Clump(new Vector3(0, 3.75f, 0), 1.0f, 1);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5 + 0.3f;
            Clump(new Vector3(Mathf.Sin(a) * 0.95f, 3.05f + 0.15f * (i % 2), Mathf.Cos(a) * 0.95f), 0.75f + 0.08f * (i % 3), i + 2);
        }
        return Done(k);
    }

    // Mushrooms for the ground cover (reference art, D-232): 0 one brown cap, 1 a
    // cluster of small pale ones, 2 a muted red cap with pale spots. Colours are
    // baked and quiet (no saturated reds); tint near white. About 0.1 to 0.15 m tall.
    public const int MushroomVariants = 3;
    public static Mesh Mushroom(int variant) => Variant("mushroom", variant, MushroomVariants, BuildMushroom);

    static ArrayMesh BuildMushroom(int v)
    {
        var k = Kit();
        var stem = new Color(0.86f, 0.81f, 0.7f);
        void One(Vector3 at, float tall, float cap, Color top, float lean)
        {
            var head = at + new Vector3(lean, tall, lean * 0.5f);
            k.Paint(stem).Tube(new[] { at, at.Lerp(head, 0.5f) + new Vector3(lean * 0.2f, 0, 0), head }, cap * 0.28f, 6, cap * 0.22f);
            k.Paint(top).Ellipsoid(head + Vector3.Up * cap * 0.15f, new Vector3(cap, cap * 0.55f, cap), 8, 4, null, 0.35f);
            k.Paint(stem, 0.85f).Ellipsoid(head + Vector3.Up * cap * 0.02f, new Vector3(cap * 0.85f, cap * 0.08f, cap * 0.85f), 8, 2);
        }
        switch (v)
        {
            case 0:
                One(Vector3.Zero, 0.1f, 0.065f, new Color(0.55f, 0.38f, 0.24f), 0.01f);
                break;
            case 1:
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 2.4f, r = 0.02f + i * 0.012f;
                    One(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), 0.05f + H(i, 70) * 0.04f, 0.025f + H(i, 71) * 0.012f, new Color(0.84f, 0.78f, 0.64f), (H(i, 72) - 0.5f) * 0.03f);
                }
                break;
            default:
                One(Vector3.Zero, 0.12f, 0.075f, new Color(0.62f, 0.27f, 0.2f), -0.01f);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 1.1f + 0.3f, r = 0.03f + H(i, 73) * 0.02f;
                    var spot = new Vector3(Mathf.Cos(a) * r - 0.01f, 0.12f + 0.075f * 0.15f + 0.035f - r * 0.35f, Mathf.Sin(a) * r - 0.005f);
                    k.Paint(new Color(0.9f, 0.86f, 0.76f)).Ellipsoid(spot, new Vector3(0.008f, 0.004f, 0.008f), 5, 2);
                }
                break;
        }
        return Done(k);
    }

    // Fallen twigs lying in the grass: a forked stick about 0.3 to 0.5 m long,
    // near-white for the bark tint.
    public const int TwigVariants = 3;
    public static Mesh Twig(int variant) => Variant("twig", variant, TwigVariants, BuildTwig);

    static ArrayMesh BuildTwig(int v)
    {
        var k = Kit();
        float len = 0.3f + V(v, 30) * 0.2f;
        var a = new Vector3(-len * 0.5f, 0.015f, 0);
        var b = new Vector3(len * 0.5f, 0.015f, (V(v, 31) - 0.5f) * 0.08f);
        var mid = a.Lerp(b, 0.5f) + new Vector3(0, 0.01f, (V(v, 32) - 0.5f) * 0.05f);
        k.Paint(Colors.White, 0.85f).Tube(new[] { a, mid, b }, 0.012f, 5, 0.006f);
        for (int i = 0; i < 1 + v % 2 + 1; i++)
        {
            float t = 0.3f + i * 0.25f;
            var root = a.Lerp(b, t);
            float side = i % 2 == 0 ? 1f : -1f;
            var tip = root + new Vector3(len * 0.22f, 0.01f + V(v, 33 + i) * 0.03f, side * len * (0.15f + V(v, 36 + i) * 0.12f));
            k.Paint(Colors.White, 0.8f).Tube(new[] { root, tip }, 0.007f, 4, 0.003f);
        }
        return Done(k);
    }

    // A cut stump with a pale face and rings, for where trees were felled.
    public static Mesh Stump => stump ??= BuildStump();

    static ArrayMesh BuildStump()
    {
        var k = Kit();
        k.Paint(Colors.White, 0.75f).Loft(new Vector3(0, -0.05f, 0), new Vector3(0, 0.4f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.3f, 0.29f), new(0.2f, 0.22f, 0.21f), new(1f, 0.19f, 0.185f),
        }, 7, 0.02f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.Tau / 4f + 0.3f;
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            k.Paint(Colors.White, 0.7f).Tube(new[] { dir * 0.1f + Vector3.Up * 0.2f, dir * 0.38f - Vector3.Up * 0.03f }, 0.07f, 5, 0.03f);
        }
        k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.4f, 0), new Vector3(0.175f, 0.008f, 0.17f), 7, 2);
        // Growth rings: a darker band between the pale face and the paler heart.
        k.Paint(new Color(0.72f, 0.62f, 0.48f)).Ellipsoid(new Vector3(0, 0.403f, 0), new Vector3(0.135f, 0.006f, 0.13f), 7, 2);
        k.Paint(new Color(0.85f, 0.76f, 0.62f)).Ellipsoid(new Vector3(0, 0.406f, 0), new Vector3(0.09f, 0.006f, 0.09f), 7, 2);
        k.Paint(new Color(0.7f, 0.6f, 0.46f)).Ellipsoid(new Vector3(0.01f, 0.409f, 0.005f), new Vector3(0.035f, 0.005f, 0.035f), 6, 2);
        return Done(k);
    }

    // --- env-4 and env-5 props (Environment, D-152). Landmark props come in seeded
    // variants that differ in shape, not just size (Justin: things placed many
    // times should look quite different every time). Pick a variant per instance
    // from its seed; variants wrap around, so any int works.

    public const int BirchVariants = 3, HeatherVariants = 3, GorseVariants = 3, OutcropVariants = 6, RiverStoneVariants = 6;
    static readonly System.Collections.Generic.Dictionary<(string, int), Mesh> Variants = new();

    static Mesh Variant(string kind, int variant, int count, System.Func<int, ArrayMesh> build)
    {
        int v = ((variant % count) + count) % count;
        if (!Variants.TryGetValue((kind, v), out var mesh)) Variants[(kind, v)] = mesh = build(v);
        return mesh;
    }

    // A seeded number 0 to 1 for variant v.
    static float V(int v, int salt) => MeshKit.Hash(v * 31 + salt, 977);

    // A birch or alder by the water: slim and pale, with dark marks across the
    // bark. Same base and top (0, 2.8, 0) as Trunk, so the leafy canopy fits; the
    // variants bend differently and carry their marks in different places.
    public static Mesh Birch(int variant) => Variant("birch", variant, BirchVariants, BuildBirch);

    static ArrayMesh BuildBirch(int v)
    {
        var k = Kit();
        float bx = (V(v, 1) - 0.5f) * 0.08f, bz = (V(v, 2) - 0.5f) * 0.08f, slim = 0.9f + V(v, 3) * 0.2f;
        k.Paint(Colors.White).Loft(new Vector3(0, -0.1f, 0), new Vector3(0, 2.8f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.17f * slim, 0.16f * slim), new(0.05f, 0.13f * slim, 0.125f * slim), new(0.5f, 0.1f * slim, 0.095f * slim, bz, bx), new(1f, 0.065f, 0.065f),
        }, 7, 0.2f);
        // Dark marks: short dashes curving round the bark, following the bend.
        int marks = 20 + (int)(V(v, 4) * 12f);
        for (int i = 0; i < marks; i++)
        {
            float y = 0.3f + (i + H(i + v * 50, 20) * 0.6f) * 2.35f / marks;
            float t = (y + 0.1f) / 2.9f;
            float bend = t < 0.5f ? Mathf.Max(0f, (t - 0.05f) / 0.45f) : (1f - t) / 0.5f;
            float r = (t < 0.5f ? Mathf.Lerp(0.13f, 0.1f, Mathf.Max(0f, (t - 0.05f) / 0.45f)) * slim : Mathf.Lerp(0.1f * slim, 0.065f, (t - 0.5f) / 0.5f));
            float a = H(i + v * 50, 21) * Mathf.Tau;
            var axisAt = new Vector3(bx * bend, y, bz * bend);
            float arc = (0.12f + H(i + v * 50, 22) * 0.18f) * (H(i + v * 50, 39) > 0.8f ? 2f : 1f);
            k.Paint(Colors.White, 0.18f).Tube(MeshKit.Circle(axisAt, Vector3.Up, r * 0.93f, 6, arc, 0f, a), 0.012f + H(i + v * 50, 41) * 0.006f, 4, 0.006f);
        }
        // Darker, rougher bark at the foot, higher on some trees than others.
        float foot = 0.25f + V(v, 5) * 0.25f;
        k.Paint(Colors.White, 0.45f).Loft(new Vector3(0, -0.1f, 0), new Vector3(0, foot, 0), new MeshKit.Ring[]
        {
            new(0f, 0.18f * slim, 0.17f * slim), new(1f, 0.125f * slim, 0.12f * slim),
        }, 7, 0.1f);
        return Done(k);
    }

    // Heather on the hilltop heath: a low mound of fine clumps, about 0.6 m across,
    // tipped with flower heads. Variants differ in clump count, spread, height and how much is
    // in flower. Colors in the vertices; tint near white.
    public static Mesh Heather(int variant) => Variant("heather", variant, HeatherVariants, BuildHeather);

    static ArrayMesh BuildHeather(int v)
    {
        var k = Kit();
        // Dusky green-brown with muted purple flower heads, close to the grass in
        // brightness so it blends. The painted shader finds the heads by their
        // purple and tones them green-brown, with a soft mauve only in late summer.
        var green = new Color(0.35f, 0.37f, 0.24f);
        var purple = new Color(0.46f, 0.34f, 0.46f);
        int clumps = 12 + (int)(V(v, 1) * 9f);
        float spread = 0.2f + V(v, 2) * 0.12f, tall = 0.8f + V(v, 3) * 0.5f, flowering = 0.55f + V(v, 4) * 0.45f;
        // Some mounds are long rather than round.
        float stretch = 0.8f + V(v, 5) * 0.5f;
        for (int i = 0; i < clumps; i++)
        {
            float a = i * 2.4f + V(v, 6) * 3f, r = Mathf.Sqrt(i / (float)clumps) * spread;
            var at = new Vector3(Mathf.Cos(a) * r * stretch, (0.1f + (spread - r) * 0.6f) * tall, Mathf.Sin(a) * r / stretch);
            float size = 0.08f + H(i + v * 40, 23) * 0.06f;
            k.Paint(green, 0.85f + H(i + v * 40, 24) * 0.25f).Ellipsoid(at, new Vector3(size, size * 0.8f, size), 6, 3);
            if (H(i + v * 40, 26) < flowering)
                k.Paint(purple, 0.85f + H(i + v * 40, 25) * 0.25f).Ellipsoid(at + Vector3.Up * size * 0.55f, new Vector3(size * 0.7f, size * 0.35f, size * 0.7f), 5, 2);
        }
        return Done(k);
    }

    // Gorse: a spiky bush about 1 m high, dark green, flecked with yellow flowers.
    // Variants differ in shape (round, tall, sprawling) and how much is in flower.
    // Colors in the vertices; tint near white.
    public static Mesh Gorse(int variant) => Variant("gorse", variant, GorseVariants, BuildGorse);

    static ArrayMesh BuildGorse(int v)
    {
        var k = Kit();
        var green = new Color(0.22f, 0.32f, 0.18f);
        var yellow = new Color(0.95f, 0.80f, 0.20f);
        // Round, tall or sprawling.
        var shape = v switch { 0 => new Vector3(1f, 1f, 1f), 1 => new Vector3(0.8f, 1.3f, 0.8f), _ => new Vector3(1.35f, 0.75f, 1.1f) };
        int lumps = 7 + (int)(V(v, 1) * 4f);
        for (int i = 0; i < lumps; i++)
        {
            float a = i * 2.4f + V(v, 2) * 3f, r = i == 0 ? 0f : 0.25f + H(i + v * 30, 26) * 0.12f;
            var at = new Vector3(Mathf.Cos(a) * r * shape.X, (0.35f + H(i + v * 30, 27) * 0.35f - r * 0.4f) * shape.Y, Mathf.Sin(a) * r * shape.Z);
            k.Paint(green, 0.8f + H(i + v * 30, 28) * 0.3f).Ellipsoid(at, Vector3.One * (0.25f + H(i + v * 30, 29) * 0.08f), 6, 4);
        }
        float flowers = 0.2f + V(v, 3) * 0.25f;
        var body = new Vector3(0.42f * shape.X, 0.38f * shape.Y, 0.42f * shape.Z);
        for (int i = 0; i < 70; i++)
        {
            float y = 1f - (i + 0.5f) / 70f * 1.6f;
            float rr = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y)), a = i * 2.39996f + v;
            var dir = new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr).Normalized();
            var root = new Vector3(0, 0.5f * shape.Y, 0) + dir * body;
            if (root.Y < 0.08f) continue;
            if (H(i + v * 30, 32) < flowers)
                k.Paint(yellow, 0.9f + H(i + v * 30, 30) * 0.15f).Ellipsoid(root + dir * 0.03f, Vector3.One * 0.035f, 5, 3);
            else
                k.Paint(green, 0.7f).Tube(new[] { root, root + dir * (0.1f + H(i + v * 30, 31) * 0.06f) }, 0.012f, 3, 0.001f, false);
        }
        return Done(k);
    }

    // Cattails at the pond edge, for the grass shader: long blades and a few stalks
    // topped with brown seed heads. UV.y runs from root to tip so they sway.
    public static Mesh Cattails => cattails ??= BuildCattails();

    static ArrayMesh BuildCattails()
    {
        const float tall = 1.5f;
        var k = Kit();
        k.SwayHeight = tall;
        for (int i = 0; i < 12; i++)
        {
            float a = i * 2.4f + H(i, 32);
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var root = dir * 0.06f * H(i, 33);
            float len = 0.9f + H(i, 34) * 0.5f;
            var tip = root + dir * (0.12f + H(i, 35) * 0.25f) + Vector3.Up * len;
            k.Paint(Colors.White, 0.85f + H(i, 36) * 0.2f).Blade(root, tip, dir.Cross(Vector3.Up), 0.035f, 0.05f);
        }
        for (int i = 0; i < 3; i++)
        {
            float a = i * 2.1f + 0.5f;
            var root = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.05f;
            float h = 1.15f + H(i, 37) * 0.3f;
            var top = root + new Vector3(0.02f * i, h, -0.015f * i);
            k.Paint(Colors.White, 0.8f).Tube(new[] { root, top + Vector3.Up * 0.12f }, 0.008f, 4, 0.004f, false);
            k.Paint(new Color(0.45f, 0.3f, 0.2f)).Tube(new[] { top - Vector3.Up * 0.12f, top }, 0.024f, 6);
        }
        return Done(k);
    }

    // A hilltop outcrop, a landmark: layers of the same rock dipping one way, run
    // together into one weathered mass, with fallen blocks at its foot and moss on
    // its ledges. Variants differ in layer count (3 to 5), dip and its direction,
    // size, how far each layer steps back, the fallen blocks and the moss. Roughly
    // 2 to 3.2 m across and 1.3 to 2.3 m high.
    public static Mesh Outcrop(int variant) => Variant("outcrop", variant, OutcropVariants, BuildOutcrop);

    static ArrayMesh BuildOutcrop(int v)
    {
        var k = Kit();
        int layers = 3 + v % 3;
        float dip = (0.08f + V(v, 1) * 0.3f) * (V(v, 2) < 0.5f ? 1f : -1f);
        float width = 1.1f + V(v, 3) * 0.5f, depth = 0.75f + V(v, 4) * 0.35f;
        float step = 0.7f + V(v, 5) * 0.2f, layerH = 0.42f + V(v, 6) * 0.15f;
        float turn = V(v, 7) * Mathf.Tau, lean = (V(v, 8) - 0.5f) * 0.3f;
        var tops = new System.Collections.Generic.List<(Vector3 At, Vector3 Size, Basis Basis)>();
        float y = layerH * 0.6f, w = width, d = depth;
        var offset = Vector3.Zero;
        for (int i = 0; i < layers; i++)
        {
            var at = new Vector3(offset.X, y, offset.Z);
            var size = new Vector3(w, layerH, d);
            var basis = new Basis(Vector3.Up, turn + (H(i + v * 20, 42) - 0.5f) * 0.4f) * new Basis(Vector3.Forward, dip + (H(i + v * 20, 40) - 0.5f) * 0.1f);
            k.Paint(Colors.White, 0.82f + H(i + v * 20, 38) * 0.2f).Ellipsoid(at, size, 8, 3, basis, 0.7f);
            tops.Add((at, size, basis));
            y += layerH * (0.75f + H(i + v * 20, 43) * 0.2f);
            w *= step; d *= step * 1.05f;
            offset += new Vector3(Mathf.Cos(turn) * lean, 0, Mathf.Sin(turn) * lean) * layerH;
        }
        // One or two layers split by a crack: a block set apart from the rest.
        if (V(v, 9) > 0.35f)
        {
            var (at, size, basis) = tops[Mathf.Min(1, layers - 1)];
            var side = basis.Column0.Normalized();
            k.Paint(Colors.White, 0.9f).Ellipsoid(at + side * size.X * 0.9f - Vector3.Up * 0.05f, size * new Vector3(0.35f, 0.9f, 0.7f), 7, 3, basis, 0.7f);
        }
        // Fallen blocks around the foot.
        int fallen = 1 + (int)(V(v, 10) * 3f);
        for (int i = 0; i < fallen; i++)
        {
            float a = turn + 1.2f + i * 2.1f + H(i + v * 20, 44);
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            float s = 0.25f + H(i + v * 20, 45) * 0.25f;
            k.Paint(Colors.White, 0.85f + H(i + v * 20, 46) * 0.15f).Ellipsoid(dir * (width + s * 0.6f), new Vector3(s * 1.2f, s * 0.7f, s), 7, 3, new Basis(Vector3.Up, a + 0.7f) * new Basis(Vector3.Forward, (H(i + v * 20, 47) - 0.5f) * 0.6f), 0.6f);
        }
        // Moss on one to three ledges, never the same ones.
        var moss = new Color(0.62f, 0.78f, 0.45f);
        for (int i = 0; i < layers; i++)
        {
            if (H(i + v * 20, 48) > 0.55f && i != layers - 1) continue;
            var (at, size, basis) = tops[i];
            k.Paint(moss, 0.85f + H(i + v * 20, 49) * 0.2f).Ellipsoid(at + Vector3.Up * size.Y * 0.62f, new Vector3(size.X * 0.55f, 0.06f, size.Z * 0.5f), 7, 2, basis);
        }
        return Done(k);
    }

    // Flat riverbank stones, about 0.3 m across: six shapes (round, long, wedge,
    // split pair, notched, domed); scale them 0.7 to 1.3.
    public static Mesh RiverStone(int variant) => Variant("riverstone", variant, RiverStoneVariants, BuildRiverStone);

    static ArrayMesh BuildRiverStone(int v)
    {
        var k = Kit();
        var turn = new Basis(Vector3.Up, v * 1.3f);
        switch (v)
        {
            case 0: k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.03f, 0), new Vector3(0.15f, 0.05f, 0.14f), 7, 3, turn, 0.5f); break;
            case 1: k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.026f, 0), new Vector3(0.2f, 0.045f, 0.08f), 7, 3, turn, 0.5f); break;
            case 2:
                k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.035f, 0), new Vector3(0.16f, 0.06f, 0.12f), 6, 3, turn * new Basis(Vector3.Forward, 0.25f), 0.4f);
                break;
            case 3:
                k.Paint(Colors.White).Ellipsoid(new Vector3(-0.06f, 0.025f, 0), new Vector3(0.09f, 0.045f, 0.1f), 6, 3, turn, 0.5f);
                k.Paint(Colors.White, 0.93f).Ellipsoid(new Vector3(0.08f, 0.022f, 0.02f), new Vector3(0.08f, 0.04f, 0.09f), 6, 3, turn * new Basis(Vector3.Up, 0.6f), 0.5f);
                break;
            case 4:
                k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.028f, 0), new Vector3(0.17f, 0.05f, 0.12f), 8, 3, turn, 0.5f);
                k.Paint(Colors.White, 0.9f).Ellipsoid(new Vector3(0.1f, 0.05f, 0.04f), new Vector3(0.07f, 0.035f, 0.06f), 6, 3, turn, 0.5f);
                break;
            default: k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.04f, 0), new Vector3(0.12f, 0.075f, 0.12f), 7, 4, turn, 0.4f); break;
        }
        return Done(k);
    }
}
