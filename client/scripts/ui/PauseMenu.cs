using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// Esc in the world opens a small, quiet menu over a darkened world: Resume, Options
// (sound levels, D-097), Controls (every key, which you can change, and the camera)
// and Quit. The world keeps running behind it (it's multiplayer);
// walking and the Use key wait while it's open.
public partial class PauseMenu : CanvasLayer
{
    public static bool IsOpen { get; private set; }

    // Other things that own Esc while they're open (a panel, chat, placing a building).
    // Main supplies them; when any says yes, Esc is theirs.
    public Func<bool> OthersOpen = () => false;
    // Opens the bug report window (Main sets it).
    public Action? ReportBug;
    public KeyHelp Keys = null!;
    // The world's music (Atmosphere's SoundScape), for the music slider; Main sets it.
    public Func<SoundScape?> Sound = () => null;

    readonly List<Control> rows = new();
    Control root = null!, column = null!;
    readonly ControlsPage page = new();
    readonly OptionsPage options = new();
    float alpha;
    int focus;

    public override void _Ready()
    {
        Layer = 4;
        SoundLevels.ApplyMaster();
        SoundLevels.Changed += () => SoundLevels.ApplyTo(Sound());

        root = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        // The world dims, most of all behind the words.
        var shade = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        shade.Material = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = """
                    shader_type canvas_item;
                    void fragment() {
                        vec2 uv = (SCREEN_UV - 0.5) * vec2(1.6, 1.0);
                        COLOR = vec4(0.03, 0.04, 0.03, 0.35 + 0.35 * (1.0 - smoothstep(0.05, 0.6, length(uv))));
                    }
                    """,
            },
        };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(shade);

        column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        column.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        column.AddThemeConstantOverride("separation", 10);
        root.AddChild(column);

        column.AddChild(Entry("Resume", Close));
        column.AddChild(Entry("Options", ShowOptions));
        column.AddChild(Entry("Controls", ShowControls));
        // The bug report window (bugfix-42, F8); Main sets ReportBug. The menu closes first
        // so the report's picture is of the world, not of this menu.
        column.AddChild(Entry("Report a bug", () => { Close(); ReportBug?.Invoke(); }));
        column.AddChild(Entry("Quit", () => GetTree().Quit()));

        page.Back = HideControls;
        root.AddChild(page);
        options.Back = HideControls;
        options.Sound = () => Sound();
        root.AddChild(options);
        // Saved keys and camera settings, once every lane has bound its own keys.
        Callable.From(ControlsPage.ApplySaved).CallDeferred();

        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-menu") >= 0)
            GetTree().CreateTimer(1).Timeout += Open;
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-options") >= 0)
            GetTree().CreateTimer(1).Timeout += () => { Open(); ShowOptions(); };
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-controls") >= 0)
            GetTree().CreateTimer(1).Timeout += () => { Open(); ShowControls(); };
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.PhysicalKeycode == Key.Escape)
        {
            if (Keys.Pinned) { Keys.Pinned = false; Open(); }
            else if (IsOpen) Close();
            else if (!OthersOpen()) Open();
            else return;
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!IsOpen || page.Visible || options.Visible) return;
        // W/S or arrows move, A/D or arrows change the volume, Enter or Space picks.
        switch (key.PhysicalKeycode)
        {
            case Key.W or Key.Up: Move(-1); break;
            case Key.S or Key.Down: Move(1); break;
            case Key.A or Key.Left when SliderIn(rows[focus]) is { } down: down.Value -= 0.1; break;
            case Key.D or Key.Right when SliderIn(rows[focus]) is { } up: up.Value += 0.1; break;
            case Key.Enter or Key.KpEnter or Key.Space: (rows[focus] as Button)?.EmitSignal(BaseButton.SignalName.Pressed); break;
        }
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        ApplySoundLevels();
        alpha = Mathf.MoveToward(alpha, IsOpen ? 1f : 0f, (float)delta * 6f);
        root.Modulate = new Color(1, 1, 1, alpha);
        root.Visible = alpha > 0f;
        for (int i = 0; i < rows.Count; i++)
            rows[i].Modulate = new Color(1, 1, 1, IsOpen && i == focus ? 1f : 0.62f);
    }

    void Open()
    {
        IsOpen = true;
        focus = 0;
        foreach (var refresh in refreshers) refresh();
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    void Close()
    {
        HideControls();
        IsOpen = false;
    }

    void ShowControls()
    {
        column.Visible = false;
        page.Open();
    }

    void ShowOptions()
    {
        column.Visible = false;
        options.Open();
    }

    // Back from either page to the menu.
    void HideControls()
    {
        page.Close();
        options.Close();
        column.Visible = true;
    }

    // The soundscape may appear after the menu; it gets the saved levels as soon as it does.
    SoundScape? leveled;

    void ApplySoundLevels()
    {
        if (Sound() is not { } sound || sound == leveled) return;
        leveled = sound;
        SoundLevels.ApplyTo(sound);
    }

    new void Hide() => IsOpen = false;

    void Move(int by) => focus = (focus + by + rows.Count) % rows.Count;

    Button Entry(string text, Action pressed)
    {
        var button = new Button { Text = text, Flat = true, FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 22);
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
            button.AddThemeColorOverride(state, Look.Cream);
        button.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.3f));
        button.AddThemeConstantOverride("outline_size", 6);
        foreach (var box in new[] { "normal", "hover", "pressed", "focus" })
            button.AddThemeStyleboxOverride(box, new StyleBoxEmpty());
        button.Pressed += pressed;
        int index = rows.Count;
        button.MouseEntered += () => focus = index;
        rows.Add(button);
        return button;
    }

    readonly List<Action> refreshers = new();

    static HSlider? SliderIn(Control row) => row.GetChildren().OfType<HSlider>().FirstOrDefault();

    // A labelled slider row; current, when given, re-reads the value each time the
    // menu opens (the M key can change music while the menu is shut).
    Control SliderRow(string text, float value, Action<double> changed, Func<float?>? current = null)
    {
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 16);
        var label = new Label { Text = text, CustomMinimumSize = new Vector2(90, 0), HorizontalAlignment = HorizontalAlignment.Right };
        Hud.Soft(label, 22, 1f);
        row.AddChild(label);
        var slider = new HSlider
        {
            MinValue = 0, MaxValue = 1, Step = 0.05,
            Value = value,
            CustomMinimumSize = new Vector2(180, 24),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            FocusMode = Control.FocusModeEnum.None,
        };
        var track = new StyleBoxFlat { BgColor = new Color(Look.Cream, 0.25f), ContentMarginTop = 1, ContentMarginBottom = 1 };
        track.SetCornerRadiusAll(2);
        var fill = new StyleBoxFlat { BgColor = new Color(Look.Cream, 0.85f), ContentMarginTop = 1, ContentMarginBottom = 1 };
        fill.SetCornerRadiusAll(2);
        slider.AddThemeStyleboxOverride("slider", track);
        slider.AddThemeStyleboxOverride("grabber_area", fill);
        slider.AddThemeStyleboxOverride("grabber_area_highlight", fill);
        slider.ValueChanged += v => changed(v);
        if (current != null)
            refreshers.Add(() => { if (current() is { } now) slider.SetValueNoSignal(now); });
        row.AddChild(slider);
        int index = rows.Count;
        row.MouseEntered += () => focus = index;
        rows.Add(row);
        return row;
    }

}
