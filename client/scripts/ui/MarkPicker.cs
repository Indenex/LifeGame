using System;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Choosing your maker's mark (ui-23; Justin: "pick from a panel and give them a bunch of
// different symbols, fantasy ones, scientific, etc, and a colour picker kinda thing").
// Tabs for each group of glyphs, a grid of them, a colour picker (earthy presets, a hue
// strip and a shade square), and the mark stamped on a pot as you choose. Done saves it
// (SetMark on the server, which checks the glyph and keeps the colour in range); Esc
// keeps your old mark. Options has "Your mark". Ceramics stamps it on what you make.
public partial class MarkPicker : CanvasLayer
{
    public IMarkLink Link = new MockMarkLink();
    // Options opens it through this; Available is false until the server link is attached
    // (or in the --screenshot-mark preview), so Options shows "Your mark" only when it works.
    public static Action? OpenFromMenu;
    public static bool Available;

    public bool IsOpen => panel.Visible;

    // Ceramics' earthy presets (shared Marks.Presets).
    static readonly Color[] Presets = Marks.Presets.Select(MarkColour.ToColor).ToArray();

    Control panel = null!;
    HBoxContainer tabs = null!;
    GridContainer grid = null!;
    PotStamp pot = null!;
    HueStrip hue = null!;
    ShadeSquare shade = null!;
    Label glyphName = null!;
    int group;
    ushort glyph;
    Color ink = MarkColour.ToColor(Marks.DefaultColor);

    public override void _Ready()
    {
        Layer = 5;
        panel = MakePanel();
        panel.Visible = false;
        AddChild(panel);
        OpenFromMenu = Open;
        bool preview = OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-mark"));
        Available = preview || Link is not MockMarkLink;
        if (preview) GetTree().CreateTimer(1.5).Timeout += Open;
    }

    // The live game's link; Ceramics' bench prompt "Choose your mark" opens the picker too.
    public void Attach(IMarkLink link)
    {
        Link = link;
        Available = true;
        CeramicsView.OpenMarkPicker = Open;
    }

    public void Open()
    {
        if (Link.Mine is { } m) { glyph = m.glyph; ink = m.color; }
        else { glyph = Link.Groups[0].Glyphs[0].Id; ink = MarkColour.ToColor(Marks.DefaultColor); }
        group = Math.Max(0, Link.Groups.ToList().FindIndex(g => g.Glyphs.Any(x => x.Id == glyph)));
        panel.Visible = true;
        Refresh();
        SyncColour();
    }

