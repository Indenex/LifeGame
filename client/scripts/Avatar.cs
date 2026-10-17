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
        animator.Surroundings = Surroundings;
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
            Position = new Vector3(0, PlayerModel.Height + 0.3f, 0),
        };
        AddChild(nameplate);
        PlaceNameplate();
    }

    // Every avatar in the world, for fidgets that notice each other (waving).
    static readonly System.Collections.Generic.List<Avatar> all = new();
    public override void _EnterTree() => all.Add(this);
    public override void _ExitTree() => all.Remove(this);

    // 0 to 1: warmth from burning fires at a spot (AvatarWork sets it from the fire table).
    public static System.Func<Vector3, float>? FireWarmthAt;

    const float WaveReach = 6f;

    (float, float?) Surroundings()
    {
        float warmth = FireWarmthAt?.Invoke(Position) ?? 0f;
        float? friend = null;
        float best = WaveReach * WaveReach;
        foreach (var a in all)
        {
            if (a == this || !a.IsVisibleInTree()) continue;
            var d = a.Position - Position;
            float d2 = d.X * d.X + d.Z * d.Z;
            if (d2 >= best || d2 < 0.5f) continue;
            // Only someone roughly in front: nobody waves over their shoulder.
            float yaw = Mathf.AngleDifference(Rotation.Y, Mathf.Atan2(d.X, d.Z));
            if (Mathf.Abs(yaw) > 1.4f) continue;
            best = d2;
            friend = yaw;
        }
        return (warmth, friend);
    }

    // Set from player_sneaking for other players; tracks the key for your own.
    public bool Sneaking;
    // Your own avatar: fired when you start or stop sneaking, so the server can tell others.
    public event System.Action<bool>? SneakChanged;

    // What this player's hands are doing, from their running craft job (AvatarWork).
    public WorkMotion Work { set => animator.Work = value; }
    // Whether that job is worked standing at a station rather than kneeling.
    public bool WorkStanding { set => animator.WorkStanding = value; }

    // Sit or lie down (anim-12), from the player's rest state: seat is the hips' height
    // when sitting on something (0 on the ground). RestKind.None gets up again. The
    // figure lies along the way the avatar faces, head forward.
    public void Rest(RestKind kind, float seat = 0f, bool asleep = false, bool instant = false)
    {
        animator.Rest = kind;
        animator.Seat = seat;
        animator.Asleep = asleep;
        if (instant) animator.SettleRest();
    }

    // Your own avatar: the rest key sits you down, lies you down from sitting, and gets
    // you up again; walking off also gets you up (the server ends the rest).
    public event System.Action<RestKind>? RestRequested;
    public const string RestAction = "rest";

    void RestKey()
    {
        if (!InputMap.HasAction(RestAction))
        {
            InputMap.AddAction(RestAction);
            InputMap.ActionAddEvent(RestAction, new InputEventKey { PhysicalKeycode = Key.H });
        }
        if (!Input.IsActionJustPressed(RestAction) || GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) return;
        RestRequested?.Invoke(animator.Rest switch { RestKind.None => RestKind.Sit, RestKind.Sit => RestKind.Lie, _ => RestKind.None });
    }

    // Turn on the spot to face a point on the ground (your own avatar; others follow the server).
    public void Face(float x, float z)
    {
        float dx = x - Position.X, dz = z - Position.Z;
        if (dx * dx + dz * dz > 1e-4f) Rotation = new Vector3(0, Mathf.Atan2(dx, dz), 0);
    }

    // Jump state from movement (bugfix-25): in the air or not, still rising, and how hard
    // the last landing was (0 to 1, set on the frame you land). The avatar's height is
    // moved by whoever moves it; this only poses the body.
    public void SetJump(bool airborne, bool rising, float landing, float windUp = 0f)
    {
        animator.Jump(airborne, rising, landing, windUp);
        // A squishy landing (#17): the body squashes down and springs back, keeping
        // its volume; taking off stretches it a little.
        if (landing > 0.01f) Squish(-0.16f * Mathf.Clamp(landing, 0.3f, 1f));
        else if (airborne && rising && !wasRising) Squish(0.07f);
        wasRising = airborne && rising;
    }

    float squish, squishTime = 99f;
    bool wasRising;

    void Squish(float amount)
    {
        squish = amount;
        squishTime = 0f;
    }

    void SquishTick(float dt)
    {
        if (squishTime > 1f) return;
        squishTime += dt;
        float y = 1f + squish * Mathf.Exp(-8f * squishTime) * Mathf.Cos(20f * squishTime);
        if (squishTime > 1f) y = 1f;
        float side = 1f / Mathf.Sqrt(y);
        figure.Scale = new Vector3(side, y, side);
    }

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
        if (actItem == null) RestoreHands();
    }
    Item? heldRight, heldLeft, workRight, workLeft;

    // Hands back to the job's (or equipped) items once nothing borrows them.
    void RestoreHands()
    {
        Grip("ToolR", RightBase, ref heldRight);
        Grip("ToolL", workLeft, ref heldLeft);
    }

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
    // Load units (roughly kg) from which a held thing is hugged in both arms (anim-18).
    const float HeavyHold = 4f;
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
        animator.CarryingHeavy = animator.Carrying && equipped is { } held && Items.IsKnown((ushort)held)
            && LifeGame.Shared.Carry.Weight(held) >= HeavyHold;
    }

    // A quick action from the server's player_act (anim-4): drinking, eating, picking
    // berries, picking something up. item shows in the right hand for its part of it:
    // food on its way to the mouth, berries and finds once they are in the hand.
    public void Act(WorkMotion motion, Item? item)
    {
        animator.Act(motion, WorkMotions.VariantOf(motion, item));
        // An empty-handed action (a flinch) gives back whatever the last one borrowed.
        if (item == null && actItem != null) RestoreHands();
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
            RestoreHands();
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
        // Pebble (the shell-backed chibi) waddles: its hips rock side to side as it walks (anim-20).
        bool pebble = PlayerModel.Chibi && PlayerModel.ChibiLook(look).Race == ChibiFigure.Race.Pebble;
        animator.Waddle = pebble ? 1f : 0f;
        // Its shell is in the way of anything reaching behind the back (anim-21).
        animator.Shell = pebble;
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
        {
            rig.Height = mesh.GetAabb().End.Y;
            nameplateY = rig.Height + 0.3f;
        }
        nameplate.Position = new Vector3(0, nameplateY, 0);
    }

    float nameplateY = PlayerModel.Height + 0.3f;

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

        if (!Remote) RestKey();
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
            SquishTick(dt);
            animator.Update(dt, speed, turnRate, sneaking);
            if (actItem != null) ShowActItem();
            // Blinks and expressions on the chibi's face parts (#16); a no-op on bodies without them.
            if (figure is Skeleton3D skeleton) PlayerModel.SetFace(skeleton, animator.Face);
            // The name comes down with the head when sitting or lying.
            nameplate.Position = new Vector3(0, nameplateY - rig.Height * animator.RestLowering, 0);
        }
    }
}
