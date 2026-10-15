using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Using finished buildings: G at a hut's doorway hangs a door (4 sticks, 2 fiber)
// or opens and shuts it; G at a storage basket opens it. Also shows weathering
// on things left untouched outside a claim (D-046).
public partial class BuildingSystem
{
    readonly Dictionary<ulong, bool> doors = new();
    readonly Dictionary<ulong, DateTimeOffset> touched = new();
    float weatherTimer;

    CanvasLayer? basketLayer;
    VBoxContainer basketInside = null!, basketCarried = null!;
    Label basketTitle = null!;
    ulong? openBasket;

    // Server queries for what this file draws; Main adds them to its subscription.
    public static IEnumerable<string> Queries() => new[]
    {
        "SELECT * FROM structure_door",
        "SELECT * FROM structure_touch",
        "SELECT * FROM basket_item",
    };

    void BindUse(DbConnection c)
    {
        c.Db.StructureDoor.OnInsert += (_, d) => SetDoor(d.StructureId, d.Closed, animate: true);
        c.Db.StructureDoor.OnUpdate += (_, _, d) => SetDoor(d.StructureId, d.Closed, animate: true);
        c.Db.StructureDoor.OnDelete += (_, d) => SetDoor(d.StructureId, null, animate: false);
        c.Db.StructureTouch.OnInsert += (_, t) => Touched(t);
        c.Db.StructureTouch.OnUpdate += (_, _, t) => Touched(t);
        c.Db.StructureTouch.OnDelete += (_, t) => touched.Remove(t.StructureId);
        c.Db.BasketItem.OnInsert += (_, _) => RefreshBasket();
        c.Db.BasketItem.OnUpdate += (_, _, _) => RefreshBasket();
        c.Db.BasketItem.OnDelete += (_, _) => RefreshBasket();
        c.Db.MyInventory.OnInsert += (_, _) => RefreshBasket();
        c.Db.MyInventory.OnUpdate += (_, _, _) => RefreshBasket();
        c.Db.MyInventory.OnDelete += (_, _) => RefreshBasket();
    }

    void SetDoor(ulong structureId, bool? closed, bool animate)
    {
        if (closed is { } shut) doors[structureId] = shut;
        else doors.Remove(structureId);
        if (views.TryGetValue(structureId, out var v)) v.SetDoor(closed, animate);
    }

    // A view made after its door row arrived picks the door up here.
    void ApplyExtras(Structure s, StructureView v)
    {
        if (doors.TryGetValue(s.Id, out var shut)) v.SetDoor(shut, animate: false);
        v.SetWeathered(Weathered(s.Id));
    }

    bool DoorShut(ulong structureId) => doors.TryGetValue(structureId, out var shut) && shut;

    // --- Weathering -------------------------------------------------------

    void Touched(StructureTouch t)
    {
        touched[t.StructureId] = DateTimeOffset.FromUnixTimeMilliseconds(t.LastTouched.MicrosecondsSinceUnixEpoch / 1000);
        if (views.TryGetValue(t.StructureId, out var v)) v.SetWeathered(Weathered(t.StructureId));
    }

    float Weathered(ulong structureId)
    {
        if (!touched.TryGetValue(structureId, out var at)) return 0f;
        double hours = (DateTimeOffset.UtcNow - at).TotalHours - Structures.WeatherAfterHours;
        return hours <= 0 ? 0f : (float)Math.Min(1.0, hours / Structures.WeatherForHours);
    }

    void UpdateWeathering(float dt)
    {
        weatherTimer -= dt;
        if (weatherTimer > 0f) return;
        weatherTimer = 30f;
        foreach (var (id, v) in views)
            v.SetWeathered(Weathered(id));
    }

    // --- G on a finished building -----------------------------------------

    // Finished huts whose doorway you stand at, and baskets within reach.
    Structure? UsableNear()
    {
        var p = me.Position;
        return rows.Values
            .Where(s => s.Complete && Usable(s, p))
            .OrderBy(s => new Vector2(s.X - p.X, s.Z - p.Z).LengthSquared())
            .FirstOrDefault();
    }

