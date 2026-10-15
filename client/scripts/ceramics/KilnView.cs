using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// An updraft kiln: a beehive of sun-dried bricks with a short chimney and a low
// door facing whoever built it. Cold, it's pale brick with a sooty inside; firing,
// the door and chimney glow and smoke rises. A label shows its load and time left.
public partial class KilnView : Node3D
{
    const float Radius = KilnModel.Radius;
    const float DomeHeight = KilnModel.DomeHeight;
    const float Course = KilnModel.Course;
    const int ChimneyCourses = KilnModel.ChimneyCourses;

    readonly OmniLight3D inner = new()
    {
        LightColor = new Color(1.0f, 0.5f, 0.2f),
        OmniRange = 4.5f,
        OmniAttenuation = 1.3f,
        ShadowEnabled = true,
        Position = new Vector3(0, 0.35f, Radius * 0.9f),
        LightEnergy = 0f,
    };
    readonly MeshInstance3D embers = new();
    readonly CpuParticles3D smoke = Smoke();
    readonly Label3D label = new()
    {
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        FontSize = 22,
        PixelSize = 0.005f,
        Modulate = Look.Cream,
        OutlineSize = 6,
        OutlineModulate = new Color(0.1f, 0.1f, 0.08f, 0.55f),
        Visible = false,
        NoDepthTest = true,
        Position = new Vector3(0, DomeHeight + ChimneyCourses * Course + 0.35f, 0),
    };
    readonly StandardMaterial3D emberMaterial = new()
    {
        AlbedoColor = new Color(0.12f, 0.06f, 0.04f),
        EmissionEnabled = true,
        Emission = new Color(1f, 0.38f, 0.1f),
        EmissionEnergyMultiplier = 0f,
    };

    int load;
    bool firing;
    double fireStart, fireFinish;
    float time;

    // In the world the brickwork is the finished structure, drawn by Building's
    // StructureView; this adds the fire, smoke and label. The offline demo has no
    // structure, so it asks for the bricks too.
    public KilnView(bool withBricks = false)
    {
        if (withBricks)
        {
            AddChild(new MeshInstance3D { Mesh = KilnModel.Interior });
            foreach (var course in KilnModel.Courses)
                AddChild(new MeshInstance3D { Mesh = course });
        }
        embers.Mesh = new CylinderMesh { TopRadius = Radius * 0.7f, BottomRadius = Radius * 0.7f, Height = 0.06f, RadialSegments = 16 };
        embers.MaterialOverride = emberMaterial;
        embers.Position = new Vector3(0, 0.03f, 0);
        AddChild(embers);
        smoke.Position = new Vector3(0, DomeHeight + ChimneyCourses * Course, 0);
        AddChild(smoke);
        AddChild(inner);
        AddChild(label);
    }

    public bool ShowLabel { set => label.Visible = value; }
    public int Load => load;
    public bool IsFiring => firing;

    public void Show(int pots, bool isFiring, double start, double finish)
    {
        load = pots;
        firing = isFiring;
        fireStart = start;
        fireFinish = finish;
        smoke.Emitting = firing;
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        double now = PotView.Now;
        float heat = 0f;
        if (firing)
        {
            float t = (float)Math.Clamp((now - fireStart) / Math.Max(fireFinish - fireStart, 1), 0, 1);
            // Builds up quickly, holds, eases off at the end as it cools.
            heat = Mathf.Min(1f, t * 6f) * (1f - 0.6f * Mathf.Max(0f, (t - 0.85f) / 0.15f));
        }
        float flicker = 0.85f + 0.1f * Mathf.Sin(time * 11.3f) + 0.05f * Mathf.Sin(time * 19.1f + 0.7f);
        inner.LightEnergy = 3.2f * heat * flicker;
        emberMaterial.EmissionEnergyMultiplier = 1.2f * heat * flicker;

        if (!label.Visible) return;
        if (firing)
        {
            int left = (int)Math.Max(0, fireFinish - now);
            label.Text = $"Kiln firing  {left / 60}:{left % 60:00}";
        }
        else
            label.Text = load == 0
                ? "Kiln, empty: bring dry pots"
                : $"Kiln: {load} of {ClayRules.KilnCapacity} pots";
    }

    static CpuParticles3D Smoke()
    {
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            VertexColorUseAsAlbedo = true,
            AlbedoTexture = new GradientTexture2D
            {
                Width = 64,
                Height = 64,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f),
                FillTo = new Vector2(0.5f, 0f),
                Gradient = new Gradient
                {
                    Offsets = new[] { 0f, 1f },
                    Colors = new[] { new Color(1, 1, 1, 0.8f), new Color(1, 1, 1, 0) },
                },
            },
        };
        var ramp = new Gradient();
        ramp.SetColor(0, new Color(0.35f, 0.33f, 0.31f, 0.0f));
        ramp.AddPoint(0.15f, new Color(0.38f, 0.36f, 0.34f, 0.55f));
        ramp.SetColor(ramp.GetPointCount() - 1, new Color(0.7f, 0.7f, 0.7f, 0f));
        return new CpuParticles3D
        {
            Amount = 24,
            Lifetime = 5f,
            Emitting = false,
            Mesh = new QuadMesh { Size = Vector2.One * 0.45f, Material = material },
            Direction = Vector3.Up,
            Spread = 10f,
            Gravity = new Vector3(0.12f, 0.15f, 0.05f),
            InitialVelocityMin = 0.35f,
            InitialVelocityMax = 0.6f,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 0.1f,
            ScaleAmountMin = 0.8f,
            ScaleAmountMax = 2.4f,
            ColorRamp = ramp,
        };
    }
}
