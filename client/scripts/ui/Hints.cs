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

    // Marks a hint as learned when the player did the thing some other way (a lane
    // that saw it happen), so it is never taught after the fact.
    public static void Done(string action) => done.Enqueue(action);
    static readonly Queue<string> done = new();

    // A one-off tip that isn't about a key ("Clay dries twice as fast beside a burning
    // fire"): queued like Request, shown once per install in the same corner.
    public static void Tip(string id, string text) =>
        tips.Enqueue(new Controls.Binding($"tip.{id}", System.Array.Empty<Key>(), null, text, OnRequest: true));
    static readonly Queue<Controls.Binding> tips = new();

    // Keys that teach the same thing as a row with a hint: S, A or D is walking.
    static readonly Dictionary<string, string> Teaches = new()
    {
        ["walk_back"] = "walk_forward", ["walk_left"] = "walk_forward", ["walk_right"] = "walk_forward",
    };
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
        // Long tips wrap upward rather than run under the inventory row.
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(380, 0);
        panel.Modulate = new Color(1, 1, 1, 0);
        root.AddChild(panel);
    }

    public override void _Input(InputEvent e)
    {
        // Watch, never consume: a key pressed means that key is learned.
        foreach (var b in Controls.World)
            if (InputMap.HasAction(b.Action) && e.IsActionPressed(b.Action))
                Learned(Teaches.GetValueOrDefault(b.Action, b.Action));
        if (showing != null && Seen(showing)) showing = null;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        while (done.TryDequeue(out var action)) Learned(action);
        if (showing != null && Seen(showing)) showing = null;
        // Never over a panel: a hint waits, and one already showing steps aside.
        bool covered = Panels.Open();
        if (covered) wait = Mathf.Max(wait, 2f);
        if (showing != null && !covered)
        {
            shownFor += dt;
            if (shownFor > Linger) { MarkSeen(showing); showing = null; }
        }
        else if (alpha <= 0f && (wait -= dt) <= 0f)
        {
            showing = null;
            while (showing == null && requested.TryDequeue(out var action))
                showing = Controls.World.FirstOrDefault(b => b.Action == action && b.Hint != null && !Seen(b));
            while (showing == null && tips.TryDequeue(out var tip))
                if (!Seen(tip)) showing = tip;
            showing ??= Controls.World.FirstOrDefault(b => b.Hint != null && !b.OnRequest && !Seen(b) && InputMap.HasAction(b.Action));
            if (showing != null) label.Text = Controls.HintText(showing);
            shownFor = 0f;
            wait = Gap;
        }
        alpha = Mathf.MoveToward(alpha, showing != null && !covered ? 1f : 0f, dt * (covered ? 6f : 1.2f));
        panel.Modulate = new Color(1, 1, 1, alpha);
    }

    void Learned(string action)
    {
        foreach (var b in Controls.World)
            if (b.Action == action && b.Hint != null && !Seen(b)) MarkSeen(b);
    }

    bool Seen(Controls.Binding b) => (bool)seen.GetValue("seen", b.Action, false);

    void MarkSeen(Controls.Binding b)
    {
        seen.SetValue("seen", b.Action, true);
        seen.Save(SavePath);
    }
}
