using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Esc in the world opens a small, quiet menu over a darkened world: Resume, Controls
// (every key, which you can change, and the camera), volume, music and Quit. The world keeps running behind it (it's multiplayer);
// walking and the Use key wait while it's open.
public partial class PauseMenu : CanvasLayer
{
    public static bool IsOpen { get; private set; }

    // Other things that own Esc while they're open (a panel, chat, placing a building).
    // Main supplies them; when any says yes, Esc is theirs.
    public Func<bool> OthersOpen = () => false;
    public KeyHelp Keys = null!;

    readonly List<Control> rows = new();
    Control root = null!, column = null!;
    readonly ControlsPage page = new();
    HSlider volume = null!;
    float alpha;
    int focus;

    public override void _Ready()
    {
        Layer = 4;
        SetVolume(Settings.Get("sound", "volume", 0.8f));

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
        column.AddChild(Entry("Controls", ShowControls));
        column.AddChild(VolumeRow());
        column.AddChild(Entry("Music on or off", () => Input.ParseInputEvent(new InputEventAction { Action = "toggle_music", Pressed = true })));
        column.AddChild(Entry("Quit", () => GetTree().Quit()));

        page.Back = HideControls;
        root.AddChild(page);
        // Saved keys and camera settings, once every lane has bound its own keys.
        Callable.From(ControlsPage.ApplySaved).CallDeferred();

        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-menu") >= 0)
            GetTree().CreateTimer(1).Timeout += Open;
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
        if (!IsOpen || page.Visible) return;
        // W/S or arrows move, A/D or arrows change the volume, Enter or Space picks.
        switch (key.PhysicalKeycode)
        {
            case Key.W or Key.Up: Move(-1); break;
            case Key.S or Key.Down: Move(1); break;
            case Key.A or Key.Left when rows[focus] == volume.GetParent(): volume.Value -= 0.1; break;
            case Key.D or Key.Right when rows[focus] == volume.GetParent(): volume.Value += 0.1; break;
            case Key.Enter or Key.KpEnter or Key.Space: (rows[focus] as Button)?.EmitSignal(BaseButton.SignalName.Pressed); break;
        }
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
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

    void HideControls()
    {
        page.Close();
        column.Visible = true;
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

    Control VolumeRow()
    {
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 16);
        var label = new Label { Text = "Volume" };
        Hud.Soft(label, 22, 1f);
        row.AddChild(label);
        volume = new HSlider
        {
            MinValue = 0, MaxValue = 1, Step = 0.05,
            Value = Settings.Get("sound", "volume", 0.8f),
            CustomMinimumSize = new Vector2(180, 24),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            FocusMode = Control.FocusModeEnum.None,
        };
        var track = new StyleBoxFlat { BgColor = new Color(Look.Cream, 0.25f), ContentMarginTop = 1, ContentMarginBottom = 1 };
        track.SetCornerRadiusAll(2);
        var fill = new StyleBoxFlat { BgColor = new Color(Look.Cream, 0.85f), ContentMarginTop = 1, ContentMarginBottom = 1 };
        fill.SetCornerRadiusAll(2);
        volume.AddThemeStyleboxOverride("slider", track);
        volume.AddThemeStyleboxOverride("grabber_area", fill);
        volume.AddThemeStyleboxOverride("grabber_area_highlight", fill);
        volume.ValueChanged += v =>
        {
            SetVolume((float)v);
            Settings.Set("sound", "volume", v);
        };
        row.AddChild(volume);
        int index = rows.Count;
        row.MouseEntered += () => focus = index;
        rows.Add(row);
        return row;
    }

    static void SetVolume(float linear) =>
        AudioServer.SetBusVolumeDb(0, linear <= 0.001f ? -80f : Mathf.LinearToDb(linear));
}
