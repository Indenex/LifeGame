using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// The client side of survival: reads your needs from the server's snapshot,
// draws berry bushes, and handles foraging, eating and drinking.
// G (the shared Use key) picks berries and drinks at water or from a pot you carry; X eats.
public partial class SurvivalClient : Node
{
    readonly NeedsHud needsHud = new();
    readonly Dictionary<ulong, BerryBush> bushes = new();

    Node world = null!;
    Terrain terrain = null!;
    Node3D player = null!;
    Hud hud = null!;
    DbConnection? conn;
    Vitals? vitals;
    uint deathsSeen = uint.MaxValue;
    bool warnedCold, warnedFood, warnedWater, wasNearHeat, wasBurdened;
    float ripenTimer;

    // How fast you walk as a fraction of normal, slower when you carry too much. Main reads this.
    public float WalkFactor { get; private set; } = 1f;
    public bool Mourning => needsHud.Mourning;

    // The subscription queries survival needs, for the chunk range Main subscribes to.
    public static IEnumerable<string> Queries(string range) => new[]
    {
        "SELECT * FROM my_vitals",
        $"SELECT * FROM forage_bush WHERE {range}",
    };

    public void Setup(Node world, Terrain terrain, Node3D player, Hud hud)
    {
        this.world = world;
        this.terrain = terrain;
        this.player = player;
        this.hud = hud;
        if (BerryCloseup.Wanted) world.AddChild(new BerryCloseup(terrain, player));
    }

    public override void _Ready()
    {
        Bind("eat", Key.X);
        AddChild(needsHud);
        RegisterUse();
    }

    // G actions go through the shared Use registry (KEYMAP.md).
    void RegisterUse()
    {
        // Target lets a bush you face beat a fire beside you (ui-4).
        Use.Register(new UseHandler
        {
            Id = "survival.forage",
            Priority = 10,
            Find = at => conn != null && NearestRipeBush() is { } bush
                ? new UseOffer("Pick berries", new Vector2(bush.X - at.X, bush.Z - at.Z).Length(), () => conn.Reducers.Forage(bush.Id),
                    Target: new Vector3(bush.X, 0, bush.Z))
                : null,
        });
        Use.Register(new UseHandler
        {
            Id = "survival.drink",
            Priority = 10,
            Find = at => conn != null && vitals != null && Current().water < 0.97f && WaterNearby()
                ? new UseOffer("Drink", World.Reach, () => conn.Reducers.Drink())
                : null,
        });
        // A pot you carry is the last resort: it only shows when you're thirsty and
        // nothing in reach offers anything else.
        Use.Register(new UseHandler
        {
            Id = "survival.drink_pot",
            Priority = 1,
            Find = at => conn != null && vitals != null && Current().water < 0.9f && !WaterNearby() && PotOfWater() is { } pot
                ? new UseOffer("Drink from your pot", float.MaxValue, () => conn.Reducers.DrinkFrom((ushort)pot))
                : null,
        });
    }

    public void Attach(DbConnection connection)
    {
        conn = connection;
        conn.Db.MyVitals.OnInsert += (_, v) => OnVitals(v);
        conn.Db.MyVitals.OnUpdate += (_, _, v) => OnVitals(v);
        conn.Db.ForageBush.OnInsert += (_, b) => AddBush(b);
        conn.Db.ForageBush.OnUpdate += (_, _, b) => { if (bushes.TryGetValue(b.Id, out var n)) n.Ripe = IsRipe(b); };
        conn.Db.ForageBush.OnDelete += (_, b) => RemoveNode(bushes, b.Id);
    }

    static void Bind(string action, Key key)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    // --- Needs -------------------------------------------------------------

    static long NowMicros => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

    static float SecondsSince(Timestamp t) => (NowMicros - t.MicrosecondsSinceUnixEpoch) / 1_000_000f;

    (float food, float water, float warmth) Current()
    {
        if (vitals is not { } v) return (1f, 1f, 1f);
        float s = SecondsSince(v.AsOf);
        return (Needs.At(v.Food, v.FoodRate, s), Needs.At(v.Water, v.WaterRate, s), Needs.At(v.Warmth, v.WarmthRate, s));
    }

    void OnVitals(Vitals v)
    {
        vitals = v;
        if (deathsSeen != uint.MaxValue && v.Deaths > deathsSeen)
        {
            needsHud.ShowDeath(v.LastDeath);
            // Wake at the edge of the wilderness. Main follows the player from here.
            player.Position = new Vector3(v.WokeX, terrain.HeightAt(v.WokeX, v.WokeZ), v.WokeZ);
            warnedCold = warnedFood = warnedWater = false;
        }
        deathsSeen = v.Deaths;
        if (v.NearHeat && !wasNearHeat) hud.Note("The fire's warmth seeps into you");
        wasNearHeat = v.NearHeat;
    }

