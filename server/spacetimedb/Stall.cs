using LifeGame.Shared;
using SpacetimeDB;

// claims-31: a trading stall. It stands inside a claim; the claim's owner and members set lots
// out on it (Stalls.Slots of them), each one thing at a time for an asking price in one kind of
// other thing, any quality. Anyone who walks up can buy, whether or not the owner is there; the
// payment waits in the stall's till for the owner and members to take. Every sale is logged on
// the stall. Everything is decided here (D-182): reach, who may set out and take, and prices.
public static partial class Module
{
    // One lot set out on a stall: Count of an item, sold one at a time for PriceCount of PriceItem.
    [SpacetimeDB.Table(Accessor = "StallLot", Public = true)]
    public partial struct StallLot
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong StructureId;
        public ushort Item;
        public byte Quality;
        public uint Count;
        public uint Wear;
        public uint Maker;
        public ushort PriceItem;
        public uint PriceCount;
    }

    // What buyers paid, waiting for the stall's keepers.
    [SpacetimeDB.Table(Accessor = "StallTill")]
    public partial struct StallTill
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong StructureId;
        public ushort Item;
        public byte Quality;
        public uint Count;
        public uint Wear;
        public uint Maker;
    }

    // "Wren bought 2 clay pots for 6 sticks".
    [SpacetimeDB.Table(Accessor = "StallSale")]
    public partial struct StallSale
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong StructureId;
        public string Text;
        public Timestamp At;
    }

    // The till and the sales of stalls in claims the viewer owns or belongs to.
    [SpacetimeDB.View(Accessor = "MyStallTill", Public = true)]
    public static List<StallTill> MyStallTill(ViewContext ctx)
    {
        var result = new List<StallTill>();
        foreach (var stallId in KeptStalls(ctx))
            result.AddRange(ctx.Db.StallTill.StructureId.Filter(stallId));
        return result;
    }

    [SpacetimeDB.View(Accessor = "MyStallSales", Public = true)]
    public static List<StallSale> MyStallSales(ViewContext ctx)
    {
        var result = new List<StallSale>();
        foreach (var stallId in KeptStalls(ctx))
            result.AddRange(ctx.Db.StallSale.StructureId.Filter(stallId));
        return result;
    }

    static IEnumerable<ulong> KeptStalls(ViewContext ctx)
    {
        var claims = ctx.Db.Claim.Owner.Filter(Me(ctx)).ToList();
        foreach (var membership in ctx.Db.ClaimMember.Member.Filter(Me(ctx)))
            if (ctx.Db.Claim.Id.Find(membership.ClaimId) is { } claim) claims.Add(claim);
        foreach (var claim in claims)
            for (int dx = -ClaimChunkReach; dx <= ClaimChunkReach; dx++)
                for (int dz = -ClaimChunkReach; dz <= ClaimChunkReach; dz++)
                    foreach (var s in ctx.Db.Structure.ByChunk.Filter((claim.ChunkX + dx, claim.ChunkZ + dz)))
                        if (s.Kind == StructureKind.Stall && Social.InClaim(claim.X, claim.Z, s.X, s.Z))
                            yield return s.Id;
    }

    // Sets `count` carried items out on the stall, sold one at a time for `priceCount` of `priceItem`.
    [SpacetimeDB.Reducer]
    public static void SetOutOnStall(ReducerContext ctx, ulong stallId, ushort item, byte quality, uint count, ushort priceItem, uint priceCount)
    {
        MeterCall(ctx);
        var s = RequireKeptStall(ctx, stallId);
        if (count == 0) throw new Exception("Nothing to set out");
        if (!Items.IsKnown(priceItem)) throw new Exception("Ask for something real");
        if (priceCount == 0) throw new Exception("Ask for something in return");
        if (priceCount > Stalls.MaxPrice) throw new Exception($"Ask for at most {Stalls.MaxPrice} of something");
        uint carried = CarriedCount(ctx, Me(ctx), (Item)item, quality);
        if (carried == 0) throw new Exception("You are not carrying that");
        if (carried < count) throw new Exception("You are not carrying that many");
        uint left = count;
        while (left > 0 && FindStack(ctx, Me(ctx), (Item)item, quality) is { } stack)
        {
            uint n = Math.Min(left, stack.Count);
            var key = KeyOf(stack);
            var same = ctx.Db.StallLot.StructureId.Filter(s.Id).FirstOrDefault(r =>
                new StackKey(r.Item, r.Quality, r.Wear, r.Maker) == key && r.PriceItem == priceItem && r.PriceCount == priceCount);
            if (same.Id == 0 && ctx.Db.StallLot.StructureId.Filter(s.Id).Count() >= Stalls.Slots)
                throw new Exception($"A stall holds {Stalls.Slots} lots");
            RemoveFromStack(ctx, stack, n);
            if (same.Id != 0) ctx.Db.StallLot.Id.Update(same with { Count = same.Count + n });
            else ctx.Db.StallLot.Insert(new StallLot { Id = 0, StructureId = s.Id, Item = item, Quality = quality, Count = n, Wear = stack.Wear, Maker = stack.Maker, PriceItem = priceItem, PriceCount = priceCount });
            left -= n;
        }
        TouchStructure(ctx, s.Id);
    }

    // A keeper takes a lot back off the stall.
    [SpacetimeDB.Reducer]
    public static void TakeBackFromStall(ReducerContext ctx, ulong lotId)
    {
        MeterCall(ctx);
        var lot = ctx.Db.StallLot.Id.Find(lotId) ?? throw new Exception("That's gone from the stall");
        RequireKeptStall(ctx, lot.StructureId);
        ctx.Db.StallLot.Id.Delete(lot.Id);
        AddToInventory(ctx, Me(ctx), (Item)lot.Item, lot.Count, lot.Quality, lot.Wear, lot.Maker);
        TouchStructure(ctx, lot.StructureId);
    }

    // Anyone in reach buys `count` from a lot, paying its price from what they carry.
    [SpacetimeDB.Reducer]
    public static void BuyFromStall(ReducerContext ctx, ulong lotId, uint count)
    {
        MeterCall(ctx);
        if (count == 0) throw new Exception("Nothing to buy");
        var lot = ctx.Db.StallLot.Id.Find(lotId) ?? throw new Exception("That's gone from the stall");
        var (s, player) = RequireFinishedInReach(ctx, lot.StructureId);
        if (ClaimAt(ctx, s.X, s.Z) is { } claim && BannedFromClaim(ctx, claim, Me(ctx)))
            throw new Exception("You are not welcome at this stall");
        if (count > lot.Count) throw new Exception($"The stall has only {lot.Count}");
        uint price = count * lot.PriceCount;
        if (CountOf(ctx, (Item)lot.PriceItem) < price)
            throw new Exception($"That costs {price} {StoreItemName(lot.PriceItem, price)}");
        // The payment goes into the till stack by stack, keeping each one's quality, wear and maker.
        uint owed = price;
        var stacks = ctx.Db.InventoryItem.ByOwnerItem.Filter((Me(ctx), lot.PriceItem)).OrderBy(r => r.Quality).ToList();
        foreach (var stack in stacks)
        {
            if (owed == 0) break;
            uint n = Math.Min(owed, stack.Count);
            var key = KeyOf(stack);
            var into = ctx.Db.StallTill.StructureId.Filter(s.Id).FirstOrDefault(r => new StackKey(r.Item, r.Quality, r.Wear, r.Maker) == key);
            if (into.Id == 0 && ctx.Db.StallTill.StructureId.Filter(s.Id).Count() >= Stalls.TillStacks)
                throw new Exception("The stall's till is full");
            RemoveFromStack(ctx, stack, n);
            if (into.Id != 0) ctx.Db.StallTill.Id.Update(into with { Count = into.Count + n });
            else ctx.Db.StallTill.Insert(new StallTill { Id = 0, StructureId = s.Id, Item = lot.PriceItem, Quality = stack.Quality, Count = n, Wear = stack.Wear, Maker = stack.Maker });
            owed -= n;
        }
        if (count == lot.Count) ctx.Db.StallLot.Id.Delete(lot.Id);
        else ctx.Db.StallLot.Id.Update(lot with { Count = lot.Count - count });
        AddToInventory(ctx, Me(ctx), (Item)lot.Item, count, lot.Quality, lot.Wear, lot.Maker);
        LogStallSale(ctx, s.Id, $"{player.Name} bought {count} {StoreItemName(lot.Item, count)} for {price} {StoreItemName(lot.PriceItem, price)}");
        TouchStructure(ctx, s.Id);
    }

    // A keeper empties the till into their bag.
    [SpacetimeDB.Reducer]
    public static void TakeFromStallTill(ReducerContext ctx, ulong stallId)
    {
        MeterCall(ctx);
        var s = RequireKeptStall(ctx, stallId);
        var rows = ctx.Db.StallTill.StructureId.Filter(s.Id).ToList();
        if (rows.Count == 0) throw new Exception("The till is empty");
        foreach (var row in rows)
        {
            ctx.Db.StallTill.Id.Delete(row.Id);
            AddToInventory(ctx, Me(ctx), (Item)row.Item, row.Count, row.Quality, row.Wear, row.Maker);
        }
        TouchStructure(ctx, s.Id);
    }

    // A finished stall in reach, in a claim the caller owns or belongs to.
    static Structure RequireKeptStall(ReducerContext ctx, ulong stallId)
    {
        var (s, _) = RequireFinishedInReach(ctx, stallId);
        if (s.Kind != StructureKind.Stall) throw new Exception("That isn't a stall");
        if (ClaimAt(ctx, s.X, s.Z) is not { } claim || !IsOwnerOrMember(ctx, claim, Me(ctx)))
            throw new Exception("Only the claim's owner and members keep this stall");
        return s;
    }

    static void LogStallSale(ReducerContext ctx, ulong stallId, string text)
    {
        ctx.Db.StallSale.Insert(new StallSale { Id = 0, StructureId = stallId, Text = text, At = ctx.Timestamp });
        var lines = ctx.Db.StallSale.StructureId.Filter(stallId).OrderBy(l => l.Id).ToList();
        for (int i = 0; i < lines.Count - Stalls.LogKept; i++) ctx.Db.StallSale.Id.Delete(lines[i].Id);
    }

    // One stall per claim, and only inside one (Claims' CheckStructureSite).
    static void CheckStallSite(ReducerContext ctx, float x, float z)
    {
        if (ClaimAt(ctx, x, z) is not { } claim) throw new Exception("A trading stall has to stand inside a claim");
        int found = 0;
        for (int dx = -ClaimChunkReach; dx <= ClaimChunkReach; dx++)
            for (int dz = -ClaimChunkReach; dz <= ClaimChunkReach; dz++)
                foreach (var s in ctx.Db.Structure.ByChunk.Filter((claim.ChunkX + dx, claim.ChunkZ + dz)))
                    if (s.Kind == StructureKind.Stall && Social.InClaim(claim.X, claim.Z, s.X, s.Z)) found++;
        if (found >= Stalls.PerClaim) throw new Exception("This claim already has a trading stall");
    }

    // A stall taken down spills its lots and its till, as a basket does.
    static void SpillStall(ReducerContext ctx, Structure s)
    {
        if (s.Kind != StructureKind.Stall) return;
        foreach (var lot in ctx.Db.StallLot.StructureId.Filter(s.Id).ToList())
        {
            SpillAt(ctx, s, lot.Item, lot.Quality, lot.Wear, lot.Maker, lot.Count);
            ctx.Db.StallLot.Id.Delete(lot.Id);
        }
        foreach (var row in ctx.Db.StallTill.StructureId.Filter(s.Id).ToList())
        {
            SpillAt(ctx, s, row.Item, row.Quality, row.Wear, row.Maker, row.Count);
            ctx.Db.StallTill.Id.Delete(row.Id);
        }
        foreach (var line in ctx.Db.StallSale.StructureId.Filter(s.Id).ToList())
            ctx.Db.StallSale.Id.Delete(line.Id);
    }

    static void SpillAt(ReducerContext ctx, Structure s, ushort item, byte quality, uint wear, uint maker, uint count)
    {
        for (uint i = 0; i < count; i++)
        {
            float a = (float)(ctx.Rng.NextDouble() * MathF.Tau), r = 0.6f + (float)ctx.Rng.NextDouble() * 0.8f;
            float x = s.X + MathF.Cos(a) * r, z = s.Z + MathF.Sin(a) * r;
            var dropped = ctx.Db.WorldItem.Insert(new WorldItem { Id = 0, Item = item, Quality = quality, Wear = wear, Maker = maker, X = x, Z = z, ChunkX = World.ChunkOf(x), ChunkZ = World.ChunkOf(z) });
            TouchGroundItem(ctx, dropped);
        }
    }
}
