using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A player: the Modeling lane's skinned figure (PlayerModel), posed every frame by
// PlayerAnimator through its named bones. Remote avatars glide
// toward the last position the server sent; the local one is moved by input.
public partial class Avatar : Node3D
{
    public Vector3 Target;
    public float TargetHeading;
    public bool Remote;

    readonly Node3D figure;
    readonly PlayerAnimator animator;
    readonly PlayerRig rig;
    readonly Label3D nameplate;
    float speed, turnRate, lastHeading;
    Vector3 lastPosition;

    static readonly StringName SneakAction = "sneak";

    public Avatar(string name)
    {
        var body = PlayerModel.Build();
        // Everyone wakes up in the leaf wrap until worn_item says otherwise (D-074).
        PlayerModel.Wear(body, PlayerModel.Body, 0);
        figure = body;
        AddChild(figure);
        rig = new PlayerRig(figure);
        PlaceMouth();
        animator = new PlayerAnimator(rig, GetInstanceId());
        animator.Impact = (motion, thrown) =>
            Impact?.Invoke(this, motion, ToolPoint(), thrown, (ushort)((WorkMotions.IsAct(motion) ? actItem : RightBase) ?? 0));
        // Breath in the cold leaves the mouth, on the head bone's attachment.
        if (figure.FindChild("HeadAttach", true, false) is Node3D head)
            head.AddChild(new Breath { Position = PlayerModel.MouthAt });

        nameplate = new Label3D
        {
            Text = name,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 26,
            PixelSize = 0.006f,
            Modulate = Look.Cream,
            OutlineSize = 6,
            OutlineModulate = new Color(0.1f, 0.1f, 0.08f, 0.55f),
            Position = new Vector3(0, 2.05f, 0),
        };
        AddChild(nameplate);
        PlaceNameplate();
    }

    // Set from player_sneaking for other players; tracks the key for your own.
    public bool Sneaking;
    // Your own avatar: fired when you start or stop sneaking, so the server can tell others.
    public event System.Action<bool>? SneakChanged;

    // What this player's hands are doing, from their running craft job (AvatarWork).
    public WorkMotion Work { set => animator.Work = value; }

    // Turn on the spot to face a point on the ground (your own avatar; others follow the server).
    public void Face(float x, float z)
    {
        float dx = x - Position.X, dz = z - Position.Z;
        if (dx * dx + dz * dz > 1e-4f) Rotation = new Vector3(0, Mathf.Atan2(dx, dz), 0);
    }

    // Jump state from movement (bugfix-25): in the air or not, still rising, and how hard
    // the last landing was (0 to 1, set on the frame you land). The avatar's height is
    // moved by whoever moves it; this only poses the body.
    public void SetJump(bool airborne, bool rising, float landing) => animator.Jump(airborne, rising, landing);

    // Lifts the figure off the ground by this many metres without moving the avatar node.
    // Use it for other players' jumps: their node glides toward the server position every
    // frame, which would pull a raised Y straight back down.
    public void SetAirHeight(float metres) => figure.Position = new Vector3(0, Mathf.Max(metres, 0f), 0);

    // Shows a garment in a clothing slot (item 0: the slot's default), from worn_item.
    public void Wear(byte slot, ushort item)
    {
        if (slot >= worn.Length || worn[slot] == item) return;
        worn[slot] = item;
        PlayerModel.Wear((Skeleton3D)figure, slot, item);
    }
    readonly ushort[] worn = new ushort[2];

    // Fired on every avatar's tool impacts (anim-7): an axe biting the trunk, a spade or
    // stick going into the ground (thrown: the dirt leaving a spade), a strike landing,
    // fuel landing on a fire. at is the right hand's tool point in the world; item is
    // the tool (or fuel) in use, 0 for bare hands. Atmosphere puts chips and dirt there;
    // Audio plays the thunk and crunch on the same frame.
    public static event System.Action<Avatar, WorkMotion, Vector3, bool, ushort>? Impact;

    Vector3 ToolPoint() => figure.FindChild("ToolR", true, false) is Node3D tool && tool.IsInsideTree()
        ? tool.GlobalPosition : GlobalPosition + Vector3.Up;

    // Puts items in the hands (null for empty), in the ToolR and ToolL fist frames.
    // A quick action (Act) borrows the right hand and gives it back after.
    public void Hold(Item? right, Item? left)
    {
        (workRight, workLeft) = (right, left);
        if (actItem == null)
        {
            Grip("ToolR", RightBase, ref heldRight);
            Grip("ToolL", left, ref heldLeft);
        }
    }
    Item? heldRight, heldLeft, workRight, workLeft;

    // The tool this player has equipped from the hotbar (crafting-29 PlayerEquip), carried
    // in the right hand when no job or quick action needs the hand. Changing it plays a
    // quick draw or stow (0.3 s) and the model swaps halfway, as the hand passes the hip.
    // instant: no draw motion (an avatar just come into view already holding it).
    public void Equip(Item? item, bool instant = false)
    {
        if (item == pendingEquip) return;
        pendingEquip = item;
        if (item == equipped) { swapIn = -1f; return; }
        if (!instant) animator.Swap();
        swapIn = instant ? 0f : PlayerAnimator.SwapSeconds * 0.5f;
    }
    Item? equipped, pendingEquip;
    float swapIn = -1f;

