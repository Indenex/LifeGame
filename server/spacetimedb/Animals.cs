using LifeGame.Shared;
using SpacetimeDB;

[SpacetimeDB.Type]
public enum AnimalSpecies
{
    Sheep,
}

// What an animal is doing, so clients can pick the matching animation.
[SpacetimeDB.Type]
public enum AnimalState
{
    Graze,
    Idle,
    Walk,
    Run,
}

// Animals think on the server a couple of times a second and only send a row when
// they actually move or change what they are doing; clients smooth the motion.
// Flocks with no player nearby are skipped entirely, so idle parts of the world
// cost nothing.
public static partial class Module
{
    const int AnimalTickMillis = 500;
    const float AnimalDt = AnimalTickMillis / 1000f;

    // A flock wakes up when any player is within this distance of it. A little more
    // than a client's view radius, so animals are already moving when they come into sight.
    const float FlockWakeRange = (World.ViewRadiusChunks + 1) * World.ChunkSize * World.CellSize;

    const float SheepWalkSpeed = 1.0f;
    const float SheepTrotSpeed = 1.7f;
    const float SheepRunSpeed = 4.4f;
    const float SheepNoticeRange = 14f;
    // Anyone moving slower than this, sneakers included, never frightens a sheep.
    const float SheepCalmSpeed = World.SneakSpeed + 0.6f;
    const float SheepHomeRange = 12f;
    // Ground lower than this is water or marsh; sheep stay out of it.
    const float SheepDryLevel = -0.85f;

    // Where each animal is, for everyone in sight of it.
    [SpacetimeDB.Table(Accessor = "Animal", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct Animal
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public AnimalSpecies Species;
        public float X;
        public float Z;
        public float Heading;
        public AnimalState State;
        public int ChunkX;
        public int ChunkZ;
        // How much wool a sheep carries, 0 (just shorn) to 100 (full fleece).
        [Default((byte)100)]
        public byte Fleece;
        // How grown up the animal is: lambs are born at LambGrowth and reach 100.
        [Default((byte)100)]
        public byte Growth;
    }

    // What each animal is thinking. Server only.
    [SpacetimeDB.Table(Accessor = "AnimalMind")]
    public partial struct AnimalMind
    {
        [PrimaryKey]
        public ulong AnimalId;
        [SpacetimeDB.Index.BTree]
        public uint FlockId;
        public bool Leader;
        public float TargetX;
        public float TargetZ;
        // 0 is calm, 1 is panicked. Above RunAt the animal bolts.
        public float Nervous;
        public float FleeX;
        public float FleeZ;
        public long StateUntil;
        // When the animal was last shorn, or 0 if never.
        [Default(0L)]
        public long ShornAt;
        // 100 is unhurt; at 0 the animal dies.
        [Default((byte)100)]
        public byte Health;
        // When the animal was last struck, or 0 if never.
        [Default(0L)]
        public long StruckAt;
        // When a lamb was born, or 0 for animals placed as adults.
        [Default(0L)]
        public long BornAt;
        // A lamb's mother, whom it trails until it is grown; 0 for none.
        [Default(0UL)]
        public ulong MotherId;
    }

    [SpacetimeDB.Table(Accessor = "Flock")]
    public partial struct Flock
    {
        [PrimaryKey]
        [AutoInc]
        public uint FlockId;
        public AnimalSpecies Species;
        public float HomeX;
        public float HomeZ;
        // How many animals the flock grows back to. 0 until first counted.
        [Default((byte)0)]
        public byte Size;
        // When the flock last gained a lamb (or a stray), or 0.
        [Default(0L)]
        public long LastBirthAt;
    }

    // Where each online player was at the last animal tick, so animals can tell
    // someone charging at them from someone standing still.
    [SpacetimeDB.Table(Accessor = "AnimalPlayerSeen")]
    public partial struct AnimalPlayerSeen
    {
        [PrimaryKey]
        public uint PlayerId;
        public float X;
        public float Z;
        public float Vx;
        public float Vz;
        public long At;
        public long StillSince;
    }

    [SpacetimeDB.Table(Accessor = "AnimalTickTimer", Scheduled = nameof(AnimalTick), ScheduledAt = nameof(ScheduledAt))]
    public partial struct AnimalTickTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // Starting flocks: one at the edge of the clearing where players wake up, the
    // rest further out. Each settles in the most open meadow near its spot.
    static readonly (float X, float Z, int Size, float Search)[] SheepFlocks =
    {
        (4f, 3f, 5, 1f),
        (-34f, 22f, 6, 14f),
        (30f, -36f, 4, 14f),
        (-42f, -30f, 5, 14f),
    };

    // Starts the animal clock and places the first flocks. Safe to call on every
    // connect, so worlds created before animals existed get them too.
    static void EnsureAnimals(ReducerContext ctx)
    {
        if (ctx.Db.AnimalTickTimer.Count == 0)
            ctx.Db.AnimalTickTimer.Insert(new AnimalTickTimer
            {
                ScheduledId = 0,
                ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(AnimalTickMillis)),
            });
        if (ctx.Db.Flock.Count > 0) return;
        foreach (var (x, z, size, search) in SheepFlocks)
            SpawnFlock(ctx, AnimalSpecies.Sheep, x, z, size, search);
    }

