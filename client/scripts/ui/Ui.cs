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
}
