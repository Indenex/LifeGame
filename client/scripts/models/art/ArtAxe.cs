using Godot;

namespace LifeGame;

// The stone axe authored in Blender (art-rnd-1 showcase, docs/art-pipeline.md): a knapped
// stone head with big painted facets, a bent wooden haft with soft grain streaks and a
// chunky cord lashing, built and baked by tools/art/axe/ and painted by script. It is
// modelled in ItemModels' held frame (grip at the origin, head towards -Y, edge towards
// +Z), so it could stand in for ItemModels.StoneAxe's held mesh as it is.
// Showcase --view=art --piece=axe stands both upright, head up, beside each other.
public static class ArtAxe
{
    public static float Spacing = 0.42f;
    public static float Height = 0.78f;
    const string Dir = "res://assets/art/axe/";

    public static Node3D Today() => Upright(new MeshInstance3D { Mesh = ItemModels.HeldMesh(LifeGame.Shared.Item.StoneAxe) });

    public static Node3D Authored() => Upright(PaintedAsset.Instance(Dir + "axe.glb", PaintedAsset.Material(Dir + "axe.png")));

    // Head up, the knob end resting on the ground.
    static Node3D Upright(Node3D axe)
    {
        var holder = new Node3D();
        axe.Rotation = new Vector3(0, 0, Mathf.Pi);
        axe.Position = new Vector3(0, 0.14f, 0);
        holder.AddChild(axe);
        return holder;
    }
}
