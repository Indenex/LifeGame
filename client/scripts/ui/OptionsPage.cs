using System;
using Godot;

namespace LifeGame;

// The Esc menu's Options page (D-097): one slider per kind of sound, with music on or
// off under Music. The levels live in the Audio lane's SoundScape, which saves them and
// eases every change so dragging never clicks; new players start each at half.
// Esc or Back returns to the menu.
public partial class OptionsPage : Control
{
    // Back to the menu (PauseMenu sets it).
    public Action Back = () => { };
    // The world's soundscape; PauseMenu sets it.
    public Func<SoundScape?> Sound = () => null;

    record Level(string Label, Func<SoundScape, float> Get, Action<SoundScape, float> Set);

    static readonly Level[] Levels =
    {
        new("Master", s => s.MasterVolume, (s, v) => s.MasterVolume = v),
        new("Music", s => s.MusicVolume, (s, v) => s.MusicVolume = v),
        new("Effects", s => s.EffectsVolume, (s, v) => s.EffectsVolume = v),
        new("Ambience", s => s.AmbienceVolume, (s, v) => s.AmbienceVolume = v),
    };

    readonly HSlider[] sliders = new HSlider[Levels.Length];
    readonly Label[] percents = new Label[Levels.Length];
    Button music = null!;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        column.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        column.AddThemeConstantOverride("separation", 12);
        AddChild(column);

        var title = new Label { Text = "Options", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(title, 26, 1f);
        column.AddChild(title);
        var heading = new Label { Text = "Sound", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(heading, 15, 0.6f);
        column.AddChild(heading);

        for (int i = 0; i < Levels.Length; i++)
        {
            var level = Levels[i];
            int at = i;
            sliders[i] = ControlsPage.SliderRow(column, level.Label, 0, 1, 0.01, 0.5, v =>
            {
                if (Sound() is { } sound) level.Set(sound, (float)v);
                percents[at].Text = Percent(v);
            });
            // The level as a percentage, beside the slider.
            percents[i] = new Label { Text = Percent(0.5), CustomMinimumSize = new Vector2(48, 0) };
            Hud.Soft(percents[i], 16, 0.7f);
            sliders[i].GetParent().AddChild(percents[i]);

            if (level.Label == "Music")
            {
                music = ControlsPage.TextButton("Music on", () =>
                {
                    if (Sound() is { } sound) sound.MusicOn = !sound.MusicOn;
                    else Input.ParseInputEvent(new InputEventAction { Action = "toggle_music", Pressed = true });
                });
                music.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                column.AddChild(music);
            }
        }

        column.AddChild(ControlsPage.TextButton("Back", () => Back()));
    }

    static string Percent(double v) => $"{Mathf.RoundToInt(v * 100)}%";

    public void Open()
    {
        for (int i = 0; i < Levels.Length; i++)
        {
            double v = Sound() is { } sound ? Levels[i].Get(sound) : 0.5;
            sliders[i].SetValueNoSignal(v);
            percents[i].Text = Percent(v);
        }
        Visible = true;
    }

    public void Close() => Visible = false;

    public override void _Process(double delta)
    {
        if (Visible) music.Text = Sound()?.MusicOn == false ? "Music off" : "Music on";
    }

    public override void _Input(InputEvent e)
    {
        if (!Visible || e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape }) return;
        Back();
        GetViewport().SetInputAsHandled();
    }
}