    static void SpawnFlock(ReducerContext ctx, AnimalSpecies species, float x, float z, int size, float search)
    {
        var (homeX, homeZ) = AnimalOpenestNear(x, z, search);
        var flock = ctx.Db.Flock.Insert(new Flock { FlockId = 0, Species = species, HomeX = homeX, HomeZ = homeZ, Size = (byte)size });
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        for (int i = 0; i < size; i++)
        {
            float angle = (float)(ctx.Rng.NextDouble() * MathF.Tau);
            float r = i == 0 ? 0f : 1.2f + (float)ctx.Rng.NextDouble() * 2.5f;
            SpawnAnimal(ctx, flock, homeX + MathF.Sin(angle) * r, homeZ + MathF.Cos(angle) * r, i == 0, 100, now);
        }
    }

    static void SpawnAnimal(ReducerContext ctx, Flock flock, float x, float z, bool leader, byte growth, long now, ulong mother = 0)
    {
        var (ax, az) = AnimalNearestDry(x, z);
        var animal = ctx.Db.Animal.Insert(new Animal
        {
            Id = 0,
            Species = flock.Species,
            X = ax,
            Z = az,
            Heading = (float)(ctx.Rng.NextDouble() * MathF.Tau),
            State = AnimalState.Graze,
            ChunkX = World.ChunkOf(ax),
            ChunkZ = World.ChunkOf(az),
            Fleece = 100,
            Growth = growth,
        });
        ctx.Db.AnimalMind.Insert(new AnimalMind
        {
            AnimalId = animal.Id,
            FlockId = flock.FlockId,
            Leader = leader,
            TargetX = ax,
            TargetZ = az,
            StateUntil = now + (long)(ctx.Rng.NextDouble() * 6_000_000),
            Health = 100,
            BornAt = growth < 100 ? now : 0,
            MotherId = mother,
        });
    }

    struct AnimalWatcher
    {
        public float X, Z, Vx, Vz, Speed, StillSeconds;
    }

    struct FlockMember
    {
        public Animal Body;
        public AnimalMind Mind;
    }

    [SpacetimeDB.Reducer]
    public static void AnimalTick(ReducerContext ctx, AnimalTickTimer timer)
    {
        RequireServer(ctx);
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        var watchers = AnimalWatchPlayers(ctx, now);
        if (watchers.Count == 0) return;
        ClearOldCarcasses(ctx, now);
        DryHides(ctx, now);

        foreach (var flock in ctx.Db.Flock.Iter().ToList())
        {
            var members = ctx.Db.AnimalMind.FlockId.Filter(flock.FlockId)
                .Select(m => ctx.Db.Animal.Id.Find(m.AnimalId) is { } body ? new FlockMember { Body = body, Mind = m } : (FlockMember?)null)
                .Where(m => m != null)
                .Select(m => m!.Value)
                .OrderBy(m => m.Body.Id)
                .ToList();
            if (FlockRegrow(ctx, flock, members, now)) continue;
            if (members.Count == 0) continue;

            float cx = members.Average(m => m.Body.X), cz = members.Average(m => m.Body.Z);
            bool awake = watchers.Any(w => AnimalDist(w.X, w.Z, cx, cz) < FlockWakeRange);
            if (awake) AnimalThinkFlock(ctx, flock, members, watchers, now);
        }
    }

