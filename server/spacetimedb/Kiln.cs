using LifeGame.Shared;
using SpacetimeDB;

// The updraft kiln (ceramics-2). Built as a blueprint from Building's B menu
// (StructureKind.Kiln, D-035) out of sun-dried bricks and daub. Dry pots from
// your bag go in through the door; fuel from your bag lights it; three minutes
// later the load comes out beside it. The load lives on the kiln row, so pots in
// a kiln aren't ClayPieces and nothing about drying or pit firing changes.
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

    // Firing state for a finished kiln structure (StructureKind.Kiln), made the
    // first time someone uses it. Position and facing are copied so a load still
    // comes out where the kiln stood if it is taken down mid-firing.
    [SpacetimeDB.Table(Accessor = "Kiln", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct Kiln
    {
        [PrimaryKey]
        public ulong StructureId;
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

    // The one kiln action. A cold kiln takes the dry pots from your bag; if it's
    // already loaded and you have no more pots to add (or it's full), it burns fuel
    // from your bag and fires.
    [SpacetimeDB.Reducer]
    public static void UseKiln(ReducerContext ctx, ulong structureId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var site = ctx.Db.Structure.Id.Find(structureId) ?? throw new Exception("That kiln is gone");
        if ((int)site.Kind != ClayRules.KilnStructureKind) throw new Exception("That isn't a kiln");
        if (!site.Complete) throw new Exception("The kiln isn't finished yet");
        if (!Structures.Contains((int)site.Kind, site.X, site.Z, site.Yaw, pos.X, pos.Z, Structures.WorkReach))
            throw new Exception("Too far away");
        var kiln = ctx.Db.Kiln.StructureId.Find(structureId) ?? ctx.Db.Kiln.Insert(new Kiln
        {
            StructureId = site.Id,
            X = site.X,
            Z = site.Z,
            Yaw = site.Yaw,
            ChunkX = World.ChunkOf(site.X),
            ChunkZ = World.ChunkOf(site.Z),
            State = (byte)KilnState.Cold,
            Load = new List<KilnPot>(),
        });
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
            ctx.Db.Kiln.StructureId.Update(kiln with { Load = load });
            return;
        }

        if (load.Count == 0)
            throw new Exception("The kiln is empty. Bring dry pots to load it.");
        BurnFuel(ctx, ClayRules.KilnFireSeconds);
        var finish = ctx.Timestamp + new TimeDuration((long)(ClayRules.KilnFireSeconds * 1_000_000));
        ctx.Db.Kiln.StructureId.Update(kiln with
        {
            State = (byte)KilnState.Firing,
            LitBy = player.PlayerId,
            FireStart = ctx.Timestamp,
            FireFinish = finish,
        });
        ctx.Db.KilnTimer.Insert(new KilnTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(finish), KilnId = kiln.StructureId });
    }

    [SpacetimeDB.Reducer]
    public static void FinishKiln(ReducerContext ctx, KilnTimer timer)
    {
        if (ctx.Db.Kiln.StructureId.Find(timer.KilnId) is not { } kiln) return;
        var load = kiln.Load ?? new List<KilnPot>();
        // Taken down while firing: the pots still come out, and the state goes.
        if (ctx.Db.Structure.Id.Find(kiln.StructureId) is null)
            ctx.Db.Kiln.StructureId.Delete(kiln.StructureId);
        else
            ctx.Db.Kiln.StructureId.Update(kiln with { State = (byte)KilnState.Cold, Load = new List<KilnPot>() });

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

    // Building calls this after a structure is taken down. A cold kiln's load
    // comes back out where it stood; a firing one finishes on its timer.
    // TODO(after #10): becomes Building's `static partial void CeramicsStructureRemoved`.
    static partial void CeramicsStructureRemoved(ReducerContext ctx, Structure s)
    {
        if ((int)s.Kind != ClayRules.KilnStructureKind || ctx.Db.Kiln.StructureId.Find(s.Id) is not { } kiln) return;
        if (kiln.State == (byte)KilnState.Firing) return;
        float side = MathF.Cos(kiln.Yaw), across = -MathF.Sin(kiln.Yaw);
        int slot = 0;
        foreach (var pot in kiln.Load ?? new List<KilnPot>())
            DropAt(ctx, kiln.X, kiln.Z, side, across, slot++, ClayRules.UnfiredItem((ClayForm)pot.Form), pot.Quality);
        ctx.Db.Kiln.StructureId.Delete(kiln.StructureId);
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
