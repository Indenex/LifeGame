using Godot;

namespace LifeGame;

// A low, rounded shrub. When ripe it is dotted with dark red berries; picked,
// it is just leaves until they grow back.
public partial class BerryBush : Node3D
{
    public static readonly Color BerryColor = new(0.55f, 0.08f, 0.16f);
    static readonly Color BushLeaf = new(0.20f, 0.33f, 0.14f);

    static Mesh? lobeMesh, berryMesh;
    readonly Node3D berries = new();

    public BerryBush(ulong id)
    {
        lobeMesh ??= Look.Faceted(new SphereMesh { Radius = 0.42f, Height = 0.6f, RadialSegments = 8, Rings = 4 }, BushLeaf);
        berryMesh ??= Look.Faceted(new SphereMesh { Radius = 0.045f, Height = 0.09f, RadialSegments = 6, Rings = 3 }, BerryColor);

        int seed = (int)(id % 100_000);
        Rotation = new Vector3(0, Terrain.Hash01(seed, 1, 40) * Mathf.Tau, 0);
        float size = 0.85f + Terrain.Hash01(seed, 2, 40) * 0.35f;
        Scale = Vector3.One * size;

        // A few overlapping lobes read as one soft shrub.
        Vector3[] lobes = { new(0, 0.3f, 0), new(0.3f, 0.22f, 0.1f), new(-0.25f, 0.2f, 0.18f), new(0.05f, 0.24f, -0.3f) };
        for (int i = 0; i < lobes.Length; i++)
        {
            float s = 0.75f + Terrain.Hash01(seed, i, 41) * 0.35f;
            AddChild(new MeshInstance3D
            {
                Mesh = lobeMesh,
                Position = lobes[i],
                Scale = new Vector3(s, s * 0.85f, s),
            });
        }

        // Berries sit just outside the leaves, mostly on the upper half.
        for (int i = 0; i < 16; i++)
        {
            float a = Terrain.Hash01(seed, i, 42) * Mathf.Tau;
            float up = 0.15f + Terrain.Hash01(seed, i, 43) * 0.75f;
            var dir = new Vector3(Mathf.Cos(a) * (1f - up * 0.6f), up, Mathf.Sin(a) * (1f - up * 0.6f)).Normalized();
            berries.AddChild(new MeshInstance3D { Mesh = berryMesh, Position = new Vector3(0, 0.22f, 0) + dir * 0.46f });
        }
        AddChild(berries);
    }

    public bool Ripe
    {
        get => berries.Visible;
        set => berries.Visible = value;
    }
}
