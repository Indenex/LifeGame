using LifeGame.Shared;
using SpacetimeDB;

// Crafting: processes run as jobs that take their inputs up front and finish on
// a schedule, so a long process is just a start time and an end time. Fires are
// world objects that burn down unless fed.
public static partial class Module
{
    // One running job per player: your hands are busy until it finishes.
    [SpacetimeDB.Table(Accessor = "CraftJob")]
    public partial struct CraftJob
    {
        [PrimaryKey]
        public Identity Owner;
        public uint PlayerId;
        public string ProcessId;
        public Timestamp StartedAt;
        public Timestamp FinishAt;
        public byte Quality;
        public bool Failed;
        // Where a fire will be lit, for processes that make one.
        public float X;
        public float Z;
        // A vessel consumed by the process (a coil pot of water for broth), at its own
        // quality, so Ceramics can hand it back unchanged (ceramics-7). 0 for none.
        [Default((byte)0)]
        public byte VesselQuality;
        // The tool in hand for the job (an item id, BestTool), 0 for none, so other players see
        // the exact tool (Animation, anim-3).
        [Default((ushort)0)]
        public ushort Tool;
    }

    // Clients see only their own job, to show its progress.
    [SpacetimeDB.View(Accessor = "MyCraftJob", Public = true)]
    public static List<CraftJob> MyCraftJob(ViewContext ctx) =>
        ctx.Db.CraftJob.Owner.Find(ctx.Sender) is { } job ? new List<CraftJob> { job } : new List<CraftJob>();