    // Reads where online players are now, works out how fast they are moving, and
    // remembers this tick's positions for the next one.
    static List<AnimalWatcher> AnimalWatchPlayers(ReducerContext ctx, long now)
    {
        var result = new List<AnimalWatcher>();
        foreach (var player in ctx.Db.Player.Iter())
        {
            if (!player.Online)
            {
                ctx.Db.AnimalPlayerSeen.PlayerId.Delete(player.PlayerId);
                continue;
            }
            if (ctx.Db.PlayerPos.PlayerId.Find(player.PlayerId) is not { } pos) continue;

            var w = new AnimalWatcher { X = pos.X, Z = pos.Z };
            if (ctx.Db.AnimalPlayerSeen.PlayerId.Find(player.PlayerId) is { } seen)
            {
                // Positions arrive in 100 ms batches, so a single sample is jittery;
                // blend it with the last estimate.
                float dt = Math.Max((now - seen.At) / 1_000_000f, 0.05f);
                w.Vx = ((pos.X - seen.X) / dt + seen.Vx) * 0.5f;
                w.Vz = ((pos.Z - seen.Z) / dt + seen.Vz) * 0.5f;
                w.Speed = MathF.Sqrt(w.Vx * w.Vx + w.Vz * w.Vz);
                long stillSince = w.Speed < 0.3f ? seen.StillSince : now;
                w.StillSeconds = (now - stillSince) / 1_000_000f;
                ctx.Db.AnimalPlayerSeen.PlayerId.Update(seen with { X = pos.X, Z = pos.Z, Vx = w.Vx, Vz = w.Vz, At = now, StillSince = stillSince });
            }
            else
            {
                ctx.Db.AnimalPlayerSeen.Insert(new AnimalPlayerSeen { PlayerId = player.PlayerId, X = pos.X, Z = pos.Z, At = now, StillSince = now });
            }
            result.Add(w);
        }
        return result;
    }

    static void AnimalThinkFlock(ReducerContext ctx, Flock flock, List<FlockMember> members, List<AnimalWatcher> watchers, long now)
    {
        // Everyone reacts to where the flock was at the start of this tick, so the
        // order sheep are processed in does not matter.
        var before = members.ToArray();
        var leader = before.FirstOrDefault(m => m.Mind.Leader);
        if (!leader.Mind.Leader) leader = before.FirstOrDefault(m => m.Body.Growth >= 100);
        if (!leader.Mind.Leader && leader.Body.Growth < 100) leader = before[0];

        for (int i = 0; i < before.Length; i++)
        {
            var body = before[i].Body;
            var mind = before[i].Mind;
            bool isLeader = body.Id == leader.Body.Id;
            var was = body;

            AnimalSense(ref mind, body, before, watchers, out bool calmedByStillPlayer);
            AnimalShyFromFires(ctx, ref mind, body);
            var mother = AnimalFindMother(ref mind, body, before);
            if (mother is { } ewe)
                LambDecide(ctx, ref body, ref mind, ewe, now);
            else
                AnimalDecide(ctx, ref body, ref mind, flock, leader.Body, isLeader, watchers, calmedByStillPlayer, now);
            AnimalMove(ctx, ref body, ref mind, before, i);
            RegrowFleece(ref body, mind, now);
            AnimalGrowAndHeal(ref body, ref mind, now);

            if (body.X != was.X || body.Z != was.Z || body.State != was.State || body.Fleece != was.Fleece || body.Growth != was.Growth
                || MathF.Abs(body.Heading - was.Heading) > 0.01f)
            {
                body.ChunkX = World.ChunkOf(body.X);
                body.ChunkZ = World.ChunkOf(body.Z);
                ctx.Db.Animal.Id.Update(body);
            }
            if (!mind.Equals(before[i].Mind))
                ctx.Db.AnimalMind.AnimalId.Update(mind);
        }
    }

