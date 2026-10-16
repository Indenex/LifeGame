using Godot;

namespace LifeGame;

// Soft ring gauges in the bottom-right corner (food, water, warmth, and load
// and stamina when they matter), frost creeping in from the screen
// edges as you get cold, and a quiet fade to black when you die.
public partial class NeedsHud : CanvasLayer
{
    // Cached names: a string here would build a new StringName every frame.
    static readonly StringName NAmount = "amount";

    static readonly Color FoodColor = new(0.86f, 0.55f, 0.32f);
    static readonly Color WaterColor = new(0.45f, 0.72f, 0.82f);
    public static readonly Color WarmColor = new(1.0f, 0.66f, 0.30f);
    static readonly Color ColdColor = new(0.62f, 0.80f, 0.95f);

    readonly NeedGauge food = new(NeedGauge.Glyph.Berry);
    readonly NeedGauge water = new(NeedGauge.Glyph.Drop);
    readonly NeedGauge warmth = new(NeedGauge.Glyph.Flame);
    readonly NeedGauge load = new(NeedGauge.Glyph.Sack) { Visible = false, Modulate = new Color(1, 1, 1, 0) };
    readonly NeedGauge stamina = new(NeedGauge.Glyph.Stride) { Visible = false, Modulate = new Color(1, 1, 1, 0), Alert = false };
    readonly ColorRect frost = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    readonly ColorRect fade = new() { Color = new Color(0.02f, 0.02f, 0.03f, 0f), MouseFilter = Control.MouseFilterEnum.Ignore };
    readonly Label deathTitle = new();
    readonly Label deathLine = new();
    float frostAmount;
    PanelContainer? gaugePanel;

    public NeedsHud()
    {
        Layer = 2;
    }

    public override void _Ready()
    {
        frost.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        frost.Material = new ShaderMaterial { Shader = new Shader { Code = FrostShader } };
        AddChild(frost);

        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var gauges = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        gauges.AddThemeConstantOverride("separation", 12);
        // The stamina ring only shows while you are short of breath (D-088).
        gauges.AddChild(stamina);
        gauges.AddChild(food);
        gauges.AddChild(water);
        gauges.AddChild(warmth);
        // The load ring only fades in when you near your carrying limit.
        gauges.AddChild(load);
        gaugePanel = Hud.Panel(gauges, Vector2.Zero);
        gaugePanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        gaugePanel.GrowHorizontal = Control.GrowDirection.Begin;
        gaugePanel.GrowVertical = Control.GrowDirection.Begin;
        gaugePanel.Position = new Vector2(-20, -20);
        root.AddChild(gaugePanel);


        fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(fade);
        var deathBox = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        deathBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        deathBox.AddThemeConstantOverride("separation", 14);
        Hud.Style(deathTitle, 34, 1f);
        Hud.Style(deathLine, 17, 0.75f);
        deathTitle.HorizontalAlignment = deathLine.HorizontalAlignment = HorizontalAlignment.Center;
        deathBox.AddChild(deathTitle);
        deathBox.AddChild(deathLine);
        deathBox.Modulate = new Color(1, 1, 1, 0);
        fade.AddChild(deathBox);
        deathBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
    }

    public void SetNeeds(float foodValue, float waterValue, float warmthValue, bool nearHeat)
    {
        food.Set(foodValue, FoodColor);
        water.Set(waterValue, WaterColor);
        // The warmth ring cools from orange to ice blue as you get colder.
        warmth.Set(warmthValue, ColdColor.Lerp(WarmColor, Mathf.Clamp(warmthValue * 1.4f - 0.2f, 0f, 1f)));
        warmth.Glow = nearHeat;
        frostAmount = Mathf.Clamp((0.55f - warmthValue) / 0.5f, 0f, 1f);
    }

    static readonly Color LoadColor = new(0.86f, 0.80f, 0.66f);
    static readonly Color OverloadColor = new(1f, 0.66f, 0.30f);
    float loadAlpha;

