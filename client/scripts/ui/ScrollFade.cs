using Godot;

namespace LifeGame;

// A soft fade into the window's paper at the bottom of a scrolling list while more of it lies
// below, so a list that ends flush at the card's edge reads as "there's more" (ui-68, PM).
// Wrap(scroll) returns a container to add in the scroll's place.
public partial class ScrollFade : Control
{
    const float Height = 34f;
    ScrollContainer scroll = null!;
    Color ground;
    bool shown;

    // Godot builds nodes through this one (editor, reloads); Wrap uses the other.
    public ScrollFade() { }

    ScrollFade(ScrollContainer scroll, Color ground)
    {
        this.scroll = scroll;
        this.ground = ground;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public static Control Wrap(ScrollContainer scroll, Color ground)
    {
        var stack = new MarginContainer { MouseFilter = MouseFilterEnum.Pass, SizeFlagsHorizontal = scroll.SizeFlagsHorizontal };
        stack.AddChild(scroll);
        stack.AddChild(new ScrollFade(scroll, ground));
        return stack;
    }

    public override void _Process(double delta)
    {
        if (scroll == null) return;
        var bar = scroll.GetVScrollBar();
        bool more = bar.Visible && bar.Value + bar.Page < bar.MaxValue - 1;
        if (more == shown) return;
        shown = more;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!shown) return;
        float bottom = Size.Y, top = Mathf.Max(0f, bottom - Height);
        // Leave the scroll bar itself clear.
        float right = Size.X - scroll.GetVScrollBar().Size.X;
        var clear = new Color(ground, 0f);
        DrawPolygon(
            new[] { new Vector2(0, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(0, bottom) },
            new[] { clear, clear, ground, ground });
    }
}
