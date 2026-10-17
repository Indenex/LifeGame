using Godot;

namespace LifeGame;

// How an item sits in a round slot, for the hotbar and the bag alike (ui-70, Justin: "Inventory
// slots should highlight the item all the time, and put a cute circle around your active item,
// it's too hard to see what's there", then "They should only be gray from the last item that was
// there and you're out"). Every item sits on a soft light plate with a small sticker shadow, in
// full colour, so it reads against the paper buttons (D-321). Only a slot whose item has run out
// shows it as a grey ghost, with no plate. The slot in hand wears a round ring just outside it.
public static class SlotLook
{
    // Adds the plate and the picture's shadow to the slot's stack, before the picture itself.
    public static (Panel Plate, TextureRect Shadow) Behind(Control stack, TextureRect picture, float slot)
    {
        var plateStyle = new StyleBoxFlat
        {
            BgColor = WarmLook.On ? new Color(1f, 0.995f, 0.97f, 0.96f) : new Color(Look.Cream, 0.2f),
            BorderColor = WarmLook.On ? new Color(WarmLook.Butter, 0.45f) : new Color(Look.Cream, 0.18f),
        };
        plateStyle.SetBorderWidthAll(1);
        plateStyle.SetCornerRadiusAll((int)(slot / 2));
        var plate = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        plate.AddThemeStyleboxOverride("panel", plateStyle);
        plate.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        plate.OffsetLeft = plate.OffsetTop = 5;
        plate.OffsetRight = plate.OffsetBottom = -5;
        // WarmLook leaves it alone: it is styled for paper already.
        WarmLook.KeepColours(plate);
        stack.AddChild(plate);

        // The same picture, darkened and nudged down a little: a soft shadow, like a sticker on card.
        var shadow = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(0.25f, 0.13f, 0.06f, 0.3f),
            Material = Silhouette,
        };
        shadow.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        shadow.OffsetLeft = picture.OffsetLeft + 1;
        shadow.OffsetTop = picture.OffsetTop + 2.5f;
        shadow.OffsetRight = picture.OffsetRight + 1;
        shadow.OffsetBottom = picture.OffsetBottom + 2.5f;
        stack.AddChild(shadow);
        return (plate, shadow);
    }

    // Full colour with its plate while you have some; a grey ghost on its own once you're out.
    public static void Set(Panel plate, TextureRect shadow, TextureRect picture, Texture2D? texture, bool have)
    {
        picture.Texture = shadow.Texture = texture;
        plate.Visible = texture != null && have;
        shadow.Visible = texture != null && have;
        picture.Material = have ? null : Grey;
        picture.Modulate = have ? Colors.White : new Color(1, 1, 1, WarmLook.On ? 0.55f : 0.4f);
    }

    // Draws a texture in one flat colour (the Modulate), keeping its shape: the shadow.
    static readonly ShaderMaterial Silhouette = Material(@"
shader_type canvas_item;
void fragment() { COLOR = vec4(COLOR.rgb, texture(TEXTURE, UV).a * COLOR.a); }");

    // Draws a texture without its colour, a little lifted: the ghost of an item you're out of.
    static readonly ShaderMaterial Grey = Material(@"
shader_type canvas_item;
void fragment() {
    vec4 c = texture(TEXTURE, UV);
    float g = dot(c.rgb, vec3(0.299, 0.587, 0.114));
    COLOR = vec4(vec3(mix(g, 0.75, 0.35)), c.a) * COLOR;
}");

    static ShaderMaterial Material(string code) => new() { Shader = new Shader { Code = code } };
}

// The ring round the slot in hand (ui-70): soft and warm, just outside the disc, with a gentle
// glow. It springs in with a little overshoot when you take something in hand ("Reduce motion"
// fades it instead) and fades out when you put it away.
public partial class ActiveRing : Control
{
    public float Radius = 30f;
    bool on;
    float show, spring = 1f, springVel;

    static Color Colour => WarmLook.On ? WarmLook.Coral : new Color(0.96f, 0.78f, 0.42f);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public void SetOn(bool now)
    {
        if (now == on) return;
        on = now;
        if (on && !Bounce.Reduce)
        {
            spring = 0.72f;
            springVel = 0f;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        show = Mathf.MoveToward(show, on ? 1f : 0f, dt * (on ? 8f : 5f));
        // A soft spring: quick, one small overshoot, settled in about a quarter second.
        springVel += ((1f - spring) * 260f - springVel * 18f) * dt;
        spring += springVel * dt;
        if (Bounce.Reduce) spring = 1f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (show <= 0.01f) return;
        var centre = Size / 2;
        float r = Radius * spring;
        var c = Colour;
        // The glow, then the ring, then a thin light line inside it so it looks soft and rounded.
        DrawArc(centre, r + 2f, 0, Mathf.Tau, 64, new Color(c, 0.22f * show), 8f, true);
        DrawArc(centre, r, 0, Mathf.Tau, 64, new Color(c, 0.95f * show), 3.5f, true);
        DrawArc(centre, r - 1f, 0, Mathf.Tau, 64, new Color(1f, 1f, 1f, 0.35f * show), 1f, true);
    }
}