    // Players closing in fast scare a sheep; a player standing still calms it.
    // Panic spreads to flock-mates that see a neighbor bolt.
    static void AnimalSense(ref AnimalMind mind, Animal body, FlockMember[] flock, List<AnimalWatcher> watchers, out bool calmedByStillPlayer)
    {
        calmedByStillPlayer = false;
        float fear = 0f, awayX = 0f, awayZ = 0f;
        foreach (var w in watchers)
        {
            float dx = body.X - w.X, dz = body.Z - w.Z;
            float d = MathF.Sqrt(dx * dx + dz * dz);
            if (d > SheepNoticeRange) continue;
            d = MathF.Max(d, 0.1f);
            float closing = (w.Vx * dx + w.Vz * dz) / d;
            float near = 1f - Math.Clamp((d - 3f) / (SheepNoticeRange - 3f), 0f, 1f);
            float threat = Math.Clamp((closing - SheepCalmSpeed) / 2f, 0f, 1f) * near;
            // Brushing past at full walk startles; a sneaking player can move among them.
            if (d < 2f && w.Speed > SheepCalmSpeed) threat = MathF.Max(threat, 0.6f);
            if (threat > 0f)
            {
                fear += threat;
                awayX += dx / d * threat;
                awayZ += dz / d * threat;
            }
            if (w.Speed < 0.3f && d < 8f) calmedByStillPlayer = true;
        }

        foreach (var mate in flock)
        {
            if (mate.Body.Id == body.Id || mate.Body.State != AnimalState.Run) continue;
            if (AnimalDist(mate.Body.X, mate.Body.Z, body.X, body.Z) > 8f) continue;
            mind.Nervous = MathF.Max(mind.Nervous, mate.Mind.Nervous * 0.85f);
            awayX += mate.Mind.FleeX * 0.5f;
            awayZ += mate.Mind.FleeZ * 0.5f;
        }

        // Fear builds faster than it fades, but a slow, careful approach never adds up to panic.
        float calm = (calmedByStillPlayer ? 0.45f : 0.3f) * AnimalDt;
        mind.Nervous = Math.Clamp(mind.Nervous + fear * 0.7f - calm, 0f, 1f);

        float len = MathF.Sqrt(awayX * awayX + awayZ * awayZ);
        if (len > 0.01f)
        {
            // Turn gradually rather than zig-zagging when several things scare it.
            float fx = mind.FleeX * 0.4f + awayX / len * 0.6f, fz = mind.FleeZ * 0.4f + awayZ / len * 0.6f;
            float fl = MathF.Sqrt(fx * fx + fz * fz);
            if (fl > 0.01f)
            {
                mind.FleeX = fx / fl;
                mind.FleeZ = fz / fl;
            }
        }
    }

