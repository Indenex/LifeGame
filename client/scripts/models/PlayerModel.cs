using Godot;

namespace LifeGame;

// The player figure the game uses, for the Animation lane's rig (anim-1). A
// Skeleton3D with HumanRig's bones and the figure's mesh as a child. Tools go
// under the "ToolR" and "ToolL" nodes, which sit in the middle of each fist
// (under BoneAttachment3Ds on HandR and HandL): shaft along local -Y, head or
// edge toward +Z. ItemModels.HeldMesh gives items already in that frame.
public static class PlayerModel
{
    // The style in use until Justin picks one (model-1).
    public static HumanFigure.Style Style = HumanFigure.Carved;

    // Clothing slots, as in shared/Clothing.cs and the worn_item table (D-074).
    public const byte Body = 0, Feet = 1;

    // The mouth, in the HeadAttach frame, for the current style.
    public static Vector3 MouthAt
    {
        get
        {
            var f = HumanFigure.Build(Style);
            float chin = 1.75f - f.HeadHeight;
            return new Vector3(0, chin + f.HeadHeight * 0.3f - f.Joints.At[HumanRig.Head].Y, f.HeadHeight * 0.42f);
        }
    }

    public static string SlotNode(byte slot) => $"Worn{slot}";

    public static Skeleton3D Build()
    {
        var skeleton = HumanFigure.Create(Style);
        float drop = HumanFigure.Build(Style).GripDrop;
        foreach (var (bone, tool) in new[] { (HumanRig.HandR, "ToolR"), (HumanRig.HandL, "ToolL") })
        {
            var hand = new BoneAttachment3D { Name = tool == "ToolR" ? "HandRAttach" : "HandLAttach", BoneName = HumanRig.Names[bone] };
            hand.AddChild(new Node3D { Name = tool, Position = new Vector3(0, -drop, 0) });
            skeleton.AddChild(hand);
        }
        // For things at the face, like breath in cold air (D-144): the head bone's
        // own frame; the mouth is at MouthAt.
        skeleton.AddChild(new BoneAttachment3D { Name = "HeadAttach", BoneName = HumanRig.Names[HumanRig.Head] });
        return skeleton;
    }

    // Puts on what a player wears in a slot (item 0 for nothing): a garment from
    // the worn_item table replaces the one shown there. An empty body slot shows
    // the leaf wrap every player wakes up in.
    public static void Wear(Skeleton3D figure, byte slot, ushort item)
    {
        var old = figure.GetNodeOrNull(SlotNode(slot));
        if (old != null)
        {
            figure.RemoveChild(old);
            old.QueueFree();
        }
        var mesh = Garments.For(Style, slot, item);
        if (mesh != null)
            figure.AddChild(new MeshInstance3D { Name = SlotNode(slot), Mesh = mesh, Skeleton = new NodePath("..") });
    }
}
