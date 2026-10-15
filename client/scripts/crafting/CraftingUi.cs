using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The crafting panel (C), the hands-on minigames for key steps, and the progress
// bar while a job runs. Processes come from shared data, so new ones show up here
// without UI changes.
public partial class CraftingUi : CanvasLayer
{
    // True while a minigame wants the keyboard, so walking pauses.
    public bool CapturesInput => minigame != null;
    public event Action<string>? Notice;

    DbConnection? conn;
    readonly PanelContainer panel;
    readonly VBoxContainer list = new();
    readonly PanelContainer progressPanel;
    readonly Label progressLabel = new();
    readonly ProgressBar progressBar = new() { MinValue = 0, MaxValue = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(260, 8) };
    Minigame_? minigame;
    CraftJob? job;
    double jobStartLocal, jobSeconds;

    public CraftingUi()
    {
        Layer = 2;
        panel = Card(new Vector2(380, 0));
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(Text("Crafting", 20));
        box.AddChild(Text("Click a process, or press its number.  C closes.", 13, 0.65f));
        list.AddThemeConstantOverride("separation", 6);
        box.AddChild(list);
        panel.AddChild(box);
        panel.Visible = false;

        progressPanel = Card(Vector2.Zero);
        var progress = new VBoxContainer();
        progress.AddChild(progressLabel);
        progress.AddChild(progressBar);
        Style(progressLabel, 15, 0.95f);
        progressPanel.AddChild(progress);
        progressPanel.Visible = false;
    }

