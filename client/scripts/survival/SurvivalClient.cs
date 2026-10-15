using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// The client side of survival: reads your needs from the server's snapshot,
// draws fires and berry bushes, and handles foraging, eating and drinking.
// G picks berries, C eats, R drinks.
public partial class SurvivalClient : Node
{
    readonly NeedsHud needsHud = new();
    readonly Dictionary<ulong, BerryBush> bushes = new();
    readonly Dictionary<ulong, Node3D> fires = new();

    Node world = null!;
    Terrain terrain = null!;
    Node3D player = null!;
    Hud hud = null!;
    DbConnection? conn;
    Vitals? vitals;
    uint deathsSeen = uint.MaxValue;
    bool warnedCold, warnedFood, warnedWater, wasNearHeat;
    float ripenTimer;

    // The subscription queries survival needs, for the chunk range Main subscribes to.
    public static IEnumerable<string> Queries(string range) => new[]
    {
        "SELECT * FROM my_vitals",
        $"SELECT * FROM heat_source WHERE {range}",
        $"SELECT * FROM forage_bush WHERE {range}",
    };

    public void Setup(Node world, Terrain terrain, Node3D player, Hud hud)
    {
        this.world = world;
        this.terrain = terrain;
        this.player = player;
        this.hud = hud;
    }

    public override void _Ready()
    {
        Bind("forage", Key.G);
        Bind("eat", Key.C);
        Bind("drink", Key.R);
        AddChild(needsHud);
    }

    public void Attach(DbConnection connection)
    {
        conn = connection;
        conn.Db.MyVitals.OnInsert += (_, v) => OnVitals(v);
        conn.Db.MyVitals.OnUpdate += (_, _, v) => OnVitals(v);
        conn.Db.HeatSource.OnInsert += (_, h) => AddFire(h);
        conn.Db.HeatSource.OnDelete += (_, h) => RemoveNode(fires, h.Id);
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

        needsHud.SetPrompt(Prompt());
    }

    // --- Actions -----------------------------------------------------------

    public override void _UnhandledInput(InputEvent e)
    {
        if (conn == null) return;
        if (e.IsActionPressed("forage"))
        {
            if (NearestRipeBush() is { } bush) conn.Reducers.Forage(bush.Id);
            else hud.Note("No ripe berries within reach");
        }
        if (e.IsActionPressed("eat"))
        {
            if (Carrying(ItemKind.Berries)) conn.Reducers.Eat(ItemKind.Berries);
            else hud.Note("You have nothing to eat");
        }
        if (e.IsActionPressed("drink"))
        {
            if (WaterNearby()) conn.Reducers.Drink();
            else hud.Note("No water within reach");
        }
    }

    string? Prompt()
    {
        if (conn == null || vitals == null) return null;
        var parts = new List<string>();
        if (NearestRipeBush() != null) parts.Add("G  pick berries");
        var (food, water, _) = Current();
        if (water < 0.97f && WaterNearby()) parts.Add("R  drink");
        if (food < 0.95f && Carrying(ItemKind.Berries)) parts.Add("C  eat berries");
        return parts.Count > 0 ? string.Join("     ", parts) : null;
    }

    bool Carrying(ItemKind kind) => conn?.Db.MyInventory.Iter().Any(i => i.Kind == kind && i.Count > 0) == true;

    ForageBush? NearestRipeBush()
    {
        if (conn == null) return null;
        var p = player.Position;
        return conn.Db.ForageBush.Iter()
            .Where(IsRipe)
            .Select(b => (b, d: new Vector2(b.X - p.X, b.Z - p.Z).Length()))
            .Where(t => t.d <= 2.3f)
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

    void AddFire(HeatSource h)
    {
        if (fires.ContainsKey(h.Id)) return;
        var fire = new Campfire(h.Id) { Position = new Vector3(h.X, terrain.HeightAt(h.X, h.Z), h.Z) };
        fires[h.Id] = fire;
        world.AddChild(fire);
    }

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
