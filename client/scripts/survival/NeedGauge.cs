using Godot;

namespace LifeGame;

// One ring gauge: a dim track, an arc for the value, and a small icon.
public partial class NeedGauge : Control
{
    public enum Glyph { Berry, Drop, Flame, Sack, Stride }

    public bool Glow;
    // Breathe to catch the eye. Left unset, a gauge breathes when its value runs low.
    public bool? Alert;
    readonly Glyph glyph;
    float value = 1f, shown = 1f, pulse, drawnShown = -1f;
    Color color = Colors.White, drawnColor;

    public NeedGauge(Glyph glyph)
    {
        this.glyph = glyph;
        CustomMinimumSize = new Vector2(46, 46);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void Set(float v, Color c)
    {
        value = v;
        color = c;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        shown = Mathf.Lerp(shown, value, 1f - Mathf.Exp(-5f * dt));
        pulse += dt;
        // Redraw only when something on the ring moves: the arc, its color, or a
        // glow or breath that animates.
        bool animating = Glow || (Alert ?? value < 0.25f);
        if (animating || Mathf.Abs(shown - drawnShown) > 0.001f || color != drawnColor)
        {
            drawnShown = shown;
            drawnColor = color;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var center = Size / 2f;
        const float radius = 19f;
        // Low needs breathe gently so they catch the eye without alarm.
        float low = (Alert ?? value < 0.25f) ? 0.5f + 0.5f * Mathf.Sin(pulse * 4f) : 1f;
        var c = new Color(color, 0.55f + 0.45f * low);

        if (Glow)
            DrawCircle(center, radius + 3f, new Color(NeedsHud.WarmColor, 0.10f + 0.05f * Mathf.Sin(pulse * 2.5f)));
        DrawArc(center, radius, 0, Mathf.Tau, 48, new Color(1, 1, 1, 0.14f), 4f, true);
        if (shown > 0.005f)
            DrawArc(center, radius, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * shown, 48, c, 4f, true);

        var icon = new Color(Look.Cream, 0.9f);
        switch (glyph)
        {
            case Glyph.Berry:
                DrawCircle(center + new Vector2(-4, 3), 4.2f, c);
                DrawCircle(center + new Vector2(4, 3), 4.2f, c);
                DrawCircle(center + new Vector2(0, -3.5f), 4.2f, c);
                DrawLine(center + new Vector2(0, -7), center + new Vector2(3, -11), icon, 1.5f, true);
                break;
            case Glyph.Drop:
                DrawCircle(center + new Vector2(0, 3), 6f, c);
                DrawColoredPolygon(new[] { center + new Vector2(-5.6f, 1.2f), center + new Vector2(0, -10), center + new Vector2(5.6f, 1.2f) }, c);
                break;
            case Glyph.Flame:
                DrawColoredPolygon(new[]
                {
                    center + new Vector2(1, -12), center + new Vector2(3, -6), center + new Vector2(6, -9),
                    center + new Vector2(7, -2), center + new Vector2(7, 3), center + new Vector2(4, 8),
                    center + new Vector2(0, 9), center + new Vector2(-4, 8), center + new Vector2(-7, 3),
                    center + new Vector2(-6, -3), center + new Vector2(-4, 0), center + new Vector2(-3, -6),
                }, c);
                DrawCircle(center + new Vector2(0, 4), 3f, new Color(1f, 0.92f, 0.75f, 0.6f * low));
                break;
            case Glyph.Sack:
                // A full sack tied at the neck.
                DrawCircle(center + new Vector2(0, 3), 7f, c);
                DrawColoredPolygon(new[] { center + new Vector2(-3.5f, -2), center + new Vector2(-2, -7), center + new Vector2(2, -7), center + new Vector2(3.5f, -2) }, c);
                DrawLine(center + new Vector2(-3, -6), center + new Vector2(3, -6), icon, 1.5f, true);
                DrawLine(center + new Vector2(-2, -7.5f), center + new Vector2(-4, -10), c, 1.5f, true);
                DrawLine(center + new Vector2(2, -7.5f), center + new Vector2(4, -10), c, 1.5f, true);
                break;
            case Glyph.Stride:
                // Two forward chevrons, like quick steps.
                DrawPolyline(new[] { center + new Vector2(-7, -6), center + new Vector2(-1, 0), center + new Vector2(-7, 6) }, c, 2.5f, true);
                DrawPolyline(new[] { center + new Vector2(0, -6), center + new Vector2(6, 0), center + new Vector2(0, 6) }, c, 2.5f, true);
                break;
        }
    }
}
