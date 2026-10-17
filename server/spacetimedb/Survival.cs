using LifeGame.Shared;
using SpacetimeDB;

// Hunger, thirst and body warmth. Needs are stored as a snapshot (value, rate,
// time) and read by extrapolating from it, so nothing is written while a
// player simply lives. A slow tick only checks for death and rewrites a row
// when the player's surroundings change how fast they warm or cool.
public static partial class Module
{
    const int SurvivalTickMillis = 2000;
    // Warmth per second from standing right by a fire at full strength: a basic
    // campfire warms you from frozen to full in about a minute (Justin, D-103).
    // Later hearths can be faster.
    const float FireHeat = 1f / 60f;

    [SpacetimeDB.Table(Accessor = "Vitals")]
    public partial struct Vitals
    {
        [PrimaryKey]
        public uint PlayerId;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        // Values at AsOf, each from 1 (full) to 0 (death), and how fast they
        // change per second from then on.
        public float Food;
        public float Water;
        public float Warmth;
        public float FoodRate;
        public float WaterRate;
        public float WarmthRate;
        public Timestamp AsOf;
        public bool NearHeat;
        public bool Wet;
        public uint Deaths;
        // What killed the player last ("hunger", "thirst", "cold"), empty if never.
        public string LastDeath;
        public Timestamp DiedAt;
        // Where the player woke after their last death. The spot is usually outside
        // the area their client is watching, so the client moves itself there.
        public float WokeX;
        public float WokeZ;
        // How soaked the player is (0 dry, 1 soaked) at AsOf, and its rate (survival-5).
        [Default(0f)]
        public float Wetness;
        [Default(0f)]
        public float WetnessRate;
        // Until when (microseconds since the epoch) a good meal lets you run longer (D-111).
        [Default(0L)]
        public long WellFedUntil;
        // A new character's first half hour online (D-115): seconds left at AsOf,
        // and -1 while online, 0 while offline or once it has run out.
        [Default(0f)]
        public float ArrivalLeft;
        [Default(0f)]
        public float ArrivalRate;
        // A new character's first hour online, thirst falls slower (survival-10):
        // seconds left at AsOf, and its rate (-1 while online, else 0).
        [Default(0f)]
        public float ThirstEaseLeft;
        [Default(0f)]
        public float ThirstEaseRate;
    }

    // How wet each player looks, from 0 (dry) to 10 (soaked), so everyone nearby
    // can see it (D-036). Coarse on purpose: it changes only a few times as a
    // player soaks or dries. A dry player has no row.
    [SpacetimeDB.Table(Accessor = "Drenched", Public = true)]
    public partial struct Drenched
    {
        [PrimaryKey]
        public uint PlayerId;
        public byte Level;
    }

    [SpacetimeDB.View(Accessor = "MyVitals", Public = true)]
    public static List<Vitals> MyVitals(ViewContext ctx) =>
        ctx.Db.Vitals.Owner.Filter(ctx.Sender).ToList();

