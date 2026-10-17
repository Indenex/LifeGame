using System;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Decorating a damp pot with slip (ceramics-14; Justin: a banded rim, and a free colour like the
// maker's mark). A small window opened from the Use prompt at the Potter's bench: a pot that shows
// the bands as you choose, one band or two, the same earthy presets, shade square and hue strip as
// the mark panel (so a slip colour keeps to the same readable range), and an Apply that sends it to
// the server (SlipPiece). Esc leaves the pot as it is.
public partial class SlipPicker : CanvasLayer
{
    public bool IsOpen => panel.Visible;

    Control panel = null!;
    SlipPot pot = null!;
    HueStrip hue = null!;
    ShadeSquare shade = null!;
    Button oneBand = null!, twoBands = null!;
    Label costLine = null!;
    Action<uint, int>? apply;
    Color ink = MarkColour.ToColor(Marks.DefaultColor);
    int bands = 1;

    public override void _Ready()
    {
        Layer = 5;
        panel = MakePanel();
        panel.Visible = false;
        AddChild(panel);
    }

    // Opens for one pot; `apply` is called with the chosen colour and band count when Apply is pressed.
    public void Open(ClayForm form, uint currentWear, Action<uint, int> onApply)
    {
        apply = onApply;
        if (Slip.Has(currentWear)) { ink = MarkColour.ToColor(Slip.Color(currentWear)); bands = Slip.Bands(currentWear); }
        pot.Form = form;
        panel.Visible = true;
        SyncColour();
        SetBands(bands);
    }

    public void Close() => panel.Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (!IsOpen || e is not InputEventKey { Pressed: true, Echo: false }) return;
        if (e.IsActionPressed("ui_cancel")) Close();
        else if (e is InputEventKey { PhysicalKeycode: Key.Enter or Key.KpEnter }) Apply();
        else return;
        GetViewport().SetInputAsHandled();
    }

    void Apply()
    {
        apply?.Invoke(Marks.ClampColor(MarkColour.ToRgb(ink)), bands);
        Close();
    }

    Control MakePanel()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        box.AddChild(Ui.Title("Slip for the rim"));

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 26);
        box.AddChild(columns);

        pot = new SlipPot { CustomMinimumSize = new Vector2(220, 190) };
        columns.AddChild(pot);

        var right = new VBoxContainer { CustomMinimumSize = new Vector2(260, 0) };
        right.AddThemeConstantOverride("separation", 10);
        columns.AddChild(right);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        oneBand = ControlsPage.TextButton("One band", () => SetBands(1));
        twoBands = ControlsPage.TextButton("Two bands", () => SetBands(2));
        row.AddChild(oneBand);
        row.AddChild(twoBands);
        right.AddChild(row);

        var presets = new GridContainer { Columns = 6 };
        presets.AddThemeConstantOverride("h_separation", 6);
        presets.AddThemeConstantOverride("v_separation", 6);
        foreach (var c in Marks.Presets.Select(MarkColour.ToColor).Select(MarkColour.Clamp))
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

        shade = new ShadeSquare { CustomMinimumSize = new Vector2(260, 100) };
        shade.Picked += (s, v) => { ink = MarkColour.Clamp(Color.FromHsv(hue.Hue, s, v)); UpdatePreview(); };
        right.AddChild(shade);
        hue = new HueStrip { CustomMinimumSize = new Vector2(260, 18) };
        hue.Picked += h => { shade.Hue = h; ink.ToHsv(out _, out float s, out float v); ink = MarkColour.Clamp(Color.FromHsv(h, s, v)); UpdatePreview(); };
        right.AddChild(hue);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 12);
        buttons.AddChild(Ui.MainButton("Apply", Apply));
        costLine = Hud.Text($"Uses {Slip.ClayCost} clean clay    Enter applies    Esc leaves it", 13);
        costLine.Modulate = new Color(1, 1, 1, 0.65f);
        buttons.AddChild(costLine);
        right.AddChild(buttons);

        return Ui.Window(box);
    }

    void SetBands(int n)
    {
        bands = n;
        oneBand.Modulate = new Color(1, 1, 1, n == 1 ? 1f : 0.6f);
        twoBands.Modulate = new Color(1, 1, 1, n == 2 ? 1f : 0.6f);
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

    // The colour the pot will really carry: clamped to the mark range and snapped to the slip grid.
    Color Stored => MarkColour.ToColor(Slip.Color(Slip.With(0, MarkColour.ToRgb(ink), bands)));

    void UpdatePreview()
    {
        pot.Show(Stored, bands);
        hue.QueueRedraw();
    }
}

// A plain clay pot, side on, with the slip bands round its rim.
public partial class SlipPot : Control
{
    public ClayForm Form = ClayForm.CoilPot;
    Color slip = Colors.White;
    int bands = 1;

    public void Show(Color colour, int count)
    {
        slip = colour;
        bands = count;
        QueueRedraw();
    }

    // Half the pot's width at a height t, 0 at the rim down to 1 at the foot (as a fraction of the belly radius).
    float Half(float t) => Form switch
    {
        ClayForm.PinchPot => 0.62f + 0.38f * MathF.Sin(MathF.Min(1f, t * 1.15f + 0.08f) * MathF.PI * 0.62f) - 0.18f * t * t,
        ClayForm.StorageJar => 0.50f + 0.50f * MathF.Sin(MathF.Min(1f, t * 1.2f + 0.1f) * MathF.PI * 0.75f) - 0.1f * t * t,
        _ => 0.66f + 0.34f * MathF.Sin(MathF.Min(1f, t * 1.2f + 0.1f) * MathF.PI * 0.7f) - 0.14f * t * t,
    };

    public override void _Draw()
    {
        var centre = new Vector2(Size.X / 2, Size.Y / 2);
        float tall = Size.Y * 0.84f, belly = Size.X * 0.4f;
        if (Form == ClayForm.PinchPot) tall *= 0.7f;
        float top = centre.Y - tall / 2;
        const int steps = 40;
        Vector2 Left(float t) => new(centre.X - Half(t) * belly, top + t * tall);
        Vector2 Right(float t) => new(centre.X + Half(t) * belly, top + t * tall);

        // A strip of the body between two heights, the same shape as the body.
        Vector2[] Strip(float a, float b)
        {
            var pts = new System.Collections.Generic.List<Vector2>();
            int n = Math.Max(2, (int)((b - a) * steps));
            for (int i = 0; i <= n; i++) pts.Add(Left(a + (b - a) * i / n));
            for (int i = n; i >= 0; i--) pts.Add(Right(a + (b - a) * i / n));
            return pts.ToArray();
        }

        DrawColoredPolygon(Strip(0f, 1f), Look.Clay);
        // The darker inside seen over the rim.
        DrawColoredPolygon(new[]
        {
            new Vector2(Left(0f).X + 3, top - 5), new Vector2(Right(0f).X - 3, top - 5),
            new Vector2(Right(0f).X - 3, top + 3), new Vector2(Left(0f).X + 3, top + 3),
        }, Look.Clay.Darkened(0.28f));
        DrawColoredPolygon(Strip(0f, 0.12f), slip);
        if (bands > 1) DrawColoredPolygon(Strip(0.2f, 0.28f), slip);
        // A soft highlight on the left of the belly.
        DrawCircle(centre + new Vector2(-belly * 0.5f, tall * 0.12f), belly * 0.16f, new Color(1, 1, 1, 0.07f));
    }
}
