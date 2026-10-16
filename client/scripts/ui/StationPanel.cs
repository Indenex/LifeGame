using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The panel at a crafting station (D-098, D-112): Use (E) at a finished station opens it.
// It lists what is made there and, for each ingredient, how much you have in your bag
// and nearby (baskets and jars within 4 m of the station, and the claim's stash).
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
    static readonly Color Short = new(0.98f, 0.76f, 0.38f);

    public bool IsOpen => station != null;

    Structure? station;
    Control window = null!;
    Label title = null!, hint = null!;
    VBoxContainer rows = null!;
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
        rows = new VBoxContainer { CustomMinimumSize = new Vector2(560, 0) };
        rows.AddThemeConstantOverride("separation", 8);
        box.AddChild(rows);
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
        foreach (var child in rows.GetChildren()) { rows.RemoveChild(child); child.QueueFree(); }
        foreach (var (def, missing, counts) in lines) rows.AddChild(Row(def, missing, counts));
        if (recipes.Count == 0) rows.AddChild(Faint("Nothing is made here yet"));
    }

    Control Row(ProcessDef def, string? missing, List<(uint bag, uint near)> counts)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        var picture = new TextureRect
        {
            Texture = def.Makes != default ? ItemIcon.For(def.Makes) : null,
            CustomMinimumSize = new Vector2(44, 44),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        row.AddChild(picture);

        var text = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 2);
        text.AddChild(Hud.Text(def.Name, 16));
        // "4 fiber: 1 in your bag, 3 nearby", one line per ingredient.
        for (int i = 0; i < def.Consumes.Length; i++)
        {
            var need = def.Consumes[i];
            var (bag, near) = counts[i];
            string what = Items.Def(need.Item).Count(need.Count).ToLowerInvariant();
            string where = (bag, near) switch
            {
                (0, 0) => "none to hand",
                (_, 0) => $"{bag} in your bag",
                (0, _) => $"{near} nearby",
                _ => $"{bag} in your bag, {near} nearby",
            };
            var line = Hud.Text($"{need.Count} {what}: {where}", 13);
            line.Modulate = bag + near >= need.Count ? new Color(1, 1, 1, 0.75f) : Colors.White;
            if (bag + near < need.Count) line.AddThemeColorOverride("font_color", Short);
            text.AddChild(line);
        }
        if (def.NeedsTool != ItemTags.None)
        {
            var tool = Hud.Text($"Using {CraftingUi.ToolName(def.NeedsTool)}", 13);
            tool.Modulate = new Color(1, 1, 1, 0.6f);
            text.AddChild(tool);
        }
        if (missing != null)
        {
            var why = Hud.Text(missing, 13);
            why.AddThemeColorOverride("font_color", Short);
            why.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            why.CustomMinimumSize = new Vector2(380, 0);
            text.AddChild(why);
        }
        row.AddChild(text);

        var make = new Button { Text = "Make", Disabled = missing != null, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(90, 36) };
        make.AddThemeFontSizeOverride("font_size", 16);
        make.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        make.Pressed += () =>
        {
            // The minigame (if any) needs the screen; the panel steps aside.
            Close();
            Crafting.Start(def);
        };
        row.AddChild(make);
        return row;
    }

    static uint Bag(DbConnection c, Item item) =>
        c.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Aggregate(0u, (n, i) => n + i.Count);

    static string Capital(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    static Label Faint(string text)
    {
        var label = Hud.Text(text, 14);
        label.Modulate = new Color(1, 1, 1, 0.5f);
        return label;
    }
}
