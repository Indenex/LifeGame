using Godot;

namespace LifeGame;

// Streaks of rain falling around the player, thick or thin with the weather and
// slanted by the wind.
public partial class Rain : Node3D
{
    readonly StandardMaterial3D material = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        BillboardMode = BaseMaterial3D.BillboardModeEnum.FixedY,
        AlbedoColor = new Color(0.78f, 0.82f, 0.88f, 0.3f),
        DisableReceiveShadows = true,
    };
    readonly ParticleProcessMaterial process = new()
    {
        EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
        EmissionBoxExtents = new Vector3(22f, 0.5f, 22f),
        Direction = Vector3.Down,
        Spread = 2f,
        InitialVelocityMin = 17f,
        InitialVelocityMax = 21f,
        Gravity = new Vector3(0, -6f, 0),
        ScaleMin = 0.7f,
        ScaleMax = 1.2f,
    };
    readonly GpuParticles3D drops;

    public Rain()
    {
        drops = new GpuParticles3D
        {
            Amount = 9000,
            Lifetime = 1.1f,
            LocalCoords = false,
            Emitting = false,
            Preprocess = 1.2f,
            VisibilityAabb = new Aabb(new Vector3(-30, -30, -30), new Vector3(60, 50, 60)),
            ProcessMaterial = process,
            DrawPass1 = new QuadMesh { Size = new Vector2(0.022f, 0.75f), Material = material },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(drops);
    }

    // amount and wind run from 0 to 1; light dims the drops at night.
    public void Follow(Vector3 focus, float amount, float wind, float light)
    {
        GlobalPosition = focus + new Vector3(0, 14f, 0);
        drops.Emitting = amount > 0.02f;
        drops.AmountRatio = Mathf.Clamp(amount, 0f, 1f);
        var slant = new Vector3(0.6f, 0f, 0.8f) * (0.04f + wind * 0.22f);
        process.Direction = (Vector3.Down + slant).Normalized();
        material.AlbedoColor = new Color(0.78f * light, 0.82f * light, 0.9f * light, 0.18f + 0.14f * amount);
    }
}
