using Godot;

namespace LifeGame;

// An isometric camera: orthographic, looking down at 30 degrees, turning in 90
// degree steps. It eases after its target so nothing ever snaps.
public partial class CameraRig : Node3D
{
    public Vector3 Focus;
    public Camera3D Camera { get; } = new()
    {
        Projection = Camera3D.ProjectionType.Orthogonal,
        Size = 15f,
        Near = 1f,
        Far = 200f,
    };

    float yawTarget = Mathf.DegToRad(45f), yaw = Mathf.DegToRad(45f);
    float sizeTarget = 15f, size = 15f;
    const float Pitch = -30f;
    const float Distance = 60f;

    public override void _Ready()
    {
        AddChild(Camera);
        Position = Focus;
    }

    // Forward and right on the ground plane, for camera-relative movement.
    public Vector3 Forward => new Vector3(-Mathf.Sin(yaw), 0, -Mathf.Cos(yaw));
    public Vector3 Right => new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));

    public void Turn(int steps) => yawTarget += steps * Mathf.Pi / 2f;

    public void Zoom(float amount) => sizeTarget = Mathf.Clamp(sizeTarget + amount, 10f, 40f);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        Position = Position.Lerp(Focus, 1f - Mathf.Exp(-6f * dt));
        yaw = Mathf.Lerp(yaw, yawTarget, 1f - Mathf.Exp(-7f * dt));
        size = Mathf.Lerp(size, sizeTarget, 1f - Mathf.Exp(-6f * dt));

        Rotation = new Vector3(Mathf.DegToRad(Pitch), yaw, 0);
        Camera.Position = new Vector3(0, 0, Distance);
        Camera.Size = size;
    }
}
