using Godot;

namespace LifeGame;

// World props in the model-1 style, for the Environment lane (model-2). Each is a
// plain mesh with near-white vertex colors, made to drop into Terrain's
// MultiMeshes: the instance color tints it, the caller's material (Look.TreeBark,
// Look.RockSurface...) shades it, and it keeps the origin and size of the mesh it
// replaces (trunk: base at y = 0, 2.8 m tall; rock: about 1 m across).
public static class PropMeshes
{
    static Mesh? trunk, rock, fallenLog, stump, birch, heather, gorse, cattails, outcrop;
    static readonly Mesh?[] riverStones = new Mesh?[3];

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

    // --- env-4 props (Environment's "corners of the forest", D-152).

    // A birch or alder by the water: slim and pale, with dark marks across the
    // bark. Same base and top as Trunk, so the leafy canopy fits it too.
    public static Mesh Birch => birch ??= BuildBirch();

    static ArrayMesh BuildBirch()
    {
        var k = Kit();
        k.Paint(Colors.White).Loft(new Vector3(0, -0.1f, 0), new Vector3(0, 2.8f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.17f, 0.16f), new(0.05f, 0.13f, 0.125f), new(0.5f, 0.1f, 0.095f, 0.015f, -0.02f), new(1f, 0.065f, 0.065f),
        }, 7, 0.2f);
        // Dark lenticels and cracks: thin dark bands part way round the trunk.
        for (int i = 0; i < 26; i++)
        {
            float y = 0.3f + i * 0.095f + H(i, 20) * 0.05f;
            // Follow the loft above: its center bends out to (-0.02, 0.015) at mid
            // height and back, and its radius narrows from 0.13 to 0.065.
            float t = (y + 0.1f) / 2.9f;
            float bend = t < 0.5f ? Mathf.Max(0f, (t - 0.05f) / 0.45f) : (1f - t) / 0.5f;
            float r = t < 0.5f ? Mathf.Lerp(0.13f, 0.1f, Mathf.Max(0f, (t - 0.05f) / 0.45f)) : Mathf.Lerp(0.1f, 0.065f, (t - 0.5f) / 0.5f);
            float a = H(i, 21) * Mathf.Tau;
            // A short dark dash curving round the bark.
            var axisAt = new Vector3(-0.02f * bend, y, 0.015f * bend);
            float arc = (0.12f + H(i, 22) * 0.18f) * (H(i, 39) > 0.8f ? 2f : 1f);
            k.Paint(Colors.White, 0.18f).Tube(MeshKit.Circle(axisAt, Vector3.Up, r * 0.93f, 6, arc, 0f, a), 0.012f + H(i, 41) * 0.006f, 4, 0.006f);
        }
        // Darker, rougher bark at the foot.
        k.Paint(Colors.White, 0.45f).Loft(new Vector3(0, -0.1f, 0), new Vector3(0, 0.35f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.18f, 0.17f), new(1f, 0.125f, 0.12f),
        }, 7, 0.1f);
        return Done(k);
    }

    // Heather on the hilltop heath: a low mound of fine clumps, about 0.6 m across,
    // purple-tipped. Its colors are in the vertices, so tint it near white.
    public static Mesh Heather => heather ??= BuildHeather();

    static ArrayMesh BuildHeather()
    {
        var k = Kit();
        var green = new Color(0.36f, 0.42f, 0.26f);
        var purple = new Color(0.62f, 0.42f, 0.62f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * 2.4f, r = Mathf.Sqrt(i / 16f) * 0.24f;
            var at = new Vector3(Mathf.Cos(a) * r, 0.1f + (0.24f - r) * 0.6f, Mathf.Sin(a) * r);
            float size = 0.09f + H(i, 23) * 0.05f;
            k.Paint(green, 0.85f + H(i, 24) * 0.25f).Ellipsoid(at, new Vector3(size, size * 0.8f, size), 6, 3);
            k.Paint(purple, 0.85f + H(i, 25) * 0.25f).Ellipsoid(at + Vector3.Up * size * 0.55f, new Vector3(size * 0.7f, size * 0.35f, size * 0.7f), 5, 2);
        }
        return Done(k);
    }

    // Gorse: a spiky bush about 1 m high, dark green, flecked with yellow flowers.
    // Colors in the vertices; tint it near white.
    public static Mesh Gorse => gorse ??= BuildGorse();

    static ArrayMesh BuildGorse()
    {
        var k = Kit();
        var green = new Color(0.22f, 0.32f, 0.18f);
        var yellow = new Color(0.95f, 0.80f, 0.20f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 2.4f, r = i == 0 ? 0f : 0.25f + H(i, 26) * 0.12f;
            var at = new Vector3(Mathf.Cos(a) * r, 0.35f + H(i, 27) * 0.35f - r * 0.4f, Mathf.Sin(a) * r);
            k.Paint(green, 0.8f + H(i, 28) * 0.3f).Ellipsoid(at, Vector3.One * (0.25f + H(i, 29) * 0.08f), 6, 4);
        }
        // Spines poking out all over, and yellow flowers among them.
        for (int i = 0; i < 70; i++)
        {
            float y = 1f - (i + 0.5f) / 70f * 1.6f;
            float rr = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y)), a = i * 2.39996f;
            var dir = new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr).Normalized();
            var root = new Vector3(0, 0.5f, 0) + dir * new Vector3(0.42f, 0.38f, 0.42f);
            if (root.Y < 0.08f) continue;
            if (i % 3 == 0)
                k.Paint(yellow, 0.9f + H(i, 30) * 0.15f).Ellipsoid(root + dir * 0.03f, Vector3.One * 0.035f, 5, 3);
            else
                k.Paint(green, 0.7f).Tube(new[] { root, root + dir * (0.1f + H(i, 31) * 0.06f) }, 0.012f, 3, 0.001f, false);
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

    // A hilltop outcrop, about 2.5 m across and 2 m high: weathered slabs stacked
    // and tilted like a broken layer of rock, moss in the cracks. A landmark.
    public static Mesh Outcrop => outcrop ??= BuildOutcrop();

    static ArrayMesh BuildOutcrop()
    {
        var k = Kit();
        // Layers of the same rock, all dipping the same way, run together into one
        // mass, each a little smaller and set back from the one below.
        const float dip = 0.2f;
        var slabs = new (Vector3 At, Vector3 Size, float Turn)[]
        {
            (new Vector3(0.05f, 0.3f, 0), new Vector3(1.35f, 0.5f, 0.95f), 0.2f),
            (new Vector3(-0.12f, 0.75f, 0.02f), new Vector3(1.15f, 0.45f, 0.8f), 0.28f),
            (new Vector3(0.0f, 1.15f, -0.08f), new Vector3(0.85f, 0.4f, 0.62f), 0.12f),
            (new Vector3(-0.2f, 1.5f, -0.05f), new Vector3(0.5f, 0.32f, 0.42f), 0.4f),
            (new Vector3(0.9f, 0.2f, 0.45f), new Vector3(0.55f, 0.35f, 0.45f), 0.9f),
            (new Vector3(-0.95f, 0.18f, -0.4f), new Vector3(0.5f, 0.3f, 0.4f), 1.7f),
        };
        for (int i = 0; i < slabs.Length; i++)
        {
            var (at, size, turn) = slabs[i];
            var basis = new Basis(Vector3.Up, turn) * new Basis(Vector3.Forward, dip + (H(i, 40) - 0.5f) * 0.1f);
            k.Paint(Colors.White, 0.85f + H(i, 38) * 0.18f).Ellipsoid(at, size, 8, 3, basis, 0.7f);
        }
        k.Paint(new Color(0.62f, 0.78f, 0.45f)).Ellipsoid(new Vector3(-0.2f, 1.62f, -0.05f), new Vector3(0.38f, 0.07f, 0.3f), 7, 2, new Basis(Vector3.Up, 0.4f) * new Basis(Vector3.Forward, dip));
        k.Paint(new Color(0.62f, 0.78f, 0.45f), 0.9f).Ellipsoid(new Vector3(-0.35f, 1.05f, 0.25f), new Vector3(0.4f, 0.06f, 0.25f), 7, 2, new Basis(Vector3.Up, 0.28f) * new Basis(Vector3.Forward, dip));
        return Done(k);
    }

    // Flat riverbank stones, about 0.3 m across; three shapes, scale them 0.7 to 1.3.
    public static Mesh RiverStone(int variant) => riverStones[variant % 3] ??= BuildRiverStone(variant % 3);

    static ArrayMesh BuildRiverStone(int v)
    {
        var k = Kit();
        var size = v switch { 0 => new Vector3(0.16f, 0.05f, 0.12f), 1 => new Vector3(0.13f, 0.06f, 0.13f), _ => new Vector3(0.18f, 0.045f, 0.09f) };
        k.Paint(Colors.White).Ellipsoid(new Vector3(0, size.Y * 0.6f, 0), size, 7, 3, new Basis(Vector3.Up, v * 1.3f), 0.5f);
        return Done(k);
    }
}
