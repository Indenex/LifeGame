using System;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The sundial's arc (ui-32, Justin: "when it's built, it adds a little curved time of day
// UI thing at the top of the screen that tracks the time of day and actually moves along
// with day/night"). A thin painted arc at top centre, no box (D-045): by day a small sun
// glides along it from sunrise (left) to sunset (right), by night a crescent moon takes the
// same path. The ends are tinted dawn and dusk; three faint dots mark dawn, noon and dusk.
// Hover gives the time in words ("Late morning"), never numbers. It shows only while you
// have a sundial (House building's building-18 flag; Main sets Has) and fades under panels.
public partial class SundialArc : CanvasLayer
{
    // The world's time of day, 0..1 (Atmosphere, which already eases /time jumps).
    public Func<float> TimeOfDay = () => 0.5f;
    // Whether the player has a sundial standing in their claim or town.
    public Func<bool> Has = () => false;

    // Room it takes at the top, for the status line to sit under while it shows.
    public static float Room { get; private set; }

    const float Width = 220f, Height = 46f;
    ArcView view = null!;
    float alpha;
    bool preview;

    public override void _Ready()
    {
        Layer = 2;
        view = new ArcView { CustomMinimumSize = new Vector2(Width, Height), MouseFilter = Control.MouseFilterEnum.Pass };
        view.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        view.GrowHorizontal = Control.GrowDirection.Both;
        view.Position = new Vector2(-Width / 2, 8);
        view.Size = new Vector2(Width, Height);
        AddChild(view);
        // --screenshot-sundial shows it without a sundial; =dawn, =noon, =dusk or =night picks the hour.
        foreach (var arg in OS.GetCmdlineUserArgs().Where(a => a.StartsWith("--screenshot-sundial")))
        {
            preview = true;
            float? at = arg.Split('=').ElementAtOrDefault(1) switch
            {
                "dawn" => 0.26f, "noon" => 0.5f, "dusk" => 0.73f, "night" => 0.0f, "morning" => 0.4f, _ => null,
            };
            if (at is { } fixedTime) TimeOfDay = () => fixedTime;
        }
    }

    public override void _Process(double delta)
    {
        bool show = (preview || Has()) && !Panels.Open();
        alpha = Mathf.MoveToward(alpha, show ? 1f : 0f, (float)delta * 3f);
        view.Modulate = new Color(1, 1, 1, alpha);
        view.Visible = alpha > 0.01f;
        Room = alpha > 0.5f ? Height + 4f : 0f;
        float t = TimeOfDay();
        view.Time = t;
        view.TooltipText = Words(t);
        view.QueueRedraw();
    }

    // The time of day in words, as a person would say it.
    public static string Words(float t)
    {
        t = ((t % 1f) + 1f) % 1f;
        return t switch
        {
            < 0.05f => "Deep night",
            < 0.20f => "The small hours",
            < 0.25f => "Dawn",
            < 0.33f => "Early morning",
            < 0.42f => "Morning",
            < 0.48f => "Late morning",
            < 0.53f => "Midday",
            < 0.60f => "Early afternoon",
            < 0.67f => "Afternoon",
            < 0.73f => "Late afternoon",
            < 0.78f => "Dusk",
            < 0.86f => "Evening",
            < 0.95f => "Night",
            _ => "Deep night",
        };
    }
}

// Draws the arc, its dawn and dusk tints, the three marks, and the sun or moon on it.
public partial class ArcView : Control
{
    public float Time;

    static readonly Color Dawn = new(0.98f, 0.70f, 0.42f), Noon = new(0.98f, 0.90f, 0.66f), Dusk = new(0.92f, 0.56f, 0.52f);
    static readonly Color NightArc = new(0.55f, 0.62f, 0.75f), SunCore = new(1f, 0.88f, 0.55f), MoonLight = new(0.88f, 0.90f, 0.96f);

    // A point on the arc, u from 0 (left, sunrise) to 1 (right, sunset).
    Vector2 At(float u)
    {
        float a = Mathf.Pi * (1f - u);
        return new Vector2(Size.X / 2 + Mathf.Cos(a) * (Size.X / 2 - 12), Size.Y - 8 - Mathf.Sin(a) * (Size.Y - 18));
    }

    public override void _Draw()
    {
        float t = ((Time % 1f) + 1f) % 1f;
        bool day = t >= WorldTime.Sunrise && t < WorldTime.Sunset;
        // Near sunrise and sunset the arc's colours cross between day and night.
        float edge = Mathf.Min(Mathf.Abs(t - WorldTime.Sunrise), Mathf.Abs(t - WorldTime.Sunset));
        float dayness = day ? Mathf.Clamp(0.5f + edge / 0.06f, 0.5f, 1f) : Mathf.Clamp(0.5f - edge / 0.06f, 0f, 0.5f);

        // The ribbon: soft and wide underneath, a finer line on top.
        const int Steps = 40;
        for (int i = 0; i < Steps; i++)
        {
            float a = i / (float)Steps, b = (i + 1) / (float)Steps;
            var dayColour = a < 0.5f ? Dawn.Lerp(Noon, a * 2f) : Noon.Lerp(Dusk, (a - 0.5f) * 2f);
            var colour = NightArc.Lerp(dayColour, dayness);
            DrawLine(At(a), At(b), new Color(colour, 0.18f), 7f, true);
            DrawLine(At(a), At(b), new Color(colour, 0.6f), 1.6f, true);
        }
        // Dawn, noon and dusk marks.
        foreach (float m in new[] { 0f, 0.5f, 1f })
            DrawCircle(At(m), 2f, new Color(Look.Cream, 0.45f));

        // The sun sets at the right end as the moon rises at the left (and back at dawn),
        // fading across about 0.03 of a day, so the glyph never jumps from end to end.
        const float Fade = 0.015f;
        float sunAlpha = Mathf.Clamp(Mathf.Min(t - WorldTime.Sunrise, WorldTime.Sunset - t) / Fade + 0.5f, 0f, 1f);
        float dayU = Mathf.Clamp((t - WorldTime.Sunrise) / (WorldTime.Sunset - WorldTime.Sunrise), 0f, 1f);
        float nightT = ((t - WorldTime.Sunset) % 1f + 1f) % 1f, nightLength = 1f - (WorldTime.Sunset - WorldTime.Sunrise);
        float nightU = Mathf.Clamp(nightT / nightLength, 0f, 1f);
        // Just before sunrise the moon is at the right end; just after sunset it starts on the left.
        if (nightT > nightLength) nightU = 1f;
        if (sunAlpha > 0.001f)
        {
            var p = At(dayU);
            DrawCircle(p, 11f, new Color(SunCore, 0.18f * sunAlpha));
            DrawCircle(p, 7.5f, new Color(SunCore, 0.35f * sunAlpha));
            DrawCircle(p, 5f, new Color(SunCore, sunAlpha));
        }
        if (sunAlpha < 0.999f)
        {
            var m = At(nightU);
            float a = 1f - sunAlpha;
            DrawCircle(m, 9f, new Color(MoonLight, 0.12f * a));
            // A thick stroke along the left of a circle reads as a crescent moon.
            DrawArc(m, 3.8f, Mathf.DegToRad(100), Mathf.DegToRad(260), 16, new Color(MoonLight, a), 3.4f, true);
        }
    }
}
