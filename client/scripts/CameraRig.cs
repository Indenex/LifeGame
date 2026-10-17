using Godot;

namespace LifeGame;

// A third-person follow camera looking down at the player, turning in 45
// degree steps. It eases after its target so nothing ever snaps.
public partial class CameraRig : Node3D
{
    // Metres; one raisable constant.
    public const float FarPlane = 1500f;
    public Vector3 Focus;
    public Camera3D Camera { get; } = new()
    {
        Fov = 45f,
        Near = 0.3f,
        // Nothing is built or drawn beyond the view radius (at most 160 m) except sky, fog and water, so a long
        // far plane costs nothing in geometry; it lets the smoke-signal columns (atmosphere) show from afar.
        Far = FarPlane,
        // The far forest softens out of focus, which sells the depth.
        Attributes = new CameraAttributesPractical
        {
            DofBlurFarEnabled = true,
            DofBlurFarDistance = 32f,
            DofBlurFarTransition = 30f,
            DofBlurAmount = 0.05f,
        },
    };

    float yawTarget = Mathf.DegToRad(45f), yaw = Mathf.DegToRad(45f);
    float distanceTarget = 16f, distance = 16f;
    // Pitch in degrees (negative looks down). Today's angle is the default; right-drag up/down
    // tilts it a little lower toward the horizon (more sky, more distance) or a little steeper.
    public const float DefaultPitch = -34f;
    public const float MinPitch = -50f, MaxPitch = -16f;
    float pitchTarget = DefaultPitch, pitch = DefaultPitch;
    // Terrain height, so a low camera never dips into a hill (set by Main).
    public System.Func<float, float, float>? GroundAt;
    const float GroundMargin = 0.9f;
    // How quickly the view catches up when it turns (Controls page; 7 is the default).
    public static float TurnEase = 7f;

    public override void _Ready()
    {
        AddChild(Camera);
        Position = Focus;
    }

    // Forward and right on the ground plane, for camera-relative movement.
    public Vector3 Forward => new Vector3(-Mathf.Sin(yaw), 0, -Mathf.Cos(yaw));
    public Vector3 Right => new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));

    public void Turn(int steps) => yawTarget += steps * Mathf.Pi / 4f;

    // Free turning, for dragging with the right mouse button (ui/CameraDrag.cs).
    public void TurnBy(float radians) => yawTarget += radians;

    // Tilt with the right mouse button dragged up or down (ui/CameraDrag.cs); radians, positive looks lower.
    public void TiltBy(float radians) => pitchTarget = Mathf.Clamp(pitchTarget - Mathf.RadToDeg(radians), MinPitch, MaxPitch);

    public void Zoom(float amount) => distanceTarget = Mathf.Clamp(distanceTarget + amount, 6f, 40f);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        Position = Position.Lerp(Focus + Vector3.Up * 1.2f * PlayerModel.Height / 1.75f, 1f - Mathf.Exp(-6f * dt));
        yaw = Mathf.Lerp(yaw, yawTarget, 1f - Mathf.Exp(-TurnEase * dt));
        distance = Mathf.Lerp(distance, distanceTarget, 1f - Mathf.Exp(-6f * dt));

        pitch = Mathf.Lerp(pitch, pitchTarget, 1f - Mathf.Exp(-TurnEase * dt));
        // Lift the view (steeper) just enough that the camera stays above the ground behind the player.
        float shown = pitch;
        if (GroundAt != null)
            for (int i = 0; i < 10; i++)
            {
                var from = Position + new Basis(Vector3.Up, yaw) * new Basis(Vector3.Right, Mathf.DegToRad(shown)) * new Vector3(0, 0, distance);
                if (from.Y >= GroundAt(from.X, from.Z) + GroundMargin || shown <= MinPitch) break;
                shown = Mathf.Max(MinPitch, shown - 3f);
            }
        Rotation = new Vector3(Mathf.DegToRad(shown), yaw, 0);
        Camera.Position = new Vector3(0, 0, distance);
    }
}