    // Berry bushes. A bush is ripe from RipeAt on, so regrowth needs no timer.
    [SpacetimeDB.Table(Accessor = "ForageBush", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct ForageBush
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public Timestamp RipeAt;
    }

    [SpacetimeDB.Table(Accessor = "SurvivalTimer", Scheduled = nameof(SurvivalTick), ScheduledAt = nameof(ScheduledAt))]
    public partial struct SurvivalTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // --- Hooks called from the connection reducers -------------------------

    static void SurvivalConnected(ReducerContext ctx, Player player)
    {
        EnsureSurvivalWorld(ctx);
        var pos = RequirePos(ctx, player);
        if (ctx.Db.Vitals.PlayerId.Find(player.PlayerId) is { } v)
        {
            // Needs were paused while offline; pick up where they left off.
            ctx.Db.Vitals.PlayerId.Update(WithSurroundings(ctx, Snapshot(ctx, v), pos, online: true));
            return;
        }
        ctx.Db.Vitals.Insert(WithSurroundings(ctx, new Vitals
        {
            PlayerId = player.PlayerId,
            Owner = player.Identity,
            Food = Needs.StartFood,
            Water = Needs.StartWater,
            Warmth = Needs.StartWarmth,
            AsOf = ctx.Timestamp,
            LastDeath = "",
            DiedAt = ctx.Timestamp,
            ArrivalLeft = Needs.ArrivalSeconds,
            ThirstEaseLeft = Needs.ThirstEaseSeconds,
        }, pos, online: true));
    }

    static void SurvivalDisconnected(ReducerContext ctx, Player player)
    {
        if (ctx.Db.Vitals.PlayerId.Find(player.PlayerId) is { } v)
            ctx.Db.Vitals.PlayerId.Update(Snapshot(ctx, v) with { FoodRate = 0, WaterRate = 0, WarmthRate = 0, WetnessRate = 0, ArrivalRate = 0, ThirstEaseRate = 0 });
        ctx.Db.Drenched.PlayerId.Delete(player.PlayerId);
    }

    // --- Reducers ----------------------------------------------------------

    [SpacetimeDB.Reducer]
    public static void Eat(ReducerContext ctx, ushort item)
    {
        var player = RequirePlayer(ctx);
        if (!Items.IsKnown(item))
            throw new Exception("You can't eat that");
        var (food, water) = Needs.Nourishment((Item)item);
        if (food <= 0 && water <= 0)
            throw new Exception("You can't eat that");
        if (CountOf(ctx, (Item)item) < 1)
            throw new Exception("You are not carrying that");
        var v = RequireVitals(ctx, player);
        if (v.Food >= 1f)
            throw new Exception("You are full");

        // Eaten from a pot, the pot goes back in your hands at its quality, as DrinkFrom does (ceramics-7).
        if (Foods.Find((Item)item)?.LeavesBehind is { } empty)
        {
            var stack = AnyStack(ctx, (Item)item)!.Value;
            RemoveFromStack(ctx, stack, 1);
            AddToInventory(ctx, ctx.Sender, empty, 1, stack.Quality, stack.Wear);
        }
        else
            Take(ctx, (Item)item, 1);
        ctx.Db.Vitals.PlayerId.Update(v with
        {
            Food = Math.Min(1f, v.Food + food),
            Water = Math.Min(1f, v.Water + water),
            WellFedUntil = Foods.Find((Item)item)?.GoodMeal == true
                ? ctx.Timestamp.MicrosecondsSinceUnixEpoch + Stamina.WellFedMicros
                : v.WellFedUntil,
        });
        NoteAct(ctx, player.PlayerId, WorkMotion.Eat, (Item)item);
    }

    [SpacetimeDB.Reducer]
    public static void Drink(ReducerContext ctx)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        if (!WaterWithinReach(ctx, pos.X, pos.Z))
            throw new Exception("No water within reach");
        var state = RequireCooldown(ctx, player);
        var v = RequireVitals(ctx, player);
        if (v.Water >= 1f)
            throw new Exception("You are not thirsty");

        ctx.Db.Vitals.PlayerId.Update(v with { Water = Math.Min(1f, v.Water + Needs.SipWater) });
        NoteAct(ctx, player.PlayerId, WorkMotion.Drink);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // Drink a pot of water you carry; the empty pot goes back in your hands.
    [SpacetimeDB.Reducer]
    public static void DrinkFrom(ReducerContext ctx, ushort item)
    {
        var player = RequirePlayer(ctx);
        var (empty, water) = PotOfWater((Item)item);
        if (water <= 0)
            throw new Exception("There is nothing to drink in that");
        var stack = AnyStack(ctx, (Item)item) ?? throw new Exception("You are not carrying that");
        var v = RequireVitals(ctx, player);
        if (v.Water >= 1f)
            throw new Exception("You are not thirsty");

        RemoveFromStack(ctx, stack, 1);
        AddToInventory(ctx, ctx.Sender, empty, 1, stack.Quality, stack.Wear);
        ctx.Db.Vitals.PlayerId.Update(v with { Water = Math.Min(1f, v.Water + water) });
    }

    // When each player last reported a stretch of running (Endurance, D-123).
    [SpacetimeDB.Table(Accessor = "RunReport")]
    public partial struct RunReport
    {
        [PrimaryKey]
        public uint PlayerId;
        public Timestamp At;
        // Where they were at that report (D-182): the next one must come from a different place.
        [Default(0f)]
        public float X;
        [Default(0f)]
        public float Z;
    }

    // The client ran for Stamina.RunPerUse seconds: train Endurance. Reports closer
    // together than a stretch could take are ignored, so a client can't speed it up.
    [SpacetimeDB.Reducer]
    public static void Ran(ReducerContext ctx)
    {
        var player = RequirePlayer(ctx);
        var last = ctx.Db.RunReport.PlayerId.Find(player.PlayerId);
        if (last is { } r && SecondsBetween(r.At, ctx.Timestamp) < Stamina.RunPerUse * 0.9f) return;
        // The server can't see a run, but it can see a player who stands still and reports one
        // (D-182): they must have moved just now and be well away from where they last reported.
        var pos = RequirePos(ctx, player);
        var state = ctx.Db.PlayerState.PlayerId.Find(player.PlayerId);
        if (state is not { } st || SecondsBetween(st.LastMoveAt, ctx.Timestamp) > Stamina.RunReportMoveWindow) return;
        if (last is { } l)
        {
            float dx = pos.X - l.X, dz = pos.Z - l.Z;
            if (dx * dx + dz * dz < Stamina.RunReportMinMeters * Stamina.RunReportMinMeters) return;
        }
        var row = new RunReport { PlayerId = player.PlayerId, At = ctx.Timestamp, X = pos.X, Z = pos.Z };
        if (last is null) ctx.Db.RunReport.Insert(row);
        else ctx.Db.RunReport.PlayerId.Update(row);
        OnProcessPerformed(ctx, player.PlayerId, Stamina.RunProcess, Items.RawQuality, false);
    }

    [SpacetimeDB.Reducer]
    public static void Forage(ReducerContext ctx, ulong bushId)
    {
        var player = RequirePlayer(ctx);
        var bush = ctx.Db.ForageBush.Id.Find(bushId) ?? throw new Exception("That bush is gone");
        var pos = RequirePos(ctx, player);
        float dx = bush.X - pos.X, dz = bush.Z - pos.Z;
        if (dx * dx + dz * dz > Needs.BushReach * Needs.BushReach)
            throw new Exception("Out of reach");
        if (ctx.Timestamp.MicrosecondsSinceUnixEpoch < bush.RipeAt.MicrosecondsSinceUnixEpoch)
            throw new Exception("No ripe berries yet");
        var state = RequireCooldown(ctx, player);

        ctx.Db.ForageBush.Id.Update(bush with { RipeAt = Later(ctx, Needs.BushRegrowMicros) });
        AddToInventory(ctx, ctx.Sender, Item.Berries, 3, Items.RawQuality);
        NoteAct(ctx, player.PlayerId, WorkMotion.Pluck, Item.Berries);
        OnProcessPerformed(ctx, player.PlayerId, Needs.ForageProcess, Items.RawQuality, false);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // Kills anyone whose needs ran out, and rewrites a player's snapshot only when
    // their surroundings changed how fast they warm or cool.
    [SpacetimeDB.Reducer]
    public static void SurvivalTick(ReducerContext ctx, SurvivalTimer timer)
    {
        RequireServer(ctx);
        foreach (var player in ctx.Db.Player.Iter().Where(p => p.Online).ToList())
        {
            if (ctx.Db.Vitals.PlayerId.Find(player.PlayerId) is not { } v) continue;
            if (ctx.Db.PlayerPos.PlayerId.Find(player.PlayerId) is not { } pos) continue;

            var now = Snapshot(ctx, v);
            string? cause = now.Warmth <= 0 ? "cold" : now.Water <= 0 ? "thirst" : now.Food <= 0 ? "hunger" : null;
            if (cause != null)
            {
                Die(ctx, player, now, pos, cause);
                continue;
            }

            NoteSwimming(ctx, player, pos);
            var next = WithSurroundings(ctx, now, pos, online: true);
            // Weather drifts slowly, so small changes in the rate wait for a bigger one.
            if (MathF.Abs(next.WarmthRate - v.WarmthRate) > 0.02f * MathF.Abs(v.WarmthRate) + 1e-7f || next.NearHeat != v.NearHeat || next.Wet != v.Wet
                || MathF.Abs(next.WetnessRate - v.WetnessRate) > 0.05f * MathF.Abs(v.WetnessRate) + 1e-7f
                || next.ArrivalRate != v.ArrivalRate || next.ThirstEaseRate != v.ThirstEaseRate)
                ctx.Db.Vitals.PlayerId.Update(next);
            ShowWetness(ctx, player.PlayerId, now.Wetness);
        }
    }

    static void ShowWetness(ReducerContext ctx, uint playerId, float wetness)
    {
        byte level = (byte)MathF.Round(Math.Clamp(wetness, 0f, 1f) * 10f);
        var row = ctx.Db.Drenched.PlayerId.Find(playerId);
        if (level == 0)
        {
            if (row != null) ctx.Db.Drenched.PlayerId.Delete(playerId);
        }
        else if (row is not { } r)
            ctx.Db.Drenched.Insert(new Drenched { PlayerId = playerId, Level = level });
        else if (r.Level != level)
            ctx.Db.Drenched.PlayerId.Update(r with { Level = level });
    }

    // --- Death -------------------------------------------------------------

    // Everything carried falls where the player died. They wake naked at the edge
    // of the wilderness; what they know stays with them.
    static void Die(ReducerContext ctx, Player player, Vitals v, PlayerPos pos, string cause)
    {
        BeforeDeathDrop(ctx, player);
        foreach (var stack in ctx.Db.InventoryItem.Owner.Filter(player.Identity).ToList())
        {
            for (uint i = 0; i < stack.Count; i++)
            {
                float angle = (float)(ctx.Rng.NextDouble() * Math.PI * 2);
                float r = 0.3f + (float)ctx.Rng.NextDouble() * 1.2f;
                float x = pos.X + MathF.Cos(angle) * r, z = pos.Z + MathF.Sin(angle) * r;
                ctx.Db.WorldItem.Insert(new WorldItem
                {
                    Id = 0,
                    Item = stack.Item,
                    Quality = stack.Quality,
                    Wear = stack.Wear,
                    Maker = stack.Maker,
                    X = x,
                    Z = z,
                    ChunkX = World.ChunkOf(x),
                    ChunkZ = World.ChunkOf(z),
                });
            }
            ctx.Db.InventoryItem.Id.Delete(stack.Id);
        }

        var (sx, sz) = WildernessEdge(ctx);
        RespawnPointHook(ctx, player.PlayerId, ref sx, ref sz);
        var spawn = new PlayerPos
        {
            PlayerId = player.PlayerId,
            X = sx,
            Z = sz,
            Heading = pos.Heading,
            ChunkX = (short)World.ChunkOf(sx),
            ChunkZ = (short)World.ChunkOf(sz),
        };
        ctx.Db.PlayerPos.PlayerId.Update(spawn);
        // Drop any step still in flight, and restart the walking allowance so a
        // late report from the old spot cannot drag the player back.
        ctx.Db.MoveInput.PlayerId.Delete(player.PlayerId);
        if (ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) is { } state)
            ctx.Db.PlayerState.PlayerId.Update(state with { LastMoveAt = ctx.Timestamp });

        ctx.Db.Vitals.PlayerId.Update(WithSurroundings(ctx, v with
        {
            Food = Needs.RespawnFood,
            Water = Needs.RespawnWater,
            Warmth = Needs.RespawnWarmth,
            Wetness = 0,
            WellFedUntil = 0,
            ArrivalLeft = 0,
            ThirstEaseLeft = 0,
            AsOf = ctx.Timestamp,
            Deaths = v.Deaths + 1,
            LastDeath = cause,
            DiedAt = ctx.Timestamp,
            WokeX = sx,
            WokeZ = sz,
        }, spawn, online: true));
        Log.Info($"{player.Name} died of {cause}");
    }

    // A dry spot on the rim of the seeded area, at a random bearing.
    static (float, float) WildernessEdge(ReducerContext ctx)
    {
        float rim = World.SpawnRadiusChunks * World.ChunkSize * World.CellSize - 6f;
        float start = (float)(ctx.Rng.NextDouble() * Math.PI * 2);
        for (int i = 0; i < 24; i++)
        {
            float angle = start + i * MathF.PI * 2f / 24f;
            float x = MathF.Cos(angle) * rim, z = MathF.Sin(angle) * rim;
            if (IsDry(x, z)) return (x, z);
        }
        return FindDry(0, 0);
    }

    // --- Needs math --------------------------------------------------------

    // --- For other lanes ----------------------------------------------------

    // Kills a player the same way hunger or cold does: everything carried drops and
    // they wake at the wilderness edge. Claims calls this with "slain" for PvP.
    static void KillPlayer(ReducerContext ctx, Player player, string cause)
    {
        if (ctx.Db.Vitals.PlayerId.Find(player.PlayerId) is not { } v) return;
        if (ctx.Db.PlayerPos.PlayerId.Find(player.PlayerId) is not { } pos) return;
        Die(ctx, player, Snapshot(ctx, v), pos, cause);
    }

    // A player's needs as of now (each 0 to 1), or null if they have no vitals yet.
    static (float food, float water, float warmth)? NeedsNow(ReducerContext ctx, uint playerId) =>
        ctx.Db.Vitals.PlayerId.Find(playerId) is { } v && Snapshot(ctx, v) is var now
            ? (now.Food, now.Water, now.Warmth)
            : null;

    static Vitals RequireVitals(ReducerContext ctx, Player player) =>
        Snapshot(ctx, ctx.Db.Vitals.PlayerId.Find(player.PlayerId) ?? throw new Exception("No vitals"));

    // The same needs, re-recorded as of now.
    static Vitals Snapshot(ReducerContext ctx, Vitals v)
    {
        float s = SecondsBetween(v.AsOf, ctx.Timestamp);
        return v with
        {
            Food = Needs.At(v.Food, v.FoodRate, s),
            Water = Needs.At(v.Water, v.WaterRate, s),
            Warmth = Needs.At(v.Warmth, v.WarmthRate, s),
            Wetness = Needs.At(v.Wetness, v.WetnessRate, s),
            ArrivalLeft = Math.Max(0f, v.ArrivalLeft + v.ArrivalRate * s),
            ThirstEaseLeft = Math.Max(0f, v.ThirstEaseLeft + v.ThirstEaseRate * s),
            AsOf = ctx.Timestamp,
        };
    }

    // Sets the rates from where the player stands. The snapshot must be current.
    static Vitals WithSurroundings(ReducerContext ctx, Vitals v, PlayerPos pos, bool online)
    {
        if (!online)
            return v with { FoodRate = 0, WaterRate = 0, WarmthRate = 0, WetnessRate = 0, ArrivalRate = 0, ThirstEaseRate = 0 };
        float heat = HeatAt(ctx, pos.X, pos.Z);
        bool wet = StandsInWater(ctx, pos.X, pos.Z);
        float rainCover = 0, shelter = 0;
        ShelterAtHook(ctx, pos.X, pos.Z, ref rainCover, ref shelter);
        float insulation = 0;
        ClothingWarmthHook(ctx, v.PlayerId, ref insulation);
        float cold = Needs.ColdDrain * (wet ? Needs.WetChill : 1f) * (1f - Math.Clamp(shelter, 0f, 1f)) * (1f - Math.Clamp(insulation, 0f, 1f)) * Chill(ctx, v.Wetness)
            * (v.ArrivalLeft > 0f ? Needs.ArrivalChill : 1f);
        return v with
        {
            WetnessRate = Needs.WetnessRate(Precipitation(ctx), rainCover, wet, heat > 0),
            FoodRate = -Needs.FoodDrain,
            WaterRate = -Needs.WaterDrain * (v.ThirstEaseLeft > 0f ? Needs.ThirstEaseFactor : 1f),
            WarmthRate = heat - cold,
            ArrivalRate = v.ArrivalLeft > 0f ? -1f : 0f,
            ThirstEaseRate = v.ThirstEaseLeft > 0f ? -1f : 0f,
            NearHeat = heat > 0,
            Wet = wet,
        };
    }

    // What wets you from the sky, 0 to 1: rain, plus falling snow at half its
    // rate (Justin, D-134). Shelter's rain cover blocks both alike.
    static float Precipitation(ReducerContext ctx)
    {
        float snow = 0;
        SnowfallAt(ctx, ref snow);
        return Math.Clamp(CurrentWeather(ctx).Rain + Needs.SnowWetting * snow, 0f, 1f);
    }

    // Seasons: Atmosphere implements this with Module.Snowfall (0..1, falling now).
    static partial void SnowfallAt(ReducerContext ctx, ref float snow);

    // Crafted fires: Crafting implements this with Module.WarmthAt, setting warmth
    // to 0..1 for the strongest burning fire near (x, z).
    static partial void FireWarmthAt(ReducerContext ctx, float x, float z, ref float warmth);

    // Warmth per second from burning campfires around a spot.
    static float HeatAt(ReducerContext ctx, float x, float z)
    {
        float warmth = 0;
        FireWarmthAt(ctx, x, z, ref warmth);
        return warmth * FireHeat;
    }

    // How much harder the cold bites: down to freezing it doubles, and being
    // soaked adds half again (rain itself only matters by making you wet). The
    // two together never pass Needs.MaxChill (D-099), so no weather kills fast.
    static float Chill(ReducerContext ctx, float wetness)
    {
        float celsius = AirTemperature(ctx);
        float cold = 1f + Needs.AirCold(celsius);
        float wet = 1f + Needs.RainChill * Math.Clamp(wetness, 0f, 1f);
        return Math.Min(cold * wet, Needs.MaxChill);
    }

    // Animals (Clothing.cs) puts worn clothes back in the inventory so they drop too.
    static partial void BeforeDeathDrop(ReducerContext ctx, Player player);

    // Clothing: Animals implements this (Clothing.cs). Insulation 0 (naked) .. 1
    // scales down the cold drain like shelter does.
    static partial void ClothingWarmthHook(ReducerContext ctx, uint playerId, ref float insulation);

    // Shelter: House building implements this with its ShelterAt. Warmth (0 open
    // sky .. 1 fully sheltered) scales down the cold drain; rain cover is for
    // when weather arrives.
    // House building implements this: a player with a sleeping mat laid out wakes on it.
    static partial void RespawnPointHook(ReducerContext ctx, uint playerId, ref float x, ref float z);

    static partial void ShelterAtHook(ReducerContext ctx, float x, float z, ref float rainCover, ref float warmth);

    // What a pot of water gives back when drunk, and how much it quenches.
    static (Item empty, float water) PotOfWater(Item item) =>
        Foods.FindPot(item) is { } pot ? (pot.Empty, pot.Water) : (item, 0f);

    // Any stack of an item the sender carries, whatever its quality.
    static InventoryItem? AnyStack(ReducerContext ctx, Item item)
    {
        foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((ctx.Sender, (ushort)item)))
            return row;
        return null;
    }

