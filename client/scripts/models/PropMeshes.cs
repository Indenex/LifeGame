using Godot;

namespace LifeGame;

// World props in the model-1 style, for the Environment lane (model-2). Each is a
// plain mesh with near-white vertex colors, made to drop into Terrain's
// MultiMeshes: the instance color tints it, the caller's material (Look.TreeBark,
// Look.RockSurface...) shades it, and it keeps the origin and size of the mesh it
// replaces (trunk: base at y = 0, 2.8 m tall; rock: about 1 m across).
public static class PropMeshes
{
    static Mesh? trunk, rock, fallenLog, stump;

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

    // A weathered boulder of three worn lumps, settled into the ground, with a
    // greener cap where moss grows on top.
    public static Mesh Rock => rock ??= BuildRock();

    static ArrayMesh BuildRock()
    {
        var k = Kit();
        k.Paint(Colors.White).Ellipsoid(new Vector3(0, 0.1f, 0), new Vector3(0.48f, 0.34f, 0.42f), 9, 5, new Basis(Vector3.Up, 0.3f), 0.4f);
        k.Paint(Colors.White, 0.95f).Ellipsoid(new Vector3(0.36f, 0.04f, 0.16f), new Vector3(0.28f, 0.22f, 0.25f), 7, 4, new Basis(Vector3.Up, 1.1f), 0.4f);
        k.Paint(Colors.White, 0.97f).Ellipsoid(new Vector3(-0.22f, 0.02f, -0.24f), new Vector3(0.22f, 0.16f, 0.2f), 7, 4, new Basis(Vector3.Up, 2.0f), 0.4f);
        k.Paint(new Color(0.62f, 0.78f, 0.45f)).Ellipsoid(new Vector3(-0.04f, 0.38f, -0.02f), new Vector3(0.3f, 0.07f, 0.26f), 8, 3, new Basis(Vector3.Up, 0.6f), 0.3f);
        return Done(k);
    }

    // A fallen trunk lying in the grass, about 3 m long, its broken end splintered,
    // moss along the top.
    public static Mesh FallenLog => fallenLog ??= BuildFallenLog();

    static ArrayMesh BuildFallenLog()
    {
        var k = Kit();
        k.Paint(Colors.White, 0.75f).Loft(new Vector3(-1.5f, 0.2f, 0), new Vector3(1.5f, 0.15f, 0.05f), new MeshKit.Ring[]
        {
            new(0f, 0.22f, 0.21f), new(0.5f, 0.2f, 0.19f, 0f, 0.01f), new(1f, 0.16f, 0.15f),
        }, 8, 0.05f);
        // Splinters at the broken end, pale.
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5f;
            var at = new Vector3(1.5f, 0.15f + Mathf.Sin(a) * 0.09f, 0.05f + Mathf.Cos(a) * 0.09f);
            k.Paint(Colors.White).Tube(new[] { at, at + new Vector3(0.12f + H(i, 1) * 0.1f, (H(i, 2) - 0.5f) * 0.04f, (H(i, 3) - 0.5f) * 0.04f) }, 0.03f, 4, 0.004f);
        }
        k.Paint(Colors.White, 0.97f).Ellipsoid(new Vector3(-1.505f, 0.2f, 0), new Vector3(0.006f, 0.19f, 0.18f), 8, 2);
        k.Paint(new Color(0.62f, 0.8f, 0.45f)).Loft(new Vector3(-1.2f, 0.37f, 0.0f), new Vector3(0.6f, 0.34f, 0.02f), new MeshKit.Ring[]
        {
            new(0f, 0.02f, 0.08f), new(0.5f, 0.03f, 0.12f), new(1f, 0.015f, 0.06f),
        }, 6, 0.3f);
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
        k.Paint(new Color(0.85f, 0.76f, 0.62f)).Ellipsoid(new Vector3(0, 0.405f, 0), new Vector3(0.09f, 0.006f, 0.09f), 7, 2);
        return Done(k);
    }
}
