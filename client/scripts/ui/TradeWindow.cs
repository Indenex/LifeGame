using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Trading face to face (D-069). G on a player asks to trade (Claims' handler); they see
// "Mara wants to trade" and press G to say yes or Esc to say no. The window shows both
// offers side by side, with what you carry below to add from. Each side accepts; any
// change to either offer clears both accepts, and the window says so. Esc calls the
// trade off; the server ends it when you walk apart, die or go offline.
public partial class TradeWindow : CanvasLayer
{
    // Talks to the server (claims-7) once Main calls Attach; a mock until then and for previews.
    public ITradeLink Link = new MockTradeLink();
    // A short line for the HUD ("You trade with Mara").
    public Action<string> Note = _ => { };

    static readonly Color Agreed = new(0.62f, 0.86f, 0.46f);
    static readonly Color Caution = new(0.98f, 0.76f, 0.38f);

    public bool IsOpen => Link.Current != null;
    // A --screenshot-trade preview keeps the pretend Mara even once connected.
    bool previewing;

    Control window = null!;
    Label title = null!, warning = null!, cleared = null!, asked = null!;
    VBoxContainer mineList = null!, theirsList = null!;
    HFlowContainer carryRow = null!;
    PanelContainer mineBox = null!, theirsBox = null!;
    Label mineState = null!, theirsState = null!, theirsHeading = null!;
    Button accept = null!;
    string shown = "";
    long lastVersion = -1;
    bool lastAnyAccepted;
    float clearedTime;

    // Swaps the mock for the server link once Main has a connection.
    public void Attach(ITradeLink link)
    {
        if (previewing) return;
        Link = link;
        Link.Said += line => Note(line);
        shown = "";
    }

    public override void _Ready()
    {
        Layer = 2;
        Link.Said += line => Note(line);
        Use.Register(new UseHandler { Id = "ui.trade", Priority = 100, Find = FindUse });

        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        // "Mara wants to trade", just above the G prompt, while someone waits on you.
        asked = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(asked, 16, 0.92f);
        asked.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        asked.GrowHorizontal = Control.GrowDirection.Both;
        asked.GrowVertical = Control.GrowDirection.Begin;
        asked.Position = new Vector2(0, -252);
        root.AddChild(asked);

        window = MakeWindow();
        window.Visible = false;
        root.AddChild(window);

        // --screenshot-trade opens a pretend trade with Mara; =asked shows her request,
        // =accepted has Mara accepted (both accepting swaps at once, so it never shows),
        // =changed shows the cleared accepts after you add something.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--screenshot-trade") && Link is MockTradeLink mock)
            {
                previewing = true;
                GetTree().CreateTimer(1.5).Timeout += () => Preview(mock, arg.Contains('=') ? arg[(arg.IndexOf('=') + 1)..] : "");
            }
    }

    void Preview(MockTradeLink mock, string what)
    {
        if (what == "asked") { mock.AskMe(); return; }
        mock.OpenNow();
        mock.Set(new TradeStack(Item.Stick, 0, 4));
        mock.Set(new TradeStack(Item.Flake, 60, 1));
        if (what == "accepted" || what == "changed")
        {
            mock.Tick(double.MaxValue);
            Refresh();
            if (what == "changed") mock.Set(new TradeStack(Item.Fiber, 0, 3));
        }
        // Each preview stays as set up; Mara doesn't make up her mind mid-shot.
        mock.Hold = true;
    }

    // Someone waits on you: G says yes, whatever you face. Its target is where you stand,
    // so it counts as in front of you and beats nearby bushes and fires.
    UseOffer? FindUse(Vector3 at) => !IsOpen && Link.Incoming is { } who
        ? new UseOffer($"Trade with {who.name}", 0f, () => Link.Answer(true), Target: at)
        : null;

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (Link is MockTradeLink mock) mock.Tick(Time.GetTicksMsec() / 1000.0);

        asked.Text = Link.Incoming is { } who ? $"{who.name} wants to trade    Esc says no"
            : Link.Outgoing is { } ask ? $"Waiting for {ask.name}    Esc takes it back"
            : "";
        Refresh();

        clearedTime -= dt;
        cleared.Modulate = new Color(1, 1, 1, Mathf.Clamp(clearedTime, 0f, 1f));
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape }) return;
        if (IsOpen || Link.Outgoing != null) Link.Cancel();
        else if (Link.Incoming != null) Link.Answer(false);
        else return;
        GetViewport().SetInputAsHandled();
    }

    // --- The window ------------------------------------------------------

    Control MakeWindow()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        title = Hud.Text("Trading", 20);
        box.AddChild(title);

        var sides = new HBoxContainer();
        sides.AddThemeConstantOverride("separation", 18);
        mineBox = Side("You give", out mineList, out mineState, out _);
        theirsBox = Side("They give", out theirsList, out theirsState, out theirsHeading);
        sides.AddChild(mineBox);
        sides.AddChild(theirsBox);
        box.AddChild(sides);

        accept = new Button { FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 38) };
        accept.AddThemeFontSizeOverride("font_size", 17);
        accept.Pressed += () => { if (Link.Current is { } t) Link.Accept(!t.MineAccepted, t.Version); };
        box.AddChild(accept);

        // Said loudly when a change wipes the accepts, so nobody agrees to the old deal.
        cleared = Hud.Text("The offer changed, so both accepts were cleared", 15);
        cleared.AddThemeColorOverride("font_color", Caution);
        cleared.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(cleared);

        var carryTitle = Hud.Text("You carry", 14);
        carryTitle.Modulate = new Color(1, 1, 1, 0.75f);
        box.AddChild(carryTitle);
        carryRow = new HFlowContainer { CustomMinimumSize = new Vector2(600, 0) };
        carryRow.AddThemeConstantOverride("h_separation", 6);
        carryRow.AddThemeConstantOverride("v_separation", 6);
        box.AddChild(carryRow);

        warning = Hud.Text("", 14);
        warning.AddThemeColorOverride("font_color", Caution);
        warning.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(warning);

        var hint = Hud.Text("Click adds or takes back one, Shift+click all    Esc calls it off", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);

        var panel = Hud.Panel(box, Vector2.Zero);
        var style = (StyleBoxFlat)panel.GetThemeStylebox("panel");
        style.BgColor = new Color(0.10f, 0.11f, 0.09f, 0.82f);
        style.ContentMarginTop = style.ContentMarginBottom = 14;
        style.ContentMarginLeft = style.ContentMarginRight = 18;
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        return panel;
    }

    static PanelContainer Side(string heading, out VBoxContainer list, out Label state, out Label label)
    {
        var col = new VBoxContainer { CustomMinimumSize = new Vector2(290, 190) };
        col.AddThemeConstantOverride("separation", 4);
        label = Hud.Text(heading, 15);
        col.AddChild(label);
        list = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        col.AddChild(list);
        state = Hud.Text("", 14);
        col.AddChild(state);

        var box = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.07f, 0.05f, 0.55f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        style.SetCornerRadiusAll(10);
        style.SetBorderWidthAll(2);
        box.AddThemeStyleboxOverride("panel", style);
        box.AddChild(col);
        return box;
    }

    void Refresh()
    {
        var trade = Link.Current;
        window.Visible = trade != null;
        if (trade == null)
        {
            lastVersion = -1;
            lastAnyAccepted = false;
            shown = "";
            return;
        }

        var carried = Link.Carried().ToList();
        string now = $"{trade.Version}|{trade.MineAccepted}|{trade.TheirsAccepted}|" + string.Join(",", carried.Select(s => $"{s.Item}.{s.Quality}.{s.Count}"));
        if (now == shown) return;
        shown = now;

        // A change after anyone accepted wipes both accepts; flag it.
        if (lastVersion >= 0 && trade.Version != lastVersion && lastAnyAccepted) clearedTime = 4f;
        lastVersion = trade.Version;
        lastAnyAccepted = trade.MineAccepted || trade.TheirsAccepted;

        title.Text = $"Trading with {trade.Partner}";
        theirsHeading.Text = $"{trade.Partner} gives";
        Fill(mineList, trade.Mine, (s, n) => Link.Set(s with { Count = s.Count - n }));
        Fill(theirsList, trade.Theirs, null);
        Mark(mineBox, mineState, trade.MineAccepted, "You accept", "You haven't accepted");
        Mark(theirsBox, theirsState, trade.TheirsAccepted, $"{trade.Partner} accepts", $"{trade.Partner} hasn't accepted");
        accept.Text = trade.MineAccepted ? $"Accepted, waiting for {trade.Partner} (click to take it back)" : "Accept this trade";
        accept.Modulate = trade.MineAccepted ? Agreed : Colors.White;

        // What you could still add: what you carry less what you already offer.
        var left = carried.Select(s => s with { Count = s.Count - Offered(trade, s) })
            .Where(s => s.Count > 0).ToList();
        foreach (var child in carryRow.GetChildren()) { carryRow.RemoveChild(child); child.QueueFree(); }
        foreach (var s in left)
            carryRow.AddChild(ItemButton(s, (_, n) => Link.Set(s with { Count = Offered(trade, s) + n })));
        if (left.Count == 0) carryRow.AddChild(Faint("Nothing more to offer"));

        // The carrying limit (D-042): warn when the swap would leave you overloaded.
        var before = Carry.Of(carried.Select(s => (s.Item, s.Count)));
        var after = Carry.Of(Merge(carried, trade.Mine, trade.Theirs));
        warning.Text = after.load > after.limit && after.load > before.load
            ? "After this trade you'd carry more than you comfortably can, and walk slower"
            : "";
    }

    static uint Offered(TradeView trade, TradeStack s) =>
        trade.Mine.FirstOrDefault(m => m.Item == s.Item && m.Quality == s.Quality)?.Count ?? 0;

    static IEnumerable<(Item, uint)> Merge(List<TradeStack> carried, IReadOnlyList<TradeStack> give, IReadOnlyList<TradeStack> get)
    {
        var counts = new Dictionary<Item, long>();
        foreach (var s in carried) counts[s.Item] = counts.GetValueOrDefault(s.Item) + s.Count;
        foreach (var s in give) counts[s.Item] = counts.GetValueOrDefault(s.Item) - s.Count;
        foreach (var s in get) counts[s.Item] = counts.GetValueOrDefault(s.Item) + s.Count;
        return counts.Where(kv => kv.Value > 0).Select(kv => (kv.Key, (uint)kv.Value));
    }

    static void Mark(PanelContainer box, Label state, bool accepted, string yes, string no)
    {
        var style = (StyleBoxFlat)box.GetThemeStylebox("panel");
        style.BorderColor = accepted ? Agreed : new Color(Look.Cream, 0.18f);
        state.Text = accepted ? "✓ " + yes : no;
        state.AddThemeColorOverride("font_color", accepted ? Agreed : new Color(Look.Cream, 0.55f));
    }

    static void Fill(VBoxContainer list, IReadOnlyList<TradeStack> stacks, Action<TradeStack, uint>? take)
    {
        foreach (var child in list.GetChildren()) { list.RemoveChild(child); child.QueueFree(); }
        foreach (var s in stacks)
        {
            var button = ItemButton(s, take);
            button.Alignment = HorizontalAlignment.Left;
            list.AddChild(button);
        }
        if (stacks.Count == 0) list.AddChild(Faint("Nothing yet"));
    }

    // An item with its picture. Click moves one, Shift+click the whole stack (move gets
    // the stack and how many); with no action it's just shown (the other side's offer).
    static Button ItemButton(TradeStack s, Action<TradeStack, uint>? move)
    {
        var button = new Button
        {
            Text = Words(s),
            Icon = ItemIcon.For(s.Item),
            Flat = true,
            FocusMode = Control.FocusModeEnum.None,
            Disabled = move == null,
        };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeConstantOverride("icon_max_width", 30);
        button.AddThemeColorOverride("font_disabled_color", Look.Cream);
        if (move != null)
            button.Pressed += () => move(s, Input.IsKeyPressed(Key.Shift) ? s.Count : 1);
        return button;
    }

    // "4 sticks", or "Decent stone flake" for just one.
    static string Words(TradeStack s)
    {
        string label = ItemLook.Label(s.Item, s.Quality, s.Count);
        return s.Count > 1 ? $"{s.Count} {char.ToLowerInvariant(label[0])}{label[1..]}" : label;
    }

    static Label Faint(string text)
    {
        var label = Hud.Text(text, 14);
        label.Modulate = new Color(1, 1, 1, 0.5f);
        return label;
    }
}
