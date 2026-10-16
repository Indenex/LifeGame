using Godot;

namespace LifeGame;

// The player figure the game uses, for the Animation lane's rig (anim-1). A
// Skeleton3D with HumanRig's bones and the figure's mesh as a child; a
// "ToolR" node (under a BoneAttachment3D on HandR, in the fist) holds tools (shaft along local -Y, head toward +Z).
public static class PlayerModel
{
    // The style in use until Justin picks one (model-1).
    public static HumanFigure.Style Style = HumanFigure.Carved;

    public static Skeleton3D Build()
    {
        var skeleton = HumanFigure.Create(Style);
        // ToolR sits in the middle of the fist, a little below the wrist joint.
        var hand = new BoneAttachment3D { Name = "HandRAttach", BoneName = HumanRig.Names[HumanRig.HandR] };
        hand.AddChild(new Node3D { Name = "ToolR", Position = new Vector3(0, -HumanFigure.Build(Style).GripDrop, 0) });
        skeleton.AddChild(hand);
        return skeleton;
    }
}
