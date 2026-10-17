using Godot;

namespace LifeGame;

// A picture card for the build menu and the crafting and station grids (ui-33). A plain
// Button doesn't measure what's inside it, so a long cost row once ran past its card into
// the next one (Justin: "the build UI is fucked up"). A Card is as wide as asked, grows
// taller to fit its content, and clips anything that would still spill; rows of icons go
// in Wrap() rows that break onto a second line inside the card instead of widening it.
public partial class Card : Button
{
    const float Pad = 8f;
    public readonly VBoxContainer Column = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly float width;

    public Card(float width, float minHeight = 0f)
    {
        this.width = width;
        FocusMode = FocusModeEnum.None;
        ClipContents = true;
        CustomMinimumSize = new Vector2(width, minHeight);
        var style = new StyleBoxFlat { BgColor = new Color(0.09f, 0.10f, 0.07f, 0.6f), BorderColor = new Color(Look.Cream, 0.22f) };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(10);
        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BorderColor = new Color(Look.Cream, 0.6f);
        AddThemeStyleboxOverride("normal", style);
        AddThemeStyleboxOverride("hover", hover);
        AddThemeStyleboxOverride("pressed", hover);
        Column.SetAnchorsPreset(LayoutPreset.FullRect);
        Column.OffsetLeft = Column.OffsetTop = Pad;
        Column.OffsetRight = Column.OffsetBottom = -Pad;
        Column.AddThemeConstantOverride("separation", 4);
        Column.MinimumSizeChanged += UpdateMinimumSize;
        AddChild(Column);
    }

    // As tall as the content needs; never narrower than asked.
    public override Vector2 _GetMinimumSize()
    {
        var inside = Column.GetCombinedMinimumSize();
        return new Vector2(Mathf.Max(width, inside.X + 2 * Pad), inside.Y + 2 * Pad);
    }

    // The width content may use inside the card.
    public float Inner => width - 2 * Pad;

    // A centred row of icons and numbers that wraps inside the card.
    public HFlowContainer Wrap()
    {
        var row = new HFlowContainer { Alignment = FlowContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(Inner, 0) };
        row.AddThemeConstantOverride("h_separation", 5);
        row.AddThemeConstantOverride("v_separation", 2);
        return row;
    }

    // A centred line of text that wraps to the card's width rather than widening it.
    public Label Line(string text, int size)
    {
        var label = Hud.Text(text, size);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(Inner, 0);
        label.MouseFilter = MouseFilterEnum.Ignore;
        return label;
    }
}
