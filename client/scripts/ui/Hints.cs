using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// First-time hints instead of a controls line (D-038): one quiet line at a time in
// the bottom-left corner, each shown once per install. A hint fades out as soon as
// you press its key, and a key you find on your own never gets one.
public partial class Hints : CanvasLayer
{
    const float FirstDelay = 6f, Gap = 14f, Linger = 14f;
    const string SavePath = "user://hints.cfg";

    readonly ConfigFile seen = new();
    readonly Label label = new();
    Label panel = null!;
    Controls.Binding? showing;
    static readonly Queue<string> requested = new();

    // Shows an OnRequest hint (or any hint) next, unless the player has seen it or
    // already uses the key. Safe to call often, e.g. every time a flock bolts.
    public static void Request(string action) => requested.Enqueue(action);
    float wait = FirstDelay, shownFor, alpha;

    public override void _Ready()
    {
        seen.Load(SavePath);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        Hud.Soft(label, 15, 0.85f);
        // Just words in the corner, no box (D-045).
        panel = label;
        panel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        panel.GrowVertical = Control.GrowDirection.Begin;
        panel.Position = new Vector2(26, -34);
        panel.Modulate = new Color(1, 1, 1, 0);
        root.AddChild(panel);
    }

    public override void _Input(InputEvent e)
    {
        // Watch, never consume: a key pressed means that key is learned.
        foreach (var b in Controls.World)
            if (b.Hint != null && !Seen(b) && InputMap.HasAction(b.Action) && e.IsActionPressed(b.Action))
                MarkSeen(b);
        if (showing != null && Seen(showing)) showing = null;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (showing != null)
        {
            shownFor += dt;
            if (shownFor > Linger) { MarkSeen(showing); showing = null; }
        }
        else if (alpha <= 0f && (wait -= dt) <= 0f)
        {
            showing = null;
            while (showing == null && requested.TryDequeue(out var action))
                showing = Controls.World.FirstOrDefault(b => b.Action == action && b.Hint != null && !Seen(b));
            showing ??= Controls.World.FirstOrDefault(b => b.Hint != null && !b.OnRequest && !Seen(b) && InputMap.HasAction(b.Action));
            if (showing != null) label.Text = showing.Hint;
            shownFor = 0f;
            wait = Gap;
        }
        alpha = Mathf.MoveToward(alpha, showing != null ? 1f : 0f, dt * 1.2f);
        panel.Modulate = new Color(1, 1, 1, alpha);
    }

    bool Seen(Controls.Binding b) => (bool)seen.GetValue("seen", b.Action, false);

    void MarkSeen(Controls.Binding b)
    {
        seen.SetValue("seen", b.Action, true);
        seen.Save(SavePath);
    }
}
