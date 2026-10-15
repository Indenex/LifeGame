using LifeGame.Shared;
using SpacetimeDB;

// The updraft kiln (ceramics-2). Built from sun-dried bricks and daub with the
// clay.kiln process, it stands where it was built. Dry pots from your bag go in
// through the door; fuel from your bag lights it; three minutes later the load
// comes out beside it. The load lives on the kiln row, so pots in a kiln aren't
// ClayPieces and nothing about drying or pit firing changes.
public static partial class Module
{
    public enum KilnState : byte
    {
        Cold,
        Firing,
    }

    [SpacetimeDB.Type]
    public partial struct KilnPot
    {
        public byte Form;       // ClayForm
        public byte Quality;
        public uint Owner;      // player id of whoever loaded it; gets the news
    }

    [SpacetimeDB.Table(Accessor = "Kiln", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct Kiln
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public uint BuiltBy;
        public float X;
        public float Z;
        // Radians about +Y; the door faces +Z.
        public float Yaw;
        public int ChunkX;
        public int ChunkZ;
        public byte State;      // KilnState
        public List<KilnPot> Load;
        public uint LitBy;
        public Timestamp FireStart;
        public Timestamp FireFinish;
    }

    [SpacetimeDB.Table(Accessor = "KilnTimer", Scheduled = nameof(FinishKiln), ScheduledAt = nameof(ScheduledAt))]
    public partial struct KilnTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
        public ulong KilnId;
    }

    // Called from OnPlaced in Ceramics.cs when clay.kiln finishes.
    static void PlaceKiln(ReducerContext ctx, CraftJob job, ProcessDef def)
    {
        string? refuse = null;
        if (ctx.Db.Kiln.BuiltBy.Filter(job.PlayerId).Count() >= ClayRules.MaxKilnsPerPlayer)
            refuse = $"You already have {ClayRules.MaxKilnsPerPlayer} kilns.";
        else if (NearestKiln(ctx, job.X, job.Z, ClayRules.KilnSpacing) is not null)
            refuse = "There is a kiln right there already.";
        if (refuse != null)
        {
            foreach (var input in def.Consumes)
                AddToInventory(ctx, job.Owner, input.Item, input.Count, Items.RawQuality);
            News(ctx, job.PlayerId, $"{refuse} The bricks and daub go back in your bag.");
            return;
        }

        // Door toward the builder.
        var crafter = ctx.Db.PlayerPos.PlayerId.Find(job.PlayerId);
        float yaw = crafter is { } c ? MathF.Atan2(c.X - job.X, c.Z - job.Z) : 0f;
        ctx.Db.Kiln.Insert(new Kiln
        {
            Id = 0,
            BuiltBy = job.PlayerId,
            X = job.X,
            Z = job.Z,
            Yaw = yaw,
            ChunkX = World.ChunkOf(job.X),
            ChunkZ = World.ChunkOf(job.Z),
            State = (byte)KilnState.Cold,
            Load = new List<KilnPot>(),
        });
        News(ctx, job.PlayerId, "Your kiln is built. Load dry pots with V, then V again with 2 sticks to fire it.");
    }

