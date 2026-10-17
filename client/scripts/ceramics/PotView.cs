using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// One piece of worked clay in the world. Its color follows its moisture as it
// dries, so you can see from across the camp which pots are ready to fire; in
// the fire it glows. A small label shows its state when you stand close.
public partial class PotView : Node3D
{
    // Pots are drawn a little larger than life so they read from the follow camera.
    public const float Size = 2.0f;

    readonly MeshInstance3D body = new();
    readonly MeshInstance3D water = new();
    // A soft warm rim light on fresh clay, so small pieces are easy to find in the
    // grass: faint while you are nearby, brighter on the piece G would tend.
    static ShaderMaterial? glowFaint, glowNear;
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
    ulong id;
    uint mark;
    ClayForm form;
    ClayStage stage;
    FireKind fire;
    int lastStep = -1;

    // Moisture the server stored, the moment it was true (unix seconds), and its rate.
    float moisture, rate;
    double moistureAt, fireStart, fireFinish;

    public PotView()
    {
        body.Scale = Vector3.One * Size;
        water.Scale = Vector3.One * Size;
        AddChild(body);
        AddChild(water);
        AddChild(label);
    }

    public static double Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    public bool ShowLabel { set => label.Visible = value; }

    // 0 hides the glow, 1 is the faint nearby glow, 2 marks the piece G would tend.
    public int Highlight
    {
        set => body.MaterialOverlay = value <= 0 || stage == ClayStage.Firing ? null
            : value > 1 ? (glowNear ??= Glow(0.4f)) : (glowFaint ??= Glow(0.18f));
    }

    static ShaderMaterial Glow(float strength)
    {
        var m = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = @"shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_back, shadows_disabled;
uniform vec3 tint;
uniform float strength;
void fragment() {
    float rim = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 3.0);
    ALBEDO = tint * rim * strength;
}",
            },
        };
        m.SetShaderParameter("tint", new Vector3(Look.Cream.R, Look.Cream.G, Look.Cream.B));
        m.SetShaderParameter("strength", strength);
        return m;
    }

    public void Show(ulong potId, ClayForm potForm, ClayStage potStage, FireKind potFire, float potMoisture, double since, float dryRate, bool full = false, uint potMark = 0)
    {
        id = potId;
        mark = potMark;
        form = potForm;
        stage = potStage;
        fire = potFire;
        moisture = potMoisture;
        moistureAt = since;
        rate = dryRate;
        body.Mesh = PotMesh.For(form, id);
        Rotation = new Vector3(0, (id % 97) / 97f * Mathf.Tau, 0);
        label.Position = new Vector3(0, PotMesh.Height(form) * Size + 0.25f, 0);
        lastStep = -1;
        Refresh();

        var line = PotMesh.WaterLine(form);
        water.Visible = full && stage == ClayStage.Fired && line.X > 0f;
        if (water.Visible)
        {
            water.Mesh = new CylinderMesh { TopRadius = line.X, BottomRadius = line.X, Height = 0.002f, RadialSegments = 24 };
            water.MaterialOverride = ClayLook.Water;
            water.Position = new Vector3(0, line.Y * Size, 0);
        }
    }

    public void Firing(double start, double finish)
    {
        stage = ClayStage.Firing;
        fireStart = start;
        fireFinish = finish;
        lastStep = -1;
        Refresh();
    }

    public float Moisture => ClayRules.MoistureAt(moisture, rate, (float)(Now - moistureAt));
    public float SecondsUntilDry => ClayRules.SecondsUntilDry(Moisture, rate);

    public ClayStage Stage => stage;

    public override void _Process(double delta)
    {
        if (stage is ClayStage.Drying or ClayStage.Firing) Refresh();
    }

    void Refresh()
    {
        if (stage == ClayStage.Firing)
        {
            float t = (float)Math.Clamp((Now - fireStart) / Math.Max(fireFinish - fireStart, 1), 0, 1);
            int step = Mathf.RoundToInt(t * 10f) + 100;
            if (step != lastStep)
            {
                lastStep = step;
                body.MaterialOverride = ClayLook.Firing(t, id);
            }
            if (label.Visible)
            {
                int left = (int)Math.Max(0, fireFinish - Now);
                label.Text = $"Firing  {left / 60}:{left % 60:00}";
            }
            return;
        }

        float now = Moisture;
        int s = Mathf.RoundToInt(now * 10f);
        if (s != lastStep)
        {
            lastStep = s;
            var plain = ClayLook.For(stage, fire, now, id);
            body.MaterialOverride = Marks.Has(mark) && form != ClayForm.Brick ? ClayLook.Stamped(plain, Marks.Glyph(mark), form) : plain;
        }
        if (label.Visible && stage == ClayStage.Drying)
        {
            string state = ClayRules.Describe(now);
            label.Text = now <= ClayRules.SafeToFire
                ? form switch
                {
                    ClayForm.Brick => "Dry brick",
                    ClayForm.StorageJar or ClayForm.Tile => "Bone dry, ready for a kiln",
                    _ => "Bone dry, ready to fire",
                }
                : $"{char.ToUpperInvariant(state[0])}{state[1..]}, drying";
        }
    }
}