    // What the right hand holds when nothing borrows it: the job's tool, else the equipped one.
    Item? RightBase => workRight ?? equipped;

    void CarryTick(float dt)
    {
        if (swapIn >= 0f && (swapIn -= dt) < 0f)
        {
            equipped = pendingEquip;
            if (actItem == null) Grip("ToolR", RightBase, ref heldRight);
        }
        animator.Carrying = equipped != null && workRight == null && actItem == null;
    }

    // A quick action from the server's player_act (anim-4): drinking, eating, picking
    // berries, picking something up. item shows in the right hand for its part of it:
    // food on its way to the mouth, berries and finds once they are in the hand.
    public void Act(WorkMotion motion, Item? item)
    {
        animator.Act(motion, WorkMotions.VariantOf(motion, item));
        // An empty-handed action (a flinch) gives back whatever the last one borrowed.
        if (item == null && actItem != null)
        {
            Grip("ToolR", RightBase, ref heldRight);
            Grip("ToolL", workLeft, ref heldLeft);
        }
        actItem = item;
        actMotion = motion;
    }
    Item? actItem;
    WorkMotion actMotion;

    void ShowActItem()
    {
        float left = animator.ActLeft, length = animator.ActLength;
        bool inLeft = WorkMotions.ActItemInLeft(actMotion);
        if (left <= 0f || actItem == null)
        {
            actItem = null;
            Grip("ToolR", RightBase, ref heldRight);
            Grip("ToolL", workLeft, ref heldLeft);
            return;
        }
        float t = length - left;
        bool show = actMotion switch
        {
            WorkMotion.Eat => t < 0.9f,
            WorkMotion.Pluck => t > 0.4f,
            WorkMotion.PickUp => t > length * 0.5f,
            WorkMotion.Feed => t < 0.45f,
            // Weapons, the bow and blades are in the hand throughout.
            _ => true,
        };
        if (inLeft) Grip("ToolL", show ? actItem : workLeft, ref heldLeft);
        else Grip("ToolR", show ? actItem : RightBase, ref heldRight);
    }

    void Grip(string hand, Item? item, ref Item? held)
    {
        if (item == held) return;
        held = item;
        if (figure.FindChild(hand, true, false) is not Node3D fist) return;
        foreach (var child in fist.GetChildren()) child.QueueFree();
        if (item is { } it) fist.AddChild(new MeshInstance3D { Mesh = ItemModels.HeldMesh(it) });
    }

    // This player's own look (skin, hair, cut, beard), from their player id (Modeling).
    public void SetLook(uint playerId) => SetLook(HumanFigure.Appearance.For(playerId));
    public void SetLook(HumanFigure.Appearance look)
    {
        PlayerModel.SetLook((Skeleton3D)figure, look);
        // A new look may be a differently shaped body: poses that reach the face re-measure it.
        rig.Measure();
        PlaceMouth();
        PlaceNameplate();
    }

    // The name floats a little above the head, whatever the body's height (a 1.75 m
    // adult or a 1.35 m chibi): the top of the body mesh plus a gap.
    void PlaceNameplate()
    {
        if (figure.GetNodeOrNull<MeshInstance3D>("Body") is { Mesh: { } mesh })
            nameplate.Position = new Vector3(0, mesh.GetAabb().End.Y + 0.3f, 0);
    }

    // Where the mouth is on the resting figure, for hand-to-mouth reaches (eating, drinking).
    void PlaceMouth()
    {
        if (figure is Skeleton3D skeleton && skeleton.FindBone("Head") is var head and >= 0)
            rig.Mouth = skeleton.GetBoneGlobalRest(head).Origin + PlayerModel.MouthAt;
    }

    public void SetPlayerName(string name) => nameplate.Text = name;

    // Claims tints a name red while that player is marked for striking someone down.
    public void SetNameTint(Color color) => nameplate.Modulate = color;

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (Remote)
        {
            // Exponential smoothing hides the 10 Hz update rate.
            Position = Position.Lerp(Target, 1f - Mathf.Exp(-12f * dt));
            Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, TargetHeading, 1f - Mathf.Exp(-10f * dt)), 0);
        }

        var flat = Position - lastPosition;
        flat.Y = 0;
        lastPosition = Position;
        float k = 1f - Mathf.Exp(-8f * dt);
        speed = Mathf.Lerp(speed, Mathf.Min(flat.Length() / Mathf.Max(dt, 1e-4f), 9f), k);
        turnRate = Mathf.Lerp(turnRate, Mathf.AngleDifference(lastHeading, Rotation.Y) / Mathf.Max(dt, 1e-4f), k);
        lastHeading = Rotation.Y;

        // Your own sneak is the key; other players' comes from the server (player_sneaking).
        bool sneaking = Remote ? Sneaking : Input.IsActionPressed(SneakAction);
        if (!Remote && sneaking != Sneaking)
        {
            Sneaking = sneaking;
            SneakChanged?.Invoke(sneaking);
        }
        // Hidden avatars (offline players) cost nothing.
        if (IsVisibleInTree())
        {
            // How far the feet are below the water line: wading, or afloat (shared/Wading.cs).
            animator.WaterDepth = Look.WaterLevel - Position.Y - figure.Position.Y;
            CarryTick(dt);
            animator.Update(dt, speed, turnRate, sneaking);
            if (actItem != null) ShowActItem();
        }
    }
}
