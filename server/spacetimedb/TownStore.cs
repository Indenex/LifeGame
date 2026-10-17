using LifeGame.Shared;
using SpacetimeDB;

// claims-28: the town store and upkeep, the Treasurer's powers (Justin picked both, 2026-10-17).
// One shared store per town, at its main marker: the claim on the roster longest (the founder's
// at first; the next-oldest once that one leaves). Any signer puts things in; the Treasurer and
// the Mayor (TownPower.Store) take them out. The Treasurer (TownPower.Upkeep) can keep up any
// claim on the roster from the store, renewing its marker without walking there. Every put, take
// and upkeep is written in the town's log. Everything is decided on the server (D-182).
public static partial class Module
{
    // What the town keeps, one row per stack (item, quality, wear, maker), as ClaimStash.
    [SpacetimeDB.Table(Accessor = "TownStore")]
    [SpacetimeDB.Index.BTree(Accessor = "ByTownItem", Columns = new[] { nameof(TownId), nameof(Item) })]
    public partial struct TownStore
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong TownId;
        public ushort Item;
        public byte Quality;
        public uint Count;
        public uint Wear;
        public uint Maker;
    }

    static StackKey KeyOf(TownStore r) => new(r.Item, r.Quality, r.Wear, r.Maker);

    // Signers see their own town's store; nobody else does.
    [SpacetimeDB.View(Accessor = "MyTownStore", Public = true)]
    public static List<TownStore> MyTownStore(ViewContext ctx)
    {
        var result = new List<TownStore>();
        foreach (var signer in ctx.Db.TownSigner.Owner.Filter(Me(ctx)))
            result.AddRange(ctx.Db.TownStore.TownId.Filter(signer.TownId));
        return result;
    }

    [SpacetimeDB.Reducer]
    public static void PutInTownStore(ReducerContext ctx, ulong claimId, ushort item, byte quality, uint count)
    {
        MeterCall(ctx);
        if (count == 0) throw new Exception("Nothing to leave");
        var mine = RequireTownStoreAt(ctx, claimId);
        uint carried = CarriedCount(ctx, Me(ctx), (Item)item, quality);
        if (carried == 0) throw new Exception("You are not carrying that");
        if (carried < count) throw new Exception("You are not carrying that many");
        // The most worn go first, and each keeps its wear and maker.
        uint left = count;
        while (left > 0 && FindStack(ctx, Me(ctx), (Item)item, quality) is { } stack)
        {
            uint n = Math.Min(left, stack.Count);
            var key = KeyOf(stack);
            var row = ctx.Db.TownStore.ByTownItem.Filter((mine.TownId, item)).FirstOrDefault(r => KeyOf(r) == key);
            if (row.Id == 0 && ctx.Db.TownStore.TownId.Filter(mine.TownId).Count() >= Towns.StoreStacksMax)
                throw new Exception("The town's store is full");
            RemoveFromStack(ctx, stack, n);
            if (row.Id != 0) ctx.Db.TownStore.Id.Update(row with { Count = row.Count + n });
            else ctx.Db.TownStore.Insert(new TownStore { Id = 0, TownId = mine.TownId, Item = item, Quality = quality, Count = n, Wear = stack.Wear, Maker = stack.Maker });
            left -= n;
        }
        TownLogLine(ctx, mine.TownId, $"{mine.OwnerName} put {count} {StoreItemName(item, count)} in the store");
    }

    [SpacetimeDB.Reducer]
    public static void TakeFromTownStore(ReducerContext ctx, ulong claimId, ushort item, byte quality, uint count)
    {
        MeterCall(ctx);
        if (count == 0) throw new Exception("Nothing to take");
        var mine = RequireTownStoreAt(ctx, claimId);
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Store))
            throw new Exception("Only the town's Treasurer or Mayor can take from the store");
        uint held = 0;
        foreach (var r in ctx.Db.TownStore.ByTownItem.Filter((mine.TownId, item)))
            if (r.Quality == quality) held += r.Count;
        if (held == 0) throw new Exception("The store has none of that");
        if (held < count) throw new Exception("The store doesn't have that many");
        uint left = count;
        while (left > 0)
        {
            // The most worn first, as the bag gives them up.
            var row = ctx.Db.TownStore.ByTownItem.Filter((mine.TownId, item)).Where(r => r.Quality == quality)
                .OrderByDescending(r => Durability.Used(r.Wear)).First();
            uint n = Math.Min(left, row.Count);
            if (row.Count == n) ctx.Db.TownStore.Id.Delete(row.Id);
            else ctx.Db.TownStore.Id.Update(row with { Count = row.Count - n });
            AddToInventory(ctx, Me(ctx), (Item)item, n, quality, row.Wear, row.Maker);
            left -= n;
        }
        TownLogLine(ctx, mine.TownId, $"{mine.OwnerName} took {count} {StoreItemName(item, count)} from the store");
    }

    // The caller's roster row, once they stand at their town's main marker.
    static TownSigner RequireTownStoreAt(ReducerContext ctx, ulong claimId)
    {
        var player = RequirePlayer(ctx);
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        var claim = RequireStandingClaim(ctx, claimId);
        if (ctx.Db.TownSigner.ClaimId.Find(claim.Id) is not { } there || there.TownId != mine.TownId)
            throw new Exception("This marker is not on your town's roster");
        if (TownMainClaim(ctx, mine.TownId) is not { } main || main.Id != claim.Id)
            throw new Exception($"The town's store is kept at {TownMainClaim(ctx, mine.TownId)?.OwnerName ?? "the main"}'s marker");
        RequireNearMarker(ctx, player, claim);
        return mine;
    }

    // The town's main marker: the standing claim that has been on the roster longest.
    static Claim? TownMainClaim(ReducerContext ctx, ulong townId)
    {
        foreach (var signer in ctx.Db.TownSigner.ByTown.Filter(townId).OrderBy(r => r.SignedAt.MicrosecondsSinceUnixEpoch).ThenBy(r => r.Id))
            if (ctx.Db.Claim.Id.Find(signer.ClaimId) is { } claim && IsStanding(ctx, claim)) return claim;
        return null;
    }

    // The Treasurer keeps up a claim on the roster from the store, as if its owner had tended it.
    [SpacetimeDB.Reducer]
    public static void KeepUpTownClaim(ReducerContext ctx, ulong atClaimId, ulong claimId)
    {
        MeterCall(ctx);
        var mine = RequireTownStoreAt(ctx, atClaimId);
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Upkeep))
            throw new Exception("Only the town's Treasurer can keep up its claims");
        var claim = RequireStandingClaim(ctx, claimId);
        if (ctx.Db.TownSigner.ClaimId.Find(claim.Id) is not { } there || there.TownId != mine.TownId)
            throw new Exception("That claim is not on your town's roster");
        foreach (var (item, need) in new[] { (Item.Stick, Towns.UpkeepSticks), (Item.Fiber, Towns.UpkeepFiber) })
            if (ctx.Db.TownStore.ByTownItem.Filter((mine.TownId, (ushort)item)).Sum(r => (long)r.Count) < need)
                throw new Exception($"The store needs {Towns.UpkeepSticks} {Items.Def(Item.Stick).Count(Towns.UpkeepSticks).ToLowerInvariant()} and {Towns.UpkeepFiber} {Items.Def(Item.Fiber).Count(Towns.UpkeepFiber).ToLowerInvariant()} to keep up a claim");
        TakeFromStoreAnyQuality(ctx, mine.TownId, Item.Stick, Towns.UpkeepSticks);
        TakeFromStoreAnyQuality(ctx, mine.TownId, Item.Fiber, Towns.UpkeepFiber);
        ctx.Db.Claim.Id.Update(claim with { TendedAt = ctx.Timestamp, ExpiresAt = After(ctx.Timestamp, ClaimLifetimeSeconds(ctx)) });
        TownLogLine(ctx, mine.TownId, $"{mine.OwnerName} kept up {claim.OwnerName}'s claim from the store");
    }

    // Uses up `count` of an item from the store, the most worn and lowest quality first.
    static void TakeFromStoreAnyQuality(ReducerContext ctx, ulong townId, Item item, uint count)
    {
        uint left = count;
        while (left > 0)
        {
            var row = ctx.Db.TownStore.ByTownItem.Filter((townId, (ushort)item))
                .OrderBy(r => r.Quality).ThenByDescending(r => Durability.Used(r.Wear)).First();
            uint n = Math.Min(left, row.Count);
            if (row.Count == n) ctx.Db.TownStore.Id.Delete(row.Id);
            else ctx.Db.TownStore.Id.Update(row with { Count = row.Count - n });
            left -= n;
        }
    }

    static string StoreItemName(ushort item, uint count) =>
        Items.IsKnown(item) ? Items.Def((Item)item).Count(count).ToLowerInvariant() : "things";

    // A merge pours the joining town's store into the one it joins.
    static void MergeTownStore(ReducerContext ctx, ulong goneTownId, ulong keepTownId)
    {
        foreach (var row in ctx.Db.TownStore.TownId.Filter(goneTownId).ToList())
        {
            var key = KeyOf(row);
            var into = ctx.Db.TownStore.ByTownItem.Filter((keepTownId, row.Item)).FirstOrDefault(r => KeyOf(r) == key);
            ctx.Db.TownStore.Id.Delete(row.Id);
            if (into.Id != 0) ctx.Db.TownStore.Id.Update(into with { Count = into.Count + row.Count });
            else ctx.Db.TownStore.Insert(row with { Id = 0, TownId = keepTownId });
        }
    }

    // A town that closes spills its store at the last signer's marker, as a falling claim spills its stash.
    static void SpillTownStore(ReducerContext ctx, ulong townId, ulong lastClaimId)
    {
        var rows = ctx.Db.TownStore.TownId.Filter(townId).ToList();
        if (rows.Count == 0) return;
        var claim = ctx.Db.Claim.Id.Find(lastClaimId);
        foreach (var row in rows)
        {
            if (claim is { } at)
                for (uint i = 0; i < row.Count; i++)
                {
                    float angle = (float)(ctx.Rng.NextDouble() * Math.PI * 2);
                    float r = 0.4f + (float)ctx.Rng.NextDouble() * 1.2f;
                    float x = at.X + MathF.Cos(angle) * r, z = at.Z + MathF.Sin(angle) * r;
                    ctx.Db.WorldItem.Insert(new WorldItem { Id = 0, Item = row.Item, Quality = row.Quality, Wear = row.Wear, Maker = row.Maker, X = x, Z = z, ChunkX = World.ChunkOf(x), ChunkZ = World.ChunkOf(z) });
                }
            ctx.Db.TownStore.Id.Delete(row.Id);
        }
    }
}
