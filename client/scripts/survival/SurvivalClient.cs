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
    readonly StaminaBar staminaBar = new();
    readonly RestSpotView restSpots = new();
    readonly DeathMarker deathMarker = new();
    // Whether the death card says which way your things lie (Justin's call, via the PM).
    const bool DeathCardDirection = true;
    readonly Dictionary<ulong, BerryBush> bushes = new();

    Node world = null!;
    Terrain terrain = null!;
    Node3D player = null!;
    WetLook wetLook = null!;
    // Finds another player's avatar by player id; Main sets it.
    public Func<uint, Node3D?> AvatarOf { get; set; } = _ => null;
    Hud hud = null!;
    DbConnection? conn;
    Vitals? vitals;
    uint deathsSeen = uint.MaxValue;
    bool warnedCold, warnedFood, warnedWater, wasNearHeat, wasBurdened;
    float ripenTimer;

    // How fast you walk as a fraction of normal, slower when you carry too much. Main reads this.
    public float WalkFactor { get; private set; } = 1f;
    // Running (D-088): hold the run key to go faster while stamina lasts.
    static readonly StringName NRun = "run";
    float stamina = 1f;
    bool winded;
    // Seconds of steady walking, to offer the run hint once on a long walk.
    float walkedFor;
    // Seconds since you last ran; refill waits a moment (D-111).
    float rested = 999f;
    // Whether Main's Walk() asked for a speed this frame; when it didn't (a panel has
    // the input), _Process keeps the bar refilling so it isn't frozen behind panels.
    bool walkedThisFrame;
    // Seconds run since the last Endurance report.
    float ranFor;
    // Deaths when the new-arrival half hour was last seen running, null once it ends.
    uint? arrivalDeaths;
    public bool Mourning => needsHud.Mourning;

    // The subscription queries survival needs, for the chunk range Main subscribes to.
    public static IEnumerable<string> Queries(string range) => new[]
    {
        "SELECT * FROM my_vitals",
        "SELECT * FROM my_respawn_point",
        "SELECT * FROM drenched",
        $"SELECT * FROM forage_bush WHERE {range}",
    };

    public void Setup(Node world, Terrain terrain, Node3D player, Hud hud)
    {
        this.world = world;
        this.terrain = terrain;
        this.player = player;
        this.hud = hud;
        wetLook = WetLook.On(player);
        restSpots.Setup(world, terrain, player, hud);
        world.AddChild(deathMarker);
        // --death-preview=SECONDS shows the death card and marker that long after start,
        // as if you had just died 13 m to the northwest. For screenshots only.
        var preview = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).FirstOrDefault(a => a.StartsWith("--death-preview="));
        if (preview != null && float.TryParse(preview["--death-preview=".Length..], out var at))
        {
            var timer = new Timer { WaitTime = at, OneShot = true, Autostart = true };
            AddChild(timer);
            timer.Timeout += () =>
            {
                var p = player.Position;
                var fell = new Vector3(p.X - 8f, terrain.HeightAt(p.X - 8f, p.Z - 10f), p.Z - 10f);
                needsHud.ShowDeath("cold", RestSpots.WokeAtEdge, DeathMarker.Direction(new Vector2(p.X, p.Z), new Vector2(fell.X, fell.Z)));
                deathMarker.Show(fell, player);
            };
        }
        if (BerryCloseup.Wanted) world.AddChild(new BerryCloseup(terrain, player));
    }

    public override void _Ready()
    {
        Bind("eat", Key.X);
        AddChild(needsHud);
        AddChild(staminaBar);
        AddChild(restSpots);
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
                ? new UseOffer(Needs.IsHazelBush(bush.Id) ? "Gather hazelnuts" : "Pick berries", new Vector2(bush.X - at.X, bush.Z - at.Z).Length(), () => conn.Reducers.Forage(bush.Id),
                    Target: new Vector3(bush.X, 0, bush.Z), Again: Use.HoldInterval)
                : null,
        });
        Use.Register(new UseHandler
        {
            Id = "survival.drink",
            Priority = 10,
            Find = at => conn != null && vitals != null && Current().water < 0.97f && WaterNearby()
                ? new UseOffer("Drink", World.Reach, () => conn.Reducers.Drink(), Again: Use.HoldInterval)
                : null,
        });
        // A pot you carry is the last resort: it only shows when you're thirsty and
        // nothing in reach offers anything else.
        Use.Register(new UseHandler
        {
            Id = "survival.drink_pot",
            Priority = 1,
            Find = at => conn != null && vitals != null && Current().water < 0.9f && !WaterNearby() && PotOfWater() is { } pot
                ? new UseOffer("Drink from what you carry", float.MaxValue, () => { conn.Reducers.DrinkFrom((ushort)pot); Sfx.Play(Cue.Drink); })
                : null,
        });
    }

    public void Attach(DbConnection connection)
    {
        conn = connection;
        restSpots.Attach(connection);
        conn.Db.MyVitals.OnInsert += (_, v) => OnVitals(v);
        conn.Db.MyVitals.OnUpdate += (_, _, v) => OnVitals(v);
        conn.Db.Drenched.OnDelete += (_, d) => { if (AvatarOf(d.PlayerId) is { } a && a.GetNodeOrNull<WetLook>(nameof(WetLook)) is { } w) w.Wetness = 0f; };
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
            // Point back to where your things fell: a line on the card and a faint light over the spot.
            var fell = new Vector2(v.DiedX, v.DiedZ);
            needsHud.ShowDeath(v.LastDeath, v.WakeKind, DeathCardDirection ? DeathMarker.Direction(new Vector2(v.WokeX, v.WokeZ), fell) : null);
            deathMarker.Show(new Vector3(v.DiedX, terrain.HeightAt(v.DiedX, v.DiedZ), v.DiedZ), player);
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
            wetLook.Wetness = Needs.At(v.Wetness, v.WetnessRate, SecondsSince(v.AsOf));
            UpdateArrival(v);
            Warn(ref warnedCold, warmth, v.Wet ? "The water is freezing. Get out and find a fire" : "You are getting dangerously cold");
            Warn(ref warnedFood, food, "Your stomach aches with hunger");
            Warn(ref warnedWater, water, "Your mouth is dry. Find water");
        }

        // Bushes ripen on their own time, so check now and then.
        ripenTimer -= (float)delta;
        if (ripenTimer <= 0 && conn != null)
        {
            ripenTimer = 1f;
            UpdateSeason();
            foreach (var b in conn.Db.ForageBush.Iter())
                if (bushes.TryGetValue(b.Id, out var node)) node.Ripe = IsRipe(b);
        }

        if (!walkedThisFrame) Rest((float)delta);
        walkedThisFrame = false;
        UpdateLoad();
        UpdateOthersWet();
    }

    // How fast to move this frame: sneaking, walking, or running while stamina lasts,
    // all slowed alike by an overload. Main's Walk() calls this once per frame.
    public float MoveSpeed(bool sneak, bool moving, float walkSpeed, float dt)
    {
        walkedThisFrame = true;
        bool running = moving && !sneak && Input.IsActionPressed(NRun) && Stamina.CanRun(stamina, winded);
        rested = running ? 0f : rested + dt;
        if (running && (ranFor += dt) >= Stamina.RunPerUse)
        {
            ranFor -= Stamina.RunPerUse;
            conn?.Reducers.Ran();
        }
        bool wellFed = vitals is { } v && v.WellFedUntil > NowMicros;
        // "Hold Shift to run" shows once, the first time you walk on for a while.
        walkedFor = moving && !sneak ? walkedFor + dt : 0f;
        if (walkedFor > 8f && walkedFor - dt <= 8f) Hints.Request("run");
        stamina = Stamina.Step(stamina, running, rested, StaminaRank(), wellFed, dt);
        if (stamina <= 0f) winded = true;
        else if (stamina >= Stamina.MinToRun) winded = false;
        staminaBar.Set(stamina, FeetOnScreen());
        float speed = sneak ? World.SneakSpeed : running ? Stamina.RunSpeed : walkSpeed;
        return speed * WalkFactor;
    }

    // Spends a fraction of the bar on a one-off effort such as a jump. False, and
    // nothing spent, while winded or if the bar holds less than the cost.
    public bool TrySpendStamina(float fraction)
    {
        if (winded || stamina < fraction) return false;
        stamina -= fraction;
        rested = 0f;
        if (stamina <= 0f) winded = true;
        staminaBar.Set(stamina, FeetOnScreen());
        return true;
    }

    // Stamina keeps refilling while you stand still with a panel open.
    void Rest(float dt)
    {
        rested += dt;
        bool wellFed = vitals is { } v && v.WellFedUntil > NowMicros;
        stamina = Stamina.Step(stamina, false, rested, StaminaRank(), wellFed, dt);
        if (stamina >= Stamina.MinToRun) winded = false;
        staminaBar.Set(stamina, FeetOnScreen());
    }

    // Endurance (D-123): stamina grows with its rank, and running trains it.
    int StaminaRank()
    {
        if (conn == null) return 0;
        foreach (var skill in conn.Db.MySkills.Iter())
            if (skill.Spec == Stamina.Skill) return Skills.Rank(skill.Points);
        return 0;
    }

    // Where the player's feet are on screen, or null when the camera can't see them.
    Vector2? FeetOnScreen()
    {
        var camera = player.GetViewport()?.GetCamera3D();
        if (camera == null || camera.IsPositionBehind(player.GlobalPosition)) return null;
        return camera.UnprojectPosition(player.GlobalPosition);
    }

    // New arrival (D-115): show the badge while it lasts, and say once when it ends.
    void UpdateArrival(Vitals v)
    {
        float left = Mathf.Max(0f, v.ArrivalLeft + v.ArrivalRate * SecondsSince(v.AsOf));
        needsHud.SetArrival(left / Needs.ArrivalSeconds);
        if (left > 0f) arrivalDeaths = v.Deaths;
        else if (arrivalDeaths is { } deaths)
        {
            // Dying ends it too, but the death card says enough then.
            if (v.Deaths == deaths) hud.Note("You've found your feet. The cold bites harder now.");
            arrivalDeaths = null;
        }
    }

        // Other players look as wet as the server says (D-036), in tenths.
    void UpdateOthersWet()
    {
        if (conn == null || vitals is not { } me) return;
        foreach (var d in conn.Db.Drenched.Iter())
            if (d.PlayerId != me.PlayerId && AvatarOf(d.PlayerId) is { } avatar)
                WetLook.On(avatar).Wetness = d.Level / 10f;
    }

    // Carrying limit (D-042): too heavy a load slows your walk.
    void UpdateLoad()
    {
        if (conn == null) return;
        float load = 0f;
        bool basket = false;
        // Tools on the doll's tools section add no weight (ui-18).
        foreach (var i in conn.Db.MyInventory.Iter().Where(i => !i.OnBelt))
        {
            load += Carry.Weight((Item)i.Item) * i.Count;
            if (i.Item == (ushort)Carry.Basket && i.Count > 0) basket = true;
        }
        float limit = Carry.Limit(basket);
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

    // Plain loops here: the Use finders run every frame, so no LINQ allocations.
    bool Carrying(Item item)
    {
        if (conn == null) return false;
        foreach (var i in conn.Db.MyInventory.Iter())
            if (i.Item == (ushort)item && i.Count > 0) return true;
        return false;
    }

    // The smallest pot of water you carry, so the big one is saved for later.
    Item? PotOfWater() => Foods.Pots.FirstOrDefault(p => Carrying(p.Full))?.Full;

    ForageBush? NearestRipeBush()
    {
        if (conn == null) return null;
        var p = player.Position;
        ForageBush? best = null;
        float bestDistance = Needs.BushReach - 0.2f;
        foreach (var b in conn.Db.ForageBush.Iter())
        {
            float d = new Vector2(b.X - p.X, b.Z - p.Z).Length();
            if (d <= bestDistance && IsRipe(b))
            {
                best = b;
                bestDistance = d;
            }
        }
        return best;
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

    // A hazel bush (survival-30) is ripe only in autumn, whatever its timer says; refreshed with the bushes.
    Season season = Season.Spring;

    void UpdateSeason()
    {
        if (conn?.Db.SeasonClock.Id.Find(0) is { } clock && conn.Db.WorldClock.Id.Find(0) is { } day)
            season = Seasons.SeasonOf(Seasons.YearFraction(NowMicros, clock.YearStart.MicrosecondsSinceUnixEpoch, (int)day.DayLengthSeconds, clock.DaysPerSeason));
    }

    bool IsRipe(ForageBush b) =>
        NowMicros >= b.RipeAt.MicrosecondsSinceUnixEpoch && (!Needs.IsHazelBush(b.Id) || Needs.HazelnutsRipe(season));

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
