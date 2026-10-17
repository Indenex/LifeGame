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

    // The trunk cut down (Environment's TreeFall): sawn through above the root flare, flat
    // at the base, the same height and branch stubs as Trunk otherwise.
    public static Mesh CutTrunk => cutTrunk ??= BuildCutTrunk();
    static Mesh? cutTrunk;

    static ArrayMesh BuildCutTrunk()
    {
        var k = Kit();
        k.Paint(Colors.White).Loft(new Vector3(0, 0.25f, 0), new Vector3(0, 2.8f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.2f, 0.19f), new(0.1f, 0.19f, 0.18f, 0.01f), new(0.47f, 0.15f, 0.145f, -0.02f, 0.025f), new(1f, 0.1f, 0.1f),
        }, 7, 0f);
        k.Paint(Colors.White, 0.92f).Tube(new[] { new Vector3(0.1f, 1.7f, 0), new Vector3(0.32f, 1.95f, 0.04f) }, 0.055f, 5, 0.04f);
        k.Paint(Colors.White, 0.92f).Tube(new[] { new Vector3(-0.06f, 2.2f, 0.06f), new Vector3(-0.24f, 2.4f, 0.2f) }, 0.045f, 5, 0.03f);
        return Done(k);
    }

    // A weathered boulder about 1 m across, settled into the ground with moss on
    // top. Four kinds, since boulders are everywhere: worn lumps, a tall split stone, a
    // low slab, and a round stone cracked in two; each in two forms (variants 0 to 3, and
    // 4 to 7 the same kinds again), so a patch of one kind needn't repeat one shape.
    public const int RockKinds = 4, RockVariants = 8, FallenLogVariants = 6, FallenGiantLogVariants = 6;

    public static Mesh Rock(int variant) => Variant("rock", variant, RockVariants, BuildRock);

    static ArrayMesh BuildRock(int v)
    {
        var k = Kit();
        var moss = new Color(0.62f, 0.78f, 0.45f);
        if (v >= RockKinds) return BuildRockB(k, v % RockKinds, moss);
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

    // The second form of each rock kind (model-28, D-166): variant v is kind v % RockKinds,
    // so a region of one kind of stone can mix both forms rather than repeat one shape.
    static ArrayMesh BuildRockB(MeshKit k, int kind, Color moss)
    {
        switch (kind)
        {
            case 0:
                // Worn lumps, lower and longer: a whaleback with two knuckles at one end.
                k.Paint(Colors.White).Ellipsoid(new Vector3(-0.08f, 0.07f, 0), new Vector3(0.58f, 0.26f, 0.36f), 9, 5, new Basis(Vector3.Up, -0.4f), 0.4f);
                k.Paint(Colors.White, 0.93f).Ellipsoid(new Vector3(0.42f, 0.12f, -0.1f), new Vector3(0.22f, 0.25f, 0.22f), 7, 4, new Basis(Vector3.Up, 0.8f), 0.4f);
                k.Paint(Colors.White, 0.97f).Ellipsoid(new Vector3(0.5f, 0.03f, 0.2f), new Vector3(0.16f, 0.13f, 0.15f), 6, 3, new Basis(Vector3.Up, 2.4f), 0.4f);
                k.Paint(moss, 0.95f).Ellipsoid(new Vector3(-0.25f, 0.29f, 0.05f), new Vector3(0.26f, 0.05f, 0.2f), 8, 3, new Basis(Vector3.Up, -0.4f), 0.3f);
                break;
            case 1:
                // Split, but squatter: one half fallen away and a chunk lying at its foot.
                k.Paint(Colors.White).Ellipsoid(new Vector3(-0.12f, 0.26f, 0), new Vector3(0.26f, 0.46f, 0.38f), 7, 5, new Basis(Vector3.Forward, -0.06f), 0.4f);
                k.Paint(Colors.White, 0.92f).Ellipsoid(new Vector3(0.26f, 0.16f, 0.04f), new Vector3(0.22f, 0.34f, 0.33f), 7, 5, new Basis(Vector3.Forward, 0.45f), 0.4f);
                k.Paint(Colors.White, 0.96f).Ellipsoid(new Vector3(0.48f, 0.05f, -0.22f), new Vector3(0.13f, 0.09f, 0.12f), 6, 3, new Basis(Vector3.Up, 1.1f), 0.4f);
                k.Paint(moss).Ellipsoid(new Vector3(-0.13f, 0.68f, 0.02f), new Vector3(0.16f, 0.05f, 0.22f), 6, 2, new Basis(Vector3.Forward, -0.06f));
                break;
            case 2:
                // Slabs stepped like a little ledge, moss along the step.
                k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.06f, 0), new Vector3(0.62f, 0.16f, 0.46f), 8, 3, new Basis(Vector3.Up, -0.3f), 0.5f);
                k.Paint(Colors.White, 0.94f).Ellipsoid(new Vector3(-0.16f, 0.2f, -0.08f), new Vector3(0.38f, 0.14f, 0.3f), 8, 3, new Basis(Vector3.Up, -0.15f) * new Basis(Vector3.Right, 0.05f), 0.5f);
                k.Paint(moss, 0.95f).Ellipsoid(new Vector3(0.2f, 0.17f, 0.1f), new Vector3(0.16f, 0.03f, 0.3f), 7, 2, new Basis(Vector3.Up, -0.3f));
                k.Paint(moss).Ellipsoid(new Vector3(-0.2f, 0.33f, -0.08f), new Vector3(0.22f, 0.03f, 0.17f), 7, 2, new Basis(Vector3.Up, -0.15f));
                break;
            default:
                // A round stone cracked three ways, the pieces settled a little apart.
                for (int i = 0; i < 3; i++)
                {
                    float a = i * Mathf.Tau / 3f + 0.4f;
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    k.Paint(Colors.White, 0.92f + i * 0.04f).Ellipsoid(dir * 0.17f + new Vector3(0, 0.16f - i * 0.02f, 0), new Vector3(0.24f, 0.34f - i * 0.03f, 0.24f), 7, 5, MeshKit.Aim(dir) * new Basis(Vector3.Right, -0.12f), 0.5f);
                }
                k.Paint(moss, 0.9f).Ellipsoid(new Vector3(0, 0.28f, 0), new Vector3(0.1f, 0.12f, 0.1f), 6, 3);
                k.Paint(moss).Ellipsoid(new Vector3(0.12f, 0.46f, -0.05f), new Vector3(0.16f, 0.04f, 0.14f), 6, 2);
                break;
        }
        return Done(k);
    }

    // A fallen trunk lying in the grass, moss along the top: straight with a
    // splintered end, or broken in two with a forked crown and a stub of root.
    public static Mesh FallenLog(int variant) => Variant("fallenlog", variant, FallenLogVariants, BuildFallenLog);

    static ArrayMesh BuildFallenLog(int v)
    {
        if (v >= 2) return BuildFallenLogB(v);
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

    // More fallen trunks (model-29, D-166) so the forest floor rarely shows the same log
    // twice: 2 an old short butt gone hollow and thick with moss; 3 a long thin pole
    // bent where it fell, with snapped branch stubs; 4 a trunk with its biggest limb
    // lying out sideways; 5 two smaller logs fallen across each other.
    // Same frame as the first two: lying along x, about 3 m long, bottom on the ground.
    static ArrayMesh BuildFallenLogB(int v)
    {
        var k = Kit();
        var moss = new Color(0.62f, 0.8f, 0.45f);
        switch (v)
        {
            case 2:
            {
                // Short and fat, half sunk, the open end showing a dark rotten hollow.
                var a = new Vector3(-1.0f, 0.24f, 0); var b = new Vector3(1.0f, 0.22f, 0.04f);
                k.Paint(Colors.White, 0.7f).Loft(a, b, new MeshKit.Ring[]
                {
                    new(0f, 0.3f, 0.29f), new(0.5f, 0.29f, 0.27f, 0f, -0.01f), new(1f, 0.27f, 0.26f),
                }, 9, 0.08f);
                k.Paint(Colors.White, 0.32f).Ellipsoid(a + new Vector3(-0.005f, 0, 0), new Vector3(0.01f, 0.2f, 0.19f), 9, 2);
                k.Paint(Colors.White, 0.95f).Ellipsoid(b + new Vector3(0.005f, 0, 0), new Vector3(0.008f, 0.25f, 0.24f), 9, 2);
                // A thick blanket of moss over the top, spilling down one side.
                k.Paint(moss).Loft(new Vector3(-0.95f, 0.47f, 0.02f), new Vector3(0.9f, 0.45f, 0.03f), new MeshKit.Ring[]
                {
                    new(0f, 0.03f, 0.14f), new(0.4f, 0.05f, 0.2f), new(1f, 0.03f, 0.12f),
                }, 7, 0.3f);
                k.Paint(moss, 0.92f).Ellipsoid(new Vector3(-0.3f, 0.3f, 0.24f), new Vector3(0.35f, 0.12f, 0.06f), 7, 3);
                k.Paint(moss, 0.95f).Ellipsoid(new Vector3(0.5f, 0.05f, -0.2f), new Vector3(0.25f, 0.06f, 0.12f), 6, 3);
                break;
            }
            case 3:
            {
                // A slim pole, bent where it struck the ground, with snapped-off branches.
                var path = new[] { new Vector3(-1.7f, 0.12f, 0), new Vector3(-0.5f, 0.11f, 0.12f), new Vector3(0.6f, 0.1f, 0.06f), new Vector3(1.7f, 0.09f, -0.2f) };
                k.Paint(Colors.White, 0.78f).Tube(path, 0.12f, 7, 0.075f);
                Splinters(k, path[3], (path[3] - path[2]).Normalized(), 0.06f);
                for (int i = 0; i < 4; i++)
                {
                    float t = -1.2f + i * 0.85f + H(i, 60) * 0.3f;
                    float side = i % 2 == 0 ? 1f : -1f;
                    var root = new Vector3(t, 0.16f, 0.06f * side);
                    var dir = new Vector3(0.35f, 0.55f + H(i, 61) * 0.4f, side * (0.5f + H(i, 62) * 0.3f)).Normalized();
                    k.Paint(Colors.White, 0.74f).Tube(new[] { root, root + dir * (0.18f + H(i, 63) * 0.2f) }, 0.035f, 5, 0.022f);
                }
                k.Paint(moss).Loft(new Vector3(-1.5f, 0.23f, 0.03f), new Vector3(-0.3f, 0.22f, 0.11f), new MeshKit.Ring[]
                {
                    new(0f, 0.012f, 0.04f), new(0.5f, 0.02f, 0.07f), new(1f, 0.01f, 0.03f),
                }, 6, 0.3f);
                break;
            }
            case 4:
            {
                // A trunk that came down with its biggest limb still on: the limb lies
                // out sideways through the grass, the crown snapped off short.
                var a = new Vector3(-1.5f, 0.2f, -0.3f); var b = new Vector3(0.9f, 0.16f, -0.15f);
                k.Paint(Colors.White, 0.74f).Loft(a, b, new MeshKit.Ring[]
                {
                    new(0f, 0.21f, 0.2f), new(0.6f, 0.18f, 0.17f, 0f, 0.015f), new(1f, 0.15f, 0.14f),
                }, 8, 0.05f);
                k.Paint(Colors.White, 0.97f).Ellipsoid(a + new Vector3(-0.005f, 0, 0), new Vector3(0.006f, 0.19f, 0.18f), 8, 2);
                Splinters(k, b, (b - a).Normalized(), 0.09f);
                var fork = new Vector3(0.2f, 0.17f, -0.2f);
                k.Paint(Colors.White, 0.71f).Tube(new[] { fork, fork + new Vector3(0.35f, -0.04f, 0.45f), fork + new Vector3(0.6f, -0.08f, 1.0f), fork + new Vector3(1.0f, -0.1f, 1.35f) }, 0.11f, 7, 0.05f);
                k.Paint(Colors.White, 0.7f).Tube(new[] { fork + new Vector3(0.6f, -0.08f, 1.0f), fork + new Vector3(0.45f, -0.02f, 1.45f) }, 0.05f, 5, 0.025f);
                // A short side shoot pointing up off the trunk, snapped at the tip.
                k.Paint(Colors.White, 0.72f).Tube(new[] { new Vector3(-0.6f, 0.3f, -0.35f), new Vector3(-0.5f, 0.62f, -0.55f) }, 0.05f, 5, 0.03f);
                k.Paint(moss).Loft(new Vector3(-1.3f, 0.39f, -0.28f), new Vector3(0.1f, 0.35f, -0.2f), new MeshKit.Ring[]
                {
                    new(0f, 0.02f, 0.08f), new(0.5f, 0.03f, 0.12f), new(1f, 0.015f, 0.06f),
                }, 6, 0.3f);
                break;
            }
            default:
            {
                // Two smaller trunks come down across each other.
                k.Paint(Colors.White, 0.76f).Loft(new Vector3(-1.4f, 0.15f, -0.1f), new Vector3(1.3f, 0.13f, 0.15f), new MeshKit.Ring[]
                {
                    new(0f, 0.16f, 0.15f), new(1f, 0.12f, 0.115f),
                }, 8, 0.05f);
                Splinters(k, new Vector3(1.3f, 0.13f, 0.15f), Vector3.Right, 0.07f);
                k.Paint(Colors.White, 0.96f).Ellipsoid(new Vector3(-1.405f, 0.15f, -0.1f), new Vector3(0.006f, 0.14f, 0.13f), 8, 2);
                // The second lies over the first, its far end resting on the ground.
                var a = new Vector3(-0.6f, 0.13f, -1.0f); var b = new Vector3(0.5f, 0.36f, 0.4f); var c = new Vector3(0.95f, 0.12f, 1.0f);
                k.Paint(Colors.White, 0.7f).Tube(new[] { a, b, c }, 0.12f, 7, 0.09f);
                k.Paint(Colors.White, 0.94f).Ellipsoid(a + (a - b).Normalized() * 0.005f, new Vector3(0.11f, 0.11f, 0.006f), 8, 2,
                    MeshKit.Aim((a - b).Normalized()));
                k.Paint(moss).Loft(new Vector3(-1.2f, 0.29f, -0.08f), new Vector3(-0.2f, 0.27f, 0.0f), new MeshKit.Ring[]
                {
                    new(0f, 0.015f, 0.06f), new(0.5f, 0.025f, 0.09f), new(1f, 0.012f, 0.05f),
                }, 6, 0.3f);
                break;
            }
        }
        return Done(k);
    }

    // Hazelnuts on the bush (model-21, for Survival's autumn hazels, D-323): two or three
    // tan nuts in their frilled green husks on a short stem, in the berry clusters' frame
    // (BerryBush): local -Z points out of the bush, the stem runs back into it along +Z.
    // Built 1.5x life size (about 12 cm across) so it reads from 5 to 10 m, like the
    // berries. Colours are in the vertices; draw it with HazelMaterial.
    public static Mesh HazelCluster(int variant) => Variant("hazelcluster", variant, HazelClusterVariants, BuildHazelCluster);
    public const int HazelClusterVariants = 3;
    public static Material HazelMaterial => hazelMaterial ??= Painted.Of(Look.Solid(Colors.White));
    static Material? hazelMaterial;

    static ArrayMesh BuildHazelCluster(int v)
    {
        var k = new MeshKit(false) { Skinned = false };
        const float life = 1.5f;
        var shell = new Color(0.56f, 0.42f, 0.27f);
        var husk = new Color(0.3f, 0.4f, 0.15f);
        k.Paint(Look.Bark.Lightened(0.15f)).Tube(new[] { new Vector3(0, 0, 0.05f), new Vector3(0, 0.005f, 0.01f), new Vector3(0, 0, -0.01f) }, 0.004f, 4, 0.003f);
        int count = 2 + v % 2;
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.Tau / count + v;
            var dir = new Vector3(Mathf.Cos(a) * 1.4f, Mathf.Sin(a) * 1.4f, -1f).Normalized();
            var at = dir * 0.028f + new Vector3(0, 0, -0.005f);
            var tilt = MeshKit.Aim(dir) * new Basis(Vector3.Right, Mathf.Pi / 2f);
            var up = tilt * Vector3.Up;
            k.Paint(shell, 0.9f + H(v * 3 + i, 40) * 0.15f).Ellipsoid(at + up * 0.006f, new Vector3(0.016f, 0.018f, 0.016f), 8, 5, tilt);
            k.Paint(shell.Lightened(0.18f)).Ellipsoid(at + up * 0.014f, new Vector3(0.0135f, 0.01f, 0.0135f), 8, 4, tilt);
            k.Paint(shell.Darkened(0.2f)).Ellipsoid(at + up * 0.024f, new Vector3(0.004f, 0.005f, 0.004f), 5, 3, tilt);
            // The husk: a leafy cup round the lower half, a frill of short teeth at its lip.
            k.Paint(husk).Loft(at - up * 0.017f, at + up * 0.004f, new MeshKit.Ring[]
            {
                new(0f, 0.006f, 0.006f), new(0.45f, 0.018f, 0.018f), new(1f, 0.021f, 0.021f),
            }, 9, 0.3f);
            for (int j = 0; j < 7; j++)
            {
                float b = j * Mathf.Tau / 7f + v;
                var side = tilt * new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                var lip = at + up * 0.003f + side * 0.02f;
                k.Paint(husk, 0.9f + H(v * 7 + j, 43) * 0.2f).Blade(lip, lip + up * (0.008f + H(j, 41) * 0.005f) + side * 0.006f, side, 0.008f, 0.002f);
            }
        }
        var arrays = k.CommitToArrays();
        var verts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        for (int n = 0; n < verts.Length; n++) verts[n] *= life;
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    // A fallen giant (model-19, for Environment's env-33 landmark, drawn 3 to 4 times
    // bigger): FallenLog's trunk, but torn up whole and long fallen. The butt end carries
    // a root plate, a ragged, lopsided disc of earth and roots (thin root tubes read as
    // spikes at that scale), and the bark is weathered grey-brown with moss along the top.
    // Even variants are straight with a splintered crown end, odd ones broken in two with
    // a forked crown lying apart; every variant has its own plate (D-166). About 3 m long
    // at scale 1, like FallenLog, so the same placement fits.
    public static Mesh FallenGiantLog(int variant) => Variant("fallengiantlog", variant, FallenGiantLogVariants, BuildFallenGiantLog);

    // Unlike FallenLog the giant carries its own colours (weathered grey-brown wood, the
    // ground's own earth, moss, grey stones), so it is drawn on Painted.Bark with a white
    // instance tint, GiantTint, a little darker or lighter per instance if wanted.
    public static readonly Color GiantTint = Colors.White;
    static readonly Color Weathered = new(0.42f, 0.36f, 0.31f);
    static readonly Color Soil = Look.Earth.Darkened(0.1f);
    static readonly Color GiantMoss = Look.Moss.Lightened(0.2f);
    static readonly Color Pebble = new(0.55f, 0.53f, 0.5f);

    static ArrayMesh BuildFallenGiantLog(int v)
    {
        var k = Kit();
        var butt = new Vector3(-1.5f, 0.3f, 0);
        RootPlate(k, butt, v);
        if (v % 2 == 0)
        {
            k.Paint(Weathered, 0.8f).Loft(butt, new Vector3(1.5f, 0.17f, 0.06f), new MeshKit.Ring[]
            {
                new(0f, 0.3f, 0.29f), new(0.12f, 0.23f, 0.22f), new(0.6f, 0.2f, 0.19f, 0f, 0.01f), new(1f, 0.16f, 0.15f),
            }, 10, 0.05f);
            Splinters(k, new Vector3(1.5f, 0.17f, 0.06f), Vector3.Right, 0.09f);
            GiantMossTop(k, new Vector3(-1.2f, 0.5f, 0.0f), new Vector3(0.8f, 0.36f, 0.03f), v);
        }
        else
        {
            k.Paint(Weathered, 0.8f).Loft(butt, new Vector3(-0.1f, 0.21f, 0.05f), new MeshKit.Ring[]
            {
                new(0f, 0.3f, 0.29f), new(0.15f, 0.22f, 0.21f), new(1f, 0.19f, 0.18f),
            }, 10, 0.05f);
            Splinters(k, new Vector3(-0.1f, 0.21f, 0.05f), Vector3.Right, 0.11f);
            var crown = new Vector3(0.15f, 0.17f, 0.2f);
            k.Paint(Weathered, 0.8f).Loft(crown, crown + new Vector3(1.1f, -0.02f, 0.25f), new MeshKit.Ring[]
            {
                new(0f, 0.17f, 0.16f), new(1f, 0.13f, 0.12f),
            }, 8, 0.05f);
            Splinters(k, crown, Vector3.Left, 0.1f);
            var fork = crown + new Vector3(1.1f, -0.02f, 0.25f);
            k.Paint(Weathered, 0.76f).Tube(new[] { fork, fork + new Vector3(0.5f, -0.04f, 0.35f), fork + new Vector3(0.9f, -0.08f, 0.45f) }, 0.1f, 6, 0.04f);
            k.Paint(Weathered, 0.76f).Tube(new[] { fork, fork + new Vector3(0.55f, 0.0f, -0.12f), fork + new Vector3(0.95f, -0.06f, -0.3f) }, 0.09f, 6, 0.035f);
            GiantMossTop(k, new Vector3(-1.15f, 0.5f, 0.0f), new Vector3(-0.3f, 0.39f, 0.04f), v);
            GiantMossTop(k, crown + new Vector3(0.1f, 0.15f, 0), crown + new Vector3(0.9f, 0.12f, 0.2f), v + 7);
        }
        return Done(k);
    }

    // A long soft band of moss along the top of a lying trunk, with a few cushions.
    static void GiantMossTop(MeshKit k, Vector3 from, Vector3 to, int seed)
    {
        k.Paint(GiantMoss).Loft(from, to, new MeshKit.Ring[]
        {
            new(0f, 0.02f, 0.1f), new(0.5f, 0.035f, 0.15f), new(1f, 0.015f, 0.06f),
        }, 6, 0.3f);
        for (int i = 0; i < 3; i++)
        {
            var at = from.Lerp(to, 0.2f + 0.3f * i + H(seed, 80 + i) * 0.1f) + new Vector3(0, 0.01f, (H(seed, 83 + i) - 0.5f) * 0.12f);
            k.Paint(GiantMoss, 0.9f + H(i, 86) * 0.15f).Ellipsoid(at, new Vector3(0.09f, 0.035f, 0.07f), 7, 3, null, 0.5f);
        }
    }

    // A torn-up root plate standing on edge at a trunk's butt, facing back along -X: a
    // wide, flat, lopsided disc of packed earth, taller on one side, its rim broken and ragged; a few
    // thick roots wandering out across it, uneven and off-centre, with earth clods and
    // stones caught between them; thin root ends dangling off the rim; moss along its top;
    // and heaped earth where its foot leans in the ground. Its roll, size and roots come
    // from the variant, so no two fallen giants match.
    static void RootPlate(MeshKit k, Vector3 butt, int v)
    {
        float roll = (H(v, 60) - 0.5f) * 1.6f;     // which way the tall side leans
        float size = 0.85f + H(v, 61) * 0.3f;
        // A flat disc on edge, its face 0.28 deep along the trunk and about 2.5 trunk widths
        // across, the trunk coming out of its middle.
        const float depth = 0.28f;
        var center = butt + new Vector3(-0.1f, 0.12f, 0);
        var tall = new Vector3(0, Mathf.Cos(roll), Mathf.Sin(roll));
        var across = new Vector3(0, -Mathf.Sin(roll), Mathf.Cos(roll));
        Vector3 Rim(float a, float r) => center + (tall * Mathf.Cos(a) + across * Mathf.Sin(a)) * r;

        // The plate: a flat-faced disc of earth and a second, smaller one pushed up to the
        // tall side, each with flat caps on both faces.
        void Disc(Vector3 at, float ry, float rz, float shade)
        {
            var front = at + new Vector3(-depth / 2f, 0, 0);
            var back = at + new Vector3(depth / 2f, 0, 0);
            var (ringY, ringZ) = (ry * size, rz * size);
            k.Paint(Soil, shade).Loft(front, back, new MeshKit.Ring[]
            {
                new(0f, ringY * 0.88f, ringZ * 0.88f), new(0.2f, ringY, ringZ), new(0.8f, ringY, ringZ), new(1f, ringY * 0.9f, ringZ * 0.9f),
            }, 12, 0.15f);
            k.Paint(Soil, shade * 0.95f).Ellipsoid(front, new Vector3(0.02f, ringY * 0.9f, ringZ * 0.9f), 12, 2);
            k.Paint(Soil, shade * 0.9f).Ellipsoid(back, new Vector3(0.02f, ringY * 0.92f, ringZ * 0.92f), 12, 2);
        }
        Disc(center, 0.66f, 0.74f, 0.95f);
        Disc(center + tall * 0.3f * size + across * 0.1f + new Vector3(0.02f, 0, 0), 0.42f, 0.4f, 1.02f);

        // Lumps of earth and root round the rim, some missing where it broke away.
        int lobes = 13 + v % 4;
        for (int i = 0; i < lobes; i++)
        {
            if (H(v * 13 + i, 62) < 0.22f) continue;
            float a = i * Mathf.Tau / lobes + (H(v * 13 + i, 63) - 0.5f) * 0.5f;
            float r = (0.6f + H(v * 13 + i, 64) * 0.1f + (Mathf.Cos(a) > 0 ? 0.1f * Mathf.Cos(a) : 0f)) * size;
            var at = Rim(a, r);
            var dir = (at - center).Normalized();
            bool earthy = H(v * 13 + i, 65) < 0.55f;
            // Flat, in the plane of the plate, so the rim reads ragged rather than beaded.
            k.Paint(earthy ? Soil : Weathered, 0.8f + H(i, 66) * 0.15f).Ellipsoid(at, new Vector3(0.11f, 0.06f + H(i, 67) * 0.06f, 0.1f + H(i, 68) * 0.08f) * size, 6, 3, new Basis(Vector3.Right, a));
            // Thin root ends dangling off the rim, sagging under their own weight.
            if (H(v * 13 + i, 69) < 0.6f)
            {
                var tip = at + dir * (0.18f + H(i, 70) * 0.14f) * size + new Vector3(-0.04f, -0.12f - H(i, 71) * 0.1f, 0);
                k.Paint(Weathered, 0.7f).Tube(new[] { at, at + dir * 0.1f * size + new Vector3(-0.02f, -0.02f, 0), tip }, 0.022f, 4, 0.004f);
            }
        }

        // A few thick roots wandering across the face from off-centre, uneven in length.
        int roots = 3 + v % 3;
        var heart = center + new Vector3(-depth / 2f - 0.01f, 0, 0) + tall * 0.05f;
        for (int i = 0; i < roots; i++)
        {
            float a = H(v * 7 + i, 72) * Mathf.Tau;
            float len = (0.4f + H(v * 7 + i, 73) * 0.32f) * size;
            var dir = tall * Mathf.Cos(a) + across * Mathf.Sin(a);
            var bend = heart + dir * len * 0.5f + across * (H(i, 74) - 0.5f) * 0.12f;
            k.Paint(Weathered, 0.72f).Tube(new[] { heart + dir * 0.04f, bend, heart + dir * len + new Vector3(0.02f, 0, 0) }, 0.055f + H(i, 75) * 0.025f, 5, 0.02f);
        }

        // Dark earth packed between the roots, and stones caught in it.
        for (int i = 0; i < 14; i++)
        {
            float a = H(v * 11 + i, 76) * Mathf.Tau;
            float r = (0.1f + H(v * 11 + i, 77) * 0.5f) * size;
            var at = Rim(a, r) + new Vector3(-depth / 2f - 0.01f - H(i, 78) * 0.02f, 0, 0);
            k.Paint(Soil, 0.85f + H(i, 79) * 0.15f).Ellipsoid(at, new Vector3(0.04f, 0.04f + H(i, 80) * 0.04f, 0.06f + H(i, 81) * 0.05f) * size, 6, 3, new Basis(Vector3.Right, a));
        }
        for (int i = 0; i < 3 + v % 3; i++)
        {
            float a = H(v * 5 + i, 82) * Mathf.Tau;
            var at = Rim(a, (0.15f + H(v * 5 + i, 83) * 0.45f) * size) + new Vector3(-depth / 2f - 0.03f, 0, 0);
            k.Paint(Pebble, 0.85f + H(i, 84) * 0.25f).Ellipsoid(at, new Vector3(0.035f, 0.03f, 0.04f) * (0.8f + H(i, 85) * 0.8f), 6, 3);
        }

        // Moss and grass still on the plate's top edge, where the forest floor was.
        for (int i = 0; i < 3; i++)
            k.Paint(GiantMoss, 0.9f + H(v + i, 86) * 0.15f).Ellipsoid(Rim((i - 1) * 0.3f, 0.74f * size), new Vector3(0.16f, 0.05f, 0.16f), 7, 3, new Basis(Vector3.Right, roll));
        // The heaped earth where the plate's foot leans in the ground.
        k.Paint(Soil, 0.9f).Ellipsoid(new Vector3(center.X - 0.05f, 0.0f, across.Z * 0.05f), new Vector3(0.32f, 0.07f, 0.7f) * size, 9, 4, null, 0.5f);
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

    public const int BirchVariants = 6, HeatherVariants = 6, GorseVariants = 6, OutcropVariants = 6, RiverStoneVariants = 6;
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
        // Some mounds are long rather than round; from variant 3 on (model-30, D-166) the
        // mound has grown out into two lobes, the second smaller, off to one side.
        float stretch = 0.8f + V(v, 5) * 0.5f;
        float lobeA = V(v, 7) * Mathf.Tau;
        var lobe = v >= 3 ? new Vector3(Mathf.Cos(lobeA), 0, Mathf.Sin(lobeA)) * spread * (1.2f + V(v, 8) * 0.4f) : Vector3.Zero;
        if (v >= 3) clumps += 6;
        for (int i = 0; i < clumps; i++)
        {
            bool second = v >= 3 && i % 3 == 2;
            float a = i * 2.4f + V(v, 6) * 3f, r = Mathf.Sqrt(i / (float)clumps) * spread * (second ? 0.75f : 1f);
            var at = new Vector3(Mathf.Cos(a) * r * stretch, (0.1f + (spread - r) * 0.6f) * tall * (second ? 0.8f : 1f), Mathf.Sin(a) * r / stretch) + (second ? lobe : Vector3.Zero);
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
        // Round, tall or sprawling; variants 3 to 5 (model-30) repeat the three with their
        // proportions pushed about and lean to one side, so no two read the same.
        var shape = (v % 3) switch { 0 => new Vector3(1f, 1f, 1f), 1 => new Vector3(0.8f, 1.3f, 0.8f), _ => new Vector3(1.35f, 0.75f, 1.1f) };
        if (v >= 3) shape *= new Vector3(0.85f + V(v, 5) * 0.35f, 0.85f + V(v, 6) * 0.3f, 0.85f + V(v, 7) * 0.35f);
        var lean = v >= 3 ? new Vector3(V(v, 8) - 0.5f, 0, V(v, 9) - 0.5f) * 0.35f : Vector3.Zero;
        int lumps = 7 + (int)(V(v, 1) * 4f);
        for (int i = 0; i < lumps; i++)
        {
            float a = i * 2.4f + V(v, 2) * 3f, r = i == 0 ? 0f : 0.25f + H(i + v * 30, 26) * 0.12f;
            var at = new Vector3(Mathf.Cos(a) * r * shape.X, (0.35f + H(i + v * 30, 27) * 0.35f - r * 0.4f) * shape.Y, Mathf.Sin(a) * r * shape.Z);
            at += lean * at.Y;
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
            root += lean * root.Y;
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