    public void Close() => panel.Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (!IsOpen || e is not InputEventKey { Pressed: true, Echo: false }) return;
        if (e.IsActionPressed("ui_cancel")) Close();
        else if (e is InputEventKey { PhysicalKeycode: Key.Enter or Key.KpEnter }) Done();
        else return;
        GetViewport().SetInputAsHandled();
    }

    void Done()
    {
        Link.Set(glyph, Link.Clamp(ink));
        Close();
    }

    Control MakePanel()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        box.AddChild(Hud.Text("Your maker's mark", 20));

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 26);
        box.AddChild(columns);

        // Left: the glyphs, by group.
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(5 * 56 + 4 * 8, 0) };
        left.AddThemeConstantOverride("separation", 10);
        columns.AddChild(left);
        tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 6);
        left.AddChild(tabs);
        grid = new GridContainer { Columns = 5 };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        left.AddChild(grid);

        // Right: the mark on a pot, its name, and the colour.
        var right = new VBoxContainer { CustomMinimumSize = new Vector2(260, 0) };
        right.AddThemeConstantOverride("separation", 10);
        columns.AddChild(right);
        pot = new PotStamp { CustomMinimumSize = new Vector2(260, 200) };
        right.AddChild(pot);
        glyphName = Hud.Text("", 15);
        glyphName.HorizontalAlignment = HorizontalAlignment.Center;
        right.AddChild(glyphName);

        var presets = new GridContainer { Columns = 6 };
        presets.AddThemeConstantOverride("h_separation", 6);
        presets.AddThemeConstantOverride("v_separation", 6);
        foreach (var c in Presets)
        {
            var swatch = new Button { CustomMinimumSize = new Vector2(30, 30), FocusMode = Control.FocusModeEnum.None };
            var style = new StyleBoxFlat { BgColor = c, BorderColor = new Color(Look.Cream, 0.3f) };
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(15);
            foreach (var state in new[] { "normal", "hover", "pressed" }) swatch.AddThemeStyleboxOverride(state, style);
            var chosen = c;
            swatch.Pressed += () => { ink = chosen; SyncColour(); };
            presets.AddChild(swatch);
        }
        right.AddChild(presets);

        shade = new ShadeSquare { CustomMinimumSize = new Vector2(260, 110) };
        shade.Picked += (s, v) => { ink = Link.Clamp(Color.FromHsv(hue.Hue, s, v)); UpdatePreview(); };
        right.AddChild(shade);
        hue = new HueStrip { CustomMinimumSize = new Vector2(260, 18) };
        hue.Picked += h => { shade.Hue = h; ink.ToHsv(out _, out float s, out float v); ink = Link.Clamp(Color.FromHsv(h, s, v)); UpdatePreview(); };
        right.AddChild(hue);

        var done = ControlsPage.TextButton("Done", Done);
        done.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        right.AddChild(done);

        var hint = Hud.Text("Your mark goes on the pots and things you make    Enter is Done    Esc keeps your mark", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);

        var outer = Hud.Panel(box, Vector2.Zero);
        var st = (StyleBoxFlat)outer.GetThemeStylebox("panel");
        st.BgColor = new Color(0.10f, 0.11f, 0.09f, 0.88f);
        st.ContentMarginTop = st.ContentMarginBottom = 14;
        st.ContentMarginLeft = st.ContentMarginRight = 18;
        outer.SetAnchorsPreset(Control.LayoutPreset.Center);
        outer.GrowHorizontal = Control.GrowDirection.Both;
        outer.GrowVertical = Control.GrowDirection.Both;
        return outer;
    }

    void Refresh()
    {
        foreach (var child in tabs.GetChildren()) { tabs.RemoveChild(child); child.QueueFree(); }
        for (int i = 0; i < Link.Groups.Count; i++)
        {
            int at = i;
            var tab = new Button { Text = Link.Groups[i].Name, Flat = i != group, FocusMode = Control.FocusModeEnum.None };
            tab.AddThemeFontSizeOverride("font_size", 15);
            tab.Modulate = new Color(1, 1, 1, i == group ? 1f : 0.65f);
            tab.Pressed += () => { group = at; Refresh(); };
            tabs.AddChild(tab);
        }
        foreach (var child in grid.GetChildren()) { grid.RemoveChild(child); child.QueueFree(); }
        foreach (var g in Link.Groups[group].Glyphs)
        {
            var tile = new Button { CustomMinimumSize = new Vector2(56, 56), FocusMode = Control.FocusModeEnum.None, TooltipText = g.Name };
            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.09f, 0.10f, 0.07f, 0.6f),
                BorderColor = new Color(Look.Cream, g.Id == glyph ? 0.9f : 0.22f),
            };
            style.SetBorderWidthAll(g.Id == glyph ? 2 : 1);
            style.SetCornerRadiusAll(10);
            foreach (var state in new[] { "normal", "hover", "pressed" }) tile.AddThemeStyleboxOverride(state, style);
            var view = new MarkView { Width = 2f };
            view.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            view.OffsetLeft = view.OffsetTop = 9;
            view.OffsetRight = view.OffsetBottom = -9;
            view.Show(g, Look.Cream);
            tile.AddChild(view);
            var chosen = g.Id;
            tile.Pressed += () => { glyph = chosen; Refresh(); };
            grid.AddChild(tile);
        }
        UpdatePreview();
    }

    void SyncColour()
    {
        ink.ToHsv(out float h, out float s, out float v);
        hue.Hue = h;
        shade.Hue = h;
        shade.Mark(s, v);
        UpdatePreview();
    }

    void UpdatePreview()
    {
        var g = Link.Find(glyph);
        pot.Show(g, Link.Clamp(ink));
        glyphName.Text = g?.Name ?? "";
        hue.QueueRedraw();
    }
}

// A plain clay pot with the mark pressed into its belly, for the preview.
public partial class PotStamp : Control
{
    Glyph? glyph;
    Color ink;

