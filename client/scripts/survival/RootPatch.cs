using Godot;

namespace LifeGame;

// A patch of wild roots in open meadow (survival-36), drawn with Modeling's wild carrot
// (model-35): a ripe rosette with white-flowered stalks, or after digging the hole with
// its earth heaped beside it. The variant comes from the row id.
public partial class RootPatch : Node3D
{
    readonly MeshInstance3D ripe, dug;

    public RootPatch(ulong id)
    {
        int variant = (int)(id % (ulong)WildRoots.Variants);
        Rotation = new Vector3(0, Terrain.Hash01((int)(id % 100_000), 1, 70) * Mathf.Tau, 0);
        ripe = new MeshInstance3D { Mesh = WildRoots.Patch(variant), MaterialOverride = WildRoots.Material };
        dug = new MeshInstance3D { Mesh = WildRoots.Dug(variant), MaterialOverride = WildRoots.Material, Visible = false };
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
