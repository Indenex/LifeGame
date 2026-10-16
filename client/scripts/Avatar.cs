using Godot;

namespace LifeGame;

// A simple, soft-shaped person. The figure is a tree of named joint pivots (see
// animation/PlayerRig.cs) that PlayerAnimator poses each frame. Remote avatars glide
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

    static ShaderMaterial? skin, hair, leaves;
    static readonly StringName SneakAction = "sneak";

    public Avatar(string name)
    {
        figure = BuildFigure();
        AddChild(figure);
        animator = new PlayerAnimator(new PlayerRig(figure), GetInstanceId());
        // Breath in the cold leaves the mouth (Head joint, or a "HeadAttach" bone attachment).
        if ((figure.FindChild("HeadAttach", true, false) ?? figure.FindChild("Head", true, false)) is Node3D head)
            head.AddChild(new Breath { Position = new Vector3(0, 0.03f, 0.17f) });

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

    // Today's capsule figure, built to the PlayerRig contract: each pivot sits at its
    // joint, +Z forward, so the figure's left is +X. The Modeling lane's PlayerModel
    // replaces this later with the same pivot names.
    static Node3D BuildFigure()
    {
        skin ??= Look.Solid(Look.Skin);
        hair ??= Look.Solid(Look.Hair);
        leaves ??= Look.Solid(Look.Leaf);

        var root = new Node3D { Name = "Figure" };
        var hips = Pivot(root, Joint.Hips, new Vector3(0, 0.9f, 0));
        // Woken up with nothing but a wrap of leaves.
        Mesh(hips, new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.24f, Height = 0.18f, RadialSegments = 9 }, leaves, new Vector3(0, -0.1f, 0));

        var spine = Pivot(hips, Joint.Spine, Vector3.Zero);
        Mesh(spine, new CapsuleMesh { Radius = 0.2f, Height = 0.64f, RadialSegments = 12, Rings = 4 }, skin, new Vector3(0, 0.22f, 0));
        var neck = Pivot(spine, Joint.Neck, new Vector3(0, 0.52f, 0));
        var head = Pivot(neck, Joint.Head, new Vector3(0, 0.05f, 0));
        Mesh(head, new SphereMesh { Radius = 0.17f, Height = 0.34f, RadialSegments = 14, Rings = 7 }, skin, new Vector3(0, 0.06f, 0));
        Mesh(head, new SphereMesh { Radius = 0.18f, Height = 0.22f, RadialSegments = 14, Rings = 5 }, hair, new Vector3(0, 0.14f, -0.02f));

        var handL = Arm(spine, Joint.ShoulderL, Joint.ElbowL, Joint.HandL, 1f);
        handL.AddChild(new Node3D { Name = "ToolL", Position = new Vector3(0, -0.05f, 0) });
        var handR = Arm(spine, Joint.ShoulderR, Joint.ElbowR, Joint.HandR, -1f);
        handR.AddChild(new Node3D { Name = "ToolR", Position = new Vector3(0, -0.05f, 0) });

        Leg(hips, Joint.HipL, Joint.KneeL, Joint.FootL, 1f);
        Leg(hips, Joint.HipR, Joint.KneeR, Joint.FootR, -1f);
        return root;
    }

    static Node3D Arm(Node3D spine, Joint shoulder, Joint elbow, Joint hand, float side)
    {
        var s = Pivot(spine, shoulder, new Vector3(0.27f * side, 0.4f, 0));
        Mesh(s, new CapsuleMesh { Radius = 0.065f, Height = 0.34f, RadialSegments = 8, Rings = 2 }, skin!, new Vector3(0, -0.13f, 0));
        var e = Pivot(s, elbow, new Vector3(0, -0.27f, 0));
        Mesh(e, new CapsuleMesh { Radius = 0.06f, Height = 0.3f, RadialSegments = 8, Rings = 2 }, skin!, new Vector3(0, -0.12f, 0));
        var h = Pivot(e, hand, new Vector3(0, -0.25f, 0));
        Mesh(h, new SphereMesh { Radius = 0.065f, Height = 0.13f, RadialSegments = 8, Rings = 4 }, skin!, new Vector3(0, -0.04f, 0));
        return h;
    }

    static void Leg(Node3D hips, Joint hip, Joint knee, Joint foot, float side)
    {
        var h = Pivot(hips, hip, new Vector3(0.1f * side, -0.06f, 0));
        Mesh(h, new CapsuleMesh { Radius = 0.088f, Height = 0.46f, RadialSegments = 8, Rings = 2 }, skin!, new Vector3(0, -0.19f, 0));
        var k = Pivot(h, knee, new Vector3(0, -0.4f, 0));
        Mesh(k, new CapsuleMesh { Radius = 0.075f, Height = 0.44f, RadialSegments = 8, Rings = 2 }, skin!, new Vector3(0, -0.19f, 0));
        var f = Pivot(k, foot, new Vector3(0, -0.4f, 0));
        Mesh(f, new CapsuleMesh { Radius = 0.05f, Height = 0.24f, RadialSegments = 8, Rings = 2 }, skin!, new Vector3(0, 0.01f, 0.05f), new Vector3(Mathf.Pi / 2f, 0, 0));
    }

    static Node3D Pivot(Node3D parent, Joint joint, Vector3 at)
    {
        var n = new Node3D { Name = joint.ToString(), Position = at };
        parent.AddChild(n);
        return n;
    }

    static void Mesh(Node3D parent, Mesh mesh, Material material, Vector3 at, Vector3 rotation = default) =>
        parent.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = at, Rotation = rotation });

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
