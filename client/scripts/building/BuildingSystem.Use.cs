using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Using finished buildings, all through the shared Use key (G): at a hut's doorway
// G hangs a door (4 sticks, 2 fiber) or opens and shuts it, and Shift+G bars or
// unbars it (D-048); a gate opens, shuts and bars the same way; at a storage
// basket G opens it. Also shows weathering on things left untouched outside a
// claim (D-046).
public partial class BuildingSystem
{
    readonly Dictionary<ulong, StructureDoor> doors = new();
    readonly Dictionary<ulong, DateTimeOffset> touched = new();
    float weatherTimer;

    CanvasLayer? basketLayer;
    // The two sides as round-slot grids (ui/Bag.cs, ui-18): drag between them to move a
    // whole stack; click moves one, Shift+click all.
    BagGrid basketInside = null!, basketCarried = null!;
    Label basketTitle = null!, basketInsideTitle = null!;
    ulong? openBasket;

    // Server queries for what this file draws; Main adds them to its subscription.
    public static IEnumerable<string> Queries() => new[]
    {
        "SELECT * FROM structure_door",
        "SELECT * FROM structure_touch",
        "SELECT * FROM basket_item",
        // A storage jar's size follows its quality (Ceramics, D-093).
        "SELECT * FROM structure_progress",
        "SELECT * FROM sleeping_mat",
    };

