using Godot;

namespace LifeGame;

// A patch of woodland mushrooms in a tree's shade (survival-35), drawn with Modeling's
// painted mushrooms (model-34): a ripe cluster, or after picking the cut stubs and two
// tiny buttons, so a picked patch still reads as one. The variant comes from the row id.
public partial class MushroomPatch : Node3D
{
    readonly MeshInstance3D ripe, picked;

    public MushroomPatch(ulong id)
    {
        int variant = (int)(id % (ulong)Mushrooms.Variants);
        Rotation = new Vector3(0, Terrain.Hash01((int)(id % 100_000), 1, 60) * Mathf.Tau, 0);
        ripe = new MeshInstance3D { Mesh = Mushrooms.Patch(variant), MaterialOverride = Mushrooms.Material };
        picked = new MeshInstance3D { Mesh = Mushrooms.Picked(variant), MaterialOverride = Mushrooms.Material, Visible = false };
        AddChild(ripe);
        AddChild(picked);
        GrassClearings.Track(this);
    }

    public bool Ripe
    {
        get => ripe.Visible;
        set
        {
            ripe.Visible = value;
            picked.Visible = !value;
        }
    }
}
