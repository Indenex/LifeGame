using LifeGame.Shared;
using SpacetimeDB;

// claims-29: who dropped a loose item, by hand, by setting it down, or by dying, so they can
// always pick it back up, on any land, someone else's claim included (Justin, 2026-10-17:
// "Yes, always"). Private: clients never learn who dropped what (D-182).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ItemDropper")]
    public partial struct ItemDropper
    {
        [PrimaryKey]
        public ulong ItemId;
        public uint PlayerId;
    }

    static void RecordDropper(ReducerContext ctx, ulong itemId, uint playerId)
    {
        if (ctx.Db.ItemDropper.ItemId.Find(itemId) is null)
            ctx.Db.ItemDropper.Insert(new ItemDropper { ItemId = itemId, PlayerId = playerId });
    }

    static bool DroppedBy(ReducerContext ctx, ulong itemId, uint playerId) =>
        ctx.Db.ItemDropper.ItemId.Find(itemId) is { } d && d.PlayerId == playerId;

    static void ForgetDropper(ReducerContext ctx, ulong itemId)
    {
        if (ctx.Db.ItemDropper.ItemId.Find(itemId) != null) ctx.Db.ItemDropper.ItemId.Delete(itemId);
    }

    // Survival's death drop calls this for each thing that falls (Die in Survival.cs).
    static void OnDeathDropped(ReducerContext ctx, Player player, WorldItem item) => RecordDropper(ctx, item.Id, player.PlayerId);

    // Run with the claim decay tick: items that weathered away or went some other way leave no dropper behind.
    static void ForgetGoneDroppers(ReducerContext ctx)
    {
        foreach (var d in ctx.Db.ItemDropper.Iter().ToList())
            if (ctx.Db.WorldItem.Id.Find(d.ItemId) is null)
                ctx.Db.ItemDropper.ItemId.Delete(d.ItemId);
    }
}
