using System;
using System.Linq;
using Godot;

namespace LifeGame;

// While the game connects and the world around you loads (ui-47, D-313's warm look): a soft
// paper veil with a little sun bobbing over the words "Waking up…", instead of a bare status
// line over a half-built world. It fades out once you stand in the world (or walk alone
// offline). Screenshot previews never see it.
public partial class WakeVeil : CanvasLayer
{
    // True once there's a world to look at (Main: spawned).
    public Func<bool> Done = () => true;

    Control root = null!;
    Label words = null!;
    SunDoodle sun = null!;
    float time, fade = 1f;
    bool leaving;

    public override void _Ready()
    {
        Layer = 20;
        root = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        // Warm paper, a touch peachier at the bottom, like early light.
        var paper = new TextureRect { Texture = Gradient(), StretchMode = TextureRect.StretchModeEnum.Scale, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize };
        paper.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(paper);

        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        column.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        column.AddThemeConstantOverride("separation", 18);
        root.AddChild(column);
        sun = new SunDoodle { CustomMinimumSize = new Vector2(120, 110), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddChild(sun);
        words = new Label { Text = "Waking up…", HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        words.AddThemeFontOverride("font", Ui.Font(800));
        words.AddThemeFontSizeOverride("font_size", 30);
        words.AddThemeColorOverride("font_color", WarmLook.Ink);
        column.AddChild(words);

        // Previews show the screen they're about, not the veil; --screenshot-wake shows the veil and keeps it.
        var args = OS.GetCmdlineUserArgs();
        if (args.Contains("--screenshot-wake")) Done = () => false;
        else if (!WarmLook.On || args.Any(a => a.StartsWith("--screenshot"))) QueueFree();
    }

    static GradientTexture2D Gradient() => new()
    {
        Gradient = new Gradient { Colors = new[] { WarmLook.Paper, new Color(0.99f, 0.88f, 0.78f) }, Offsets = new[] { 0f, 1f } },
        FillFrom = new Vector2(0.5f, 0f),
        FillTo = new Vector2(0.5f, 1f),
        Width = 4,
        Height = 64,
    };

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        time += dt;
        sun.Time = time;
        // The dots breathe while you wait.
        words.Text = "Waking up" + new string('.', 1 + (int)(time * 2f) % 3);
        if (!leaving && Done()) leaving = true;
        if (!leaving) return;
        fade = Mathf.MoveToward(fade, 0f, dt / 0.6f);
        root.Modulate = new Color(1, 1, 1, fade);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        if (fade <= 0f) QueueFree();
    }
}

// A hand-drawn sun, bobbing gently with its rays turning slowly.
public partial class SunDoodle : Control
{
    public float Time;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var c = Size / 2 + new Vector2(0, Mathf.Sin(Time * 2.2f) * 6f);
        const float radius = 28f;
        DrawCircle(c, radius, new Color(WarmLook.Butter, 0.75f));
        var ring = Enumerable.Range(0, 33).Select(i => c + Vector2.FromAngle(i * Mathf.Tau / 32) * radius
            + new Vector2(Mathf.Sin(i * 2.3f) * 0.8f, Mathf.Cos(i * 1.7f) * 0.8f)).ToArray();
        DrawPolyline(ring, WarmLook.Butter.Darkened(0.2f), 3f, true);
        for (int i = 0; i < 10; i++)
        {
            var d = Vector2.FromAngle(Time * 0.4f + i * Mathf.Tau / 10);
            DrawLine(c + d * (radius + 8), c + d * (radius + 18), WarmLook.Butter.Darkened(0.15f), 3f, true);
        }
        // A sleepy smile: two closed eyes and a little curve.
        var ink = new Color(WarmLook.Ink, 0.8f);
        DrawArc(c + new Vector2(-9, -3), 4f, 0.2f, Mathf.Pi - 0.2f, 8, ink, 2.2f, true);
        DrawArc(c + new Vector2(9, -3), 4f, 0.2f, Mathf.Pi - 0.2f, 8, ink, 2.2f, true);
        DrawArc(c + new Vector2(0, 6), 7f, 0.3f, Mathf.Pi - 0.3f, 10, ink, 2.2f, true);
    }
}
