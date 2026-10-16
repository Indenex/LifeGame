using System;
using Godot;

namespace LifeGame;

// The Esc menu's Options page (D-097): one slider per kind of sound, and music on or
// off. Every slider saves as it moves. Esc or Back returns to the menu.
public partial class OptionsPage : Control
{
    // Back to the menu (PauseMenu sets it).
    public Action Back = () => { };
    // The world's soundscape, so music on/off works from here too; PauseMenu sets it.
    public Func<SoundScape?> Sound = () => null;

    static readonly (string key, string label)[] Sliders =
    {
        ("master", "Master"),
        ("music", "Music"),
        ("effects", "Work and effects"),
        ("ambience", "Ambience"),
    };

    readonly HSlider[] sliders = new HSlider[Sliders.Length];
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
        var help = new Label { Text = "Sound", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(help, 15, 0.6f);
        column.AddChild(help);

        for (int i = 0; i < Sliders.Length; i++)
        {
            var (key, label) = Sliders[i];
            sliders[i] = ControlsPage.SliderRow(column, label, 0, 1, 0.05, SoundLevels.Get(key), v =>
            {
                SoundLevels.Set(key, (float)v);
                // Until Audio reads SoundLevels.Music, the music level goes to the soundscape too.
                if (key == "music" && Sound() is { } sound) sound.MusicVolume = (float)v;
            });
        }

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 40);
        music = ControlsPage.TextButton("Music on", () =>
        {
            if (Sound() is { } sound) sound.MusicOn = !sound.MusicOn;
            else Input.ParseInputEvent(new InputEventAction { Action = "toggle_music", Pressed = true });
        });
        buttons.AddChild(music);
        buttons.AddChild(ControlsPage.TextButton("Back", () => Back()));
        column.AddChild(buttons);
    }

    public void Open()
    {
        for (int i = 0; i < Sliders.Length; i++) sliders[i].SetValueNoSignal(SoundLevels.Get(Sliders[i].key));
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