    static void AnimalDecide(ReducerContext ctx, ref Animal body, ref AnimalMind mind, Flock flock, Animal leader, bool isLeader,
        List<AnimalWatcher> watchers, bool calmedByStillPlayer, long now)
    {
        const float RunAt = 0.5f, CalmAt = 0.2f;

        if (mind.Nervous > RunAt || (body.State == AnimalState.Run && mind.Nervous > CalmAt))
        {
            body.State = AnimalState.Run;
            mind.TargetX = body.X + mind.FleeX * 6f;
            mind.TargetZ = body.Z + mind.FleeZ * 6f;
            return;
        }
        if (body.State == AnimalState.Run)
        {
            // Stop and stare back for a moment before going back to grazing.
            body.State = AnimalState.Idle;
            mind.StateUntil = now + AnimalMicros(2.5f, 4.5f, ctx);
            return;
        }

        float fromLeader = AnimalDist(body.X, body.Z, leader.X, leader.Z);
        bool straggling = !isLeader && fromLeader > 6.5f;

        if (body.State == AnimalState.Walk)
        {
            if (AnimalDist(body.X, body.Z, mind.TargetX, mind.TargetZ) > 0.4f && !(straggling && now >= mind.StateUntil)) return;
            body.State = ctx.Rng.NextDouble() < 0.75 ? AnimalState.Graze : AnimalState.Idle;
            mind.StateUntil = now + AnimalMicros(4f, 11f, ctx);
            return;
        }

        if (now < mind.StateUntil && !straggling) return;

        // A calm sheep may wander over to someone who has been standing still for a while.
        if (mind.Nervous < 0.05f && calmedByStillPlayer && ctx.Rng.NextDouble() < 0.12)
        {
            foreach (var w in watchers)
            {
                float d = AnimalDist(w.X, w.Z, body.X, body.Z);
                if (w.StillSeconds < 4f || d > 12f || d < 2.5f) continue;
                var (tx, tz) = AnimalNearestDry(w.X + (body.X - w.X) / d * 1.6f, w.Z + (body.Z - w.Z) / d * 1.6f);
                AnimalStartWalk(ref body, ref mind, tx, tz, ctx, now);
                return;
            }
        }

        if (isLeader)
        {
            if (ctx.Rng.NextDouble() < 0.55)
            {
                // Wander somewhere new, preferring open grass to the woods, and
                // drifting back toward home when far from it.
                float tx = body.X, tz = body.Z, best = float.MaxValue;
                for (int k = 0; k < 3; k++)
                {
                    float angle = (float)(ctx.Rng.NextDouble() * MathF.Tau);
                    float r = 3f + (float)ctx.Rng.NextDouble() * 6f;
                    float cx = body.X + MathF.Sin(angle) * r, cz = body.Z + MathF.Cos(angle) * r;
                    if (AnimalWoodland(cx, cz) < best)
                        (tx, tz, best) = (cx, cz, AnimalWoodland(cx, cz));
                }
                if (AnimalDist(tx, tz, flock.HomeX, flock.HomeZ) > SheepHomeRange)
                {
                    tx = body.X + (flock.HomeX - body.X) * 0.4f;
                    tz = body.Z + (flock.HomeZ - body.Z) * 0.4f;
                }
                (tx, tz) = AnimalNearestDry(tx, tz);
                AnimalStartWalk(ref body, ref mind, tx, tz, ctx, now);
            }
            else
            {
                body.State = ctx.Rng.NextDouble() < 0.7 ? AnimalState.Graze : AnimalState.Idle;
                mind.StateUntil = now + AnimalMicros(5f, 12f, ctx);
            }
            return;
        }

        if (fromLeader > 3.5f)
        {
            float angle = (float)(ctx.Rng.NextDouble() * MathF.Tau);
            float r = 1f + (float)ctx.Rng.NextDouble() * 2f;
            var (tx, tz) = AnimalNearestDry(leader.X + MathF.Sin(angle) * r, leader.Z + MathF.Cos(angle) * r);
            AnimalStartWalk(ref body, ref mind, tx, tz, ctx, now);
            return;
        }

        double roll = ctx.Rng.NextDouble();
        if (roll < 0.15)
        {
            // Shuffle a step or two to a fresh patch of grass.
            float angle = (float)(roll / 0.15 * MathF.Tau);
            var (tx, tz) = AnimalNearestDry(body.X + MathF.Sin(angle) * 1.5f, body.Z + MathF.Cos(angle) * 1.5f);
            AnimalStartWalk(ref body, ref mind, tx, tz, ctx, now);
            return;
        }
        body.State = roll < 0.75 ? AnimalState.Graze : AnimalState.Idle;
        mind.StateUntil = now + AnimalMicros(4f, 10f, ctx);
    }

    static void AnimalStartWalk(ref Animal body, ref AnimalMind mind, float tx, float tz, ReducerContext ctx, long now)
    {
        body.State = AnimalState.Walk;
        mind.TargetX = tx;
        mind.TargetZ = tz;
        // Give up on a walk that takes far too long, so nobody gets stuck.
        mind.StateUntil = now + AnimalMicros(12f, 16f, ctx);
    }

