using LifeGame.Shared;
using SpacetimeDB;

// The clay trough (ceramics-13): a big kiln-fired basin, set down like a storage jar,
// that you fill by hand from pots of water. It holds a good deal (its quality sets the
// size), anyone with access can drink from it or scoop a pot back out, and it needs no
// upkeep: the water simply stays (D-148). The structure itself is a Def row in
// shared/Structures.cs; only the water level lives in a table here. No pipe network
// for now (Justin picked the trough, 2026-10-17); the network design is kept in
// tickets/ceramics-13.md.
public static partial class Module
{
    // Litres in a finished trough, made on the first pour.
    [SpacetimeDB.Table(Accessor = "TroughWater", Public = true)]
    public partial struct TroughWater
    {
        [PrimaryKey]
        public ulong StructureId;
        public uint Litres;
    }

    static (Structure s, Player player) RequireTrough(ReducerContext ctx, ulong structureId)
    {
        var (s, player) = RequireFinishedInReach(ctx, structureId);
        if (!Structures.Shape((int)s.Kind).Trough) throw new Exception("That isn't a trough");
        CheckClaimAccess(ctx, s.X, s.Z);
        return (s, player);
    }

    static int TroughCapacity(ReducerContext ctx, Structure s) =>
        Structures.TroughLitres(ctx.Db.StructureProgress.StructureId.Find(s.Id)?.Quality ?? Items.RawQuality);

    // Pours a carried pot of water in; the empty pot goes back in your hands.
    [SpacetimeDB.Reducer]
    public static void PourIntoTrough(ReducerContext ctx, ulong structureId)
    {
        var (s, player) = RequireTrough(ctx, structureId);
        InventoryItem? stack = null;
        foreach (var item in new[] { Item.CoilPotOfWater, Item.PinchPotOfWater })
            if (AnyStack(ctx, item) is { } found) { stack = found; break; }
        if (stack == null) throw new Exception("You need a pot of water to pour in");
        var filled = (Item)stack.Value.Item;
        int litres = ClayRules.PotLitres(filled);
        int have = (int)(ctx.Db.TroughWater.StructureId.Find(structureId)?.Litres ?? 0);
        int cap = TroughCapacity(ctx, s);
        if (have + litres > cap) throw new Exception(have >= cap ? "The trough is full" : "There isn't room for a whole pot");
        var state = RequireCooldown(ctx, player);

        RemoveFromStack(ctx, stack.Value, 1);
        AddToInventory(ctx, ctx.Sender, Foods.FindPot(filled)!.Empty, 1, stack.Value.Quality, stack.Value.Wear, stack.Value.Maker);
        SetTroughLitres(ctx, structureId, have + litres);
        TouchStructure(ctx, structureId);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // A drink from the trough: one litre, a sip of thirst.
    [SpacetimeDB.Reducer]
    public static void DrinkFromTrough(ReducerContext ctx, ulong structureId)
    {
        var (_, player) = RequireTrough(ctx, structureId);
        var row = ctx.Db.TroughWater.StructureId.Find(structureId);
        if (row is not { Litres: > 0 }) throw new Exception("The trough is dry");
        var state = RequireCooldown(ctx, player);
        var v = RequireVitals(ctx, player);
        if (v.Water >= 1f) throw new Exception("You are not thirsty");

        ctx.Db.Vitals.PlayerId.Update(v with { Water = Math.Min(1f, v.Water + Needs.SipWater) });
        SetTroughLitres(ctx, structureId, (int)row.Value.Litres - 1);
        TouchStructure(ctx, structureId);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // Scoops carried empty pots full from the trough, as many as it has water for.
    [SpacetimeDB.Reducer]
    public static void ScoopFromTrough(ReducerContext ctx, ulong structureId)
    {
        var (_, player) = RequireTrough(ctx, structureId);
        int have = (int)(ctx.Db.TroughWater.StructureId.Find(structureId)?.Litres ?? 0);
        if (have <= 0) throw new Exception("The trough is dry");
        var empty = ctx.Db.InventoryItem.Owner.Filter(ctx.Sender)
            .Where(i => Items.IsKnown(i.Item) && ClayRules.FilledItem((Item)i.Item) is not null)
            .ToList();
        if (empty.Count == 0) throw new Exception("You have no fired pot to fill");
        var state = RequireCooldown(ctx, player);
        bool any = false;
        foreach (var stack in empty)
        {
            var filled = ClayRules.FilledItem((Item)stack.Item)!.Value;
            int each = ClayRules.PotLitres(filled);
            int n = (int)Math.Min(stack.Count, have / each);
            if (n <= 0) continue;
            RemoveFromStack(ctx, stack, (uint)n);
            AddToInventory(ctx, ctx.Sender, filled, (uint)n, stack.Quality, stack.Wear, stack.Maker);
            have -= n * each;
            any = true;
        }
        if (!any) throw new Exception("There isn't enough water left to fill a pot");
        SetTroughLitres(ctx, structureId, have);
        TouchStructure(ctx, structureId);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    static void SetTroughLitres(ReducerContext ctx, ulong structureId, int litres)
    {
        var row = new TroughWater { StructureId = structureId, Litres = (uint)Math.Max(0, litres) };
        if (ctx.Db.TroughWater.StructureId.Find(structureId) is null) ctx.Db.TroughWater.Insert(row);
        else ctx.Db.TroughWater.StructureId.Update(row);
    }
}