    // --- Water and ground --------------------------------------------------

    static float GroundHeight(ReducerContext ctx, int cellX, int cellZ)
    {
        short depth = ctx.Db.TerrainEdit.CellKey.Find(World.CellKey(cellX, cellZ))?.Depth ?? 0;
        return World.Height(cellX, cellZ, depth);
    }

    static bool StandsInWater(ReducerContext ctx, float x, float z) =>
        GroundHeight(ctx, World.CellOf(x), World.CellOf(z)) < Needs.WaterLevel - 0.05f;

    // Rivers, ponds, and holes dug below the water line all count.
    static bool WaterWithinReach(ReducerContext ctx, float x, float z)
    {
        int cx = World.CellOf(x), cz = World.CellOf(z);
        int r = (int)MathF.Ceiling(World.Reach);
        for (int ox = -r; ox <= r; ox++)
            for (int oz = -r; oz <= r; oz++)
            {
                float dx = cx + ox - x, dz = cz + oz - z;
                if (dx * dx + dz * dz <= World.Reach * World.Reach && GroundHeight(ctx, cx + ox, cz + oz) < Needs.WaterLevel)
                    return true;
            }
        return false;
    }

    static bool IsDry(float x, float z) =>
        World.BaseHeight(World.CellOf(x), World.CellOf(z)) > Needs.WaterLevel + 0.3f;

