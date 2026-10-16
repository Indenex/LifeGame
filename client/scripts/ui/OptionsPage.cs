using System;
using Godot;

namespace LifeGame;

// The Esc menu's Options page (D-097): one slider per kind of sound, with music on or
// off under Music. The levels live in SoundLevels, which saves them and hands them on
// (the Audio lane eases each change so dragging never clicks); new players start each
// at half. Under Graphics, how far the distant land reaches (env-9). Esc or Back
// returns to the menu.
public partial class OptionsPage : Control
{
    // Back to the menu (PauseMenu sets it).
    public Action Back = () => { };
    // The world's soundscape; PauseMenu sets it.
    public Func<SoundScape?> Sound = () => null;

    record Level(string Label, string Key);

    static readonly Level[] Levels =
    {
        new("Master", "master"),
        new("Music", "music"),
        new("Effects", "effects"),
        new("Ambience", "ambience"),
    };

    readonly HSlider[] sliders = new HSlider[Levels.Length];
    readonly Label[] percents = new Label[Levels.Length];
    Button music = null!;
    Button distantLand = null!;

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // The same soft dark panel as the other windows (trade, equipment), centred (D-125).
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 12);
        var panel = Hud.Panel(column, Vector2.Zero);
        var style = (StyleBoxFlat)panel.GetThemeStylebox("panel");
        style.BgColor = new Color(0.10f, 0.11f, 0.09f, 0.82f);
        style.ContentMarginTop = style.ContentMarginBottom = 18;
        style.ContentMarginLeft = style.ContentMarginRight = 28;
        panel.SetAnchorsPreset(LayoutPreset.Center);
        panel.GrowHorizontal = GrowDirection.Both;
        panel.GrowVertical = GrowDirection.Both;
        AddChild(panel);

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
            sliders[i] = ControlsPage.SliderRow(column, level.Label, 0, 1, 0.01, SoundLevels.Get(level.Key), v =>
            {
                SoundLevels.Set(level.Key, (float)v);
                percents[at].Text = Percent(v);
            });
            // The level as a percentage, beside the slider.
            percents[i] = new Label { Text = Percent(SoundLevels.Get(level.Key)), CustomMinimumSize = new Vector2(48, 0) };
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

        // How far the land shows beyond the loaded chunks (env-9); slow PCs turn it down.
        var graphics = new Label { Text = "Graphics", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(graphics, 15, 0.6f);
        column.AddChild(graphics);
        distantLand = ControlsPage.TextButton(DistantLandText(), () =>
        {
            FarLand.Level = (FarLand.Reach)(((int)FarLand.Level + 1) % FarLand.ReachNames.Length);
            distantLand.Text = DistantLandText();
        });
        distantLand.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(distantLand);

        var back = ControlsPage.TextButton("Back", () => Back());
        back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(back);
    }

    static string DistantLandText() => $"Distant land: {FarLand.ReachNames[(int)FarLand.Level]}";

    static string Percent(double v) => $"{Mathf.RoundToInt(v * 100)}%";

    public void Open()
    {
        for (int i = 0; i < Levels.Length; i++)
        {
            double v = SoundLevels.Get(Levels[i].Key);
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
