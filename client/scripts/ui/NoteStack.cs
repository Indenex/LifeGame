using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The notes just before the newest one (ui-28): when a new note replaces one still showing,
// the older line moves up here and fades out on its own time, so two notes in a row are
// both read. At most three wait above the newest; Hud keeps the newest itself.
public partial class NoteStack : VBoxContainer
{
    const int Most = 3;
    readonly List<(Label label, float time)> lines = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Alignment = AlignmentMode.End;
        AddThemeConstantOverride("separation", 2);
    }

    // A note that was still showing, with how long it had left.
    public void Push(Label like, float timeLeft)
    {
        if (string.IsNullOrEmpty(like.Text) || timeLeft <= 0.2f) return;
        var line = (Label)like.Duplicate();
        line.Scale = Vector2.One;
        line.Position = Vector2.Zero;
        line.SetAnchorsPreset(LayoutPreset.TopLeft);
        line.HorizontalAlignment = HorizontalAlignment.Center;
        line.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(line);
        // Older lines show a little smaller and dimmer, and stay long enough to read.
        lines.Add((line, Mathf.Max(timeLeft, 2.5f)));
        while (lines.Count > Most)
        {
            lines[0].label.QueueFree();
            lines.RemoveAt(0);
        }
    }

    public override void _Process(double delta)
    {
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var (label, time) = lines[i];
            time -= (float)delta;
            if (time <= 0f)
            {
                label.QueueFree();
                lines.RemoveAt(i);
                continue;
            }
            lines[i] = (label, time);
            label.Modulate = new Color(1, 1, 1, Mathf.Clamp(time * 1.5f, 0f, 1f) * 0.75f);
        }
    }
}
