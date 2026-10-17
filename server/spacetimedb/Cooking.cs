using LifeGame.Shared;
using SpacetimeDB;

// Roasting: meat set over a burning fire cooks while you wait and burns if you
// leave it. Nothing is scheduled; how done it is comes from when it went on.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Roast", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct Roast
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong FireId;
        public Identity Owner;
        public uint PlayerId;
        public ushort Item;
        public byte Quality;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public Timestamp PutAt;
    }

    // What a roast was when it went on (bugfix-54, hidden values). Private: the public Roast row
    // carries 0 in Quality, so other players can't read how good the raw food was.
    [SpacetimeDB.Table(Accessor = "RoastQuality")]
    public partial struct RoastQuality
    {
        [PrimaryKey]
        public ulong RoastId;
        public byte Quality;
    }

    static byte RoastInputQuality(ReducerContext ctx, ulong roastId) =>
        ctx.Db.RoastQuality.RoastId.Find(roastId)?.Quality ?? Items.RawQuality;

    static void DeleteRoast(ReducerContext ctx, ulong roastId)
    {
        ctx.Db.Roast.Id.Delete(roastId);
        ctx.Db.RoastQuality.RoastId.Delete(roastId);
    }

    // The burning fire nearest a spot within reach, if any.
    static Fire? BurningFireNear(ReducerContext ctx, float x, float z, float reach)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        Fire? best = null;
        float bestD = reach * reach;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var fire in ctx.Db.Fire.ByChunk.Filter((cx + dx, cz + dz)))
                {
                    float ex = fire.X - x, ez = fire.Z - z, d = ex * ex + ez * ez;
                    if (fire.FuelUntil.MicrosecondsSinceUnixEpoch > now && d <= bestD) { best = fire; bestD = d; }
                }
        return best;
    }

    // Called by FinishCraft for Outcome.Roast: the meat goes over the fire picked at the start.
    static void PutOnFire(ReducerContext ctx, CraftJob job, ProcessDef def, byte inputQuality)
    {
        if (BurningFireNear(ctx, job.X, job.Z, 0.5f) is not { } fire)
        {
            // The fire went out while the meat was being set up: give it back.
            AddToInventory(ctx, job.Owner, def.Consumes[0].Item, 1, inputQuality);
            return;
        }
        float a = (float)(ctx.Rng.NextDouble() * MathF.Tau);
        float x = fire.X + MathF.Cos(a) * 0.2f, z = fire.Z + MathF.Sin(a) * 0.2f;
        var put = ctx.Db.Roast.Insert(new Roast
        {
            Id = 0,
            FireId = fire.Id,
            Owner = job.Owner,
            PlayerId = job.PlayerId,
            Item = (ushort)def.Consumes[0].Item,
            Quality = 0,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            PutAt = ctx.Timestamp,
        });
        ctx.Db.RoastQuality.Insert(new RoastQuality { RoastId = put.Id, Quality = inputQuality });
    }

    // Takes your roast off the fire: roast mutton if it is done, burnt if it was left too long.
    [SpacetimeDB.Reducer]
    public static void TakeOffFire(ReducerContext ctx, ulong roastId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var roast = ctx.Db.Roast.Id.Find(roastId) ?? throw new Exception("That is gone");
        if (roast.Owner != Me(ctx))
            throw new Exception("That isn't yours");
        RequireReach(pos, roast.X, roast.Z);
        float seconds = SecondsBetween(roast.PutAt, ctx.Timestamp);
        if (!Roasting.Done(seconds))
            throw new Exception("It isn't cooked yet");
        roast = roast with { Quality = RoastInputQuality(ctx, roast.Id) };
        DeleteRoast(ctx, roast.Id);
        var (item, quality) = RoastResult(ctx, roast, seconds);
        AddToInventory(ctx, Me(ctx), item, 1, quality);
    }

    static (Item item, byte quality) RoastResult(ReducerContext ctx, Roast roast, float seconds)
    {
        bool burnt = Roasting.Burnt(seconds);
        byte cap = 100;
        var def = Foods.FindRoast((Item)roast.Item) ?? throw new Exception("That doesn't roast");
        string process = def.Process;
        QualityCapFor(ctx, roast.PlayerId, process, ref cap);
        byte quality = burnt ? Items.RawQuality : Processes.Quality(roast.Quality, Roasting.Score(seconds), cap);
        OnProcessPerformed(ctx, roast.PlayerId, process, quality, burnt);
        return burnt ? (def.Burnt, Items.RawQuality) : (def.Cooked, quality);
    }

    // When a fire burns out, whatever was on it drops beside it as it is: raw if
    // it never cooked, otherwise cooked or burnt by how long it had.
    static void DropRoasts(ReducerContext ctx, Fire fire)
    {
        foreach (var roast in ctx.Db.Roast.FireId.Filter(fire.Id).ToList())
        {
            var raw = roast with { Quality = RoastInputQuality(ctx, roast.Id) };
            DeleteRoast(ctx, roast.Id);
            float seconds = SecondsBetween(raw.PutAt, fire.FuelUntil);
            var (item, quality) = Roasting.Done(seconds) ? RoastResult(ctx, raw, seconds) : ((Item)raw.Item, raw.Quality);
            ctx.Db.WorldItem.Insert(new WorldItem
            {
                Id = 0,
                Item = (ushort)item,
                Quality = quality,
                X = roast.X,
                Z = roast.Z,
                ChunkX = roast.ChunkX,
                ChunkZ = roast.ChunkZ,
            });
        }
    }
}
