using Godot;

namespace LifeGame;

// The arrow a shot sends: a thin dark shaft that flies straight to where the animal
// was and vanishes. The server has already decided the hit; this is only the picture.
// Modeling's ItemModels.ArrowFlight mesh (PR #235) replaces the plain shaft when it lands.
public partial class ArrowFlight : MeshInstance3D
{
    const float Speed = 55f;
    Vector3 to;
    float left;

    public static void Launch(Node parent, Vector3 from, Vector3 target)
    {
        float distance = from.DistanceTo(target);
        if (distance < 0.5f) return;
        var arrow = new ArrowFlight
        {
            Mesh = new CylinderMesh { TopRadius = 0.01f, BottomRadius = 0.01f, Height = 0.7f, RadialSegments = 4 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.30f, 0.22f, 0.14f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            to = target,
            left = distance / Speed,
        };
        parent.AddChild(arrow);
        arrow.GlobalPosition = from;
        // The cylinder runs along Y; turn it so it points at the target.
        var dir = (target - from).Normalized();
        var up = Mathf.Abs(dir.Y) > 0.99f ? Vector3.Right : Vector3.Up;
        arrow.LookAt(target, up);
        arrow.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2f);
    }

    public override void _Process(double delta)
    {
        left -= (float)delta;
        if (left <= 0f) { QueueFree(); return; }
        GlobalPosition = GlobalPosition.MoveToward(to, Speed * (float)delta);
    }
}
