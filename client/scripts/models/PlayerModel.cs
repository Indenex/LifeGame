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

    // The chibi roster (model-6, D-249): Bramble or Pebble by the look's race, on
    // the 1.35 m base, the player figure since Justin turned it on. `--legacy-player`
    // on the command line brings back the 1.75 m figure for comparison. Its clothes
    // are cut to it (ChibiFigure.Garment).
    public static bool Chibi = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--legacy-player") < 0;

    // How tall the player figure stands, for the camera and the nameplate.
    public static float Height => Chibi ? ChibiFigure.Height : 1.75f;

    public static ChibiFigure.Look ChibiLook(HumanFigure.Appearance look) => new((ChibiFigure.Race)look.Race, look.Skin, look.Hair, look.Cut);

    // Clothing slots, as in shared/Clothing.cs and the worn_item table (D-074).
    public const byte Body = 0, Feet = 1;

    // The mouth, in the HeadAttach frame, for the current style.
    public static Vector3 MouthAt
    {
        get
        {
            var f = Chibi ? ChibiFigure.Build(ChibiLook(HumanFigure.Appearance.Default)) : HumanFigure.Build(Style);
            float chin = (Chibi ? ChibiFigure.Height : 1.75f) - f.HeadHeight;
            return new Vector3(0, chin + f.HeadHeight * 0.3f - f.Joints.At[HumanRig.Head].Y, f.HeadHeight * 0.42f);
        }
    }

    public static string SlotNode(byte slot) => $"Worn{slot}";

    public static Skeleton3D Build() => Build(HumanFigure.Appearance.Default);

    // A player's own look (skin, hair, cut, beard) on the shared figure.
    public static Skeleton3D Build(HumanFigure.Appearance look)
    {
        var skeleton = Chibi ? ChibiFigure.Create(ChibiLook(look)) : HumanFigure.Create(Style, look);
        float drop = Chibi ? ChibiFigure.Build(ChibiLook(look)).GripDrop : HumanFigure.Build(Style).GripDrop;
        Remember(skeleton, look);
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

    // Gives a built figure another look; the skeleton and anything worn stay.
    public static void SetLook(Skeleton3D figure, HumanFigure.Appearance look)
    {
        if (Chibi)
        {
            Remember(figure, look);
            var built = ChibiFigure.Build(ChibiLook(look));
            if (figure.GetNodeOrNull<MeshInstance3D>("Body") is { } chibi) chibi.Mesh = built.Mesh;
            // Put back on whatever it wears, cut for the new race.
            for (byte slot = 0; slot <= Feet; slot++)
                Wear(figure, slot, figure.HasMeta($"worn{slot}") ? (ushort)figure.GetMeta($"worn{slot}").AsInt32() : (ushort)0);
            return;
        }
        if (figure.GetNodeOrNull<MeshInstance3D>("Body") is { } body)
            body.Mesh = HumanFigure.Build(Style, look).Mesh;
    }

    // A figure keeps its look, so its chibi wrap can be put back on.
    static void Remember(Node figure, HumanFigure.Appearance look) =>
        figure.SetMeta("look", new int[] { look.Skin, look.Hair, look.Cut, look.Beard ? 1 : 0, look.Race });

    static HumanFigure.Appearance LookOf(Node figure) =>
        figure.HasMeta("look") && figure.GetMeta("look").AsInt32Array() is { Length: 5 } l ? new(l[0], l[1], l[2], l[3] != 0, l[4]) : HumanFigure.Appearance.Default;

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
        figure.SetMeta($"worn{slot}", item);
        var mesh = Chibi ? ChibiFigure.Garment(ChibiLook(LookOf(figure)), slot, item) : Garments.For(Style, slot, item);
        if (mesh != null)
            figure.AddChild(new MeshInstance3D { Name = SlotNode(slot), Mesh = mesh, Skeleton = new NodePath("..") });
    }
}