    [SpacetimeDB.Table(Accessor = "CraftTimer", Scheduled = nameof(FinishCraft), ScheduledAt = nameof(ScheduledAt))]
    public partial struct CraftTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
        public Identity Owner;
    }

    // A campfire. Public and indexed by chunk, so nearby clients draw it and
    // other systems can ask how warm a spot is (see WarmthAt).
    [SpacetimeDB.Table(Accessor = "Fire", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct Fire
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public uint LitBy;
        public Timestamp LitAt;
        // The fire is burning while the clock is before this.
        public Timestamp FuelUntil;
        // The most seconds of fuel this fire can hold ahead of now, from the lighter's (and any
        // better feeder's) Firecraft and a hearth's scale, the same cap FeedFire enforces.
        // Clients read it for the smoke signal at 75% of the cap (Atmosphere). 0 on fires
        // from before this column.
        [Default(0f)]
        public float FuelCapSeconds;
    }

    [SpacetimeDB.Table(Accessor = "FireTimer", Scheduled = nameof(BurnOutFires), ScheduledAt = nameof(ScheduledAt))]
    public partial struct FireTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // Hooks for the skills system. It implements these in its own file; until it
    // does, the compiler drops the calls and every cap stays at 100.
    static partial void OnProcessPerformed(ReducerContext ctx, uint playerId, string processId, byte quality, bool failed);
    // Lanes refuse a craft before its inputs are taken by setting refusal (bugfix-20).
    static partial void CanStartCraft(ReducerContext ctx, uint playerId, ProcessDef def, ref string? refusal);
    static partial void QualityCapFor(ReducerContext ctx, uint playerId, string processId, ref byte cap);
    // Skill shortens processes: scale `seconds` down (never below zero).
    static partial void DurationFor(ReducerContext ctx, uint playerId, string processId, ref float seconds);

    // Processes with Outcome.Placed set something down at the job's X and Z. Each
    // lane implements its own hook and ignores process ids it doesn't own
    // (Ceramics.cs for clay.*, Leather.cs for hide.*).
    static partial void OnPlaced(ReducerContext ctx, CraftJob job, ProcessDef def);
    static partial void AnimalsOnPlaced(ReducerContext ctx, CraftJob job, ProcessDef def);
    // A craft job has started or ended (finished or failed), for sounds other players
    // hear (D-072). Atmosphere implements them in WorkSounds.cs.
    static partial void OnCraftStarted(ReducerContext ctx, CraftJob job);
    static partial void OnCraftEnded(ReducerContext ctx, CraftJob job);

    // Starts a process. The client plays the hands-on minigame first and sends its
    // score from 0 to 1; hands-off processes ignore it.
    [SpacetimeDB.Reducer]
    public static void StartCraft(ReducerContext ctx, string processId, float score) =>
        StartCraftCore(ctx, processId, score, null);

    // Lights a fire where the player chose (crafting-28): the same craft as StartCraft with
    // the spot named. Only fire processes take a spot; it must be within reach and clear
    // ground by the same rules as the automatic spot (Hearths.cs ClearForFire).
    [SpacetimeDB.Reducer]
    public static void StartFireAt(ReducerContext ctx, string processId, float score, float x, float z)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z)) throw new Exception("Not there");
        StartCraftCore(ctx, processId, score, (x, z));
    }

    static void StartCraftCore(ReducerContext ctx, string processId, float score, (float X, float Z)? chosenSpot)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var def = Processes.Find(processId) ?? throw new Exception("Unknown process");
        if (ctx.Db.CraftJob.Owner.Find(ctx.Sender) is not null)
            throw new Exception("Your hands are busy");
        if (!float.IsFinite(score))
            throw new Exception("Invalid score");
        if (def.Minigame == Minigame.None)
            score = Processes.DefaultScore;
        score = Math.Clamp(score, 0f, 1f);

        if (def.NeedsTool != ItemTags.None && BestTool(ctx, def.NeedsTool) is null)
            throw new Exception(NoToolMessage(ctx, def.NeedsTool, "You need the right tool"));
        // Station crafts are made at a finished station in a claim (Stations.cs).
        Structure? station = null;
        if (def.Station != StationKind.None)
            station = StationNear(ctx, def.Station, pos.X, pos.Z)
                ?? throw new Exception($"You need to be at a {StationInfo.Name(def.Station)} in your claim");
        // Find the fire's spot before anything is used up, in case there is none.
        if (chosenSpot is not null && def.Outcome != Outcome.Fire)
            throw new Exception("Only a fire can be lit somewhere in particular");
        (float X, float Z)? fireSpot = def.Outcome == Outcome.Fire ? FireSpot(ctx, pos, chosenSpot) : null;

        // Check everything first so a missing input never half-consumes the rest.
        foreach (var input in def.Consumes)
            if (CountFor(ctx, station, input.Item) < input.Count)
                throw new Exception($"You need {input.Count} {Items.Def(input.Item).Count(input.Count).ToLowerInvariant()}");
        // A stone used as the hammer cannot also be the stone being knapped.
        if (def.NeedsTool.HasFlag(ItemTags.Hammer) && !HasSpareHammer(ctx, def))
            throw new Exception("You need a second stone to strike with");
        // Another lane can refuse before anything is used up (Ceramics: too much clay out).
        string? refusal = null;
        CanStartCraft(ctx, player.PlayerId, def, ref refusal);
        if (refusal != null) throw new Exception(refusal);

        float inputQuality = 0f;
        uint taken = 0;
        byte vesselQuality = 0;
        foreach (var input in def.Consumes)
        {
            float q = TakeFor(ctx, station, input.Item, input.Count);
            if (ClayRules.IsVessel(input.Item)) vesselQuality = (byte)Math.Round(q / input.Count);
            inputQuality += q;
            taken += input.Count;
        }
        inputQuality = taken > 0 ? inputQuality / taken : Items.RawQuality;

        byte cap = 100;
        QualityCapFor(ctx, player.PlayerId, def.Id, ref cap);
        bool failed = score < def.FailBelow;

        // A placed thing is set down a step in front of the crafter; a fire goes on
        // clear ground near there, or into a cold hearth within reach (Hearths.cs).
        float fx = pos.X + MathF.Sin(pos.Heading) * 1.2f, fz = pos.Z + MathF.Cos(pos.Heading) * 1.2f;
        if (fireSpot is { } spot) (fx, fz) = spot;
        if (station is { } st) (fx, fz) = BesideStation(st, pos.X, pos.Z);
        // Roasting goes over a fire already burning within reach.
        if (def.Outcome == Outcome.Roast)
        {
            var fire = BurningFireNear(ctx, pos.X, pos.Z, World.Reach) ?? throw new Exception("You need a burning fire within reach");
            if (ctx.Db.Roast.FireId.Filter(fire.Id).Count() >= Roasting.PerFire)
                throw new Exception("That fire is full");
            (fx, fz) = (fire.X, fire.Z);
        }
        // Chopping takes a log from the tree within reach (Woodcutting.cs).
        if (def.Outcome == Outcome.Chop)
            (fx, fz) = TreeToChop(ctx, pos.X, pos.Z);
        // A roast keeps its meat's quality until it comes off the fire.
        byte quality = def.Outcome == Outcome.Roast ? (byte)Math.Round(inputQuality) : Processes.Quality(inputQuality, score, cap);
        if (def.QualityBonus > 0 && def.Outcome != Outcome.Roast) quality = (byte)Math.Min(cap, quality + def.QualityBonus);
        // Better work takes a little longer (D-113); fires and roasts keep their times.
        float seconds = def.Seconds;
        if (def.Outcome is not (Outcome.Fire or Outcome.Roast))
            seconds *= Processes.QualityTimeScale(quality);
        DurationFor(ctx, player.PlayerId, def.Id, ref seconds);
        // A better tool works faster (D-089).
        ushort toolUsed = 0;
        if (def.NeedsTool != ItemTags.None && BestTool(ctx, def.NeedsTool) is { } tool)
        {
            if (Items.Def((Item)tool.Item).HasQuality)
                seconds *= 1f - Processes.ToolSpeedup(tool.Quality);
            toolUsed = tool.Item;
            WearTool(ctx, tool);
        }
        seconds = Math.Max(seconds, 0.5f);
        var finish = ctx.Timestamp + new TimeDuration((long)(seconds * 1_000_000));
        var started = ctx.Db.CraftJob.Insert(new CraftJob
        {
            Owner = ctx.Sender,
            PlayerId = player.PlayerId,
            ProcessId = def.Id,
            StartedAt = ctx.Timestamp,
            FinishAt = finish,
            Quality = quality,
            Failed = failed,
            X = fx,
            Z = fz,
            VesselQuality = vesselQuality,
            Tool = toolUsed,
        });
        ctx.Db.CraftTimer.Insert(new CraftTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(finish), Owner = ctx.Sender });
        OnCraftStarted(ctx, started);
    }

    [SpacetimeDB.Reducer]
    public static void FinishCraft(ReducerContext ctx, CraftTimer timer)
    {
        RequireServer(ctx);
        if (ctx.Db.CraftJob.Owner.Find(timer.Owner) is not { } job) return;
        ctx.Db.CraftJob.Owner.Delete(job.Owner);
        OnCraftEnded(ctx, job);
        var def = Processes.Find(job.ProcessId);
        if (def is null) return;

        // A roast counts for skills when it comes off the fire (Cooking.cs), not when it goes on.
        if (def.Outcome != Outcome.Roast)
            OnProcessPerformed(ctx, job.PlayerId, def.Id, job.Quality, job.Failed);
        if (job.Failed) return;

        if (def.Outcome == Outcome.Fire)
            LightFire(ctx, job.PlayerId, job.X, job.Z);
        else if (def.Outcome == Outcome.Placed)
        {
            OnPlaced(ctx, job, def);
            AnimalsOnPlaced(ctx, job, def);
        }
        else if (def.Outcome == Outcome.Roast)
            PutOnFire(ctx, job, def, job.Quality);
        else if (def.Outcome == Outcome.Chop)
            ChopTree(ctx, job, def);
        else
            AddToInventory(ctx, job.Owner, def.Makes, def.MakesCount, Items.Def(def.Makes).HasQuality ? job.Quality : Items.RawQuality);
    }

    // Puts one burnable item on a fire within reach.
    [SpacetimeDB.Reducer]
    public static void FeedFire(ReducerContext ctx, ulong fireId, ushort item)
    {
        var pos = RequirePos(ctx, RequirePlayer(ctx));
        var fire = ctx.Db.Fire.Id.Find(fireId) ?? throw new Exception("That fire is out");
        RequireReach(pos, fire.X, fire.Z);
        if (!Items.IsKnown(item) || Items.Def((Item)item).FuelSeconds <= 0)
            throw new Exception("That will not burn");
        if (CountOf(ctx, (Item)item) < 1)
            throw new Exception("You are not carrying that");

        // Fuel that would burn past the cap is refused and kept (D-090, D-121). The cap
        // grows with the feeder's Firecraft; a built hearth scales fuel and cap alike.
        float scale = 1f;
        FireFuelScaleHook(ctx, fire.X, fire.Z, ref scale);
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        long fuel = (long)(Items.Def((Item)item).FuelSeconds * scale * 1_000_000);
        long cap = now + (long)(Fires.MaxSecondsFor(SkillRank(ctx, pos.PlayerId, "fire.hand_drill")) * scale * 1_000_000);
        long from = Math.Max(fire.FuelUntil.MicrosecondsSinceUnixEpoch, now);
        if (from + fuel > cap)
            throw new Exception("The fire is burning as high as it will");
        Take(ctx, (Item)item, 1);
        float capSeconds = Fires.MaxSecondsFor(SkillRank(ctx, pos.PlayerId, "fire.hand_drill")) * scale;
        ctx.Db.Fire.Id.Update(fire with { FuelUntil = new Timestamp(from + fuel), FuelCapSeconds = MathF.Max(fire.FuelCapSeconds, capSeconds) });
        NoteAct(ctx, pos.PlayerId, WorkMotion.Feed, (Item)item);
    }

    // Fires that have burned out leave a cold hearth; old cold hearths are cleared away.
    [SpacetimeDB.Reducer]
    public static void BurnOutFires(ReducerContext ctx, FireTimer timer)
    {
        RequireServer(ctx);
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var row in ctx.Db.Fire.Iter().ToList())
        {
            // A fire on a raised hearth feeds itself from storage nearby (D-149, Building).
            var fire = row;
            var until = fire.FuelUntil;
            FireTopUpHook(ctx, fire, ref until);
            if (until.MicrosecondsSinceUnixEpoch != fire.FuelUntil.MicrosecondsSinceUnixEpoch)
            {
                fire = fire with { FuelUntil = until };
                ctx.Db.Fire.Id.Update(fire);
            }
            if (fire.FuelUntil.MicrosecondsSinceUnixEpoch < now)
            {
                DropRoasts(ctx, fire);
                ctx.Db.Fire.Id.Delete(fire.Id);
                LeaveColdHearth(ctx, fire);
            }
        }
        ClearOldHearths(ctx);
    }

    // How warm a spot is from nearby burning fires, 0 (none) to 1 (right beside
    // one). For the survival system's warmth needs.
    public static float WarmthAt(ReducerContext ctx, float x, float z)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        float warmth = 0f;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var fire in ctx.Db.Fire.ByChunk.Filter((cx + dx, cz + dz)))
                    if (fire.FuelUntil.MicrosecondsSinceUnixEpoch > now)
                    {
                        float radius = Fires.WarmthRadius;
                        FireWarmthRadiusHook(ctx, fire.X, fire.Z, ref radius);
                        warmth = Math.Max(warmth, Fires.WarmthFrom(fire.X, fire.Z, x, z, radius));
                    }
        return warmth;
    }

    // How far a fire's warmth reaches (default Fires.WarmthRadius). House building's
    // stone-ringed hearth sets Fires.StoneRingWarmthRadius for a fire burning on it.
    static partial void FireWarmthRadiusHook(ReducerContext ctx, float fireX, float fireZ, ref float radius);

    // Survival asks how warm a spot is through this hook (see Survival.cs).
    static partial void FireWarmthAt(ReducerContext ctx, float x, float z, ref float warmth) =>
        warmth = Math.Max(warmth, WarmthAt(ctx, x, z));

    static void LightFire(ReducerContext ctx, uint playerId, float x, float z)
    {
        RekindleAt(ctx, x, z);
        float scale = 1f;
        FireFuelScaleHook(ctx, x, z, ref scale);
        ctx.Db.Fire.Insert(new Fire
        {
            Id = 0,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            LitBy = playerId,
            LitAt = ctx.Timestamp,
            FuelUntil = ctx.Timestamp + new TimeDuration((long)(Fires.StartSeconds * scale * 1_000_000)),
            FuelCapSeconds = Fires.MaxSecondsFor(SkillRank(ctx, playerId, "fire.hand_drill")) * scale,
        });
        EnsureFireTimer(ctx);
    }

    static void EnsureFireTimer(ReducerContext ctx)
    {
        if (ctx.Db.FireTimer.Count > 0) return;
        ctx.Db.FireTimer.Insert(new FireTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(10)) });
    }

    static uint CountOf(ReducerContext ctx, Item item)
    {
        uint n = 0;
        foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((ctx.Sender, (ushort)item)))
            n += row.Count;
        return n;
    }

    // Takes items, best quality first, and returns the summed quality taken.
    static float Take(ReducerContext ctx, Item item, uint count)
    {
        var stacks = ctx.Db.InventoryItem.ByOwnerItem.Filter((ctx.Sender, (ushort)item)).ToList();
        stacks.Sort((a, b) => b.Quality.CompareTo(a.Quality));
        float sum = 0f;
        foreach (var stack in stacks)
        {
            if (count == 0) break;
            uint n = Math.Min(count, stack.Count);
            RemoveFromStack(ctx, stack, n);
            sum += n * stack.Quality;
            count -= n;
        }
        return sum;
    }

    // The best carried item with all of these properties that still works. A spent tool
    // is skipped (shared/Durability.cs); between equals the more worn one goes first.
    static InventoryItem? BestTool(ReducerContext ctx, ItemTags tags)
    {
        InventoryItem? best = null;
        foreach (var row in ctx.Db.InventoryItem.Owner.Filter(ctx.Sender))
            if (Items.IsKnown(row.Item) && (Items.Def((Item)row.Item).Tags & tags) == tags
                && !(Durability.Wears((Item)row.Item) && Durability.IsSpent(row.Quality, row.Wear))
                && (best is null || row.Quality > best.Value.Quality
                    || (row.Quality == best.Value.Quality && Durability.Used(row.Wear) > Durability.Used(best.Value.Wear))))
                best = row;
        return best;
    }

    // True when a hammer remains after the process takes its inputs.
    static bool HasSpareHammer(ReducerContext ctx, ProcessDef def)
    {
        uint hammers = 0;
        foreach (var row in ctx.Db.InventoryItem.Owner.Filter(ctx.Sender))
            if (Items.IsKnown(row.Item) && Items.Def((Item)row.Item).Tags.HasFlag(ItemTags.Hammer))
                hammers += row.Count;
        foreach (var input in def.Consumes)
            if (Items.Def(input.Item).Tags.HasFlag(ItemTags.Hammer))
                hammers -= Math.Min(hammers, input.Count);
        return hammers > 0;
    }
}
