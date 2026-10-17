using System;
using Godot;

namespace LifeGame;

// How big the windows, HUD and words are drawn (ui-63). Auto keeps the designed size up to a
// 1080-line window and grows with taller ones, so a 1440p or 4K screen isn't all tiny print;
// the player can pick a fixed step instead. Only the 2D layer scales (the root window's
// ContentScaleFactor, which also oversamples fonts so words stay crisp); the world is untouched.
public static class UiScale
{
    public static readonly string[] Names = { "Auto", "80%", "90%", "100%", "115%", "130%", "150%", "175%", "200%" };
    static readonly float[] Steps = { 0f, 0.8f, 0.9f, 1f, 1.15f, 1.3f, 1.5f, 1.75f, 2f };

    public static int Setting
    {
        get => Math.Clamp(Settings.Get("graphics", "ui_scale", 0), 0, Steps.Length - 1);
        set
        {
            Settings.Set("graphics", "ui_scale", Math.Clamp(value, 0, Steps.Length - 1));
            Apply();
        }
    }

    static Window? root;

    // Called once the root window exists; follows the window as it's resized.
    public static void Hook(Window window)
    {
        if (root == window) return;
        root = window;
        window.SizeChanged += Apply;
        Apply();
    }

    public static float Factor(Vector2I window) =>
        Setting == 0 ? Mathf.Clamp(Mathf.Snapped(window.Y / 1080f, 0.05f), 1f, 2f) : Steps[Setting];

    static void Apply()
    {
        if (root == null || !GodotObject.IsInstanceValid(root)) return;
        float factor = Factor(root.Size);
        if (!Mathf.IsEqualApprox(root.ContentScaleFactor, factor)) root.ContentScaleFactor = factor;
    }
}
