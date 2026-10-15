using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// An updraft kiln: a beehive of sun-dried bricks with a short chimney and a low
// door facing whoever built it. Cold, it's pale brick with a sooty inside; firing,
// the door and chimney glow and smoke rises. A label shows its load and time left.
public partial class KilnView : Node3D
{
    const float Radius = 0.78f;     // outer radius at the ground
    const float DomeHeight = 1.15f;
    const float Course = 0.095f;    // one row of bricks
    const float DoorHalfAngle = 0.42f;
    const float DoorHeight = 0.5f;
    const float ChimneyRadius = 0.2f;
    const int ChimneyCourses = 5;

    static readonly Color Brick = new(0.66f, 0.53f, 0.40f);
    static readonly Color Soot = new(0.09f, 0.07f, 0.06f);

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

    public KilnView()
    {
        AddChild(BrickWork(1234));
        AddChild(new MeshInstance3D
        {
            // The sooty inside, seen through the door and between the bricks.
            Mesh = new SphereMesh { Radius = Radius * 0.93f, Height = DomeHeight * 1.86f, RadialSegments = 20, Rings = 10 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = Soot, CullMode = BaseMaterial3D.CullModeEnum.Front, Roughness = 1f },
        });
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
                ? "Kiln, empty: V loads dry pots"
                : $"Kiln: {load} of {ClayRules.KilnCapacity} pots. V adds more, or fires it";
    }

    // Courses of bricks laid round a beehive dome, leaving a door, then a chimney.
    static MultiMeshInstance3D BrickWork(int seed)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)seed };
        var bricks = new System.Collections.Generic.List<(Transform3D, Color)>();
        int courses = Mathf.CeilToInt(DomeHeight / Course);
        for (int c = 0; c < courses; c++)
        {
            float y = c * Course + Course * 0.5f;
            float s = Mathf.Clamp(y / DomeHeight, 0f, 1f);
            float r = Mathf.Max(ChimneyRadius + 0.02f, Radius * Mathf.Sqrt(1f - s * s * s));
            // Slope of the dome here, so each brick leans in with the wall.
            float lean = Mathf.Atan2(Radius * 1.5f * s * s / Mathf.Max(0.2f, Mathf.Sqrt(1f - s * s * s)), DomeHeight) * 0.9f;
            Lay(bricks, rng, y, r, lean, c, door: y < DoorHeight);
        }
        for (int c = 0; c < ChimneyCourses; c++)
            Lay(bricks, rng, DomeHeight + c * Course + Course * 0.5f, ChimneyRadius, 0f, courses + c, door: false, small: true);

        var mm = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = BrickMesh,
            InstanceCount = bricks.Count,
        };
        for (int i = 0; i < bricks.Count; i++)
        {
            mm.SetInstanceTransform(i, bricks[i].Item1);
            mm.SetInstanceColor(i, bricks[i].Item2);
        }
        return new MultiMeshInstance3D { Multimesh = mm };
    }

    static void Lay(System.Collections.Generic.List<(Transform3D, Color)> bricks, RandomNumberGenerator rng,
        float y, float r, float lean, int course, bool door, bool small = false)
    {
        float length = small ? 0.16f : 0.25f;
        int count = Math.Max(5, Mathf.FloorToInt(Mathf.Tau * r / length));
        float offset = course % 2 == 0 ? 0f : 0.5f;  // stagger the joints
        for (int i = 0; i < count; i++)
        {
            float a = (i + offset) / count * Mathf.Tau;
            float fromDoor = Mathf.Abs(Mathf.Wrap(a, -Mathf.Pi, Mathf.Pi));
            if (door && fromDoor < DoorHalfAngle) continue;
            var basis = new Basis(Vector3.Up, a) * new Basis(Vector3.Right, -lean);
            float stretch = Mathf.Tau * r / count / 0.24f * 0.96f;
            basis *= Basis.FromScale(new Vector3(stretch * (small ? 0.75f : 1f), 1f, small ? 0.8f : 1f));
            var at = new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r);
            float shade = rng.RandfRange(0.86f, 1.08f);
            var tint = new Color(Brick.R * shade, Brick.G * shade * rng.RandfRange(0.97f, 1.02f), Brick.B * shade);
            // Bricks round the door and near the top are smoke-stained.
            float stain = Mathf.Clamp((door ? 0f : Mathf.Max(0f, 0.6f - fromDoor) * 0.8f) + (small ? 0.35f : 0f), 0f, 0.6f);
            bricks.Add((new Transform3D(basis, at), tint.Lerp(Soot, stain)));
        }
    }

    static Mesh? brickMesh;
    static Mesh BrickMesh => brickMesh ??= MakeBrick();

    static Mesh MakeBrick()
    {
        var mesh = (ArrayMesh)Look.Faceted(new BoxMesh { Size = new Vector3(0.24f, Course * 0.9f, 0.12f) }, Colors.White);
        return mesh;
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