    public void Show(Glyph? g, Color c)
    {
        glyph = g;
        ink = c;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var c = Size / 2;
        float r = Mathf.Min(Size.X, Size.Y) * 0.42f;
        // Belly, shoulder and rim, from a few circles' worth of points.
        var body = Enumerable.Range(0, 48).Select(i =>
        {
            float a = i * Mathf.Tau / 48;
            float w = r * (Mathf.Sin(a) > 0 ? 1f : 0.92f);
            return c + new Vector2(Mathf.Cos(a) * w, Mathf.Sin(a) * r * 0.82f + r * 0.08f);
        }).ToArray();
        DrawColoredPolygon(body, Look.Clay);
        DrawRect(new Rect2(c.X - r * 0.48f, c.Y - r * 0.86f, r * 0.96f, r * 0.2f), Look.Clay.Darkened(0.12f));
        DrawRect(new Rect2(c.X - r * 0.56f, c.Y - r * 0.92f, r * 1.12f, r * 0.08f), Look.Clay.Lightened(0.08f));
        // A soft highlight on the left of the belly.
        DrawCircle(c + new Vector2(-r * 0.45f, -r * 0.05f), r * 0.18f, new Color(1, 1, 1, 0.08f));
        if (glyph == null) return;
        float side = r * 0.62f;
        var origin = c + new Vector2(-side / 2, -side / 2 + r * 0.1f);
        foreach (var stroke in glyph.Strokes)
            if (stroke.Length > 1)
                DrawPolyline(stroke.Select(p => origin + new Vector2(p.x, p.y) * side).ToArray(), ink, 3f, true);
    }
}

// A strip of hues; click or drag to pick one.
public partial class HueStrip : Control
{
    public float Hue;
    public event Action<float>? Picked;

    public override void _Draw()
    {
        const int steps = 48;
        float w = Size.X / steps;
        for (int i = 0; i < steps; i++)
            DrawRect(new Rect2(i * w, 0, w + 1, Size.Y), Color.FromHsv(i / (float)steps, 0.6f, 0.75f));
        DrawRect(new Rect2(Hue * Size.X - 2, -2, 4, Size.Y + 4), Look.Cream, false, 2f);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } b) Pick(b.Position.X);
        else if (e is InputEventMouseMotion { ButtonMask: MouseButtonMask.Left } m) Pick(m.Position.X);
    }

    void Pick(float x)
    {
        Hue = Mathf.Clamp(x / Size.X, 0f, 0.999f);
        QueueRedraw();
        Picked?.Invoke(Hue);
    }
}

// Saturation across, brightness up, for the chosen hue; the corners stay in the world's
// range because MarkPicker clamps what comes out.
public partial class ShadeSquare : Control
{
    float hue, s = 0.6f, v = 0.6f;
    public float Hue { get => hue; set { hue = value; QueueRedraw(); } }
    public event Action<float, float>? Picked;

    public void Mark(float sat, float val)
    {
        s = sat;
        v = val;
        QueueRedraw();
    }

    public override void _Draw()
    {
        const int nx = 24, ny = 10;
        float w = Size.X / nx, h = Size.Y / ny;
        for (int x = 0; x < nx; x++)
            for (int y = 0; y < ny; y++)
                DrawRect(new Rect2(x * w, y * h, w + 1, h + 1), Color.FromHsv(hue, x / (float)(nx - 1) * 0.8f, 0.9f - y / (float)(ny - 1) * 0.68f));
        var at = new Vector2(s / 0.8f * Size.X, (0.9f - v) / 0.68f * Size.Y);
        DrawArc(at, 6f, 0, Mathf.Tau, 16, Look.Cream, 2f, true);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } b) Pick(b.Position);
        else if (e is InputEventMouseMotion { ButtonMask: MouseButtonMask.Left } m) Pick(m.Position);
    }

    void Pick(Vector2 p)
    {
        s = Mathf.Clamp(p.X / Size.X, 0f, 1f) * 0.8f;
        v = 0.9f - Mathf.Clamp(p.Y / Size.Y, 0f, 1f) * 0.68f;
        QueueRedraw();
        Picked?.Invoke(s, v);
    }
}
