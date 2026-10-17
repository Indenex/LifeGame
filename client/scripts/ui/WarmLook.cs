using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// The warm look for windows (D-313, Justin: "Cutesy, relaxing, with a hint of realism"; he picked
// "warm paper windows"). Every centre window, any panel framed with Ui.Window or Ui.Frame,
// becomes warm paper: cream, round corners, a soft shadow. Everything inside is re-inked: cream
// text turns soft brown in Nunito, dark slot grounds turn to paper shades, cream borders to
// brown. It happens here, in one place, as things are added and whenever a panel later changes
// a colour, so no panel needs its own warm version. --cold shows the old dark look, for
// comparing.
public partial class WarmLook : Node
{
    public static bool On { get; private set; } = true;

    public static readonly Color Paper = new(0.99f, 0.95f, 0.88f), Ink = new(0.42f, 0.30f, 0.24f),
        Coral = new(0.95f, 0.56f, 0.47f), Butter = new(0.98f, 0.82f, 0.45f), Sage = new(0.60f, 0.78f, 0.55f);

    const string Mark = "warm_window";
    static readonly string[] ColourKeys = { "font_color", "font_hover_color", "font_pressed_color", "font_disabled_color", "font_placeholder_color", "font_uneditable_color" };
    static readonly string[] BoxKeys = { "panel", "normal", "hover", "pressed", "disabled", "focus", "read_only" };

    readonly List<Control> windows = new();
    float scan;

    public override void _EnterTree()
    {
        On = !OS.GetCmdlineUserArgs().Contains("--cold");
        if (On) GetTree().NodeAdded += Added;
    }

    // Ui.Window calls this for its frame; panels framed by hand with Ui.Frame are found as they're added.
    public static void Frame(PanelContainer panel, StyleBoxFlat style)
    {
        if (!On) return;
        panel.SetMeta(Mark, true);
        style.BgColor = Paper;
        style.BorderColor = new Color(Ink, 0.25f);
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(26);
        style.ShadowColor = new Color(0.15f, 0.08f, 0.05f, 0.30f);
        style.ShadowSize = 18;
        style.ShadowOffset = new Vector2(0, 6);
        style.ContentMarginLeft += 8;
        style.ContentMarginRight += 8;
        style.ContentMarginTop += 4;
        style.ContentMarginBottom += 4;
    }

    void Added(Node node)
    {
        if (node is not Control c) return;
        // After the panel has set its own colours.
        Callable.From(() =>
        {
            if (!IsInstanceValid(c) || !c.IsInsideTree()) return;
            if (c is PanelContainer p && !p.HasMeta(Mark) && IsHandFramed(p))
            {
                var style = (StyleBoxFlat)((StyleBoxFlat)p.GetThemeStylebox("panel")).Duplicate();
                Frame(p, style);
                p.AddThemeStyleboxOverride("panel", style);
            }
            if (c.HasMeta(Mark) && !windows.Contains(c)) windows.Add(c);
            if (InWindow(c)) Reink(c);
        }).CallDeferred();
    }

    // A panel framed by hand with the shared window colour (Ui.Frame) is a window too.
    static bool IsHandFramed(PanelContainer p) =>
        p.HasThemeStyleboxOverride("panel") && p.GetThemeStylebox("panel") is StyleBoxFlat s
        && Mathf.IsEqualApprox(s.BgColor.R, Ui.Frame.R) && Mathf.IsEqualApprox(s.BgColor.G, Ui.Frame.G)
        && Mathf.IsEqualApprox(s.BgColor.B, Ui.Frame.B) && s.BgColor.A > 0.85f;

    static bool InWindow(Control c)
    {
        for (Node? n = c; n != null; n = n.GetParent())
            if (n.HasMeta(Mark)) return true;
        return false;
    }

