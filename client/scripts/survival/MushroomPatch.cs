using Godot;

namespace LifeGame;

// A small cluster of woodland mushrooms in a tree's shade (survival-35): pale stems and
// warm brown caps of a few sizes, some leaning. Picked, only two tiny buttons are left
// until the patch comes back. A stand-in until Modeling makes the real ones.
public partial class MushroomPatch : Node3D
{
    static readonly Color Stem = new(0.86f, 0.80f, 0.68f);
    public static readonly Color CapColor = new(0.52f, 0.33f, 0.18f);
    static readonly Color CapLight = new(0.66f, 0.48f, 0.30f);

    static Mesh? stemMesh, capMesh, capLightMesh;
    readonly Node3D grown = new();

    public MushroomPatch(ulong id)
    {
        stemMesh ??= PaintedMesh(Look.Faceted(new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.016f, Height = 1f, RadialSegments = 6, Rings = 1 }, Stem));
        capMesh ??= PaintedMesh(Look.Faceted(new SphereMesh { Radius = 0.05f, Height = 0.05f, IsHemisphere = true, RadialSegments = 8, Rings = 3 }, CapColor));
        capLightMesh ??= PaintedMesh(Look.Faceted(new SphereMesh { Radius = 0.05f, Height = 0.05f, IsHemisphere = true, RadialSegments = 8, Rings = 3 }, CapLight));

        int seed = (int)(id % 100_000);
        Rotation = new Vector3(0, Terrain.Hash01(seed, 1, 60) * Mathf.Tau, 0);

        int count = 4 + (int)(Terrain.Hash01(seed, 2, 60) * 4f);
        for (int i = 0; i < count; i++)
        {
            // Bunched toward the middle, the biggest first, the rest smaller round them.
            float a = i * 2.4f + Terrain.Hash01(seed, i, 61);
            float r = i == 0 ? 0f : 0.05f + Terrain.Hash01(seed, i, 62) * 0.14f;
            float size = i == 0 ? 1.15f : 0.55f + Terrain.Hash01(seed, i, 63) * 0.55f;
            grown.AddChild(Mushroom(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), size,
                lean: (Terrain.Hash01(seed, i, 64) - 0.5f) * 0.5f, light: Terrain.Hash01(seed, i, 65) < 0.35f));
        }
        AddChild(grown);

        // Two buttons just breaking the ground, always there, so a picked patch still reads as one.
        for (int i = 0; i < 2; i++)
        {
            float a = Terrain.Hash01(seed, i, 66) * Mathf.Tau;
            AddChild(Mushroom(new Vector3(Mathf.Cos(a) * 0.08f, 0, Mathf.Sin(a) * 0.08f), 0.3f, 0f, false));
        }
    }

    static Node3D Mushroom(Vector3 at, float size, float lean, bool light)
    {
        float height = 0.07f * size;
        var m = new Node3D { Position = at, Rotation = new Vector3(lean, 0, lean * 0.6f), Scale = Vector3.One * size };
        m.AddChild(new MeshInstance3D { Mesh = stemMesh, Position = new Vector3(0, height / size * 0.5f, 0), Scale = new Vector3(1, height / size, 1) });
        m.AddChild(new MeshInstance3D { Mesh = light ? capLightMesh : capMesh, Position = new Vector3(0, height / size - 0.008f, 0), Scale = new Vector3(1, 0.9f, 1) });
        return m;
    }

    static Mesh PaintedMesh(Mesh mesh)
    {
        if (mesh is ArrayMesh array && array.SurfaceGetMaterial(0) is ShaderMaterial solid) array.SurfaceSetMaterial(0, Painted.Of(solid));
        return mesh;
    }

    public bool Ripe
    {
        get => grown.Visible;
        set => grown.Visible = value;
    }
}
