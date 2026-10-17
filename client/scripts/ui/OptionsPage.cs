using System;
using Godot;

namespace LifeGame;

// The Esc menu's Options page (D-097): one slider per kind of sound, with music on or
// off under Music. The levels live in SoundLevels, which saves them and hands them on
// (the Audio lane eases each change so dragging never clicks); new players start each
// at half. Under Graphics, how far the world is drawn (env-9). Esc or Back
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
    Button lightRays = null!;
    Button softGlow = null!, vignette = null!;
    Button shadows = null!;
    Button ambientLife = null!;
    HSlider view = null!;
    Button markButton = null!;
    Label viewName = null!;
    Label sizeName = null!;
    ScrollContainer scroll = null!;
    VBoxContainer column = null!;

    void Fit()
    {
        if (scroll == null) return;
        var inside = column.GetCombinedMinimumSize();
        // The window's own frame and margins (Ui.Window, roomy) plus a little air.
        float room = Mathf.Max(200f, Size.Y - 2 * 18f - 48f);
        scroll.CustomMinimumSize = new Vector2(inside.X + (inside.Y > room ? 12f : 0f), Mathf.Min(inside.Y, room));
    }

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // The player's UI size applies from the start (ui-63).
        UiScale.Hook(GetTree().Root);

        // The same soft dark panel as the other windows (trade, equipment), centred (D-125).
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 12);
        // The page scrolls when it's taller than the window (ui-67: at 720p the title and the
        // last rows were cut off); Fit sizes it on opening and when the window changes.
        scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, MouseFilter = MouseFilterEnum.Pass };
        scroll.AddChild(column);
        this.column = column;
        var panel = Ui.Window(scroll, roomy: true);
        AddChild(panel);
        Resized += Fit;
        column.MinimumSizeChanged += Fit;

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

        // How far the world is drawn (env-9); slow PCs turn it down.
        var graphics = new Label { Text = "Graphics", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(graphics, 15, 0.6f);
        column.AddChild(graphics);
        // Four steps, Short to Very far; the name shows beside the slider.
        view = ControlsPage.SliderRow(column, "View distance", 0, ViewDistance.Names.Length - 1, 1, (int)ViewDistance.Setting, v =>
        {
            ViewDistance.Setting = (ViewDistance.Level)(int)v;
            viewName.Text = ViewDistance.Names[(int)v];
        });
        viewName = new Label { Text = ViewDistance.Names[(int)ViewDistance.Setting], CustomMinimumSize = new Vector2(70, 0) };
        Hud.Soft(viewName, 16, 0.7f);
        view.GetParent().AddChild(viewName);
        // How big the windows, HUD and words are (ui-63); Auto grows them on big screens.
        var size = ControlsPage.SliderRow(column, "UI size", 0, UiScale.Names.Length - 1, 1, UiScale.Setting, v =>
        {
            UiScale.Setting = (int)v;
            sizeName.Text = UiScale.Names[(int)v];
        });
        sizeName = new Label { Text = UiScale.Names[UiScale.Setting], CustomMinimumSize = new Vector2(70, 0) };
        Hud.Soft(sizeName, 16, 0.7f);
        size.GetParent().AddChild(sizeName);
        // How thick the grass is drawn (Environment, env-32); Low for slower PCs.
        Button grass = null!;
        grass = ControlsPage.TextButton(GrassText(), () =>
        {
            GrassDetail.Setting = (GrassDetail.Setting + 1) % GrassDetail.Names.Length;
            grass.Text = GrassText();
        });
        grass.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(grass);
        // Beams of low sunlight through the air (Atmosphere); Off for slower PCs.
        lightRays = ControlsPage.TextButton(RaysText(), () =>
        {
            Atmosphere.GodRays = (Atmosphere.GodRays + 1) % 3;
            Settings.Set("graphics", "light_rays", Atmosphere.GodRays);
            lightRays.Text = RaysText();
        });
        lightRays.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(lightRays);
        // A soft lens (Atmosphere, D-313): warm glow and light leaks, and a vignette.
        softGlow = ControlsPage.TextButton(OnOff("Soft glow", Atmosphere.SoftGlow), () =>
        {
            Atmosphere.SoftGlow = !Atmosphere.SoftGlow;
            Settings.Set("graphics", "soft_glow", Atmosphere.SoftGlow ? 1 : 0);
            softGlow.Text = OnOff("Soft glow", Atmosphere.SoftGlow);
        });
        softGlow.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(softGlow);
        vignette = ControlsPage.TextButton(OnOff("Vignette", Atmosphere.Vignette), () =>
        {
            Atmosphere.Vignette = !Atmosphere.Vignette;
            Settings.Set("graphics", "vignette", Atmosphere.Vignette ? 1 : 0);
            vignette.Text = OnOff("Vignette", Atmosphere.Vignette);
        });
        vignette.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(vignette);
        // Bouncy windows, squishy buttons, wiggling items (D-314); off for a calmer screen.
        Button motion = null!;
        motion = ControlsPage.TextButton(OnOff("Reduce motion", Bounce.Reduce), () =>
        {
            Bounce.Reduce = !Bounce.Reduce;
            motion.Text = OnOff("Reduce motion", Bounce.Reduce);
        });
        motion.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(motion);

        // Water reflections (Atmosphere, atmosphere-31): off, normal or high.
        Button reflections = null!;
        reflections = ControlsPage.TextButton(ReflectText(), () =>
        {
            Atmosphere.WaterReflections = (Atmosphere.WaterReflections + 1) % 3;
            Settings.Set("graphics", "water_reflections", Atmosphere.WaterReflections);
            reflections.Text = ReflectText();
        });
        reflections.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(reflections);
        // Sun shadow quality (SunShadows): Low for slower PCs.
        shadows = ControlsPage.TextButton(ShadowsText(), () =>
        {
            SunShadows.Setting = (SunShadows.Level)(((int)SunShadows.Setting + 1) % SunShadows.Names.Length);
            shadows.Text = ShadowsText();
        });
        shadows.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(shadows);
        // Butterflies, pollen and fireflies (Environment, env-30); Off for slower PCs.
        ambientLife = ControlsPage.TextButton(LifeText(), () =>
        {
            AmbientLife.Setting = (AmbientLife.Setting + 1) % AmbientLife.Names.Length;
            ambientLife.Text = LifeText();
        });
        ambientLife.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(ambientLife);

        // How you look (ui-21): the same picker as at first join.
        var lookButton = ControlsPage.TextButton("Your look", () => LookPicker.OpenFromMenu?.Invoke());
        lookButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(lookButton);

        // Your maker's mark (ui-23).
        markButton = ControlsPage.TextButton("Your mark", () => MarkPicker.OpenFromMenu?.Invoke());
        markButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(markButton);

        var back = ControlsPage.TextButton("Back", () => Back());
        back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(back);
    }

    static string Percent(double v) => $"{Mathf.RoundToInt(v * 100)}%";

    static string ReflectText() => $"Water reflections: {new[] { "Off", "Normal", "High" }[Atmosphere.WaterReflections]}";
    static string OnOff(string name, bool on) => $"{name}: {(on ? "On" : "Off")}";
    static string ShadowsText() => $"Shadows: {SunShadows.Names[(int)SunShadows.Setting]}";
    static string LifeText() => $"Ambient life: {AmbientLife.Names[AmbientLife.Setting]}";

    static string GrassText() => $"Grass: {GrassDetail.Names[GrassDetail.Setting]}";

    static string RaysText() => $"Light rays: {new[] { "Off", "Normal", "High" }[Atmosphere.GodRays]}";

    public void Open()
    {
        markButton.Visible = MarkPicker.Available;
        for (int i = 0; i < Levels.Length; i++)
        {
            double v = SoundLevels.Get(Levels[i].Key);
            sliders[i].SetValueNoSignal(v);
            percents[i].Text = Percent(v);
        }
        view.SetValueNoSignal((int)ViewDistance.Setting);
        viewName.Text = ViewDistance.Names[(int)ViewDistance.Setting];
        lightRays.Text = RaysText();
        softGlow.Text = OnOff("Soft glow", Atmosphere.SoftGlow);
        vignette.Text = OnOff("Vignette", Atmosphere.Vignette);
        shadows.Text = ShadowsText();
        Visible = true;
        Fit();
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