    // A sack ring next to the needs, shown from 80% of your limit. It fills toward
    // the limit, and turns amber and breathes once the load slows you down.
    public void SetLoad(float carried, float limit)
    {
        float fraction = limit > 0 ? carried / limit : 0f;
        bool over = carried > limit;
        load.Set(Mathf.Clamp(fraction, 0f, 1f), over ? OverloadColor : LoadColor);
        load.Alert = over;
        loadAlpha = fraction >= 0.8f ? 1f : 0f;
    }

    static readonly Color StaminaColor = new(0.78f, 0.86f, 0.62f);
    float staminaAlpha;

    // A stride ring, shown only while stamina is below full.
    public void SetStamina(float value)
    {
        stamina.Set(value, StaminaColor);
        staminaAlpha = value < 0.995f ? 1f : 0f;
    }

    // True while the death card covers the screen, so nothing offers itself over it.
    public bool Mourning => fade.Color.A > 0.02f;

    public void ShowDeath(string cause)
    {
        deathTitle.Text = cause switch
        {
            "cold" => "You froze to death.",
            "thirst" => "You died of thirst.",
            "hunger" => "You starved.",
            "slain" => "You were struck down.",
            _ => "You died.",
        };
        deathLine.Text = "You wake at the edge of the wilderness with nothing.\nWhat you learned stays with you.";
        var box = fade.GetChild<Control>(0);
        var tween = CreateTween();
        tween.TweenProperty(fade, "color:a", 1f, 0.8f);
        tween.Parallel().TweenProperty(box, "modulate:a", 1f, 1.2f);
        tween.TweenInterval(2.8f);
        tween.TweenProperty(box, "modulate:a", 0f, 1.0f);
        tween.Parallel().TweenProperty(fade, "color:a", 0f, 2.2f);
    }

    public override void _Process(double delta)
    {
        // The rings fade out while a panel (journal, claims, menu) covers the world.
        if (gaugePanel != null)
        {
            float shown = Panels.Open() ? 0f : 1f;
            gaugePanel.Modulate = new Color(1, 1, 1, Mathf.MoveToward(gaugePanel.Modulate.A, shown, (float)delta * 4f));
        }
        float a = Mathf.MoveToward(load.Modulate.A, loadAlpha, (float)delta * 2f);
        load.Modulate = new Color(1, 1, 1, a);
        load.Visible = a > 0.01f;
        // Fade in quickly when you start running, out gently once you've caught your breath.
        float sa = Mathf.MoveToward(stamina.Modulate.A, staminaAlpha, (float)delta * (staminaAlpha > 0f ? 4f : 1.5f));
        stamina.Modulate = new Color(1, 1, 1, sa);
        stamina.Visible = sa > 0.01f;

        var material = (ShaderMaterial)frost.Material;
        float current = (float)material.GetShaderParameter(NAmount);
        material.SetShaderParameter(NAmount, Mathf.Lerp(current, frostAmount, 1f - Mathf.Exp(-2f * (float)delta)));
    }

    // Icy blue creeping in from the corners, with a crystalline edge.
    const string FrostShader = """
        shader_type canvas_item;
        uniform float amount = 0.0;

        float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
        float noise(vec2 p) {
            vec2 i = floor(p), f = fract(p);
            vec2 u = f * f * (3.0 - 2.0 * f);
            return mix(mix(hash(i), hash(i + vec2(1, 0)), u.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), u.x), u.y);
        }

        void fragment() {
            vec2 uv = (SCREEN_UV - 0.5) * vec2(1.0, 0.75);
            float d = length(uv);
            float n = noise(SCREEN_UV * 9.0) * 0.6 + noise(SCREEN_UV * 31.0) * 0.4;
            float reach = mix(0.75, 0.32, amount);
            float edge = smoothstep(reach, reach + 0.25, d + (n - 0.5) * 0.22);
            float sparkle = step(0.93, noise(SCREEN_UV * 140.0)) * edge * 0.35;
            vec3 ice = mix(vec3(0.70, 0.82, 0.92), vec3(0.95, 0.98, 1.0), n);
            COLOR = vec4(ice + sparkle, edge * amount * 0.55);
        }
        """;
}
