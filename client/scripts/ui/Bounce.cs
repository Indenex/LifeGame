using Godot;

namespace LifeGame;

// A bouncy, cutesy UI (D-314, Justin): windows pop open with a soft bounce, buttons squish when
// pressed, and things you can pick up wiggle when the mouse comes over them. All of it is subtle
// and quick (under 200 ms), and Options has "Reduce motion" to turn it off. It hangs off the shared
// pieces (Ui.Window's frame, every button, bag and bar slots, cards), so no panel needs code of
// its own.
public partial class Bounce : Node
{
    // Options: "Reduce motion" (saved).
    public static bool Reduce
    {
        get => Settings.Get("ui", "reduce_motion", false);
        set => Settings.Set("ui", "reduce_motion", value);
    }

    const float PopTime = 0.18f, SquishTime = 0.12f, WiggleTime = 0.2f;

    public override void _EnterTree() => GetTree().NodeAdded += Added;

    void Added(Node node)
    {
        switch (node)
        {
            // Bag and bar slots, and craft and build cards: a wiggle on hover. (Cards are buttons
            // too, so they squish as well.)
            case BagSlotView or HotbarSlotView or Card:
                ((Control)node).MouseEntered += () => Wiggle((Control)node);
                if (node is Card card) Squishy(card);
                break;
            case BaseButton button:
                Squishy(button);
                break;
            case PanelContainer panel:
                // Windows: a soft pop each time they open. The mark is set before the window
                // enters the tree (Ui.Window, or WarmLook on hand-framed ones a moment later).
                Callable.From(() =>
                {
                    if (IsInstanceValid(panel) && panel.HasMeta(Ui.WindowMark) && !panel.HasMeta("bounce"))
                    {
                        panel.SetMeta("bounce", true);
                        panel.VisibilityChanged += () => { if (panel.IsVisibleInTree()) Pop(panel); };
                        if (panel.IsVisibleInTree()) Pop(panel);
                    }
                }).CallDeferred();
                break;
        }
    }

    static void Squishy(BaseButton b)
    {
        b.ButtonDown += () => Squish(b, 0.93f, SquishTime * 0.5f);
        b.ButtonUp += () => Squish(b, 1f, SquishTime, spring: true);
    }

    // A window grows from a little small to a touch too big and settles, fading in.
    static void Pop(Control c)
    {
        if (Reduce) return;
        c.PivotOffset = c.Size / 2;
        c.Scale = new Vector2(0.92f, 0.92f);
        var tween = c.CreateTween().SetParallel();
        tween.TweenProperty(c, "scale", Vector2.One, PopTime).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        c.Modulate = new Color(c.Modulate, 0f);
        tween.TweenProperty(c, "modulate:a", 1f, PopTime * 0.6f);
    }

    static void Squish(Control c, float to, float time, bool spring = false)
    {
        if (Reduce) return;
        c.PivotOffset = c.Size / 2;
        var tween = c.CreateTween();
        tween.TweenProperty(c, "scale", new Vector2(to, to), time)
            .SetTrans(spring ? Tween.TransitionType.Back : Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }

    // A small damped shake of the angle, back to where it was.
    static void Wiggle(Control c)
    {
        if (Reduce || c.HasMeta("wiggling")) return;
        c.SetMeta("wiggling", true);
        float rest = c.RotationDegrees;
        c.PivotOffset = c.Size / 2;
        var tween = c.CreateTween();
        float step = WiggleTime / 4f;
        tween.TweenProperty(c, "rotation_degrees", rest + 4f, step);
        tween.TweenProperty(c, "rotation_degrees", rest - 3f, step);
        tween.TweenProperty(c, "rotation_degrees", rest + 1.5f, step);
        tween.TweenProperty(c, "rotation_degrees", rest, step);
        tween.TweenCallback(Callable.From(() => c.RemoveMeta("wiggling")));
    }
}
