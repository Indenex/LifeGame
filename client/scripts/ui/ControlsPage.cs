using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// The Controls page of the Esc menu: every key, which you can change by clicking it
// and pressing a new one, the camera's turn speed and mouse sensitivity, and a way
// back to the defaults. Changes save at once (ui/Settings.cs); the F1 list, the hints
// and the G prompt all read the live keys.
public partial class ControlsPage : Control
{
    const float DefaultEase = 7f, DefaultSensitivity = 1f;

    // Back to the menu (PauseMenu sets it).
    public Action Back = () => { };

    readonly GridContainer grid = new() { Columns = 2 };
    readonly Dictionary<string, Button> keys = new();
    HSlider turn = null!, sensitivity = null!;
    string? capturing;

    // Startup: camera settings and the player's saved keys. Deferred so every lane has
    // bound its own keys first, and those defaults are what Reset goes back to.
    public static void ApplySaved()
    {
        CameraRig.TurnEase = Settings.Get("camera", "turn_ease", DefaultEase);
        CameraDrag.Sensitivity = Settings.Get("camera", "sensitivity", DefaultSensitivity);
        Controls.ApplySaved();
    }

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 12);
        // On the warm look the page is a paper window like Options (ui-68); the cold look
        // keeps its words straight over the dimmed world.
        if (WarmLook.On) AddChild(Ui.Window(column, roomy: true));
        else
        {
            column.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(column);
        }

        var title = new Label { Text = "Controls", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(title, 26, 1f);
        column.AddChild(title);
        var help = new Label { Text = "Click a key, then press the new one.  Esc goes back.", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(help, 14, 0.6f);
        column.AddChild(help);

        // The list scrolls when the window is short.
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
        };
        grid.AddThemeConstantOverride("h_separation", 28);
        grid.AddThemeConstantOverride("v_separation", 6);
        scroll.AddChild(grid);
        column.AddChild(scroll);
        scrollBox = scroll;

        turn = SliderRow(column, "Turn speed", 3.5, 14, 0.5, Settings.Get("camera", "turn_ease", DefaultEase), v =>
        {
            CameraRig.TurnEase = (float)v;
            Settings.Set("camera", "turn_ease", v);
        });
        sensitivity = SliderRow(column, "Mouse sensitivity", 0.3, 2.5, 0.1, Settings.Get("camera", "sensitivity", DefaultSensitivity), v =>
        {
            CameraDrag.Sensitivity = (float)v;
            Settings.Set("camera", "sensitivity", v);
        });

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 40);
        buttons.AddChild(TextButton("Reset to defaults", Reset));
        buttons.AddChild(TextButton("Back", () => Back()));
        column.AddChild(buttons);
    }

    ScrollContainer scrollBox = null!;

    public void Open()
    {
        Fill();
        Visible = true;
        capturing = null;
        keys.Values.FirstOrDefault()?.GrabFocus();
    }

    public void Close()
    {
        capturing = null;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        // A list that fits; taller than half the window, it scrolls.
        float room = Mathf.Max(GetViewportRect().Size.Y * 0.52f, 200f);
        scrollBox.CustomMinimumSize = new Vector2(grid.GetCombinedMinimumSize().X + 16, Mathf.Min(grid.GetCombinedMinimumSize().Y, room));
    }

    public override void _Input(InputEvent e)
    {
        if (!Visible) return;
        if (capturing != null)
        {
            // The next key or mouse button becomes the action's key; Esc keeps the old one.
            if (e is InputEventKey { Pressed: true, Echo: false } key)
            {
                if (key.PhysicalKeycode != Key.Escape) Controls.Rebind(capturing, new InputEventKey { PhysicalKeycode = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode });
                Captured();
            }
            else if (e is InputEventMouseButton { Pressed: true } button && button.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.WheelLeft or MouseButton.WheelRight))
            {
                Controls.Rebind(capturing, new InputEventMouseButton { ButtonIndex = button.ButtonIndex });
                Captured();
            }
            return;
        }
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape })
        {
            GetViewport().SetInputAsHandled();
            Back();
        }
    }

    void Captured()
    {
        capturing = null;
        GetViewport().SetInputAsHandled();
        Refresh();
    }

    void Fill()
    {
        foreach (var child in grid.GetChildren())
        {
            grid.RemoveChild(child);
            child.QueueFree();
        }
        keys.Clear();
        foreach (var b in Controls.Rebindable)
        {
            var what = new Label { Text = b.Label, VerticalAlignment = VerticalAlignment.Center };
            Hud.Soft(what, 17, 0.85f);
            grid.AddChild(what);
            var button = KeyButton();
            string action = b.Action;
            button.Pressed += () =>
            {
                capturing = action;
                Refresh();
            };
            keys[action] = button;
            grid.AddChild(button);
        }
        Refresh();
    }

    void Refresh()
    {
        foreach (var (action, button) in keys)
        {
            var names = InputMap.HasAction(action)
                ? InputMap.ActionGetEvents(action).Select(Controls.EventName).OfType<string>().Distinct().ToList()
                : new List<string>();
            button.Text = action == capturing ? "Press a key" : names.Count > 0 ? string.Join("  ", names) : "none";
            button.Modulate = new Color(1, 1, 1, action == capturing ? 1f : 0.9f);
        }
    }

    void Reset()
    {
        Controls.ResetAll();
        turn.Value = DefaultEase;
        sensitivity.Value = DefaultSensitivity;
        capturing = null;
        Refresh();
    }

    static Button KeyButton()
    {
        var button = new Button { CustomMinimumSize = new Vector2(120, 30), FocusMode = FocusModeEnum.All };
        button.AddThemeFontSizeOverride("font_size", 16);
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
            button.AddThemeColorOverride(state, Look.Cream);
        StyleBoxFlat Cap(float border, float bg)
        {
            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.09f, 0.10f, 0.07f, bg),
                BorderColor = new Color(Look.Cream, border),
                ContentMarginLeft = 12,
                ContentMarginRight = 12,
                ContentMarginTop = 2,
                ContentMarginBottom = 4,
            };
            style.SetBorderWidthAll(1);
            style.BorderWidthBottom = 3;
            style.SetCornerRadiusAll(6);
            return style;
        }
        button.AddThemeStyleboxOverride("normal", Cap(0.45f, 0.35f));
        button.AddThemeStyleboxOverride("hover", Cap(0.9f, 0.45f));
        button.AddThemeStyleboxOverride("pressed", Cap(1f, 0.55f));
        button.AddThemeStyleboxOverride("focus", Cap(0.9f, 0f));
        return button;
    }

    internal static Button TextButton(string text, Action pressed)
    {
        var button = new Button { Text = text, Flat = true, FocusMode = FocusModeEnum.All };
        button.AddThemeFontSizeOverride("font_size", 20);
        foreach (var state in new[] { "font_color", "font_pressed_color" })
            button.AddThemeColorOverride(state, new Color(Look.Cream, 0.7f));
        foreach (var state in new[] { "font_hover_color", "font_focus_color" })
            button.AddThemeColorOverride(state, Look.Cream);
        button.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.3f));
        button.AddThemeConstantOverride("outline_size", 6);
        foreach (var box in new[] { "normal", "hover", "pressed", "focus" })
            button.AddThemeStyleboxOverride(box, new StyleBoxEmpty());
        button.Pressed += pressed;
        return button;
    }

    internal static HSlider SliderRow(Container column, string text, double min, double max, double step, double value, Action<double> changed)
    {
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 16);
        var label = new Label { Text = text, CustomMinimumSize = new Vector2(190, 0), HorizontalAlignment = HorizontalAlignment.Right };
        Hud.Soft(label, 18, 0.85f);
        row.AddChild(label);
        var slider = new HSlider
        {
            MinValue = min, MaxValue = max, Step = step, Value = value,
            CustomMinimumSize = new Vector2(200, 24),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            FocusMode = FocusModeEnum.All,
        };
        var track = new StyleBoxFlat { BgColor = new Color(Look.Cream, 0.25f), ContentMarginTop = 1, ContentMarginBottom = 1 };
        track.SetCornerRadiusAll(2);
        var fill = new StyleBoxFlat { BgColor = new Color(Look.Cream, 0.85f), ContentMarginTop = 1, ContentMarginBottom = 1 };
        fill.SetCornerRadiusAll(2);
        slider.AddThemeStyleboxOverride("slider", track);
        slider.AddThemeStyleboxOverride("grabber_area", fill);
        slider.AddThemeStyleboxOverride("grabber_area_highlight", fill);
        slider.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        slider.ValueChanged += v => changed(v);
        row.AddChild(slider);
        column.AddChild(row);
        return slider;
    }
}