    static bool Usable(Structure s, Vector3 p)
    {
        if (s.Kind == StructureKind.Basket)
            return Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, p.X, p.Z, 1.2f);
        if (s.Kind != StructureKind.ClayHut) return false;
        // Within a step of the doorway, inside or out.
        var (lx, lz) = Structures.ToLocal(s.X, s.Z, s.Yaw, p.X, p.Z);
        return new Vector2(lx, lz - 1.7f).Length() < 1.2f;
    }

    void Use()
    {
        if (conn() is not { } c || UsableNear() is not { } s) return;
        if (s.Kind == StructureKind.Basket)
        {
            if (openBasket == s.Id) CloseBasket();
            else OpenBasket(s.Id);
            return;
        }
        if (doors.ContainsKey(s.Id))
        {
            c.Reducers.ToggleDoor(s.Id);
            return;
        }
        uint sticks = Carried(c, Item.Stick), fiber = Carried(c, Item.Fiber);
        if (sticks < Structures.DoorSticks || fiber < Structures.DoorFiber)
            hud.Note($"A door takes {Structures.DoorSticks} sticks and {Structures.DoorFiber} fiber");
        else
            c.Reducers.HangDoor(s.Id);
    }

    static uint Carried(DbConnection c, Item item) =>
        (uint)c.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Sum(i => i.Count);

    string? UsePrompt()
    {
        if (UsableNear() is not { } s) return null;
        if (s.Kind == StructureKind.Basket)
        {
            int stacks = conn()?.Db.BasketItem.Iter().Count(b => b.StructureId == s.Id) ?? 0;
            return openBasket == s.Id ? null : $"Storage basket ({stacks} of {Structures.BasketStacks})    G open";
        }
        if (doors.TryGetValue(s.Id, out var shut)) return shut ? "G open the door" : "G shut the door";
        return $"G hang a door ({Structures.DoorSticks} sticks, {Structures.DoorFiber} fiber)";
    }

    // --- Basket panel -----------------------------------------------------

    void OpenBasket(ulong id)
    {
        if (basketLayer == null) MakeBasketPanel();
        openBasket = id;
        basketLayer!.Visible = true;
        RefreshBasket();
    }

    void CloseBasket()
    {
        openBasket = null;
        if (basketLayer != null) basketLayer.Visible = false;
    }

    void MakeBasketPanel()
    {
        basketLayer = new CanvasLayer { Layer = 2, Visible = false };
        AddChild(basketLayer);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        basketTitle = Hud.Text("Storage basket", 18);
        box.AddChild(basketTitle);
        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 28);
        basketInside = new VBoxContainer();
        basketCarried = new VBoxContainer();
        columns.AddChild(Column("In the basket", basketInside));
        columns.AddChild(Column("You carry", basketCarried));
        box.AddChild(columns);
        var hint = Hud.Text("Click moves one, Shift+click moves all    G or Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);
        var panel = Hud.Panel(box, Vector2.Zero);
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        basketLayer.AddChild(panel);
    }

    static Control Column(string title, VBoxContainer list)
    {
        var col = new VBoxContainer { CustomMinimumSize = new Vector2(230, 0) };
        var label = Hud.Text(title, 14);
        label.Modulate = new Color(1, 1, 1, 0.75f);
        col.AddChild(label);
        col.AddChild(list);
        return col;
    }

    void RefreshBasket()
    {
        if (openBasket is not { } id || basketLayer == null || conn() is not { } c) return;
        Fill(basketInside, c.Db.BasketItem.Iter().Where(b => b.StructureId == id)
            .Select(b => (ItemLook.Label((Item)b.Item, b.Quality, b.Count), (Action<bool>)(all => c.Reducers.BasketTake(b.Id, all ? b.Count : 1)))));
        Fill(basketCarried, c.Db.MyInventory.Iter().Where(i => i.Count > 0)
            .Select(i => (ItemLook.Label((Item)i.Item, i.Quality, i.Count), (Action<bool>)(all => c.Reducers.BasketPut(id, i.Item, i.Quality, all ? i.Count : 1)))));
        int stacks = c.Db.BasketItem.Iter().Count(b => b.StructureId == id);
        basketTitle.Text = $"Storage basket    {stacks} of {Structures.BasketStacks}";
    }

    static void Fill(VBoxContainer list, IEnumerable<(string label, Action<bool> move)> entries)
    {
        foreach (var child in list.GetChildren()) child.QueueFree();
        foreach (var child in list.GetChildren()) list.RemoveChild(child);
        bool any = false;
        foreach (var (label, move) in entries)
        {
            any = true;
            var button = new Button { Text = label, Alignment = HorizontalAlignment.Left, Flat = true, FocusMode = Control.FocusModeEnum.None };
            button.AddThemeFontSizeOverride("font_size", 15);
            button.Pressed += () => move(Input.IsKeyPressed(Key.Shift));
            list.AddChild(button);
        }
        if (!any)
        {
            var empty = Hud.Text("Nothing", 14);
            empty.Modulate = new Color(1, 1, 1, 0.5f);
            list.AddChild(empty);
        }
    }

    // Closes the basket when you walk away or press Esc; G toggles it and doors.
    void UpdateUse()
    {
        if (openBasket is { } id && (!rows.TryGetValue(id, out var s) || !Usable(s, me.Position)))
            CloseBasket();
    }

    bool UseInput(InputEvent e)
    {
        if (openBasket != null && e.IsActionPressed("build_cancel"))
        {
            CloseBasket();
            return true;
        }
        if (!e.IsActionPressed("build_work") || e.IsEcho() || NearestSite() != null) return false;
        if (UsableNear() == null) return false;
        Use();
        return true;
    }
}
