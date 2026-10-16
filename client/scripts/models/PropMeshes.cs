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
        k.Paint(new Color(0.85f, 0.76f, 0.62f)).Ellipsoid(new Vector3(0, 0.405f, 0), new Vector3(0.09f, 0.006f, 0.09f), 7, 2);
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
    // purple-tipped. Variants differ in clump count, spread, height and how much is
    // in flower. Colors in the vertices; tint near white.
    public static Mesh Heather(int variant) => Variant("heather", variant, HeatherVariants, BuildHeather);

    static ArrayMesh BuildHeather(int v)
    {
        var k = Kit();
        var green = new Color(0.36f, 0.42f, 0.26f);
        var purple = new Color(0.62f, 0.42f, 0.62f);
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