    // The one kiln action. A cold kiln takes the dry pots from your bag; if it's
    // already loaded and you have no more pots to add (or it's full), it burns fuel
    // from your bag and fires.
    [SpacetimeDB.Reducer]
    public static void UseKiln(ReducerContext ctx, ulong kilnId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var kiln = ctx.Db.Kiln.Id.Find(kilnId) ?? throw new Exception("That kiln is gone");
        RequireReach(pos, kiln.X, kiln.Z);
        if (kiln.State == (byte)KilnState.Firing)
            throw new Exception("The kiln is firing");

        var pots = ctx.Db.InventoryItem.Owner.Filter(ctx.Sender)
            .Where(i => Items.IsKnown(i.Item) && ClayRules.FormOfUnfired((Item)i.Item) is not null)
            .ToList();
        var load = kiln.Load ?? new List<KilnPot>();
        if (pots.Count > 0 && load.Count < ClayRules.KilnCapacity)
        {
            foreach (var stack in pots)
            {
                uint used = 0;
                while (used < stack.Count && load.Count < ClayRules.KilnCapacity)
                {
                    load.Add(new KilnPot
                    {
                        Form = (byte)ClayRules.FormOfUnfired((Item)stack.Item)!.Value,
                        Quality = stack.Quality,
                        Owner = player.PlayerId,
                    });
                    used++;
                }
                if (used > 0) RemoveFromStack(ctx, stack, used);
            }
            ctx.Db.Kiln.Id.Update(kiln with { Load = load });
            return;
        }

        if (load.Count == 0)
            throw new Exception("The kiln is empty. Bring dry pots to load it.");
        BurnFuel(ctx, ClayRules.KilnFireSeconds);
        var finish = ctx.Timestamp + new TimeDuration((long)(ClayRules.KilnFireSeconds * 1_000_000));
        ctx.Db.Kiln.Id.Update(kiln with
        {
            State = (byte)KilnState.Firing,
            LitBy = player.PlayerId,
            FireStart = ctx.Timestamp,
            FireFinish = finish,
        });
        ctx.Db.KilnTimer.Insert(new KilnTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(finish), KilnId = kiln.Id });
    }

    [SpacetimeDB.Reducer]
    public static void FinishKiln(ReducerContext ctx, KilnTimer timer)
    {
        if (ctx.Db.Kiln.Id.Find(timer.KilnId) is not { } kiln) return;
        var load = kiln.Load ?? new List<KilnPot>();
        ctx.Db.Kiln.Id.Update(kiln with { State = (byte)KilnState.Cold, Load = new List<KilnPot>() });

        // Pots come out in a row in front of the door.
        float side = MathF.Cos(kiln.Yaw), across = -MathF.Sin(kiln.Yaw);
        float ox = kiln.X + MathF.Sin(kiln.Yaw) * 1.1f, oz = kiln.Z + MathF.Cos(kiln.Yaw) * 1.1f;
        int whole = 0, cracked = 0, slot = 0;
        foreach (var pot in load)
        {
            var form = (ClayForm)pot.Form;
            bool crack = ctx.Rng.NextDouble() < ClayRules.CrackChance(FireKind.Kiln, pot.Quality);
            OnProcessPerformed(ctx, pot.Owner, ClayRules.KilnFireProcess, pot.Quality, crack);
            if (crack)
            {
                for (int i = 0; i < ClayRules.SherdsPerCrack; i++)
                    DropAt(ctx, ox, oz, side, across, slot++, Item.Potsherd, Items.RawQuality);
                cracked++;
            }
            else
            {
                DropAt(ctx, ox, oz, side, across, slot++, ClayRules.FiredItem(form), ClayRules.KilnFired(pot.Quality));
                whole++;
            }
        }
        foreach (var owner in load.Select(p => p.Owner).Append(kiln.LitBy).Distinct())
            News(ctx, owner, cracked == 0
                ? $"The kiln has cooled: {whole} {(whole == 1 ? "pot" : "pots")} came out whole."
                : $"The kiln has cooled: {whole} whole, {cracked} cracked.");
    }

    // Takes fuel from the caller's bag worth at least this many seconds of fire,
    // smallest fuels first; throws without taking anything if there isn't enough.
    static void BurnFuel(ReducerContext ctx, float seconds)
    {
        var fuel = ctx.Db.InventoryItem.Owner.Filter(ctx.Sender)
            .Where(i => Items.IsKnown(i.Item) && Items.Def((Item)i.Item).FuelSeconds > 0)
            .OrderBy(i => Items.Def((Item)i.Item).FuelSeconds)
            .ToList();
        float have = fuel.Sum(i => Items.Def((Item)i.Item).FuelSeconds * i.Count);
        if (have < seconds)
            throw new Exception("You need fuel to fire the kiln: 2 sticks or more.");
        float left = seconds;
        foreach (var stack in fuel)
        {
            if (left <= 0) break;
            float each = Items.Def((Item)stack.Item).FuelSeconds;
            uint take = (uint)Math.Min(stack.Count, Math.Ceiling(left / each));
            RemoveFromStack(ctx, stack, take);
            left -= take * each;
        }
    }

    static Kiln? NearestKiln(ReducerContext ctx, float x, float z, float within)
    {
        Kiln? best = null;
        float bestD = within * within;
        int chunkX = World.ChunkOf(x), chunkZ = World.ChunkOf(z);
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var k in ctx.Db.Kiln.ByChunk.Filter((chunkX + dx, chunkZ + dz)))
                {
                    float d = (k.X - x) * (k.X - x) + (k.Z - z) * (k.Z - z);
                    if (d < bestD) { bestD = d; best = k; }
                }
        return best;
    }

    static void DropAt(ReducerContext ctx, float ox, float oz, float side, float across, int slot, Item item, byte quality)
    {
        // Alternate left and right of the door, 0.3 m apart.
        float offset = ((slot + 1) / 2) * 0.3f * (slot % 2 == 0 ? 1 : -1);
        float x = ox + side * offset, z = oz + across * offset;
        ctx.Db.WorldItem.Insert(new WorldItem
        {
            Id = 0, Item = (ushort)item, Quality = quality,
            X = x, Z = z, ChunkX = World.ChunkOf(x), ChunkZ = World.ChunkOf(z),
        });
    }
}
