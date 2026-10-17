using LifeGame.Shared;
using SpacetimeDB;

// Snares and other traps (animals-34, shared/Traps.cs). A trap is a placeable item set on the ground
// (PlaceItem). Setting one down adds a SnareState row; from then on the animal tick rolls a small chance
// each half second for an animal of a target species within the trap's reach (D-182: the server decides
// every catch). A catch is kept in the trap until its owner, or a member of the claim it stands in,
// collects it. The trap then re-sets itself (D-148: no chore) and falls apart after its last catch.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "SnareState", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct SnareState
    {
        // The placed WorldItem that is the trap.
        [PrimaryKey]
        public ulong ItemId;
        // Who set it (the character key, Me(ctx)).
        public Identity Owner;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        // 0 while it waits; otherwise the caught animal's AnimalDefs id plus one.
        public byte Caught;
        public long CaughtAt;
    }

    // Called by PlaceItem after the item is down.
    static partial void OnItemPlaced(ReducerContext ctx, WorldItem placed)
    {
        if (Traps.Find(placed.Item) is null) return;
        ctx.Db.SnareState.Insert(new SnareState
        {
            ItemId = placed.Id,
            Owner = Me(ctx),
            X = placed.X,
            Z = placed.Z,
            ChunkX = placed.ChunkX,
            ChunkZ = placed.ChunkZ,
        });
    }

    // From the animal tick: waiting traps roll for the animals near them.
    static void TrapTick(ReducerContext ctx, long now)
    {
        foreach (var listed in ctx.Db.SnareState.Iter().ToList())
        {
            var snare = listed;
            if (ctx.Db.WorldItem.Id.Find(snare.ItemId) is not { } item || Traps.Find(item.Item) is not { } def)
            {
                // Picked up or weathered away.
                ctx.Db.SnareState.ItemId.Delete(snare.ItemId);
                continue;
            }
            if (snare.Caught != 0) continue;

            int cx = snare.ChunkX, cz = snare.ChunkZ;
            for (int dx = -1; dx <= 1 && snare.Caught == 0; dx++)
                for (int dz = -1; dz <= 1 && snare.Caught == 0; dz++)
                    foreach (var animal in ctx.Db.Animal.ByChunk.Filter((cx + dx, cz + dz)).ToList())
                    {
                        if (Array.IndexOf(def.Targets, (byte)animal.Species) < 0 || animal.Growth < 100) continue;
                        if (AnimalDist(animal.X, animal.Z, snare.X, snare.Z) > def.Reach) continue;
                        if (ctx.Rng.NextDouble() >= def.CatchPerTick) continue;
                        ctx.Db.Animal.Id.Delete(animal.Id);
                        ctx.Db.AnimalMind.AnimalId.Delete(animal.Id);
                        snare = snare with { Caught = (byte)((byte)animal.Species + 1), CaughtAt = now };
                        ctx.Db.SnareState.ItemId.Update(snare);
                        break;
                    }
        }
    }

    // Whether the caller may take what is in a snare: whoever set it, or the owner or a member of the claim it stands in.
    static bool MayTakeCatch(ReducerContext ctx, SnareState snare) =>
        snare.Owner == Me(ctx)
        || (ClaimAt(ctx, snare.X, snare.Z) is { } claim && IsOwnerOrMember(ctx, claim, Me(ctx)));

    [SpacetimeDB.Reducer]
    public static void CollectSnare(ReducerContext ctx, ulong itemId)
    {
        var player = RequirePlayer(ctx);
        var snare = ctx.Db.SnareState.ItemId.Find(itemId) ?? throw new Exception("That snare is gone");
        var item = ctx.Db.WorldItem.Id.Find(itemId) ?? throw new Exception("That snare is gone");
        var def = Traps.Find(item.Item) ?? throw new Exception("That snare is gone");
        RequireReach(RequirePos(ctx, player), snare.X, snare.Z);
        if (snare.Caught == 0) throw new Exception("Nothing in the snare yet");
        if (!MayTakeCatch(ctx, snare)) throw new Exception("That catch is not yours to take");

        // The catch is dressed on the spot like a butchered animal: the species' own cuts and fur.
        byte cap = 100;
        QualityCapFor(ctx, player.PlayerId, ButcherProcess, ref cap);
        byte quality = Math.Min(item.Quality, cap);
        OnProcessPerformed(ctx, player.PlayerId, ButcherProcess, quality, false);
        var animal = AnimalDefs.Get((byte)(snare.Caught - 1));
        for (int i = 0; i < animal.MeatGrown; i++) DropAtSnare(ctx, snare, animal.Meat, quality);
        if (animal.Hide is { } hide) DropAtSnare(ctx, snare, hide, quality);
        if (animal.Extra is { } extra) DropAtSnare(ctx, snare, extra, quality);

        // It re-sets itself; the catch is counted in the item's wear so picking it up and setting it down does not renew it.
        int used = Durability.Used(item.Wear) + 1;
        if (used >= def.Catches)
        {
            ctx.Db.WorldItem.Id.Delete(itemId);
            ctx.Db.SnareState.ItemId.Delete(itemId);
            return;
        }
        ctx.Db.WorldItem.Id.Update(item with { Wear = Durability.Pack(used, Durability.Origin(item.Wear)) });
        ctx.Db.SnareState.ItemId.Update(snare with { Caught = 0, CaughtAt = 0 });
    }

    static void DropAtSnare(ReducerContext ctx, SnareState snare, Item what, byte quality)
    {
        float angle = (float)(ctx.Rng.NextDouble() * MathF.Tau);
        float r = 0.5f + (float)ctx.Rng.NextDouble() * 0.4f;
        float x = snare.X + MathF.Sin(angle) * r, z = snare.Z + MathF.Cos(angle) * r;
        ctx.Db.WorldItem.Insert(new WorldItem
        {
            Id = 0,
            Item = (ushort)what,
            Quality = quality,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
        });
    }

    // PickUp asks this first: a snare holding a catch is not picked up until the catch is taken, so
    // nobody can walk off with the trap and the animal in it.
    static partial void PickUpGuard(ReducerContext ctx, WorldItem item)
    {
        if (ctx.Db.SnareState.ItemId.Find(item.Id) is { Caught: not 0 })
            throw new Exception("There is a catch in that snare. Take it first");
    }
}
