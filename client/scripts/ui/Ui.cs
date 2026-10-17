using System;
using Godot;

namespace LifeGame;

// The one look every window shares (ui-25): the soft dark frame (D-125), its corner,
// margins and title size. A panel calls Window() for its frame and Title() for its
// heading, so a change here restyles them all.
public static class Ui
{
    // Nearly opaque, so world labels (nameplates, claim markers) don't read through a window
    // over a dark forest (Render B on #494).
    public static readonly Color Frame = new(0.10f, 0.11f, 0.09f, 0.94f);
    public const int Corner = 12, TitleSize = 20;

    // A centred window around the content; roomy gives the bigger margins of a full page (Options).
    public static PanelContainer Window(Control content, bool roomy = false)
    {
        var panel = Hud.Panel(content, Vector2.Zero);
        var style = (StyleBoxFlat)panel.GetThemeStylebox("panel");
        style.BgColor = Frame;
        style.SetCornerRadiusAll(Corner);
        style.ContentMarginTop = style.ContentMarginBottom = roomy ? 18 : 14;
        style.ContentMarginLeft = style.ContentMarginRight = roomy ? 28 : 18;
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        return panel;
    }

    public static Label Title(string text) => Hud.Text(text, TitleSize);

    // Nunito (SIL OFL, client/assets/fonts; D-313 "cutesy, relaxing"): the rounded, friendly
    // face for the warm look, at a weight from 200 (light) to 1000 (black). Falls back to the
    // default font if the file is missing.
    static FontFile? nunito;
    static bool triedNunito;
    public static Font Font(int weight = 600)
    {
        if (!triedNunito)
        {
            triedNunito = true;
            if (ResourceLoader.Exists("res://assets/fonts/Nunito.ttf")) nunito = GD.Load<FontFile>("res://assets/fonts/Nunito.ttf");
        }
        var face = new FontVariation { BaseFont = nunito ?? ThemeDB.FallbackFont };
        face.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), weight } };
        return face;
    }

    // The one filled button in a window: the thing to do there (Done, Send). Dimmed when disabled.
    public static Button MainButton(string text, Action pressed)
    {
        var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(150, 38) };
        b.AddThemeFontSizeOverride("font_size", 16);
        StyleBoxFlat Box(Color c)
        {
            var s = new StyleBoxFlat { BgColor = c };
            s.SetCornerRadiusAll(8);
            return s;
        }
        b.AddThemeStyleboxOverride("normal", Box(new Color(0.55f, 0.42f, 0.24f, 0.95f)));
        b.AddThemeStyleboxOverride("hover", Box(new Color(0.64f, 0.5f, 0.3f, 1f)));
        b.AddThemeStyleboxOverride("pressed", Box(new Color(0.64f, 0.5f, 0.3f, 1f)));
        b.AddThemeStyleboxOverride("disabled", Box(new Color(0.3f, 0.27f, 0.22f, 0.7f)));
        b.AddThemeColorOverride("font_color", Look.Cream);
        b.AddThemeColorOverride("font_hover_color", Look.Cream);
        b.AddThemeColorOverride("font_disabled_color", new Color(Look.Cream, 0.45f));
        b.Pressed += pressed;
        return b;
    }
}
