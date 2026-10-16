using LifeGame.Shared;
using SpacetimeDB;

// Cooking in a fired pot (ceramics-7): a coil pot of water and food set by a
// burning fire simmers into broth or stew. It never burns, and keeps simmering
// if the fire dies (D-081: survival nudges, never punishes). Its owner takes it
// off with Use once it's done; eaten, it gives the coil pot back (Survival.cs).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "SimmerPot", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct SimmerPot
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong FireId;
        public Identity Owner;
        [SpacetimeDB.Index.BTree]
        public uint PlayerId;
        public ushort Dish;      // Item: PotOfBroth or PotOfStew
        // The pot's own quality: the dish carries it, so eating gives the pot back as it was.
        public byte Quality;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public Timestamp PutAt;
    }

    // Outcome.Placed for cook.broth and cook.stew: the pot goes by the burning fire
    // within reach of the cook. With no fire, or no room, the food comes back.
    static void SetToSimmer(ReducerContext ctx, CraftJob job, ProcessDef def, Item dish)
    {
        var pos = ctx.Db.PlayerPos.PlayerId.Find(job.PlayerId);
        float px = pos?.X ?? job.X, pz = pos?.Z ?? job.Z;
        string? refusal = null;
        var fire = BurningFireNear(ctx, px, pz, World.Reach);
        if (fire is null) refusal = "You need a burning fire within reach to cook in a pot.";
        else if (ctx.Db.SimmerPot.FireId.Filter(fire.Value.Id).Count() >= ClayRules.PotsPerFire) refusal = "There is no room by that fire for another pot.";
        else if (ctx.Db.SimmerPot.PlayerId.Filter(job.PlayerId).Count() >= ClayRules.SimmersPerPlayer) refusal = "You have two pots on the fire already.";
        if (refusal != null)
        {
            foreach (var input in def.Consumes)
                AddToInventory(ctx, job.Owner, input.Item, input.Count,
                    ClayRules.IsVessel(input.Item) ? PotQuality(job) : Items.RawQuality);
            News(ctx, job.PlayerId, refusal + " The food goes back in your bag.");
            return;
        }

        var f = fire!.Value;
        // Round the fire's edge, facing the cook, beside any pot already there.
        float toward = MathF.Atan2(px - f.X, pz - f.Z);
        bool taken = ctx.Db.SimmerPot.FireId.Filter(f.Id).Any();
        float a = toward + (taken ? 0.9f : 0f);
        float x = f.X + MathF.Sin(a) * ClayRules.SimmerRing, z = f.Z + MathF.Cos(a) * ClayRules.SimmerRing;
        ctx.Db.SimmerPot.Insert(new SimmerPot
        {
            Id = 0,
            FireId = f.Id,
            Owner = job.Owner,
            PlayerId = job.PlayerId,
            Dish = (ushort)dish,
            Quality = PotQuality(job),
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            PutAt = ctx.Timestamp,
        });
        News(ctx, job.PlayerId, $"Your {ClayRules.DishName(dish)} is simmering. In about {ClayRules.SimmerSeconds(dish) / 60f:0.#} min, {{use}} by the pot takes it off.");
    }

    // The coil pot's quality as it went in; older jobs without one fall back to the craft's.
    static byte PotQuality(CraftJob job) => job.VesselQuality > 0 ? job.VesselQuality : job.Quality;

    // Takes your pot off once it has simmered long enough.
    [SpacetimeDB.Reducer]
    public static void TakeOffPot(ReducerContext ctx, ulong potId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var pot = ctx.Db.SimmerPot.Id.Find(potId) ?? throw new Exception("That pot is gone");
        if (pot.Owner != ctx.Sender)
            throw new Exception("That isn't your pot");
        RequireReach(pos, pot.X, pot.Z);
        float left = ClayRules.SimmerSeconds((Item)pot.Dish) - SecondsBetween(pot.PutAt, ctx.Timestamp);
        if (left > 0)
            throw new Exception($"Still simmering, about {MathF.Ceiling(left):0} s left");
        ctx.Db.SimmerPot.Id.Delete(pot.Id);
        if (ClayRules.KeepsPot((Item)pot.Dish))
        {
            // Tallow is skimmed off the top; the pot comes back empty (ceramics-9).
            AddToInventory(ctx, ctx.Sender, (Item)pot.Dish, 1, Items.RawQuality);
            AddToInventory(ctx, ctx.Sender, Item.CoilPot, 1, pot.Quality);
        }
        else
            AddToInventory(ctx, ctx.Sender, (Item)pot.Dish, 1, pot.Quality);
    }
}
