using Godot;

namespace LifeGame;

// A fixed-angle 2.5D camera that eases after its target and turns in 90 degree
// steps. All motion is damped so nothing ever snaps.
public partial class CameraRig : Node3D
{
    public Vector3 Focus;
    public Camera3D Camera { get; } = new() { Fov = 32f, Far = 400f };

    float yawTarget = Mathf.DegToRad(45f), yaw = Mathf.DegToRad(45f);
    float distanceTarget = 22f, distance = 22f;
    const float Pitch = -42f;

    public override void _Ready()
    {
        AddChild(Camera);
        Position = Focus;
    }

    // Forward and right on the ground plane, for camera-relative movement.
    public Vector3 Forward => new Vector3(-Mathf.Sin(yaw), 0, -Mathf.Cos(yaw));
    public Vector3 Right => new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));

    public void Turn(int steps) => yawTarget += steps * Mathf.Pi / 2f;

    public void Zoom(float amount) => distanceTarget = Mathf.Clamp(distanceTarget + amount, 10f, 40f);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        Position = Position.Lerp(Focus, 1f - Mathf.Exp(-6f * dt));
        yaw = Mathf.Lerp(yaw, yawTarget, 1f - Mathf.Exp(-7f * dt));
        distance = Mathf.Lerp(distance, distanceTarget, 1f - Mathf.Exp(-6f * dt));

        Rotation = new Vector3(Mathf.DegToRad(Pitch), yaw, 0);
        Camera.Position = new Vector3(0, 0, distance);
    }
}