    void BindUse(DbConnection c)
    {
        c.Db.StructureDoor.OnInsert += (_, d) => SetDoor(d, animate: true);
        c.Db.StructureDoor.OnUpdate += (_, _, d) => SetDoor(d, animate: true);
        c.Db.StructureDoor.OnDelete += (_, d) =>
        {
            doors.Remove(d.StructureId);
            if (views.TryGetValue(d.StructureId, out var v)) v.SetDoor(null, false, animate: false);
        };
        RegisterUse(c);
        BindMats(c);
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

    void SetDoor(StructureDoor d, bool animate)
    {
        doors[d.StructureId] = d;
        if (views.TryGetValue(d.StructureId, out var v)) v.SetDoor(d.Closed, d.Locked, animate);
    }

    // A view made after its door row arrived picks the door up here.
    void ApplyExtras(Structure s, StructureView v)
    {
        if (doors.TryGetValue(s.Id, out var d)) v.SetDoor(d.Closed, d.Locked, animate: false);
        v.SetWeathered(Weathered(s.Id));
    }

    bool DoorShut(ulong structureId) => doors.TryGetValue(structureId, out var d) && d.Closed;

    // --- Weathering -------------------------------------------------------

    void Touched(StructureTouch t)
    {
        touched[t.StructureId] = DateTimeOffset.FromUnixTimeMilliseconds(t.LastTouched.MicrosecondsSinceUnixEpoch / 1000);
        if (views.TryGetValue(t.StructureId, out var v)) v.SetWeathered(Weathered(t.StructureId));
    }

    float Weathered(ulong structureId)
    {
        if (!touched.TryGetValue(structureId, out var at)) return 0f;
        double hours = (DateTimeOffset.UtcNow - at).TotalHours
            - (rows.TryGetValue(structureId, out var s) ? Structures.WeatherAfterFor((int)s.Kind) : Structures.WeatherAfterHours);
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
        // Just inside the server's reach, so a store opens wherever putting into it works.
        if (Structures.IsStore((int)s.Kind))
            return Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, p.X, p.Z, Structures.WorkReach - 0.2f);
        if (Structures.IsGate((int)s.Kind))
            return Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, p.X, p.Z, 1.0f);
        if (!Structures.HasDoorway((int)s.Kind)) return false;
        // Within a step of the doorway, inside or out.
        var (lx, lz) = Structures.ToLocal(s.X, s.Z, s.Yaw, p.X, p.Z);
        return new Vector2(lx, lz - Structures.Shape((int)s.Kind).DoorwayZ).Length() < 1.2f;
    }

    // G on a finished hut's doorway or a basket. Priority sits just under adding materials (30).
    void RegisterUse(DbConnection c)
    {
        Use.Register(new UseHandler
        {
            Id = "building.use",
            Priority = 28,
            Find = at =>
            {
                if (UsableNear() is not { } s) return null;
                float d = new Vector2(s.X - at.X, s.Z - at.Z).Length();
                if (Structures.IsStore((int)s.Kind))
                    return openBasket == s.Id ? null : new UseOffer($"Open the {Structures.Shape((int)s.Kind).Name.Split(' ')[^1].ToLowerInvariant()}", d, () => OpenBasket(s.Id), Target: new Vector3(s.X, at.Y, s.Z));
                // The doorway, so the hut you face wins over a basket beside you.
                var (dx, dz) = Structures.ToWorld(s.X, s.Z, s.Yaw, 0f, Structures.Shape((int)s.Kind).DoorwayZ);
                var doorway = new Vector3(dx, at.Y, dz);
                bool gate = Structures.IsGate((int)s.Kind);
                if (!doors.TryGetValue(s.Id, out var door))
                    return gate ? null : Carried(c, Item.Stick) >= Structures.DoorSticks && Carried(c, Item.Fiber) >= Structures.DoorFiber
                        ? new UseOffer("Hang a door", d, () => c.Reducers.HangDoor(s.Id), Target: doorway)
                        : null;
                // Whoever hung it, or a member of the claim it stands in, may bar it, as the server's CanLock (bugfix-15).
                bool mine = door.HungBy == c.Identity || MayBar(s.X, s.Z);
                // Shift+G bars and unbars; members of the claim can too, the server decides.
                Action act = () =>
                {
                    if (Input.IsKeyPressed(Key.Shift)) c.Reducers.ToggleLock(s.Id);
                    else c.Reducers.ToggleDoor(s.Id);
                };
                if (door.Locked && !mine) return new UseOffer("Barred", d, act, Target: doorway);
                string what = gate ? "gate" : "door";
                string verb = door.Closed ? (door.Locked ? "Unbar and open" : $"Open the {what}") : $"Shut the {what}";
                if (mine) verb += door.Locked ? "    Shift unbar" : "    Shift bar";
                return new UseOffer(verb, d, act, Target: doorway);
            },
        });
    }

    static uint Carried(DbConnection c, Item item)
    {
        uint n = 0;
        foreach (var i in c.Db.MyInventory.Iter())
            if (i.Item == (ushort)item) n += i.Count;
        return n;
    }

    // The door costs show beside the doorway when you can't hang one yet.
    string? UsePrompt()
    {
        if (UsableNear() is not { } s || !Structures.HasDoorway((int)s.Kind) || doors.ContainsKey(s.Id)) return null;
        return $"A door takes {Structures.DoorSticks} sticks and {Structures.DoorFiber} fiber";
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
        basketInside = new BagGrid
        {
            Kind = "basket", Width = 5, Empty = "Nothing",
            Source = () => conn() is { } c && openBasket is { } id
                ? c.Db.BasketItem.Iter().Where(b => b.StructureId == id).Select(b => new BagStack((Item)b.Item, b.Quality, b.Count, b.Id))
                : Enumerable.Empty<BagStack>(),
            Clicked = (s, all) => conn()?.Reducers.BasketTake(s.Id, all ? s.Count : 1),
            Accepts = d => d["from"].AsString() == "bag",
            Receive = d => { if (openBasket is { } id) conn()?.Reducers.BasketPut(id, (ushort)d["item"].AsInt32(), (byte)d["quality"].AsInt32(), (uint)d["count"].AsInt64()); },
        };
        basketCarried = new BagGrid
        {
            Kind = "bag", Width = 5, Empty = "Nothing",
            Source = () => conn() is { } c
                ? c.Db.MyInventory.Iter().Where(i => i.Count > 0).Select(i => new BagStack((Item)i.Item, i.Quality, i.Count))
                : Enumerable.Empty<BagStack>(),
            Clicked = (s, all) => { if (openBasket is { } id) conn()?.Reducers.BasketPut(id, (ushort)s.Item, s.Quality, all ? s.Count : 1); },
            Accepts = d => d["from"].AsString() == "basket",
            Receive = d => conn()?.Reducers.BasketTake((ulong)d["id"].AsInt64(), (uint)d["count"].AsInt64()),
        };
        columns.AddChild(Column("In the basket", basketInside, out basketInsideTitle));
        columns.AddChild(Column("You carry", basketCarried, out _));
        box.AddChild(columns);
        var hint = Hud.Text($"Drag moves a whole stack, click moves one, Shift+click moves all    {Controls.KeyFor("use") ?? "E"} or Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);
        var panel = Hud.Panel(box, Vector2.Zero);
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        basketLayer.AddChild(panel);
    }

    static Control Column(string title, Control list, out Label label)
    {
        var col = new VBoxContainer { CustomMinimumSize = new Vector2(5 * BagGrid.Slot + 4 * 10, 0) };
        label = Hud.Text(title, 14);
        label.Modulate = new Color(1, 1, 1, 0.75f);
        col.AddChild(label);
        col.AddChild(list);
        return col;
    }

    void RefreshBasket()
    {
        if (openBasket is not { } id || basketLayer == null || conn() is not { } c) return;
        basketInside.Refresh();
        basketCarried.Refresh();
        int stacks = c.Db.BasketItem.Iter().Count(b => b.StructureId == id);
        var shape = rows.TryGetValue(id, out var s) ? Structures.Shape((int)s.Kind) : Structures.Shape((int)StructureKind.Basket);
        byte quality = c.Db.StructureProgress.StructureId.Find(id)?.Quality ?? Items.RawQuality;
        int room = s != null ? Structures.StoreStacksFor((int)s.Kind, quality) : shape.StoreStacks;
        basketTitle.Text = $"{shape.Name}    {stacks} of {room}";
        // "In the basket", "In the jar": whichever store this is.
        basketInsideTitle.Text = $"In the {shape.Name.Split(' ')[^1].ToLowerInvariant()}";
    }

    // Closes the basket when you walk away or press Esc; G toggles it and doors.
    void UpdateUse()
    {
        if (openBasket is { } id && (!rows.TryGetValue(id, out var s) || !Usable(s, me.Position)))
            CloseBasket();
    }

    bool UseInput(InputEvent e)
    {
        if (openBasket == null || !(e.IsActionPressed("build_cancel") || e.IsActionPressed("use"))) return false;
        CloseBasket();
        return true;
    }
}
