using Godot;

namespace LifeGame;

// A small campfire: a ring of stones, crossed logs, soft flames, rising sparks
// and a warm light that breathes. Stands in for crafted fires until those exist.
public partial class Campfire : Node3D
{
    static readonly Color Flame = new(1.0f, 0.55f, 0.18f);
    static readonly Color Ember = new(1.0f, 0.72f, 0.35f);
    static readonly Color Char = new(0.18f, 0.13f, 0.11f);

    static Mesh? stoneMesh, logMesh;
    static StandardMaterial3D? flameMaterial, smokeMaterial;

    readonly OmniLight3D light = new()
    {
        LightColor = new Color(1.0f, 0.62f, 0.32f),
        LightEnergy = 2.2f,
        OmniRange = 8f,
        OmniAttenuation = 1.4f,
        ShadowEnabled = true,
        LightVolumetricFogEnergy = 0.6f,
        Position = new Vector3(0, 0.6f, 0),
    };
    readonly float seed;
    float time;

    public Campfire(ulong id)
    {
        seed = (id % 97) * 1.37f;
        stoneMesh ??= Look.Faceted(new SphereMesh { Radius = 0.13f, Height = 0.16f, RadialSegments = 6, Rings = 3 }, Look.Rock);
        logMesh ??= Look.Faceted(new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.07f, Height = 0.75f, RadialSegments = 6 }, Char.Lerp(Look.Bark, 0.5f));

        for (int i = 0; i < 9; i++)
        {
            float a = i * Mathf.Tau / 9f + Terrain.Hash01(i, (int)id, 5) * 0.3f;
            float scale = 0.8f + Terrain.Hash01(i, (int)id, 6) * 0.5f;
            AddChild(new MeshInstance3D
            {
                Mesh = stoneMesh,
                Position = new Vector3(Mathf.Cos(a) * 0.48f, 0.04f, Mathf.Sin(a) * 0.48f),
                Rotation = new Vector3(0, a * 2.3f, 0),
                Scale = Vector3.One * scale,
            });
        }
        // Logs leaning together over the coals.
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.Tau / 3f + 0.4f;
            AddChild(new MeshInstance3D
            {
                Mesh = logMesh,
                Position = new Vector3(Mathf.Cos(a) * 0.16f, 0.16f, Mathf.Sin(a) * 0.16f),
                Rotation = new Vector3(0, -a, Mathf.DegToRad(62f)),
            });
        }
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.2f, Height = 0.12f, RadialSegments = 10, Rings = 3 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = Char,
                EmissionEnabled = true,
                Emission = Ember,
                EmissionEnergyMultiplier = 1.6f,
            },
            Position = new Vector3(0, 0.03f, 0),
        });

        AddChild(Flames());
        AddChild(Sparks());
        AddChild(Smoke());
        AddChild(light);
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        // Two slow waves and a quick one, so the glow breathes rather than strobes.
        float flicker = Mathf.Sin(time * 2.1f + seed) * 0.18f + Mathf.Sin(time * 5.3f + seed * 2f) * 0.1f + Mathf.Sin(time * 13f) * 0.05f;
        light.LightEnergy = 2.2f + flicker;
        light.Position = new Vector3(Mathf.Sin(time * 3.1f) * 0.04f, 0.6f + flicker * 0.08f, Mathf.Cos(time * 2.7f) * 0.04f);
    }

    static CpuParticles3D Flames() => new()
    {
        Amount = 28,
        Lifetime = 0.7f,
        Mesh = new QuadMesh { Size = new Vector2(0.24f, 0.32f), Material = FlameMaterial },
        EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
        EmissionSphereRadius = 0.14f,
        Direction = Vector3.Up,
        Spread = 12f,
        Gravity = new Vector3(0, 1.2f, 0),
        InitialVelocityMin = 0.4f,
        InitialVelocityMax = 0.8f,
        ScaleAmountMin = 0.7f,
        ScaleAmountMax = 1.3f,
        ScaleAmountCurve = Fade(1f, 0.2f),
        ColorRamp = Ramp(new Color(1f, 0.85f, 0.5f, 0.95f), new Color(Flame, 0.8f), new Color(0.8f, 0.2f, 0.05f, 0f)),
        Position = new Vector3(0, 0.12f, 0),
    };

    static CpuParticles3D Sparks() => new()
    {
        Amount = 10,
        Lifetime = 1.8f,
        Mesh = new QuadMesh { Size = new Vector2(0.03f, 0.03f), Material = FlameMaterial },
        EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
        EmissionSphereRadius = 0.15f,
        Direction = Vector3.Up,
        Spread = 25f,
        Gravity = new Vector3(0, 0.3f, 0),
        InitialVelocityMin = 0.6f,
        InitialVelocityMax = 1.4f,
        TangentialAccelMin = -0.4f,
        TangentialAccelMax = 0.4f,
        ColorRamp = Ramp(new Color(1f, 0.9f, 0.6f, 1f), new Color(Ember, 0.8f), new Color(Flame, 0f)),
        Position = new Vector3(0, 0.3f, 0),
    };

    static CpuParticles3D Smoke() => new()
    {
        Amount = 12,
        Lifetime = 4f,
        Mesh = new QuadMesh { Size = new Vector2(0.5f, 0.5f), Material = SmokeMaterial },
        Direction = Vector3.Up,
        Spread = 10f,
        Gravity = new Vector3(0.12f, 0.2f, 0.05f),
        InitialVelocityMin = 0.3f,
        InitialVelocityMax = 0.5f,
        ScaleAmountMin = 0.8f,
        ScaleAmountMax = 1.4f,
        ScaleAmountCurve = Fade(0.6f, 2.4f),
        ColorRamp = Ramp(new Color(0.5f, 0.48f, 0.45f, 0f), new Color(0.55f, 0.53f, 0.5f, 0.22f), new Color(0.6f, 0.6f, 0.6f, 0f)),
        Position = new Vector3(0, 0.7f, 0),
    };

    static StandardMaterial3D FlameMaterial => flameMaterial ??= new StandardMaterial3D
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        BlendMode = BaseMaterial3D.BlendModeEnum.Add,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
        VertexColorUseAsAlbedo = true,
        AlbedoTexture = SoftDot(),
        DisableReceiveShadows = true,
    };

    static StandardMaterial3D SmokeMaterial => smokeMaterial ??= new StandardMaterial3D
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
        VertexColorUseAsAlbedo = true,
        AlbedoTexture = SoftDot(),
        DisableReceiveShadows = true,
    };

    // A round, soft-edged dot used for every particle.
    static GradientTexture2D SoftDot()
    {
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(1, 1, 1, 1));
        gradient.SetColor(1, new Color(1, 1, 1, 0));
        return new GradientTexture2D
        {
            Gradient = gradient,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(0.5f, 0f),
            Width = 64,
            Height = 64,
        };
    }

    static Gradient Ramp(Color start, Color middle, Color end)
    {
        var gradient = new Gradient();
        gradient.SetColor(0, start);
        gradient.SetColor(1, end);
        gradient.AddPoint(0.4f, middle);
        return gradient;
    }

    static Curve Fade(float from, float to)
    {
        var curve = new Curve { MaxValue = 3f };
        curve.AddPoint(new Vector2(0, from));
        curve.AddPoint(new Vector2(1, to));
        return curve;
    }
}