    // Panels change colours as you use them (hover rings, a chosen slot, a refreshed list), so the
    // open windows are looked over a few times a second; only what changed is re-inked.
    public override void _Process(double delta)
    {
        if (!On || (scan -= (float)delta) > 0f) return;
        scan = 0.2f;
        windows.RemoveAll(w => !IsInstanceValid(w));
        foreach (var w in windows)
            if (w.IsVisibleInTree()) Walk(w);
    }

    static void Walk(Node n)
    {
        foreach (var child in n.GetChildren())
        {
            if (child is Control c) Reink(c);
            Walk(child);
        }
    }

    // Cream on dark becomes brown on paper. Each override remembers what it was turned into, so
    // looking again changes only what the panel has set since.
    static void Reink(Control c)
    {
        if (c.HasMeta(Mark)) return;
        foreach (var key in ColourKeys)
        {
            if (!c.HasThemeColorOverride(key)) continue;
            var now = c.GetThemeColor(key);
            string meta = "warm_c_" + key;
            if (c.HasMeta(meta) && c.GetMeta(meta).AsColor() == now) continue;
            var warm = Recolour(now);
            c.AddThemeColorOverride(key, warm);
            c.SetMeta(meta, warm);
        }
        if (c is Label or Button or LineEdit or RichTextLabel && !c.HasMeta("warm_font"))
        {
            c.SetMeta("warm_font", true);
            c.AddThemeFontOverride("font", Ui.Font(c is Label ? 650 : 750));
            // Paper needs no shadow or outline behind the words.
            c.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0));
            c.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0));
            if (!c.HasThemeColorOverride("font_color"))
            {
                c.AddThemeColorOverride("font_color", Ink);
                c.SetMeta("warm_c_font_color", Ink);
            }
        }
        foreach (var key in BoxKeys)
        {
            if (!c.HasThemeStyleboxOverride(key) || c.GetThemeStylebox(key) is not StyleBoxFlat flat) continue;
            if (flat.HasMeta("warm")) continue;
            var warm = Restyle(flat);
            warm.SetMeta("warm", true);
            c.AddThemeStyleboxOverride(key, warm);
        }
        // Pictures and swatches keep their own colours; a faded modulate keeps its fade.
    }

    static StyleBoxFlat Restyle(StyleBoxFlat old)
    {
        var s = (StyleBoxFlat)old.Duplicate();
        s.BgColor = Recolour(old.BgColor, ground: true);
        s.BorderColor = Recolour(old.BorderColor);
        if (old.ShadowSize > 0) s.ShadowColor = new Color(0.25f, 0.12f, 0.08f, Mathf.Min(old.ShadowColor.A, 0.18f));
        // Rounder, friendlier corners.
        s.CornerRadiusTopLeft = Mathf.Max(old.CornerRadiusTopLeft, 10);
        s.CornerRadiusTopRight = Mathf.Max(old.CornerRadiusTopRight, 10);
        s.CornerRadiusBottomLeft = Mathf.Max(old.CornerRadiusBottomLeft, 10);
        s.CornerRadiusBottomRight = Mathf.Max(old.CornerRadiusBottomRight, 10);
        return s;
    }

    // The old palette's colours, in the warm one. Cream (text, borders) becomes ink; dark grounds
    // become paper a shade darker the darker they were; other colours (amber warnings, gold
    // rings, swatches) stay themselves but deepen enough to read on paper.
    public static Color Recolour(Color c, bool ground = false)
    {
        if (c.A <= 0.001f) return c;
        if (c.R > 0.8f && c.G > 0.75f && c.B > 0.6f) return new Color(Ink, Mathf.Clamp(c.A * 1.1f, 0f, 1f));
        float lum = c.Luminance;
        if (lum < 0.3f)
        {
            float depth = Mathf.Clamp((0.3f - lum) / 0.3f, 0f, 1f);
            return ground
                ? new Color(Paper.Darkened(0.05f + 0.07f * depth), Mathf.Clamp(0.35f + c.A, 0f, 1f))
                : new Color(Ink, c.A);
        }
        return c.Darkened(0.3f);
    }
}
