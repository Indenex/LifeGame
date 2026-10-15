using Godot;

namespace LifeGame;

// A simple, soft-shaped person with swinging arms and legs. Remote avatars glide
// toward the last position the server sent; the local one is moved by input.
public partial class Avatar : Node3D
{
    public Vector3 Target;
    public float TargetHeading;
    public bool Remote;

    readonly Node3D body = new();
    readonly Node3D leftLeg, rightLeg, leftArm, rightArm;
    readonly Label3D nameplate;
    float walkPhase, speed;
    Vector3 lastPosition;

    static ShaderMaterial? skin, hair, leaves;

    public Avatar(string name)
    {
        skin ??= Look.Solid(Look.Skin);
        hair ??= Look.Solid(Look.Hair);
        leaves ??= Look.Solid(Look.Leaf);

        body.AddChild(Part(new CapsuleMesh { Radius = 0.2f, Height = 0.72f, RadialSegments = 12, Rings = 4 }, skin, new Vector3(0, 1.02f, 0)));
        body.AddChild(Part(new SphereMesh { Radius = 0.17f, Height = 0.34f, RadialSegments = 14, Rings = 7 }, skin, new Vector3(0, 1.52f, 0)));
        body.AddChild(Part(new SphereMesh { Radius = 0.18f, Height = 0.22f, RadialSegments = 14, Rings = 5 }, hair, new Vector3(0, 1.6f, -0.02f)));
        // Woken up with nothing but a wrap of leaves.
        body.AddChild(Part(new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.24f, Height = 0.18f, RadialSegments = 9 }, leaves, new Vector3(0, 0.78f, 0)));

        leftLeg = Limb(new Vector3(-0.1f, 0.74f, 0), 0.085f, 0.72f);
        rightLeg = Limb(new Vector3(0.1f, 0.74f, 0), 0.085f, 0.72f);
        leftArm = Limb(new Vector3(-0.27f, 1.3f, 0), 0.065f, 0.6f);
        rightArm = Limb(new Vector3(0.27f, 1.3f, 0), 0.065f, 0.6f);
        AddChild(body);

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

    static MeshInstance3D Part(Mesh mesh, Material material, Vector3 at) =>
        new() { Mesh = mesh, MaterialOverride = material, Position = at };

    // A limb hangs from a pivot at the hip or shoulder so it can swing.
    Node3D Limb(Vector3 pivot, float radius, float length)
    {
        var joint = new Node3D { Position = pivot };
        joint.AddChild(Part(new CapsuleMesh { Radius = radius, Height = length, RadialSegments = 8, Rings = 2 }, skin!, new Vector3(0, -length / 2f + radius, 0)));
        body.AddChild(joint);
        return joint;
    }

    public void SetPlayerName(string name) => nameplate.Text = name;

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
        speed = Mathf.Lerp(speed, Mathf.Min(flat.Length() / Mathf.Max(dt, 1e-4f), 6f), 1f - Mathf.Exp(-8f * dt));
        float stride = Mathf.Clamp(speed / 3.5f, 0f, 1f);
        walkPhase += speed * dt * 2.4f;

        float swing = Mathf.Sin(walkPhase) * 0.55f * stride;
        leftLeg.Rotation = new Vector3(swing, 0, 0);
        rightLeg.Rotation = new Vector3(-swing, 0, 0);
        leftArm.Rotation = new Vector3(-swing * 0.8f, 0, 0.08f);
        rightArm.Rotation = new Vector3(swing * 0.8f, 0, -0.08f);

        // Breathe when idle, bob when walking.
        float idle = Mathf.Sin(Time.GetTicksMsec() / 1000f * 1.6f + GetInstanceId() % 7) * 0.008f * (1f - stride);
        body.Position = new Vector3(0, Mathf.Abs(Mathf.Cos(walkPhase)) * 0.04f * stride + idle, 0);
    }
}
