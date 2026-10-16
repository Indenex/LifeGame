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
    // True while the panel is up (ui/Panels.cs reads it, so the G prompt steps aside).
    public bool PanelOpen => panel.Visible;
    public event Action<string>? Notice;

    DbConnection? conn;
    readonly PanelContainer panel;
    readonly VBoxContainer list = new();
    // The list scrolls once it would run past the bottom of the screen (the clay tier is long).
    readonly ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
    readonly HBoxContainer tabs = new();
    // Processes are grouped into tabs by tier, so the list stays short as tiers grow.
    static readonly string[] TierNames = { "Bare hands", "Fire and stone", "Cord and haft", "Clay", "Tier 4", "Tier 5" };
    int tier;
    readonly PanelContainer progressPanel;
    readonly Label progressLabel = new();
    readonly ProgressBar progressBar = new() { MinValue = 0, MaxValue = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(260, 8) };
    Minigame_? minigame;
    CraftJob? job;
    double jobStartLocal, jobSeconds, roastRefresh;

    public CraftingUi()
    {
        Layer = 3; // above the HUD (Layer 2), so open panels cover prompts and rings
        panel = Card(new Vector2(380, 0));
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(Text("Crafting", 20));
        box.AddChild(Text("Click a process or press its number.  Tab switches tier.  C or Esc closes.", 13, 0.65f));
        tabs.AddThemeConstantOverride("separation", 4);
        box.AddChild(tabs);
        list.AddThemeConstantOverride("separation", 6);
        list.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(list);
        box.AddChild(scroll);
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
        // Pinned to the right edge; it grows leftward and downward to fit its rows.
        panel.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.OffsetLeft = panel.OffsetRight = -24;
        panel.OffsetTop = panel.OffsetBottom = -200;
        root.AddChild(panel);
        progressPanel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        progressPanel.GrowHorizontal = Control.GrowDirection.Both;
        progressPanel.Position = new Vector2(-150, -130);
        root.AddChild(progressPanel);
        // --screenshot-minigame shows the hand-drill game's box without a server.
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-minigame") >= 0)
            AddChild(new DrillGame(Processes.List.First(p => p.Minigame == Minigame.Drill)).Root);
    }

    public void Attach(DbConnection conn)
    {
        this.conn = conn;
        conn.Db.MyInventory.OnInsert += (_, _) => Refresh();
        conn.Db.MyInventory.OnUpdate += (_, _, _) => Refresh();
        conn.Db.MyInventory.OnDelete += (_, _) => Refresh();
        conn.Db.MyCraftJob.OnInsert += (_, j) => StartedJob(j);
        conn.Db.MyCraftJob.OnDelete += (_, j) => FinishedJob(j);
        conn.Db.Roast.OnInsert += (_, _) => Refresh();
        conn.Db.Roast.OnDelete += (_, _) => Refresh();
        // G beside a fire takes your meat off once it is done; ahead of feeding the fire.
        Use.Register(new UseHandler
        {
            Id = "crafting.take_roast",
            Priority = 25,
            Find = at =>
            {
                var (roast, seconds) = OldestDoneRoast();
                if (roast == null) return null;
                float d = new Vector2(roast.X - at.X, roast.Z - at.Z).Length();
                if (d > World.Reach) return null;
                return new UseOffer(Roasting.Burnt(seconds) ? "Take the burnt meat off" : "Take the meat off the fire", d,
                    () => conn.Reducers.TakeOffFire(roast.Id));
            },
        });
    }

    // Opens the panel on a tier; used by --screenshot-craft.
    public void Open(int onTier)
    {
        tier = onTier;
        panel.Visible = true;
        Refresh();
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
        if (panel.Visible && e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape })
        {
            panel.Visible = false;
            GetViewport().SetInputAsHandled();
        }
        else if (panel.Visible && e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Key0 })
        {
            TakeOffFire();
            GetViewport().SetInputAsHandled();
        }
        else if (panel.Visible && e is InputEventKey { Pressed: true, Echo: false } k
            && k.PhysicalKeycode >= Key.Key1 && k.PhysicalKeycode <= Key.Key9)
        {
            int index = (int)(k.PhysicalKeycode - Key.Key1);
            var shown = InTier();
            if (index < shown.Count) Begin(shown[index]);
            GetViewport().SetInputAsHandled();
        }
        else if (panel.Visible && e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Tab } tabKey)
        {
            var tiers = Tiers();
            int at = Math.Max(0, tiers.IndexOf(tier));
            tier = tiers[(at + (tabKey.ShiftPressed ? tiers.Count - 1 : 1)) % tiers.Count];
            Refresh();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        minigame?.Tick((float)delta);
        if (minigame is { Done: true } done) EndMinigame(done.Score);
        if (panel.Visible)
        {
            // The panel's top sits 200 px above the middle; keep its foot clear of the HUD.
            float room = Math.Max(160f, GetViewport().GetVisibleRect().Size.Y / 2 - 50);
            scroll.CustomMinimumSize = new Vector2(0, Math.Min(list.GetCombinedMinimumSize().Y, room));
        }

        // Roasts change from browning to ready to burnt while you watch.
        roastRefresh -= delta;
        if (roastRefresh <= 0 && panel.Visible && MyRoasts().Any())
        {
            roastRefresh = 1;
            Refresh();
        }

        if (job != null)
        {
            double t = Time.GetTicksMsec() / 1000.0 - jobStartLocal;
            progressBar.Value = Math.Clamp(t / Math.Max(jobSeconds, 0.01), 0, 1);
        }
    }

    // --- Starting a process ----------------------------------------------

    bool reopenAfterMinigame;

    // Starts a process from outside the panel, such as G next to a tree.
    public void Start(ProcessDef def) => Begin(def);

    void Begin(ProcessDef def)
    {
        if (conn == null) { Notice?.Invoke("Crafting needs the server"); return; }
        if (job != null) { Notice?.Invoke("Your hands are busy"); return; }
        if (Missing(def) is { } missing) { Notice?.Invoke(missing); return; }

        // The panel stays open after a craft, so pressing the same number repeats
        // it; it only steps aside while a minigame is played.
        // Skill hook (journal/CraftingUi.Skills.cs): a Master just does the work.
        float? skipped = null;
        if (def.Minigame != Minigame.None) SkipMinigame(def, ref skipped);
        if (skipped is { } sure)
        {
            conn.Reducers.StartCraft(def.Id, sure);
            return;
        }
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
        reopenAfterMinigame = panel.Visible;
        panel.Visible = false;
        AddChild(minigame.Root);
        MinigameSound(def, true);
    }

    // Sound hook: Atmosphere implements this in its own file to play the work sounds
    // (Sound.Working) while any minigame is open, Drill and Strike here or Shape and
    // Coil from Ceramics, so the hands-on part is never silent (D-036). Unimplemented,
    // the compiler drops the calls.
    static partial void MinigameSound(ProcessDef def, bool playing);

    // Other lanes add their own minigames in their own folder (see ceramics/CraftingUi.Clay.cs).
    partial void MinigameFor(ProcessDef def, ref Minigame_? game);

    // Skill hooks, implemented in journal/CraftingUi.Skills.cs: skip the minigame
    // with this score, and how many steps (strikes, coils, strokes) it takes.
    static partial void SkipMinigame(ProcessDef def, ref float? score);
    static partial void StepsFor(ProcessDef def, ref int steps);
    static partial void Mastered(ProcessDef def, ref bool mastered);
    // The row's note on the minigame: a Master does it by heart (D-047).
    static string HandsOn(ProcessDef def)
    {
        if (def.Minigame == Minigame.None) return "";
        bool mastered = false;
        Mastered(def, ref mastered);
        return mastered ? "  ·  by heart. Hold Ctrl to work it by hand" : "  ·  hands-on";
    }
    static int Steps(ProcessDef def, int baseSteps)
    {
        StepsFor(def, ref baseSteps);
        return Math.Max(1, baseSteps);
    }
    static string Times(int n, string one, string many) =>
        (n <= 5 ? new[] { "", "One", "Two", "Three", "Four", "Five" }[n] : n.ToString()) + " " + (n == 1 ? one : many);

    void EndMinigame(float? score)
    {
        if (minigame == null) return;
        var def = minigame.Process;
        minigame.Root.QueueFree();
        minigame = null;
        MinigameSound(def, false);
        if (reopenAfterMinigame)
        {
            panel.Visible = true;
            Refresh();
        }
        if (score is { } s && conn != null) conn.Reducers.StartCraft(def.Id, s);
    }

    // What is missing to run a process, and where to get it, or null when
    // everything is at hand. Shown under a greyed-out recipe and as a notice.
    string? Missing(ProcessDef def)
    {
        // Station crafts are made standing at the station (Stations.cs).
        Structure? station = null;
        if (def.Station != StationKind.None)
        {
            station = conn == null ? null : Stations.AtPlayer(conn, def.Station);
            if (station == null)
                return $"Made at a {StationInfo.Name(def.Station)}. Build one inside your claim and stand at it.";
        }
        foreach (var input in def.Consumes)
        {
            uint have = Count(input.Item) + (station != null && conn != null ? Stations.StoredNear(conn, station, input.Item) : 0);
            if (have >= input.Count) continue;
            uint lacking = input.Count - have;
            string what = Items.Def(input.Item).Count(lacking).ToLowerInvariant();
            return $"You need {(have > 0 ? lacking + " more" : lacking.ToString())} {what}. {WhereFrom(input.Item)}";
        }
        if (def.NeedsTool.HasFlag(ItemTags.Hammer))
        {
            // The stone being worked cannot also be the one striking it.
            uint used = def.Consumes.Where(i => Items.Def(i.Item).Tags.HasFlag(ItemTags.Hammer)).Aggregate(0u, (n, i) => n + i.Count);
            if (HammerCount() < used + 1)
                return $"You need {(used > 0 ? "a second stone" : "a stone")} to strike with. {WhereFrom(Item.Stone)}";
        }
        else if (def.NeedsTool != ItemTags.None && !HasTool(def.NeedsTool))
        {
            // Name the simplest thing that would do, and how it is made.
            var tool = Items.All.Values.Where(d => (d.Tags & def.NeedsTool) == def.NeedsTool)
                .OrderBy(d => Processes.List.FirstOrDefault(p => p.Makes == d.Id)?.Tier ?? 0).FirstOrDefault();
            return tool == null ? "You need the right tool"
                : $"You need {Article(tool.Name.ToLowerInvariant())}. {WhereFrom(tool.Id)}";
        }
        return null;
    }

    // One short line on where a new player gets an item.
    static string WhereFrom(Item item)
    {
        string Key(string action) =>
            Controls.World.FirstOrDefault(b => b.Action == action) is { } b && Controls.KeysFor(b) is { } k ? $" ({k})" : "";
        return item switch
        {
            Item.Stick => $"Pick them up under the trees{Key("pick_up")}.",
            Item.Log => $"Chop one from a tree with a stone axe{Key("use")}.",
            Item.Stone or Item.Fiber => $"Pick it up off the ground{Key("pick_up")}.",
            Item.Clay => $"Dig{Key("dig")} on the bare tan patches where the grass thins out.",
            Item.Potsherd => "Sherds are what's left of a pot that cracks in the fire.",
            _ when Processes.List.FirstOrDefault(p => p.Makes == item) is { } maker => $"Make it first: {maker.Name}.",
            _ => "",
        };
    }

    static string Article(string noun) => ("aeiou".Contains(noun[0]) ? "an " : "a ") + noun;

    uint Count(Item item) =>
        conn?.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Aggregate(0u, (n, i) => n + i.Count) ?? 0;

    uint HammerCount() =>
        conn?.Db.MyInventory.Iter().Where(i => Items.IsKnown(i.Item) && Items.Def((Item)i.Item).Tags.HasFlag(ItemTags.Hammer))
            .Aggregate(0u, (n, i) => n + i.Count) ?? 0;

    bool HasTool(ItemTags tags) =>
        conn?.Db.MyInventory.Iter().Any(i => Items.IsKnown(i.Item) && (Items.Def((Item)i.Item).Tags & tags) == tags) ?? false;

    // What a tool tag reads as in a sentence: "using a cutting edge".
    internal static string ToolName(ItemTags tags) => string.Join(" and ", Enum.GetValues<ItemTags>()
        .Where(t => t != ItemTags.None && tags.HasFlag(t))
        .Select(t => t switch
        {
            ItemTags.Hammer => "a hammer",
            ItemTags.Cutting => "a cutting edge",
            ItemTags.Chopping => "a chopping tool",
            ItemTags.Piercing => "a sharp point",
            ItemTags.Binding => "something to bind with",
            ItemTags.Kindling => "kindling",
            ItemTags.Digging => "a digging tool",
            ItemTags.FireBow => "a bow drill",
            ItemTags.Container => "a container",
            _ => t.ToString().ToLowerInvariant(),
        }));

    static List<int> Tiers() => Processes.List.Select(p => p.Tier).Distinct().OrderBy(t => t).ToList();

    List<ProcessDef> InTier() => Processes.List.Where(p => p.Tier == tier).ToList();

    void Refresh()
    {
        if (!panel.Visible) return;
        foreach (var child in tabs.GetChildren()) child.QueueFree();
        foreach (int t in Tiers())
        {
            int ready = Processes.List.Count(p => p.Tier == t && Missing(p) == null);
            var tabButton = new Button
            {
                Text = ready > 0 ? $"{TierName(t)} ({ready})" : TierName(t),
                ToggleMode = true,
                ButtonPressed = t == tier,
                FocusMode = Control.FocusModeEnum.None,
            };
            tabButton.AddThemeFontSizeOverride("font_size", 13);
            int captured = t;
            tabButton.Pressed += () => { tier = captured; Refresh(); };
            tabs.AddChild(tabButton);
        }

        foreach (var child in list.GetChildren()) child.QueueFree();
        foreach (var (roast, seconds) in MyRoasts())
        {
            var name = Items.IsKnown(roast.Item) ? Items.Def((Item)roast.Item).Name.ToLowerInvariant() : "meat";
            var row = new Button
            {
                Text = Roasting.Done(seconds)
                    ? $"0.  Take your {name} off the fire\n      {Roasting.State(seconds)}"
                    : $"     Your {name} is over the fire\n      browning, ready in {Roasting.CookSeconds - seconds:0}s",
                Alignment = HorizontalAlignment.Left,
                Disabled = !Roasting.Done(seconds),
                Flat = true,
                FocusMode = Control.FocusModeEnum.None,
            };
            row.AddThemeColorOverride("font_color", Roasting.Burnt(seconds) ? new Color(0.8f, 0.6f, 0.5f) : new Color(1f, 0.82f, 0.5f));
            row.AddThemeColorOverride("font_disabled_color", new Color(Look.Cream, 0.5f));
            row.AddThemeFontSizeOverride("font_size", 14);
            row.Pressed += TakeOffFire;
            list.AddChild(row);
        }
        int n = 1;
        foreach (var def in InTier())
        {
            string inputs = string.Join(", ", def.Consumes.Select(i => $"{i.Count} {Items.Def(i.Item).Count(i.Count).ToLowerInvariant()}"));
            if (def.NeedsTool != ItemTags.None) inputs += $", using {ToolName(def.NeedsTool)}";
            if (def.Station != StationKind.None) inputs += $", at a {StationInfo.Name(def.Station)}";
            string? missing = Missing(def);
            bool ready = missing == null;
            var button = new Button
            {
                Text = $"{n}.  {def.Name}\n      {inputs}  ·  {def.Seconds:0}s{HandsOn(def)}" + (missing != null ? $"\n      {missing}" : ""),
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

    // Your roasts on fires nearby, with how long each has been cooking.
    // The roast that has been on longest among those done, as the panel orders them.
    // A plain loop: the Use key asks every frame (bugfix-8).
    (Roast? roast, float seconds) OldestDoneRoast()
    {
        (Roast? roast, float seconds) best = (null, 0f);
        if (conn?.Identity is not { } me) return best;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        foreach (var r in conn.Db.Roast.Iter())
        {
            if (r.Owner != me) continue;
            float seconds = (now - r.PutAt.MicrosecondsSinceUnixEpoch) / 1_000_000f;
            if (Roasting.Done(seconds) && (best.roast == null || seconds > best.seconds)) best = (r, seconds);
        }
        return best;
    }

    IEnumerable<(Roast roast, float seconds)> MyRoasts()
    {
        if (conn?.Identity is not { } me) return Enumerable.Empty<(Roast, float)>();
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        return conn.Db.Roast.Iter().Where(r => r.Owner == me)
            .Select(r => (r, (now - r.PutAt.MicrosecondsSinceUnixEpoch) / 1_000_000f))
            .OrderByDescending(t => t.Item2).ToList();
    }

    // Takes off the roast that has been on longest, if it is done.
    void TakeOffFire()
    {
        if (conn == null) return;
        var (roast, seconds) = MyRoasts().FirstOrDefault();
        if (roast == null) { Notice?.Invoke("Nothing of yours is on a fire"); return; }
        if (!Roasting.Done(seconds)) { Notice?.Invoke("It isn't cooked yet"); return; }
        conn.Reducers.TakeOffFire(roast.Id);
    }

    static string TierName(int t) => t < TierNames.Length ? TierNames[t] : $"Tier {t}";

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
        else if (def.Outcome == Outcome.Roast)
            Notice?.Invoke($"It's over the fire. Press G by the fire once it's browned ({Roasting.BestSeconds:0}s is about right)");
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
            // Low on the screen, so the player and the fire stay in view (Hud's lanes).
            Hud.PlaceLow(card);
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
        // About 14 good strokes catch an ember for a novice, fewer with skill.
        readonly float stroke;
        float heat, peak, elapsed;
        Key last;

        // A bow drill turns the spindle several times a stroke, so it needs half as many.
        public DrillGame(ProcessDef process) : base(process, process.NeedsTool.HasFlag(ItemTags.FireBow)
            ? "Alternate A and D to saw the bow. Keep the rhythm."
            : "Alternate A and D to spin the drill. Keep the rhythm.") =>
            stroke = 1.05f / Steps(process, process.NeedsTool.HasFlag(ItemTags.FireBow) ? 7 : 14);

        public override void Key(Key key)
        {
            bool left = key is Godot.Key.A or Godot.Key.Left, right = key is Godot.Key.D or Godot.Key.Right;
            if (!left && !right) return;
            bool alternated = left ? last != Godot.Key.A : last != Godot.Key.D;
            heat += alternated ? stroke : -0.04f;
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
    // bright spot. Three strikes (fewer with skill); each is scored by how close it landed.
    sealed class StrikeGame : Minigame_
    {
        readonly int strikes;
        readonly List<float> hits = new();
        readonly ColorRect zone = new() { Color = new Color(1f, 0.92f, 0.6f, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore };
        readonly RandomNumberGenerator rng = new();
        float t, target, speed;

        public StrikeGame(ProcessDef process) : base(process, "Press Space as the mark crosses the bright spot.")
        {
            strikes = Steps(process, 3);
            Hint.Text = $"Press Space as the mark crosses the bright spot. {Times(strikes, "strike", "strikes")}.    Esc gives up.";
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
            if (hits.Count >= strikes)
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
