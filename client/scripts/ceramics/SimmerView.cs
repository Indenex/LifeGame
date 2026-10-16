using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A coil pot of broth or stew simmering at a fire's edge (ceramics-7): the fired
// pot, the dish's surface just below the rim, and a thread of steam. Close by, a
// label counts down, then says it's ready.
public partial class SimmerView : Node3D
{
    public static readonly Color Broth = new(0.42f, 0.12f, 0.16f);
    public static readonly Color Stew = new(0.40f, 0.26f, 0.13f);

    readonly MeshInstance3D body = new();
    readonly MeshInstance3D surface = new();
    readonly CpuParticles3D steam = Steam();
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
    };
    readonly MeshInstance3D floaters = new();
    Item dish;
    double putAt;

    public double SecondsLeft => Math.Max(0, putAt + ClayRules.SimmerSeconds(dish) - PotView.Now);

    public bool ShowLabel { set => label.Visible = value; }

    public SimmerView()
    {
        body.Scale = Vector3.One * PotView.Size;
        AddChild(body);
        // Modeling's stones the pot sits on (unscaled; built in the pot's own frame).
        AddChild(new MeshInstance3D { Mesh = CookingMeshes.Under });
        AddChild(surface);
        AddChild(steam);
        AddChild(label);
    }

    public void Show(ulong id, Item dish, double putAt)
    {
        this.dish = dish;
        this.putAt = putAt;
        body.Mesh = PotMesh.For(ClayForm.CoilPot, id);
        body.MaterialOverride = ClayLook.For(ClayStage.Fired, FireKind.Pit, 0f, id);
        var line = PotMesh.WaterLine(ClayForm.CoilPot) * PotView.Size;
        surface.Mesh = new CylinderMesh { TopRadius = line.X, BottomRadius = line.X, Height = 0.004f, RadialSegments = 24 };
        surface.MaterialOverride = Look.Solid(dish == Item.PotOfStew ? Stew : Broth);
        surface.Position = new Vector3(0, line.Y, 0);
        // What floats in it, just above the liquid disc.
        floaters.Mesh = CookingMeshes.Floaters(dish == Item.PotOfStew ? CookingMeshes.Dish.Stew : dish == Item.Tallow ? CookingMeshes.Dish.Tallow : CookingMeshes.Dish.Broth, line.X);
        floaters.Position = new Vector3(0, line.Y + 0.002f, 0);
        if (floaters.GetParent() == null) AddChild(floaters);
        steam.Position = new Vector3(0, PotMesh.Height(ClayForm.CoilPot) * PotView.Size, 0);
        steam.Emitting = true;
        label.Position = new Vector3(0, PotMesh.Height(ClayForm.CoilPot) * PotView.Size + 0.3f, 0);
        Rotation = new Vector3(0, (id % 97) / 97f * Mathf.Tau, 0);
    }

    public override void _Process(double delta)
    {
        if (!label.Visible) return;
        int left = (int)Math.Ceiling(SecondsLeft);
        string name = ClayRules.DishName(dish);
        label.Text = left > 0 ? $"Simmering {name}  {left / 60}:{left % 60:00}" : $"The {name} is ready";
    }

    static CpuParticles3D Steam()
    {
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            VertexColorUseAsAlbedo = true,
            AlbedoTexture = new GradientTexture2D
            {
                Width = 32,
                Height = 32,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f),
                FillTo = new Vector2(0.5f, 0f),
                Gradient = new Gradient
                {
                    Offsets = new[] { 0f, 1f },
                    Colors = new[] { new Color(1, 1, 1, 0.7f), new Color(1, 1, 1, 0) },
                },
            },
        };
        var ramp = new Gradient();
        ramp.SetColor(0, new Color(0.95f, 0.95f, 0.92f, 0.0f));
        ramp.AddPoint(0.2f, new Color(0.95f, 0.95f, 0.92f, 0.35f));
        ramp.SetColor(ramp.GetPointCount() - 1, new Color(1f, 1f, 1f, 0f));
        return new CpuParticles3D
        {
            Amount = 10,
            Lifetime = 2.2f,
            Emitting = false,
            Mesh = new QuadMesh { Size = Vector2.One * 0.14f, Material = material },
            Direction = Vector3.Up,
            Spread = 12f,
            Gravity = new Vector3(0.04f, 0.08f, 0.02f),
            InitialVelocityMin = 0.15f,
            InitialVelocityMax = 0.3f,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 0.05f,
            ScaleAmountMin = 0.8f,
            ScaleAmountMax = 2.0f,
            ColorRamp = ramp,
        };
    }
}
