using Godot;

namespace LifeGame;

// A patch of wild roots in open meadow (survival-36): a low rosette of leaves with one
// or two flowering stalks crowned in white, like wild carrot; dug up, a little mound of
// turned earth with a few torn leaves until it grows back. A stand-in until Modeling
// paints one; the shape varies by row id.
public partial class RootPatch : Node3D
{
    static readonly Color Leaf = new(0.30f, 0.42f, 0.18f);
    static readonly Color Flower = new(0.90f, 0.88f, 0.80f);
    static Mesh? leafMesh, stalkMesh, headMesh, moundMesh;
    readonly Node3D ripe = new(), dug = new();

    public RootPatch(ulong id)
    {
        leafMesh ??= Look.Faceted(new PrismMesh { Size = new Vector3(0.07f, 0.02f, 0.22f), LeftToRight = 0.5f }, Leaf);
        stalkMesh ??= Look.Faceted(new CylinderMesh { TopRadius = 0.005f, BottomRadius = 0.008f, Height = 0.42f, RadialSegments = 4 }, Leaf.Darkened(0.15f));
        headMesh ??= Look.Faceted(new SphereMesh { Radius = 0.05f, Height = 0.03f, RadialSegments = 7, Rings = 2 }, Flower);
        moundMesh ??= Look.Faceted(new SphereMesh { Radius = 0.18f, Height = 0.08f, RadialSegments = 7, Rings = 3 }, Look.Earth.Darkened(0.1f));

        int seed = (int)(id % 100_000);
        Rotation = new Vector3(0, Terrain.Hash01(seed, 1, 70) * Mathf.Tau, 0);
        Scale = Vector3.One * (0.85f + Terrain.Hash01(seed, 2, 70) * 0.3f);

        int leaves = 6 + (int)(Terrain.Hash01(seed, 3, 70) * 4f);
        for (int i = 0; i < leaves; i++)
        {
            float a = i * Mathf.Tau / leaves + Terrain.Hash01(seed, i, 71) * 0.4f;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            ripe.AddChild(new MeshInstance3D
            {
                Mesh = leafMesh,
                Position = dir * 0.1f + Vector3.Up * 0.02f,
                Rotation = new Vector3(-0.35f - Terrain.Hash01(seed, i, 72) * 0.3f, a, 0),
            });
        }
        int stalks = 1 + (int)(Terrain.Hash01(seed, 4, 70) * 2f);
        for (int s = 0; s < stalks; s++)
        {
            var lean = new Vector3((Terrain.Hash01(seed, s, 73) - 0.5f) * 0.3f, 0, (Terrain.Hash01(seed, s, 74) - 0.5f) * 0.3f);
            var stalk = new Node3D { Position = new Vector3(s * 0.05f, 0, -s * 0.04f), Rotation = lean };
            float h = 0.85f + Terrain.Hash01(seed, s, 75) * 0.3f;
            stalk.AddChild(new MeshInstance3D { Mesh = stalkMesh, Position = Vector3.Up * 0.21f * h, Scale = new Vector3(1, h, 1) });
            stalk.AddChild(new MeshInstance3D { Mesh = headMesh, Position = Vector3.Up * 0.42f * h });
            ripe.AddChild(stalk);
        }

        dug.AddChild(new MeshInstance3D { Mesh = moundMesh });
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.Tau / 3 + Terrain.Hash01(seed, i, 76);
            dug.AddChild(new MeshInstance3D
            {
                Mesh = leafMesh,
                Position = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 0.2f + Vector3.Up * 0.01f,
                Rotation = new Vector3(-0.05f, a, 0),
                Scale = Vector3.One * 0.7f,
            });
        }
        dug.Visible = false;
        AddChild(ripe);
        AddChild(dug);
    }

    public bool Ripe
    {
        get => ripe.Visible;
        set
        {
            ripe.Visible = value;
            dug.Visible = !value;
        }
    }
}
