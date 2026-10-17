using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Leaves and blossom drifting down around the player (atmosphere-24, Justin's
// pick): autumn leaves under the broadleaf trees, pale petals in spring, more when
// the wind gets up, all blown the way the wind goes. One small GPU particle system
// per entry in shared/SeasonEvents.cs, only in a box round the player, so it costs
// next to nothing and nothing at all out of season.
//
// Command line: --leaves=1 holds every entry at full, whatever the season, for
// screenshots (combine with --season=0.62 for autumn colours on the trees).
public partial class FallingLeaves : Node3D
{
    // The box they come down through: about 28 m across, from the crowns (8 to 14 m
    // up) to the ground.
    const float Reach = 14f, Height = 11f;

    readonly GpuParticles3D[] systems = new GpuParticles3D[SeasonEvents.All.Length];
    readonly ParticleProcessMaterial[] processes = new ParticleProcessMaterial[SeasonEvents.All.Length];
    float? argLeaves;

    public override void _Ready()
    {
        argLeaves = float.TryParse(Arg("leaves"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
        for (int i = 0; i < systems.Length; i++)
        {
            (systems[i], processes[i]) = Build(SeasonEvents.All[i]);
            AddChild(systems[i]);
        }
    }

    // year 0 to 1, wind 0 to 1, woods 0 open to 1 deep woods; windDir the way it blows.
    public void Follow(Vector3 focus, float year, float wind, float woods, Vector2 windDir)
    {
        GlobalPosition = focus + new Vector3(0f, Height, 0f);
        // Light pieces ride the wind: a gentle drift in still air, swept along in a gale.
        float push = 0.3f + wind * 2.2f;
        for (int i = 0; i < systems.Length; i++)
        {
            float amount = argLeaves ?? SeasonEvents.Strength(SeasonEvents.All[i], year, wind, woods);
            systems[i].Emitting = amount > 0.02f;
            systems[i].AmountRatio = Mathf.Clamp(amount, 0f, 1f);
            processes[i].Gravity = new Vector3(windDir.X * push, -1.1f, windDir.Y * push);
        }
    }

    static (GpuParticles3D, ParticleProcessMaterial) Build(SeasonEventDef e)
    {
        // A stepped ramp, so each piece takes one of the entry's colours at random.
        var offsets = new float[e.Colors.Length];
        var tints = new Color[e.Colors.Length];
        for (int c = 0; c < e.Colors.Length; c++)
        {
            offsets[c] = c / (float)e.Colors.Length;
            tints[c] = new Color(e.Colors[c][0], e.Colors[c][1], e.Colors[c][2]);
        }
        var colors = new Gradient { Offsets = offsets, Colors = tints, InterpolationMode = Gradient.InterpolationModeEnum.Constant };

        // Each piece shrinks away in its last moments instead of popping out.
        var size = new Curve();
        size.AddPoint(new Vector2(0f, 0f));
        size.AddPoint(new Vector2(0.05f, 1f));
        size.AddPoint(new Vector2(0.85f, 1f));
        size.AddPoint(new Vector2(1f, 0f));

        var process = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(Reach, 3f, Reach),
            Direction = Vector3.Down,
            Spread = 40f,
            InitialVelocityMin = 0.1f,
            InitialVelocityMax = 0.4f,
            Gravity = new Vector3(0f, -1.1f, 0f),
            // Air drag holds them to a slow fall, about a metre a second.
            DampingMin = 1.2f,
            DampingMax = 1.8f,
            // Flutter and swirl as they come down.
            TurbulenceEnabled = true,
            TurbulenceNoiseStrength = 1.2f,
            TurbulenceNoiseScale = 3f,
            TurbulenceInfluenceMin = 0.08f,
            TurbulenceInfluenceMax = 0.2f,
            AngleMin = 0f,
            AngleMax = 360f,
            AngularVelocityMin = -160f,
            AngularVelocityMax = 160f,
            ScaleMin = 0.75f,
            ScaleMax = 1.25f,
            ScaleCurve = new CurveTexture { Curve = size },
            ColorInitialRamp = new GradientTexture1D { Gradient = colors },
        };
        var particles = new GpuParticles3D
        {
            Amount = 120,
            Lifetime = 12f,
            LocalCoords = false,
            Emitting = false,
            Preprocess = 6f,
            VisibilityAabb = new Aabb(new Vector3(-Reach - 10f, -Height - 4f, -Reach - 10f), new Vector3(Reach * 2f + 20f, Height + 10f, Reach * 2f + 20f)),
            ProcessMaterial = process,
            DrawPass1 = new QuadMesh
            {
                Size = new Vector2(e.Size, e.Size * 0.65f),
                Material = new StandardMaterial3D
                {
                    VertexColorUseAsAlbedo = true,
                    BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    Roughness = 1f,
                },
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        return (particles, process);
    }

    static string? Arg(string name)
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith($"--{name}="))
                return arg[(name.Length + 3)..];
        foreach (var arg in OS.GetCmdlineArgs())
            if (arg.StartsWith($"--{name}="))
                return arg[(name.Length + 3)..];
        return null;
    }
}