    static void AnimalMove(ReducerContext ctx, ref Animal body, ref AnimalMind mind, FlockMember[] flock, int self)
    {
        if (body.State is not (AnimalState.Walk or AnimalState.Run)) return;

        float dx = mind.TargetX - body.X, dz = mind.TargetZ - body.Z;
        float d = MathF.Sqrt(dx * dx + dz * dz);
        if (d < 0.01f) return;
        float speed = body.State == AnimalState.Run ? SheepRunSpeed : d > 5f ? SheepTrotSpeed : SheepWalkSpeed;
        // A wounded animal can't keep up its full gallop, and lambs are a bit slower.
        if (body.State == AnimalState.Run)
            speed *= (0.6f + 0.4f * mind.Health / 100f) * (body.Growth < 100 ? 0.85f : 1f);
        float step = MathF.Min(speed * AnimalDt, d);
        float nx = body.X + dx / d * step, nz = body.Z + dz / d * step;

        // Keep a little personal space.
        for (int j = 0; j < flock.Length; j++)
        {
            if (j == self) continue;
            float sx = nx - flock[j].Body.X, sz = nz - flock[j].Body.Z;
            float sd = MathF.Sqrt(sx * sx + sz * sz);
            if (sd > 0.01f && sd < 0.9f)
            {
                nx += sx / sd * (0.9f - sd) * 0.5f;
                nz += sz / sd * (0.9f - sd) * 0.5f;
            }
        }

        if (!AnimalCanStand(nx, nz) || AnimalStepBlocked(ctx, body.X, body.Z, nx, nz))
        {
            if (body.State == AnimalState.Run)
            {
                // Swerve along the shore (or round a fire) instead of running into it.
                float c = MathF.Cos(1.1f), s = MathF.Sin(1.1f);
                (mind.FleeX, mind.FleeZ) = (mind.FleeX * c - mind.FleeZ * s, mind.FleeX * s + mind.FleeZ * c);
            }
            else
            {
                mind.TargetX = body.X;
                mind.TargetZ = body.Z;
            }
            return;
        }

        body.Heading = MathF.Atan2(nx - body.X, nz - body.Z);
        body.X = nx;
        body.Z = nz;
    }

    // How thickly wooded a spot is, 0 to 1. Matches how the client scatters trees
    // (Terrain.AddVegetation, including the treeless clearing around the spawn point),
    // so sheep can favor open grass.
    static float AnimalWoodland(float x, float z) =>
        x * x + z * z < 12f * 12f ? 0f : World.Noise(World.CellOf(x), World.CellOf(z), 24f, 33);

    // The most open dry spot within range of (x, z).
    static (float, float) AnimalOpenestNear(float x, float z, float range)
    {
        var (bx, bz) = AnimalNearestDry(x, z);
        float best = AnimalWoodland(bx, bz);
        for (float dx = -range; dx <= range; dx += 2f)
            for (float dz = -range; dz <= range; dz += 2f)
            {
                float px = x + dx, pz = z + dz;
                if (dx * dx + dz * dz > range * range || !AnimalCanStand(px, pz)) continue;
                float w = AnimalWoodland(px, pz);
                if (w < best)
                    (bx, bz, best) = (px, pz, w);
            }
        return (bx, bz);
    }

    static bool AnimalCanStand(float x, float z) => World.BaseHeight(World.CellOf(x), World.CellOf(z)) > SheepDryLevel;

    // The closest dry spot to (x, z), searching outward in rings.
    static (float, float) AnimalNearestDry(float x, float z)
    {
        if (AnimalCanStand(x, z)) return (x, z);
        for (int ring = 1; ring <= 24; ring++)
            for (int k = 0; k < 8 * ring; k++)
            {
                float angle = k * MathF.Tau / (8 * ring);
                float px = x + MathF.Sin(angle) * ring, pz = z + MathF.Cos(angle) * ring;
                if (AnimalCanStand(px, pz)) return (px, pz);
            }
        return (x, z);
    }

    static float AnimalDist(float ax, float az, float bx, float bz)
    {
        float dx = ax - bx, dz = az - bz;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    static long AnimalMicros(float minSeconds, float maxSeconds, ReducerContext ctx) =>
        (long)((minSeconds + ctx.Rng.NextDouble() * (maxSeconds - minSeconds)) * 1_000_000);
}
