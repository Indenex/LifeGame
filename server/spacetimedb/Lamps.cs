using LifeGame.Shared;
using SpacetimeDB;

// Clay oil lamps (ceramics-9): a fired lamp and a lump of tallow, lit from a burning
// fire or another lamp, set down to glow for a while. A lamp is lit while BurnsUntil
// is in the future, and the client draws its light from that. Once a minute, a lamp
// about to go out takes a tallow from baskets, jars or the claim stash nearby that its
// owner may use, so a lamp in a stocked home never needs tending (D-147).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Lamp", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct Lamp
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public Identity Owner;
        [SpacetimeDB.Index.BTree]
        public uint PlayerId;
        public byte Quality;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public Timestamp BurnsUntil;
        // The maker (shared/Marks.cs) from the lamp's Wear number, so a lamp keeps who made it when set down and picked up.
        [Default(0u)]
        public uint Mark;
    }

    [SpacetimeDB.Table(Accessor = "LampTimer", Scheduled = nameof(RefillLamps), ScheduledAt = nameof(ScheduledAt))]
    public partial struct LampTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // Sets a lamp down where you point, lit with one tallow.
    [SpacetimeDB.Reducer]
    public static void SetLamp(ReducerContext ctx, float x, float z)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z)) throw new Exception("Invalid position");
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        RequireReach(pos, x, z);
        RequireClaimAccess(ctx, x, z);
        var lamp = AnyStack(ctx, Item.ClayLamp) ?? throw new Exception("You have no clay lamp");
        if (CountOf(ctx, Item.Tallow) < 1) throw new Exception("You need tallow to burn in it");
        if (!FlameNear(ctx, x, z)) throw new Exception("Light it from a burning fire or a lit lamp nearby");
        if (ctx.Db.Lamp.PlayerId.Filter(player.PlayerId).Count() >= ClayRules.LampsPerPlayer)
            throw new Exception("You have set down enough lamps already");

        byte quality = lamp.Quality;
        uint mark = lamp.Wear;
        RemoveFromStack(ctx, lamp, 1);
        Take(ctx, Item.Tallow, 1);
        ctx.Db.Lamp.Insert(new Lamp
        {
            Id = 0,
            Owner = ctx.Sender,
            PlayerId = player.PlayerId,
            Quality = quality,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            BurnsUntil = ctx.Timestamp + new TimeDuration((long)(ClayRules.LampSecondsPerTallow * 1_000_000)),
            Mark = mark,
        });
        if (ctx.Db.LampTimer.Count == 0)
            ctx.Db.LampTimer.Insert(new LampTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(ClayRules.LampRefillCheckSeconds)) });
    }

    // Lamps about to go out (or out) take one tallow from storage nearby, if there is any.
    [SpacetimeDB.Reducer]
    public static void RefillLamps(ReducerContext ctx, LampTimer timer)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        long soon = (long)(ClayRules.LampRefillCheckSeconds * 1.5f * 1_000_000);
        long per = (long)(ClayRules.LampSecondsPerTallow * 1_000_000);
        int count = 0;
        foreach (var lamp in ctx.Db.Lamp.Iter().ToList())
        {
            count++;
            long left = Math.Max(0, lamp.BurnsUntil.MicrosecondsSinceUnixEpoch - now);
            if (left > soon || !TakeStoredTallow(ctx, lamp)) continue;
            ctx.Db.Lamp.Id.Update(lamp with { BurnsUntil = new Timestamp(now + left + per) });
        }
        if (count == 0)
            foreach (var t in ctx.Db.LampTimer.Iter().ToList())
                ctx.Db.LampTimer.ScheduledId.Delete(t.ScheduledId);
    }

    // One tallow from a basket or jar within reach of the lamp, or else the claim's
    // stash, that the lamp's owner may use.
    static bool TakeStoredTallow(ReducerContext ctx, Lamp lamp)
    {
        foreach (var s in StructuresNear(ctx, lamp.X, lamp.Z))
        {
            if (!s.Complete || !Structures.IsStore((int)s.Kind) || Dist(s.X, s.Z, lamp.X, lamp.Z) > StationInfo.StorageReach) continue;
            if (ClaimAt(ctx, s.X, s.Z) is { } c && !HasClaimRights(ctx, c, lamp.Owner)) continue;
            foreach (var row in ctx.Db.BasketItem.StructureId.Filter(s.Id))
            {
                if (row.Item != (ushort)Item.Tallow) continue;
                if (row.Count <= 1) ctx.Db.BasketItem.Id.Delete(row.Id);
                else ctx.Db.BasketItem.Id.Update(row with { Count = row.Count - 1 });
                return true;
            }
        }
        if (ClaimAt(ctx, lamp.X, lamp.Z) is { } claim && HasClaimRights(ctx, claim, lamp.Owner))
            foreach (var row in ctx.Db.ClaimStash.ByClaimItem.Filter((claim.Id, (ushort)Item.Tallow)))
            {
                if (row.Count <= 1) ctx.Db.ClaimStash.Id.Delete(row.Id);
                else ctx.Db.ClaimStash.Id.Update(row with { Count = row.Count - 1 });
                return true;
            }
        return false;
    }

    // G by a lamp: a lit one takes another tallow (anyone may feed it); an empty one
    // is picked back up by whoever may build there.
    [SpacetimeDB.Reducer]
    public static void TendLamp(ReducerContext ctx, ulong lampId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var lamp = ctx.Db.Lamp.Id.Find(lampId) ?? throw new Exception("That lamp is gone");
        RequireReach(pos, lamp.X, lamp.Z);
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        long left = Math.Max(0, lamp.BurnsUntil.MicrosecondsSinceUnixEpoch - now);
        if (CountOf(ctx, Item.Tallow) > 0 && (left > 0 || FlameNear(ctx, lamp.X, lamp.Z)))
        {
            long add = (long)(ClayRules.LampSecondsPerTallow * 1_000_000);
            if (left + add > add * ClayRules.MaxLampTallow) throw new Exception("The lamp is full");
            Take(ctx, Item.Tallow, 1);
            ctx.Db.Lamp.Id.Update(lamp with { BurnsUntil = new Timestamp(now + left + add) });
            return;
        }
        if (left > 0) throw new Exception("It is burning. Bring tallow to keep it going");
        RequireClaimAccess(ctx, lamp.X, lamp.Z);
        ctx.Db.Lamp.Id.Delete(lamp.Id);
        AddToInventory(ctx, ctx.Sender, Item.ClayLamp, 1, lamp.Quality, lamp.Mark);
    }

    // A burning fire or a lit lamp close enough to light a lamp from.
    static bool FlameNear(ReducerContext ctx, float x, float z)
    {
        if (NearestBurningFire(ctx, x, z, ClayRules.LightFromReach) is not null) return true;
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        float r2 = ClayRules.LightFromReach * ClayRules.LightFromReach;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var l in ctx.Db.Lamp.ByChunk.Filter((cx + dx, cz + dz)))
                    if (l.BurnsUntil.MicrosecondsSinceUnixEpoch > now && (l.X - x) * (l.X - x) + (l.Z - z) * (l.Z - z) <= r2) return true;
        return false;
    }
}
