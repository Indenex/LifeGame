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
        animator = new PlayerAnimator(new PlayerRig(figure), GetInstanceId());
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

    // Puts items in the hands (null for empty), in the ToolR and ToolL fist frames.
    public void Hold(Item? right, Item? left)
    {
        Grip("ToolR", right, ref heldRight);
        Grip("ToolL", left, ref heldLeft);
    }
    Item? heldRight, heldLeft;

    void Grip(string hand, Item? item, ref Item? held)
    {
        if (item == held) return;
        held = item;
        if (figure.FindChild(hand, true, false) is not Node3D fist) return;
        foreach (var child in fist.GetChildren()) child.QueueFree();
        if (item is { } it) fist.AddChild(new MeshInstance3D { Mesh = ItemModels.HeldMesh(it) });
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
        if (IsVisibleInTree()) animator.Update(dt, speed, turnRate, sneaking);
    }
}
