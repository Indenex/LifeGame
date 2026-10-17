using LifeGame.Shared;
using SpacetimeDB;

// Batch devices (crafting-23, D-195): the settling tub, crushing block and tempering
// trough. Load one with a batch and it works through it in rounds of five on its own,
// making poorer work; stay and play the minigame for a round and it makes better work
// and more of it. Numbers and rules are in shared/Devices.cs. Output waits in the
// device until it is collected. A device is a station: it stands in a claim and only
// people with rights to the claim can use it.
public static partial class Module
{
    // One batch per device. Remaining counts units not yet finished, this round included.
    [SpacetimeDB.Table(Accessor = "DeviceJob", Public = true)]
    public partial struct DeviceJob
    {
        [PrimaryKey]
        public ulong StructureId;
        public Identity Owner;
        public uint PlayerId;
        public string ProcessId;
        public uint Remaining;
        public uint RoundUnits;
        public Timestamp RoundStart;
        public Timestamp RoundEnd;
        public bool Attended;
        public float Score;
        // The average quality of what was loaded, and the skill cap at loading time.
        public float InputQuality;
        public byte Cap;
        // Identifies the round's timer, so a round that was sped up ignores its old timer.
        public uint Token;
        // Who is working the round (the attended speed-up holds only while they stay), 0 for none.
        [Default(0u)]
        public uint WorkerId;
    }

