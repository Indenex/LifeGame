using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The panel at a crafting station (D-098, D-112): Use (E) at a finished station opens it.
// It shows what is made there as the crafting menu's picture cards (ui/CraftGrid.cs,
// ui-25), counting each ingredient in your bag and nearby (baskets and jars within 4 m
// of the station, and the claim's stash).
// Make hands the craft to the C panel's own path (CraftingUi.Start), so minigames and
// skills work the same; the server (Stations.cs) has the last word. Walking away, Use or
// Esc closes it. Main sets Conn, Crafting and Me.
public partial class StationPanel : CanvasLayer
{
    public Func<DbConnection?> Conn = () => null;
    public CraftingUi Crafting = null!;
    // Where you stand; set by Main.
    public Func<Vector3> Me = () => Vector3.Zero;

    static readonly StringName NUse = "use";

    public bool IsOpen => station != null;

    Structure? station;
    Control window = null!;
    Label title = null!, hint = null!;
    readonly CraftGrid grid = new();
    string shown = "";

    public override void _Ready()
    {
        Layer = 2;
        window = MakeWindow();
        window.Visible = false;
        AddChild(window);
        Use.Register(new UseHandler { Id = "ui.station", Priority = 30, Find = FindUse });
    }

    // Use (E) at a finished station you stand at opens its panel.
    UseOffer? FindUse(Vector3 at)
    {
        if (IsOpen || Conn() is not { } c) return null;
        Structure? best = null;
        float bestD = float.MaxValue;
        foreach (StationKind kind in Enum.GetValues<StationKind>())
        {
            if (kind == StationKind.None || Stations.NearestOf(c, kind, at) is not { } s) continue;
            float d = new Vector2(s.X - at.X, s.Z - at.Z).Length();
            if (d < bestD) { best = s; bestD = d; }
        }
        if (best is not { } found) return null;
        var kindOf = Structures.Shape((int)found.Kind).Station;
        return new UseOffer($"Use the {StationInfo.Name(kindOf)}", bestD, () => Open(found), Target: new Vector3(found.X, at.Y, found.Z));
    }

    public void Open(Structure at)
    {
        station = at;
        shown = "";
        window.Visible = true;
        Refresh();
    }

    public void Close()
    {
        station = null;
        grid.ClearSearch();
        window.Visible = false;
    }

    public override void _Process(double delta)
    {
        TipFirstStation();
        if (station is not { } s) return;
        var kind = Structures.Shape((int)s.Kind).Station;
        // Walked away, or the station is gone: close.
        if (Conn() is not { } c || Stations.NearestOf(c, kind, Me()) is not { } still || still.Id != s.Id)
        {
            Close();
            return;
        }
        station = still;
        Refresh();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!IsOpen) return;
        if (grid.Typing && e is not InputEventKey { PhysicalKeycode: Key.Escape }) return;
        if (e.IsActionPressed(NUse) || e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape })
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    // The fourth first-evening tip (D-150): once your first station stands, how to use
    // it. Shown once per install through the hint corner; checked about once a second.
    bool tipped;
    float tipWait;

    void TipFirstStation()
    {
        if (tipped || (tipWait -= (float)GetProcessDeltaTime()) > 0f) return;
        tipWait = 1f;
        if (Conn() is not { } c || c.Identity is not { } me) return;
        var mine = c.Db.Structure.Iter().FirstOrDefault(s => s.Complete && s.Owner == me
            && Structures.Shape((int)s.Kind).Station != StationKind.None);
        if (mine == null) return;
        tipped = true;
        string name = StationInfo.Name(Structures.Shape((int)mine.Kind).Station);
        Hints.Tip("station_use", $"Stand at the {name} and press {Controls.KeyFor("use") ?? "E"} to see what's made there.");
    }

    // --- Layout ------------------------------------------------------------

    Control MakeWindow()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        title = Hud.Text("", 20);
        box.AddChild(title);
        grid.Recipes = () => station is { } s ? Processes.List.Where(p => p.Station == Structures.Shape((int)s.Kind).Station) : Enumerable.Empty<ProcessDef>();
        grid.Missing = def => Crafting.Missing(def);
        grid.Have = item => Conn() is { } c && station is { } s ? Bag(c, item) + Stations.StoredNear(c, s, item) : 0;
        grid.Make = def =>
        {
            // A minigame needs the screen, so the panel steps aside; otherwise it stays to make another.
            if (def.Minigame != Minigame.None) Close();
            Crafting.Start(def);
        };
        box.AddChild(grid);
        hint = Hud.Text($"Takes from your bag first, then baskets and jars nearby, then the claim's stash    {Controls.KeyFor("use") ?? "E"} or Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);

        // The same soft dark panel as the other windows (D-125).
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

    // --- Contents ----------------------------------------------------------

    void Refresh()
    {
        if (station is not { } s || Conn() is not { } c) return;
        var kind = Structures.Shape((int)s.Kind).Station;
        var recipes = Processes.List.Where(p => p.Station == kind).ToList();

        // Rebuild only when something a row shows has changed.
        var lines = recipes.Select(p => (p, Crafting.Missing(p), p.Consumes.Select(i => (Bag(c, i.Item), Stations.StoredNear(c, s, i.Item))).ToList())).ToList();
        string now = s.Id + "|" + string.Join(";", lines.Select(l => l.Item2 + ":" + string.Join(",", l.Item3)));
        if (now == shown) return;
        shown = now;

        title.Text = Capital(StationInfo.Name(kind));
        grid.Refresh();
    }

    static uint Bag(DbConnection c, Item item) =>
        c.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Aggregate(0u, (n, i) => n + i.Count);

    static string Capital(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

}
