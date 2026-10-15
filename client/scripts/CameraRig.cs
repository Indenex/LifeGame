using Godot;

namespace LifeGame;

// A third-person follow camera looking down at the player, turning in 45
// degree steps. It eases after its target so nothing ever snaps.
public partial class CameraRig : Node3D
{
    public Vector3 Focus;
    public Camera3D Camera { get; } = new()
    {
        Fov = 45f,
        Near = 0.3f,
        Far = 250f,
    };

    float yawTarget = Mathf.DegToRad(45f), yaw = Mathf.DegToRad(45f);
    float distanceTarget = 16f, distance = 16f;
    const float Pitch = -34f;

    public override void _Ready()
    {
        AddChild(Camera);
        Position = Focus;
    }

    // Forward and right on the ground plane, for camera-relative movement.
    public Vector3 Forward => new Vector3(-Mathf.Sin(yaw), 0, -Mathf.Cos(yaw));
    public Vector3 Right => new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));

    public void Turn(int steps) => yawTarget += steps * Mathf.Pi / 4f;

    public void Zoom(float amount) => distanceTarget = Mathf.Clamp(distanceTarget + amount, 6f, 40f);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        Position = Position.Lerp(Focus + Vector3.Up * 1.2f, 1f - Mathf.Exp(-6f * dt));
        yaw = Mathf.Lerp(yaw, yawTarget, 1f - Mathf.Exp(-7f * dt));
        distance = Mathf.Lerp(distance, distanceTarget, 1f - Mathf.Exp(-6f * dt));

        Rotation = new Vector3(Mathf.DegToRad(Pitch), yaw, 0);
        Camera.Position = new Vector3(0, 0, distance);
    }
}