    [SpacetimeDB.Table(Accessor = "DeviceTimer", Scheduled = nameof(FinishDeviceRound), ScheduledAt = nameof(ScheduledAt))]
    public partial struct DeviceTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
        public ulong StructureId;
        public uint Token;
    }

    // What a device has made and not yet given up.
    [SpacetimeDB.Table(Accessor = "DeviceOutput", Public = true)]
    public partial struct DeviceOutput
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong StructureId;
        public ushort Item;
        public byte Quality;
        public uint Count;
        // Who loaded the batch: its products carry their maker's mark (crafting-25). 0 for none.
        [Default(0u)]
        public uint Maker;
    }

    // The device the player is standing at, if they may use it: finished, within working
    // reach, and in a claim they have rights to.
    static Structure RequireDevice(ReducerContext ctx, ulong structureId, PlayerPos pos)
    {
        var s = ctx.Db.Structure.Id.Find(structureId) ?? throw new Exception("That's gone");
        var station = Structures.Shape((int)s.Kind).Station;
        if (!s.Complete || !Devices.IsDevice(station)) throw new Exception("That isn't a working device");
        if (Dist(s.X, s.Z, pos.X, pos.Z) - StationRadius(s) > Structures.WorkReach)
            throw new Exception("Too far away");
        if (ClaimAt(ctx, s.X, s.Z) is not { } claim || !HasClaimRights(ctx, claim, Me(ctx)))
            throw new Exception($"A {StationInfo.Name(station)} works only inside a claim you have rights to");
        return s;
    }

    // Loads a batch: takes the inputs now (bag, nearby storage, then the claim's stash)
    // and starts the first round.
    [SpacetimeDB.Reducer]
    public static void LoadDevice(ReducerContext ctx, ulong structureId, string processId, uint count)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var device = RequireDevice(ctx, structureId, pos);
        var kind = Structures.Shape((int)device.Kind).Station;
        var def = Processes.Find(processId);
        if (def is null || !Devices.Runs(kind, processId))
            throw new Exception($"The {StationInfo.Name(kind)} doesn't do that");
        if (ctx.Db.DeviceJob.StructureId.Find(structureId) is not null)
            throw new Exception($"The {StationInfo.Name(kind)} is already working");

        // As many units as were asked for, the batch cap allows, and the bag and storage can supply.
        uint units = Math.Min(count, (uint)Devices.BatchCap);
        foreach (var input in def.Consumes)
            units = Math.Min(units, CountFor(ctx, device, input.Item) / input.Count);
        if (units == 0)
        {
            var missing = def.Consumes.First(i => CountFor(ctx, device, i.Item) < i.Count);
            throw new Exception($"You need {missing.Count} {Items.Def(missing.Item).Count(missing.Count).ToLowerInvariant()}");
        }
        // The hammer that strikes is not one of the stones being crushed.
        if (def.NeedsTool.HasFlag(ItemTags.Hammer))
        {
            uint hammers = 0;
            foreach (var row in ctx.Db.InventoryItem.Owner.Filter(Me(ctx)))
                if (Items.IsKnown(row.Item) && Items.Def((Item)row.Item).Tags.HasFlag(ItemTags.Hammer)) hammers += row.Count;
            foreach (var input in def.Consumes)
                if (Items.Def(input.Item).Tags.HasFlag(ItemTags.Hammer)) hammers -= Math.Min(hammers, input.Count * units);
            if (hammers == 0) throw new Exception("You need a second stone to strike with");
        }

        float inputSum = 0f;
        uint taken = 0;
        foreach (var input in def.Consumes)
        {
            inputSum += TakeFor(ctx, device, input.Item, input.Count * units);
            taken += input.Count * units;
        }
        float inputQuality = taken > 0 ? inputSum / taken : Items.RawQuality;
        byte cap = 100;
        QualityCapFor(ctx, player.PlayerId, def.Id, ref cap);

        var job = new DeviceJob
        {
            StructureId = structureId,
            Owner = Me(ctx),
            PlayerId = player.PlayerId,
            ProcessId = def.Id,
            Remaining = units,
            InputQuality = inputQuality,
            Cap = cap,
        };
        StartDeviceRound(ctx, job);
    }

    // Starts the next round of a batch, unattended, and sets its timer.
    static void StartDeviceRound(ReducerContext ctx, DeviceJob job)
    {
        var def = Processes.Find(job.ProcessId)!;
        uint units = Math.Min(job.Remaining, (uint)Devices.RoundUnits);
        float seconds = def.Seconds;
        DurationFor(ctx, job.PlayerId, def.Id, ref seconds);
        seconds = Devices.UnitSeconds(Math.Max(seconds, 0.5f), false, Devices.Plain(def)) * units;
        job.RoundUnits = units;
        job.Attended = false;
        job.WorkerId = 0;
        job.Score = 0f;
        job.Token += 1;
        job.RoundStart = ctx.Timestamp;
        job.RoundEnd = ctx.Timestamp + new TimeDuration((long)(seconds * 1_000_000));
        if (ctx.Db.DeviceJob.StructureId.Find(job.StructureId) is null) ctx.Db.DeviceJob.Insert(job);
        else ctx.Db.DeviceJob.StructureId.Update(job);
        ctx.Db.DeviceTimer.Insert(new DeviceTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(job.RoundEnd), StructureId = job.StructureId, Token = job.Token });
    }

    // Stay and work the device through the round: the client plays the minigame first
    // and sends its score from 0 to 1. The rest of the round goes quicker and its work
    // is better. A round can be worked once.
    [SpacetimeDB.Reducer]
    public static void WorkDevice(ReducerContext ctx, ulong structureId, float score)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var device = RequireDevice(ctx, structureId, pos);
        var job = ctx.Db.DeviceJob.StructureId.Find(structureId) ?? throw new Exception("There is nothing to work");
        if (Processes.Find(job.ProcessId) is { } running && Devices.Plain(running))
            throw new Exception("It just runs. There's nothing to do but leave it");
        if (!float.IsFinite(score)) throw new Exception("Invalid score");
        if (job.Attended) throw new Exception("You're already working it");

        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        long left = Math.Max(0, job.RoundEnd.MicrosecondsSinceUnixEpoch - now);
        job.Attended = true;
        job.WorkerId = player.PlayerId;
        job.Score = TrustScore(ctx, player.PlayerId, job.ProcessId, Devices.MinigameOf(Structures.Shape((int)device.Kind).Station), score);
        job.RoundEnd = new Timestamp(now + (long)(left * (Devices.AttendedTime / Devices.UnattendedTime)));
        job.Token += 1;
        ctx.Db.DeviceJob.StructureId.Update(job);
        ctx.Db.DeviceTimer.Insert(new DeviceTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(job.RoundEnd), StructureId = structureId, Token = job.Token });
    }

    [SpacetimeDB.Reducer]
    public static void FinishDeviceRound(ReducerContext ctx, DeviceTimer timer)
    {
        RequireServer(ctx);
        if (ctx.Db.DeviceJob.StructureId.Find(timer.StructureId) is not { } job || job.Token != timer.Token) return;
        var def = Processes.Find(job.ProcessId);
        if (def is null) { ctx.Db.DeviceJob.StructureId.Delete(job.StructureId); return; }

        // What the same work by hand would come out at. Clay, grit and the other flat-quality
        // materials always come out of the hands at the raw quality, so a device is judged
        // against that; the penalty and bonuses then carry into the pots made from them.
        byte handQuality = Items.Def(def.Makes).HasQuality
            ? Processes.Quality(job.InputQuality, job.Attended ? job.Score : Processes.DefaultScore, job.Cap)
            : Items.RawQuality;
        bool plain = Devices.Plain(def);
        byte quality = Devices.Quality(handQuality, job.Attended, job.Score, plain);
        uint made = Devices.Quantity(def.MakesCount * job.RoundUnits, job.Attended, job.Score, plain);
        // A device taken down mid-batch hands what it made straight to the owner.
        if (ctx.Db.Structure.Id.Find(job.StructureId) is null)
            AddToInventory(ctx, job.Owner, def.Makes, made, quality, 0u, job.PlayerId);
        else
            AddDeviceOutput(ctx, job.StructureId, def.Makes, quality, made, job.PlayerId);
        for (int i = 0; i < Devices.SkillReports((int)job.RoundUnits, job.Attended, plain); i++)
            OnProcessPerformed(ctx, job.PlayerId, def.Id, quality, false);

        job.Remaining -= job.RoundUnits;
        if (job.Remaining == 0 || ctx.Db.Structure.Id.Find(job.StructureId) is null)
            ctx.Db.DeviceJob.StructureId.Delete(job.StructureId);
        else
            StartDeviceRound(ctx, job);
    }

    static void AddDeviceOutput(ReducerContext ctx, ulong structureId, Item item, byte quality, uint count, uint maker)
    {
        foreach (var row in ctx.Db.DeviceOutput.StructureId.Filter(structureId))
            if (row.Item == (ushort)item && row.Quality == quality && row.Maker == maker)
            {
                ctx.Db.DeviceOutput.Id.Update(row with { Count = row.Count + count });
                return;
            }
        ctx.Db.DeviceOutput.Insert(new DeviceOutput { Id = 0, StructureId = structureId, Item = (ushort)item, Quality = quality, Count = count, Maker = maker });
    }

    // Takes everything the device has made into the bag.
    [SpacetimeDB.Reducer]
    public static void CollectDevice(ReducerContext ctx, ulong structureId)
    {
        var player = RequirePlayer(ctx);
        RequireDevice(ctx, structureId, RequirePos(ctx, player));
        var rows = ctx.Db.DeviceOutput.StructureId.Filter(structureId).ToList();
        if (rows.Count == 0) throw new Exception("There is nothing to collect yet");
        foreach (var row in rows)
        {
            AddToInventory(ctx, Me(ctx), (Item)row.Item, row.Count, row.Quality, 0u, row.Maker);
            ctx.Db.DeviceOutput.Id.Delete(row.Id);
        }
    }
}
