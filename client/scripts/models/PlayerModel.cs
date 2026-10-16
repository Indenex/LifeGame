using Godot;

namespace LifeGame;

// The player figure the game uses, for the Animation lane's rig (anim-1). A
// Skeleton3D with HumanRig's bones and the figure's mesh as a child; a
// "ToolR" attachment on HandR holds tools (shaft along local -Y, head toward +Z).
public static class PlayerModel
{
    // The style in use until Justin picks one (model-1).
    public static HumanFigure.Style Style = HumanFigure.Carved;

    public static Skeleton3D Build()
    {
        var skeleton = HumanFigure.Create(Style);
        skeleton.AddChild(new BoneAttachment3D { Name = "ToolR", BoneName = HumanRig.Names[HumanRig.HandR] });
        return skeleton;
    }
}
