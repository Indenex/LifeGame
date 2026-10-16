using Godot;

namespace LifeGame;

// The new-arrival marker (D-115): a small sprout in a thin ring that drains over a
// new character's first half hour, while the cold bites at half strength. It sits
// just after the warmth ring and fades away when the half hour is up.
public partial class ArrivalBadge : Control
{
    static readonly Color Ring = new(0.74f, 0.86f, 0.58f);
    float left, drawn = -1f;

    public ArrivalBadge()
    {
        CustomMinimumSize = new Vector2(24, 46);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    // The share of the half hour still left, 0 to 1.
    public void Set(float fraction) => left = Mathf.Clamp(fraction, 0f, 1f);

    public override void _Process(double delta)
    {
        float target = left > 0f ? 1f : 0f;
        float a = Mathf.MoveToward(Modulate.A, target, (float)delta * (target > 0f ? 3f : 0.6f));
        Modulate = new Color(1, 1, 1, a);
        Visible = a > 0.01f;
        // The ring moves a hair every few seconds; redraw only when it does.
        if (Mathf.Abs(left - drawn) > 0.002f)
        {
            drawn = left;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var center = Size / 2f;
        const float radius = 10f;
        DrawArc(center, radius, 0, Mathf.Tau, 32, new Color(1, 1, 1, 0.14f), 2.5f, true);
        if (drawn > 0.005f)
            DrawArc(center, radius, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * drawn, 32, Ring, 2.5f, true);
        // A sprout: a short stem and two leaves.
        var c = new Color(Look.Cream, 0.9f);
        DrawLine(center + new Vector2(0, 5), center + new Vector2(0, -2), c, 1.5f, true);
        DrawColoredPolygon(new[] { center + new Vector2(0, -1), center + new Vector2(-5, -4), center + new Vector2(-1, -5) }, c);
        DrawColoredPolygon(new[] { center + new Vector2(0, -2), center + new Vector2(5, -6), center + new Vector2(1, -6) }, c);
    }
}