    public override void _Ready()
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        panel.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        panel.Position = new Vector2(-410, -200);
        root.AddChild(panel);
        progressPanel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        progressPanel.GrowHorizontal = Control.GrowDirection.Both;
        progressPanel.Position = new Vector2(-150, -130);
        root.AddChild(progressPanel);
    }

    public void Attach(DbConnection conn)
    {
        this.conn = conn;
        conn.Db.MyInventory.OnInsert += (_, _) => Refresh();
        conn.Db.MyInventory.OnUpdate += (_, _, _) => Refresh();
        conn.Db.MyInventory.OnDelete += (_, _) => Refresh();
        conn.Db.MyCraftJob.OnInsert += (_, j) => StartedJob(j);
        conn.Db.MyCraftJob.OnDelete += (_, j) => FinishedJob(j);
    }

    public void Toggle()
    {
        if (minigame != null) return;
        panel.Visible = !panel.Visible;
        Refresh();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (minigame != null)
        {
            if (e is InputEventKey { Pressed: true, Echo: false } key)
            {
                if (key.PhysicalKeycode == Key.Escape) EndMinigame(null);
                else minigame.Key(key.PhysicalKeycode);
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (panel.Visible && e is InputEventKey { Pressed: true, Echo: false } k
            && k.PhysicalKeycode >= Key.Key1 && k.PhysicalKeycode <= Key.Key9)
        {
            int index = (int)(k.PhysicalKeycode - Key.Key1);
            if (index < Processes.List.Count) Begin(Processes.List[index]);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        minigame?.Tick((float)delta);
        if (minigame is { Done: true } done) EndMinigame(done.Score);

        if (job != null)
        {
            double t = Time.GetTicksMsec() / 1000.0 - jobStartLocal;
            progressBar.Value = Math.Clamp(t / Math.Max(jobSeconds, 0.01), 0, 1);
        }
    }

    // --- Starting a process ----------------------------------------------

    void Begin(ProcessDef def)
    {
        if (conn == null) { Notice?.Invoke("Crafting needs the server"); return; }
        if (job != null) { Notice?.Invoke("Your hands are busy"); return; }
        if (Missing(def) is { } missing) { Notice?.Invoke(missing); return; }

        panel.Visible = false;
        minigame = def.Minigame switch
        {
            Minigame.Drill => new DrillGame(def),
            Minigame.Strike => new StrikeGame(def),
            _ => null,
        };
        if (minigame == null) MinigameFor(def, ref minigame);
        if (minigame == null)
        {
            conn.Reducers.StartCraft(def.Id, Processes.DefaultScore);
            return;
        }
        AddChild(minigame.Root);
    }

    // Other lanes add their own minigames in their own folder (see ceramics/CraftingUi.Clay.cs).
    partial void MinigameFor(ProcessDef def, ref Minigame_? game);

    void EndMinigame(float? score)
    {
        if (minigame == null) return;
        var def = minigame.Process;
        minigame.Root.QueueFree();
        minigame = null;
        if (score is { } s && conn != null) conn.Reducers.StartCraft(def.Id, s);
    }

    // What is missing to run a process, or null when everything is at hand.
    string? Missing(ProcessDef def)
    {
        foreach (var input in def.Consumes)
            if (Count(input.Item) < input.Count)
                return $"You need {input.Count} {Items.Def(input.Item).Count(input.Count).ToLowerInvariant()}";
        if (def.NeedsTool.HasFlag(ItemTags.Hammer))
        {
            // The stone being worked cannot also be the one striking it.
            uint used = def.Consumes.Where(i => Items.Def(i.Item).Tags.HasFlag(ItemTags.Hammer)).Aggregate(0u, (n, i) => n + i.Count);
            if (HammerCount() < used + 1)
                return "You need a second stone to strike with";
        }
        else if (def.NeedsTool != ItemTags.None && !HasTool(def.NeedsTool))
            return "You need the right tool";
        return null;
    }

    uint Count(Item item) =>
        conn?.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Aggregate(0u, (n, i) => n + i.Count) ?? 0;

    uint HammerCount() =>
        conn?.Db.MyInventory.Iter().Where(i => Items.IsKnown(i.Item) && Items.Def((Item)i.Item).Tags.HasFlag(ItemTags.Hammer))
            .Aggregate(0u, (n, i) => n + i.Count) ?? 0;

    bool HasTool(ItemTags tags) =>
        conn?.Db.MyInventory.Iter().Any(i => Items.IsKnown(i.Item) && (Items.Def((Item)i.Item).Tags & tags) == tags) ?? false;

    void Refresh()
    {
        if (!panel.Visible) return;
        foreach (var child in list.GetChildren()) child.QueueFree();
        int n = 1;
        foreach (var def in Processes.List)
        {
            string inputs = string.Join(", ", def.Consumes.Select(i => $"{i.Count} {Items.Def(i.Item).Count(i.Count).ToLowerInvariant()}"));
            if (def.NeedsTool != ItemTags.None) inputs += $", using a {def.NeedsTool.ToString().ToLowerInvariant()}";
            bool ready = Missing(def) == null;
            var button = new Button
            {
                Text = $"{n}.  {def.Name}\n      {inputs}  ·  {def.Seconds:0}s{(def.Minigame != Minigame.None ? "  ·  hands-on" : "")}",
                Alignment = HorizontalAlignment.Left,
                Disabled = !ready,
                Flat = true,
                FocusMode = Control.FocusModeEnum.None,
            };
            button.AddThemeColorOverride("font_color", Look.Cream);
            button.AddThemeColorOverride("font_disabled_color", new Color(Look.Cream, 0.35f));
            button.AddThemeFontSizeOverride("font_size", 14);
            var captured = def;
            button.Pressed += () => Begin(captured);
            list.AddChild(button);
            n++;
        }
    }

    // --- Job progress ----------------------------------------------------

    void StartedJob(CraftJob j)
    {
        job = j;
        jobStartLocal = Time.GetTicksMsec() / 1000.0;
        jobSeconds = (j.FinishAt.MicrosecondsSinceUnixEpoch - j.StartedAt.MicrosecondsSinceUnixEpoch) / 1_000_000.0;
        progressLabel.Text = Processes.Find(j.ProcessId)?.Doing + "...";
        progressPanel.Visible = true;
    }

    void FinishedJob(CraftJob j)
    {
        job = null;
        progressPanel.Visible = false;
        var def = Processes.Find(j.ProcessId);
        if (def == null) return;
        if (j.Failed)
            Notice?.Invoke(def.Outcome == Outcome.Fire ? "The ember dies before it catches" : "It breaks wrong. The material is lost");
        else if (def.Outcome == Outcome.Fire)
            Notice?.Invoke("The tinder catches. You have fire.");
        else if (def.Outcome != Outcome.Placed)
            Notice?.Invoke($"Made: {ItemLook.Label(def.Makes, j.Quality, def.MakesCount)}");
        Refresh();
    }

    // --- Minigames --------------------------------------------------------

    abstract class Minigame_
    {
        public readonly ProcessDef Process;
        public readonly Control Root;
        protected readonly Label Title = new(), Hint = new();
        protected readonly ProgressBar Bar = new() { MinValue = 0, MaxValue = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(420, 18) };
        public bool Done;
        public float Score;

        protected Minigame_(ProcessDef process, string hint)
        {
            Process = process;
            var card = Card(Vector2.Zero);
            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 10);
            Style(Title, 20, 1f);
            Title.Text = process.Name;
            Style(Hint, 14, 0.75f);
            Hint.Text = hint + "    Esc gives up.";
            box.AddChild(Title);
            box.AddChild(Bar);
            box.AddChild(Hint);
            card.AddChild(box);
            card.SetAnchorsPreset(Control.LayoutPreset.Center);
            card.GrowHorizontal = Control.GrowDirection.Both;
            card.GrowVertical = Control.GrowDirection.Both;
            Root = card;
        }

        public abstract void Key(Key key);
        public abstract void Tick(float dt);
    }

    // Spin the drill: alternate A and D (or the arrow keys) to build heat faster
    // than it bleeds away. Reaching full heat makes an ember.
    sealed class DrillGame : Minigame_
    {
        const float Limit = 8f;
        float heat, peak, elapsed;
        Key last;

        public DrillGame(ProcessDef process) : base(process, "Alternate A and D to spin the drill. Keep the rhythm.") { }

        public override void Key(Key key)
        {
            bool left = key is Godot.Key.A or Godot.Key.Left, right = key is Godot.Key.D or Godot.Key.Right;
            if (!left && !right) return;
            bool alternated = left ? last != Godot.Key.A : last != Godot.Key.D;
            heat += alternated ? 0.075f : -0.04f;
            last = left ? Godot.Key.A : Godot.Key.D;
        }

        public override void Tick(float dt)
        {
            elapsed += dt;
            heat = Math.Clamp(heat - 0.22f * dt, 0f, 1f);
            peak = Math.Max(peak, heat);
            Bar.Value = heat;
            Bar.Modulate = new Color(1f, 1f - heat * 0.6f, 1f - heat * 0.9f);
            Title.Text = heat < 0.4f ? Process.Name : heat < 0.8f ? "Smoke..." : "An ember is forming...";
            if (heat >= 1f || elapsed >= Limit)
            {
                // Finishing early is rewarded; running out of time keeps the best heat reached.
                Score = heat >= 1f ? 0.7f + 0.3f * (1f - elapsed / Limit) : peak * 0.8f;
                Done = true;
            }
        }
    }

    // Knap: a mark sweeps along the core. Strike with Space as it crosses the
    // bright spot. Three strikes; each is scored by how close it landed.
    sealed class StrikeGame : Minigame_
    {
        const int Strikes = 3;
        readonly List<float> hits = new();
        readonly ColorRect zone = new() { Color = new Color(1f, 0.92f, 0.6f, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore };
        readonly RandomNumberGenerator rng = new();
        float t, target, speed;

        public StrikeGame(ProcessDef process) : base(process, "Press Space as the mark crosses the bright spot. Three strikes.")
        {
            rng.Randomize();
            Bar.AddChild(zone);
            Next();
        }

        void Next()
        {
            target = rng.RandfRange(0.2f, 0.8f);
            speed = rng.RandfRange(1.1f, 1.6f) + hits.Count * 0.25f;
            zone.Position = new Vector2((target - 0.06f) * Bar.CustomMinimumSize.X, 0);
            zone.Size = new Vector2(0.12f * Bar.CustomMinimumSize.X, Bar.CustomMinimumSize.Y);
        }

        public override void Key(Key key)
        {
            if (key != Godot.Key.Space) return;
            float mark = (float)Bar.Value;
            hits.Add(Math.Clamp(1f - Math.Abs(mark - target) / 0.3f, 0f, 1f));
            Title.Text = hits[^1] > 0.8f ? "A clean flake comes away" : hits[^1] > 0.4f ? "It chips" : "A glancing blow";
            if (hits.Count >= Strikes)
            {
                Score = hits.Average();
                Done = true;
            }
            else Next();
        }

        public override void Tick(float dt)
        {
            t += dt * speed;
            Bar.Value = 0.5f + 0.5f * Mathf.Sin(t * 2.2f);
        }
    }

    // --- Styling ----------------------------------------------------------

    static Label Text(string text, int size, float alpha = 1f)
    {
        var label = new Label { Text = text };
        Style(label, size, alpha);
        return label;
    }

    static void Style(Label label, int size, float alpha)
    {
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(Look.Cream, alpha));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.35f));
        label.AddThemeConstantOverride("shadow_offset_y", 1);
    }

    static PanelContainer Card(Vector2 minSize)
    {
        var card = new PanelContainer { CustomMinimumSize = minSize };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.10f, 0.08f, 0.78f),
            ContentMarginLeft = 18,
            ContentMarginRight = 18,
            ContentMarginTop = 14,
            ContentMarginBottom = 14,
        };
        style.SetCornerRadiusAll(14);
        card.AddThemeStyleboxOverride("panel", style);
        return card;
    }
}
