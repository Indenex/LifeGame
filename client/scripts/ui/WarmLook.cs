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
    static readonly string[] BoxKeys = { "panel", "normal", "hover", "pressed", "disabled", "focus", "read_only", "background", "fill" };

    readonly List<Control> windows = new();
    float scan;

    public override void _EnterTree()
    {
        On = !OS.GetCmdlineUserArgs().Contains("--cold");
        if (!On) return;
        GetTree().NodeAdded += Added;
        var warm = Defaults();
        GetTree().Root.Theme = warm;
        // Windows and the HUD sit under CanvasLayers, and a theme set on the root window doesn't
        // reach through a CanvasLayer: their plain buttons and text boxes fell back to Godot's
        // own dark grey (ui-58, Render B's pairs). Merging into the engine's default theme
        // reaches every control; anything that styles itself still wins.
        ThemeDB.GetDefaultTheme().MergeWith(warm);
    }

    // The game-wide defaults (ui-44): Nunito for every control that doesn't choose a font, and
    // hover tooltips as small paper notes in brown ink instead of Godot's grey box.
    static Theme Defaults()
    {
        var theme = new Theme { DefaultFont = Ui.Font(650), DefaultFontSize = 15 };
        var note = new StyleBoxFlat
        {
            BgColor = Paper,
            BorderColor = new Color(Ink, 0.28f),
            ShadowColor = new Color(0.15f, 0.08f, 0.05f, 0.25f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(0, 2),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
        };
        note.SetBorderWidthAll(2);
        note.SetCornerRadiusAll(12);
        theme.SetStylebox("panel", "TooltipPanel", note);
        theme.SetColor("font_color", "TooltipLabel", Ink);
        theme.SetColor("font_shadow_color", "TooltipLabel", new Color(0, 0, 0, 0));
        theme.SetFontSize("font_size", "TooltipLabel", 14);
        theme.SetFont("font", "TooltipLabel", Ui.Font(700));

        // Buttons and text boxes that bring no style of their own (ui-51): paper a shade darker
        // than the window, a soft brown edge, brown words. Godot's own dark grey fills read as
        // cold holes in the paper. A selected tab chip (a plain button among flat ones) gets the
        // same paper look.
        foreach (var (state, bg, edge) in new[]
        {
            ("normal", Paper.Darkened(0.07f), 0.22f),
            ("hover", Butter.Lerp(Paper, 0.55f), 0.35f),
            ("pressed", Paper.Darkened(0.13f), 0.35f),
            ("disabled", Paper.Darkened(0.04f), 0.12f),
            ("focus", new Color(0, 0, 0, 0), 0f),
        })
        {
            var s = new StyleBoxFlat
            {
                BgColor = bg, BorderColor = new Color(Ink, edge),
                // Snug sides, so rows of tabs and item buttons fit as they did on the dark look (ui-59).
                ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 3, ContentMarginBottom = 3,
            };
            s.SetBorderWidthAll(state == "focus" ? 0 : 2);
            s.SetCornerRadiusAll(14);
            theme.SetStylebox(state, "Button", s);
        }
        foreach (var key in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
            theme.SetColor(key, "Button", Ink);
        theme.SetColor("font_disabled_color", "Button", new Color(Ink, 0.45f));

        // Text boxes: a lighter paper, like a slip of card to write on.
        foreach (var (state, bg, edge) in new[] { ("normal", Paper.Lightened(0.4f), 0.28f), ("focus", new Color(0, 0, 0, 0), 0f), ("read_only", Paper.Darkened(0.04f), 0.15f) })
        {
            var s = new StyleBoxFlat
            {
                BgColor = bg, BorderColor = new Color(state == "focus" ? Coral : Ink, state == "focus" ? 0.8f : edge),
                ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4,
                DrawCenter = state != "focus",
            };
            s.SetBorderWidthAll(2);
            s.SetCornerRadiusAll(12);
            theme.SetStylebox(state, "LineEdit", s);
        }
        theme.SetColor("font_color", "LineEdit", Ink);
        theme.SetColor("font_placeholder_color", "LineEdit", new Color(Ink, 0.45f));
        theme.SetColor("caret_color", "LineEdit", Coral.Darkened(0.2f));
        theme.SetColor("selection_color", "LineEdit", new Color(Butter, 0.5f));
        theme.SetColor("clear_button_color", "LineEdit", new Color(Ink, 0.6f));
        return theme;
    }

    // The one filled button in a window (Ui.MainButton): soft coral with cream words on paper,
    // like the character page's Play, rather than re-inked (brown on brown would not read).
    public static void MainButton(Button b)
    {
        if (!On) return;
        b.SetMeta(Keep, true);
        b.AddThemeFontOverride("font", Ui.Font(800));
        foreach (var (state, c) in new[] { ("normal", Coral), ("hover", Coral.Lightened(0.1f)), ("pressed", Coral.Darkened(0.06f)), ("disabled", Coral.Lerp(Paper, 0.6f)) })
        {
            var s = new StyleBoxFlat { BgColor = c, ShadowColor = new Color(Coral.Darkened(0.4f), 0.3f), ShadowSize = state == "pressed" ? 2 : 5, ShadowOffset = new Vector2(0, state == "pressed" ? 1 : 3) };
            s.SetCornerRadiusAll(20);
            b.AddThemeStyleboxOverride(state, s);
        }
        foreach (var key in new[] { "font_color", "font_hover_color", "font_pressed_color" }) b.AddThemeColorOverride(key, new Color(1f, 0.98f, 0.95f));
        b.AddThemeColorOverride("font_disabled_color", new Color(1f, 0.98f, 0.95f, 0.7f));
    }

    // A panel that sits on the world rather than in a window (the needs gauges, the land chip):
    // a small paper chip, round and a little see-through so the world shows at its edge, like
    // the bar's paper buttons (ui-45, ui-54). Its insides are inked and left alone after.
    public static void HudChip(PanelContainer p)
    {
        if (!On) return;
        var s = new StyleBoxFlat
        {
            BgColor = new Color(Paper, 0.9f),
            BorderColor = new Color(Ink, 0.22f),
            ShadowColor = new Color(0.15f, 0.08f, 0.05f, 0.22f),
            ShadowSize = 8,
            ShadowOffset = new Vector2(0, 3),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
        };
        s.SetBorderWidthAll(2);
        s.SetCornerRadiusAll(22);
        p.AddThemeStyleboxOverride("panel", s);
        p.SetMeta(Keep, true);
        InkInside(p);
    }

    // Words floating in the world (names, chat bubbles, cues, "Your waking place", claim
    // markers; ui-56): the warm look's rounded Nunito, and a soft brown edge instead of a near
    // black one, so they sit in the warm light like the HUD's words do. A slanted font (emotes,
    // cues) stays slanted. Colours are left alone.
    static void WarmWorldWords(Label3D l)
    {
        if (l.HasMeta(Keep)) return;
        l.SetMeta(Keep, true);
        l.Font = l.Font is FontVariation slant && slant.BaseFont == null
            ? new FontVariation { BaseFont = Ui.Font(800), VariationTransform = slant.VariationTransform }
            : l.Font == null ? Ui.Font(800) : l.Font;
        var edge = l.OutlineModulate;
        if (edge.Luminance < 0.2f) l.OutlineModulate = new Color(0.24f, 0.13f, 0.07f, edge.A);
    }

    // Controls that style themselves for paper are left as they are.
    const string Keep = "warm_keep";

    // A colour swatch shows a real colour (skin, hair, a mark's ink): never re-inked.
    public static void KeepColours(Control c) => c.SetMeta(Keep, true);

    // The ring round a swatch: cream on dark, brown ink on paper.
    public static Color RingColour(float alpha) => On ? new Color(Ink, alpha) : new Color(Look.Cream, alpha);

    // Content already on paper (a tooltip note): ink it like a window's, without a frame.
    public static void InkInside(Control c)
    {
        if (On) c.SetMeta(Mark, true);
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
        if (node is Label3D words)
        {
            // After whoever made it has set its font and colours.
            Callable.From(() => { if (IsInstanceValid(words)) WarmWorldWords(words); }).CallDeferred();
            return;
        }
        if (node is not Control c) return;
        // After the panel has set its own colours.
        Callable.From(() =>
        {
            if (!IsInstanceValid(c) || !c.IsInsideTree()) return;
            if (c is PanelContainer p && !p.HasMeta(Mark) && IsHandFramed(p))
            {
                var style = (StyleBoxFlat)((StyleBoxFlat)p.GetThemeStylebox("panel")).Duplicate();
                p.SetMeta(Ui.WindowMark, true);
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
        if (c.HasMeta(Mark) || c.HasMeta(Keep)) return;
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
            // Bars keep their thin corners.
            var warm = Restyle(flat, round: key is not ("background" or "fill"));
            warm.SetMeta("warm", true);
            c.AddThemeStyleboxOverride(key, warm);
        }
        // Pictures and swatches keep their own colours; a faded modulate keeps its fade.
    }

    static StyleBoxFlat Restyle(StyleBoxFlat old, bool round = true)
    {
        var s = (StyleBoxFlat)old.Duplicate();
        s.BgColor = Recolour(old.BgColor, ground: true);
        s.BorderColor = Recolour(old.BorderColor);
        if (old.ShadowSize > 0) s.ShadowColor = new Color(0.25f, 0.12f, 0.08f, Mathf.Min(old.ShadowColor.A, 0.18f));
        if (!round) return s;
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
