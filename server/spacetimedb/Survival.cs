using LifeGame.Shared;
using SpacetimeDB;

// Hunger, thirst and body warmth. Needs are stored as a snapshot (value, rate,
// time) and read by extrapolating from it, so nothing is written while a
// player simply lives. A slow tick only checks for death and rewrites a row
// when the player's surroundings change how fast they warm or cool.
public static partial class Module
{
    const int SurvivalTickMillis = 2000;
    const int PlaceholderFires = 3;
    const float BushReach = 2.5f;
    // Warmth per second from standing right by a fire at full strength.
    const float FireHeat = 1f / 30f;

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
    }

    [SpacetimeDB.View(Accessor = "MyVitals", Public = true)]
    public static List<Vitals> MyVitals(ViewContext ctx) =>
        ctx.Db.Vitals.Owner.Filter(ctx.Sender).ToList();

    // Anything that warms people standing near it: campfires now, hearths and
    // kilns later. Crafting adds a row when a fire is lit and removes it when
    // the fire dies. Strength is warmth gained per second at the center.
    [SpacetimeDB.Table(Accessor = "HeatSource", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct HeatSource
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public float Radius;
        public float Strength;
        // Placeholder fires stand in until crafted fires exist; they never go out.
        public bool Placeholder;
    }

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
        }, pos, online: true));
    }

    static void SurvivalDisconnected(ReducerContext ctx, Player player)
    {
        if (ctx.Db.Vitals.PlayerId.Find(player.PlayerId) is { } v)
            ctx.Db.Vitals.PlayerId.Update(Snapshot(ctx, v) with { FoodRate = 0, WaterRate = 0, WarmthRate = 0 });
    }

    // --- Reducers ----------------------------------------------------------

    [SpacetimeDB.Reducer]
    public static void Eat(ReducerContext ctx, ItemKind kind)
    {
        var player = RequirePlayer(ctx);
        var (food, water) = Nourishment(kind);
        if (food <= 0 && water <= 0)
            throw new Exception("You can't eat that");
        var stack = FindStack(ctx, ctx.Sender, kind) ?? throw new Exception("You are not carrying that");
        var v = RequireVitals(ctx, player);
        if (v.Food >= 1f)
            throw new Exception("You are full");

        if (stack.Count <= 1)
            ctx.Db.InventoryItem.Id.Delete(stack.Id);
        else
            ctx.Db.InventoryItem.Id.Update(stack with { Count = stack.Count - 1 });
        ctx.Db.Vitals.PlayerId.Update(v with
        {
            Food = Math.Min(1f, v.Food + food),
            Water = Math.Min(1f, v.Water + water),
        });
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
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    [SpacetimeDB.Reducer]
    public static void Forage(ReducerContext ctx, ulong bushId)
    {
        var player = RequirePlayer(ctx);
        var bush = ctx.Db.ForageBush.Id.Find(bushId) ?? throw new Exception("That bush is gone");
        var pos = RequirePos(ctx, player);
        float dx = bush.X - pos.X, dz = bush.Z - pos.Z;
        if (dx * dx + dz * dz > BushReach * BushReach)
            throw new Exception("Out of reach");
        if (ctx.Timestamp.MicrosecondsSinceUnixEpoch < bush.RipeAt.MicrosecondsSinceUnixEpoch)
            throw new Exception("No ripe berries yet");
        var state = RequireCooldown(ctx, player);

        ctx.Db.ForageBush.Id.Update(bush with { RipeAt = Later(ctx, Needs.BushRegrowMicros) });
        AddToInventory(ctx, ctx.Sender, ItemKind.Berries, 3);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // Kills anyone whose needs ran out, and rewrites a player's snapshot only when
    // their surroundings changed how fast they warm or cool.
    [SpacetimeDB.Reducer]
    public static void SurvivalTick(ReducerContext ctx, SurvivalTimer timer)
    {
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

            var next = WithSurroundings(ctx, now, pos, online: true);
            if (MathF.Abs(next.WarmthRate - v.WarmthRate) > 1e-7f || next.NearHeat != v.NearHeat || next.Wet != v.Wet)
                ctx.Db.Vitals.PlayerId.Update(next);
        }
    }

    // --- Death -------------------------------------------------------------

    // Everything carried falls where the player died. They wake naked at the edge
    // of the wilderness; what they know stays with them.
    static void Die(ReducerContext ctx, Player player, Vitals v, PlayerPos pos, string cause)
    {
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
                    Kind = stack.Kind,
                    X = x,
                    Z = z,
                    ChunkX = World.ChunkOf(x),
                    ChunkZ = World.ChunkOf(z),
                });
            }
            ctx.Db.InventoryItem.Id.Delete(stack.Id);
        }

        var (sx, sz) = WildernessEdge(ctx);
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
            AsOf = ctx.Timestamp,
        };
    }

    // Sets the rates from where the player stands. The snapshot must be current.
    static Vitals WithSurroundings(ReducerContext ctx, Vitals v, PlayerPos pos, bool online)
    {
        if (!online)
            return v with { FoodRate = 0, WaterRate = 0, WarmthRate = 0 };
        float heat = HeatAt(ctx, pos.X, pos.Z);
        bool wet = StandsInWater(ctx, pos.X, pos.Z);
        float rainCover = 0, shelter = 0;
        ShelterAtHook(ctx, pos.X, pos.Z, ref rainCover, ref shelter);
        float cold = Needs.ColdDrain * (wet ? Needs.WetChill : 1f) * (1f - Math.Clamp(shelter, 0f, 1f));
        return v with
        {
            FoodRate = -Needs.FoodDrain,
            WaterRate = -Needs.WaterDrain,
            WarmthRate = heat - cold,
            NearHeat = heat > 0,
            Wet = wet,
        };
    }

    // Crafted fires: Crafting implements this with Module.WarmthAt, setting warmth
    // to 0..1 for the strongest burning fire near (x, z).
    static partial void FireWarmthAt(ReducerContext ctx, float x, float z, ref float warmth);

    // Warmth per second from the fires around a spot; strongest at the center.
    static float HeatAt(ReducerContext ctx, float x, float z)
    {
        float crafted = 0;
        FireWarmthAt(ctx, x, z, ref crafted);
        return MathF.Max(crafted * FireHeat, PlaceholderHeatAt(ctx, x, z));
    }

    // Placeholder campfires, kept until crafted fires are on main.
    static float PlaceholderHeatAt(ReducerContext ctx, float x, float z)
    {
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        float best = 0;
        for (int ox = -1; ox <= 1; ox++)
            for (int oz = -1; oz <= 1; oz++)
                foreach (var h in ctx.Db.HeatSource.ByChunk.Filter((cx + ox, cz + oz)))
                {
                    float dx = h.X - x, dz = h.Z - z;
                    float d = MathF.Sqrt(dx * dx + dz * dz);
                    if (d < h.Radius)
                        best = MathF.Max(best, h.Strength * (1f - 0.6f * d / h.Radius));
                }
        return best;
    }

    // Shelter: House building implements this with its ShelterAt. Warmth (0 open
    // sky .. 1 fully sheltered) scales down the cold drain; rain cover is for
    // when weather arrives.
    static partial void ShelterAtHook(ReducerContext ctx, float x, float z, ref float rainCover, ref float warmth);

    static (float food, float water) Nourishment(ItemKind kind) => kind switch
    {
        ItemKind.Berries => (Needs.BerryFood, Needs.BerryWater),
        _ => (0f, 0f),
    };

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

    // Starts the tick and seeds fires and bushes the first time anyone connects,
    // so a database published before survival existed picks them up too.
    static void EnsureSurvivalWorld(ReducerContext ctx)
    {
        if (ctx.Db.SurvivalTimer.Count == 0)
            ctx.Db.SurvivalTimer.Insert(new SurvivalTimer
            {
                ScheduledId = 0,
                ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(SurvivalTickMillis)),
            });

        if (!ctx.Db.HeatSource.Iter().Any(h => h.Placeholder))
        {
            // Smoldering campfires left by earlier wanderers, near where people wake.
            (float, float)[] spots = { (4, 5), (-13, 8), (9, -14) };
            foreach (var (x0, z0) in spots.Take(PlaceholderFires))
            {
                var (x, z) = FindDry(x0, z0);
                ctx.Db.HeatSource.Insert(new HeatSource
                {
                    Id = 0,
                    X = x,
                    Z = z,
                    ChunkX = World.ChunkOf(x),
                    ChunkZ = World.ChunkOf(z),
                    Radius = 4.5f,
                    Strength = FireHeat,
                    Placeholder = true,
                });
            }
        }

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
}
