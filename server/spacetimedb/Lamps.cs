using LifeGame.Shared;
using SpacetimeDB;

// Clay oil lamps (ceramics-9): a fired lamp and a lump of tallow, lit from a burning
// fire or another lamp, set down to glow for a while. Nothing is scheduled: a lamp is
// lit while BurnsUntil is in the future, and the client draws its light from that.
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
        });
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
        AddToInventory(ctx, ctx.Sender, Item.ClayLamp, 1, lamp.Quality);
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
