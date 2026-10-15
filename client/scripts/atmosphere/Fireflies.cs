using Godot;

namespace LifeGame;

// Soft green-gold lights drifting low over the grass on clear nights.
public partial class Fireflies : Node3D
{
    readonly GpuParticles3D flies;

    public Fireflies()
    {
        var curve = new Curve();
        curve.AddPoint(new Vector2(0f, 0f));
        curve.AddPoint(new Vector2(0.2f, 1f));
        curve.AddPoint(new Vector2(0.75f, 1f));
        curve.AddPoint(new Vector2(1f, 0f));
        flies = new GpuParticles3D
        {
            Amount = 70,
            Lifetime = 7f,
            LocalCoords = false,
            Emitting = false,
            Preprocess = 7f,
            VisibilityAabb = new Aabb(new Vector3(-20, -5, -20), new Vector3(40, 10, 40)),
            ProcessMaterial = new ParticleProcessMaterial
            {
                EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
                EmissionBoxExtents = new Vector3(16f, 0.8f, 16f),
                Direction = Vector3.Up,
                Spread = 180f,
                InitialVelocityMin = 0.1f,
                InitialVelocityMax = 0.35f,
                Gravity = Vector3.Zero,
                TurbulenceEnabled = true,
                TurbulenceNoiseStrength = 1.5f,
                TurbulenceNoiseScale = 4f,
                TurbulenceInfluenceMin = 0.05f,
                TurbulenceInfluenceMax = 0.12f,
                ScaleCurve = new CurveTexture { Curve = curve },
            },
            DrawPass1 = new SphereMesh
            {
                Radius = 0.035f,
                Height = 0.07f,
                RadialSegments = 6,
                Rings = 3,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.1f, 0.12f, 0.05f),
                    EmissionEnabled = true,
                    Emission = new Color(0.75f, 1f, 0.35f),
                    EmissionEnergyMultiplier = 6f,
                },
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(flies);
    }

    public void Follow(Vector3 focus, float amount)
    {
        GlobalPosition = focus + new Vector3(0, 1.1f, 0);
        flies.Emitting = amount > 0.05f;
        flies.AmountRatio = Mathf.Clamp(amount, 0f, 1f);
    }
}