    // The nearest dry spot to (x, z), searching outward in rings.
    static (float, float) FindDry(float x, float z)
    {
        for (int ring = 0; ring < 20; ring++)
            for (int i = 0; i < 8 + ring * 4; i++)
            {
                float angle = i * MathF.PI * 2f / (8 + ring * 4);
                float px = x + MathF.Cos(angle) * ring * 1.5f, pz = z + MathF.Sin(angle) * ring * 1.5f;
                if (IsDry(px, pz)) return (px, pz);
            }
        return (x, z);
    }

    // --- World setup -------------------------------------------------------

    // Starts the tick and seeds berry bushes the first time anyone connects,
    // so a database published before survival existed picks them up too.
    static void EnsureSurvivalWorld(ReducerContext ctx)
    {
        if (ctx.Db.SurvivalTimer.Count == 0)
            ctx.Db.SurvivalTimer.Insert(new SurvivalTimer
            {
                ScheduledId = 0,
                ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(SurvivalTickMillis)),
            });

        if (ctx.Db.ForageBush.Count == 0)
        {
            int r = World.SpawnRadiusChunks;
            float span = World.ChunkSize * World.CellSize;
            for (int cx = -r; cx < r; cx++)
                for (int cz = -r; cz < r; cz++)
                    for (int i = 0; i < Needs.BushesPerChunk; i++)
                    {
                        float x = (cx + (float)ctx.Rng.NextDouble()) * span;
                        float z = (cz + (float)ctx.Rng.NextDouble()) * span;
                        if (!IsDry(x, z)) continue;
                        ctx.Db.ForageBush.Insert(new ForageBush
                        {
                            Id = 0,
                            X = x,
                            Z = z,
                            ChunkX = World.ChunkOf(x),
                            ChunkZ = World.ChunkOf(z),
                            RipeAt = ctx.Timestamp,
                        });
                    }
        }
    }

    static Timestamp Later(ReducerContext ctx, long micros) =>
        ctx.Timestamp + new TimeDuration(micros);

    // Carrying limit (D-042): an overloaded player walks slower. Move scales its
    // speed check by this, matching the client's slowdown.
    static float CarryWalkFactor(ReducerContext ctx, Identity owner)
    {
        var (load, limit) = Carry.Of(ctx.Db.InventoryItem.Owner.Filter(owner).Where(i => !i.OnBelt).Select(i => ((Item)i.Item, i.Count)));
        return Carry.WalkFactor(load, limit);
    }
}