    // Says something once when a need runs low, and again only after it recovers.
    void Warn(ref bool warned, float value, string text)
    {
        if (value < Needs.Low && !warned)
        {
            warned = true;
            hud.Note(text);
        }
        else if (value > Needs.Low + 0.1f) warned = false;
    }

    public override void _Process(double delta)
    {
        if (vitals is { } v)
        {
            var (food, water, warmth) = Current();
            needsHud.SetNeeds(food, water, warmth, v.NearHeat);
            Warn(ref warnedCold, warmth, v.Wet ? "The water is freezing. Get out and find a fire" : "You are getting dangerously cold");
            Warn(ref warnedFood, food, "Your stomach aches with hunger");
            Warn(ref warnedWater, water, "Your mouth is dry. Find water");
        }

        // Bushes ripen on their own time, so check now and then.
        ripenTimer -= (float)delta;
        if (ripenTimer <= 0 && conn != null)
        {
            ripenTimer = 1f;
            foreach (var b in conn.Db.ForageBush.Iter())
                if (bushes.TryGetValue(b.Id, out var node)) node.Ripe = IsRipe(b);
        }

        UpdateLoad();
    }

    // Carrying limit (D-042): too heavy a load slows your walk.
    void UpdateLoad()
    {
        if (conn == null) return;
        var (load, limit) = Carry.Of(conn.Db.MyInventory.Iter().Select(i => ((Item)i.Item, i.Count)));
        WalkFactor = Carry.WalkFactor(load, limit);
        needsHud.SetLoad(load, limit);
        bool burdened = load > limit;
        if (burdened && !wasBurdened) hud.Note("You are carrying too much and slow down");
        wasBurdened = burdened;
    }

    // --- Actions -----------------------------------------------------------

    public override void _UnhandledInput(InputEvent e)
    {
        if (conn == null) return;
        if (e.IsActionPressed("eat"))
        {
            var carried = conn.Db.MyInventory.Iter().Where(i => i.Count > 0).Select(i => (Item)i.Item);
            if (Needs.BestFood(Current().food, carried) is { } food) conn.Reducers.Eat((ushort)food);
            else if (Carrying(Item.RawMutton)) hud.Note("Raw mutton needs cooking first");
            else hud.Note("You have nothing to eat");
        }
    }

    bool Carrying(Item item) => conn?.Db.MyInventory.Iter().Any(i => i.Item == (ushort)item && i.Count > 0) == true;

    // The smallest pot of water you carry, so the big one is saved for later.
    Item? PotOfWater() =>
        Carrying(Item.PinchPotOfWater) ? Item.PinchPotOfWater :
        Carrying(Item.CoilPotOfWater) ? Item.CoilPotOfWater : null;

    ForageBush? NearestRipeBush()
    {
        if (conn == null) return null;
        var p = player.Position;
        return conn.Db.ForageBush.Iter()
            .Where(IsRipe)
            .Select(b => (b, d: new Vector2(b.X - p.X, b.Z - p.Z).Length()))
            .Where(t => t.d <= Needs.BushReach - 0.2f)
            .OrderBy(t => t.d)
            .Select(t => t.b)
            .FirstOrDefault();
    }

    // Mirrors the server: any ground below the water line within reach.
    bool WaterNearby()
    {
        var p = player.Position;
        int cx = World.CellOf(p.X), cz = World.CellOf(p.Z);
        int r = (int)MathF.Ceiling(World.Reach);
        float reach = World.Reach - 0.2f;
        for (int ox = -r; ox <= r; ox++)
            for (int oz = -r; oz <= r; oz++)
            {
                float dx = cx + ox - p.X, dz = cz + oz - p.Z;
                if (dx * dx + dz * dz <= reach * reach && terrain.TileHeight(cx + ox, cz + oz) < Needs.WaterLevel)
                    return true;
            }
        return false;
    }

    static bool IsRipe(ForageBush b) => NowMicros >= b.RipeAt.MicrosecondsSinceUnixEpoch;

    // --- World objects -----------------------------------------------------

    void AddBush(ForageBush b)
    {
        if (bushes.ContainsKey(b.Id)) return;
        var bush = new BerryBush(b.Id) { Position = new Vector3(b.X, terrain.HeightAt(b.X, b.Z) - 0.05f, b.Z), Ripe = IsRipe(b) };
        bushes[b.Id] = bush;
        world.AddChild(bush);
    }

    static void RemoveNode<T>(Dictionary<ulong, T> nodes, ulong id) where T : Node
    {
        if (nodes.Remove(id, out var node)) node.QueueFree();
    }
}
