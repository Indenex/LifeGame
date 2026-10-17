using LifeGame.Shared;
using SpacetimeDB;

// The bow and arrows' side of shooting (animals-15). Animals owns the shot, the hit and
// the damage (Bow.cs); Crafting owns the items. This finds the shooter's best working
// bow and an arrow, uses the arrow up, wears the bow a little (shared/Durability.cs) and
// hands the item powers and qualities back. A spent bow is skipped until it is mended.
public static partial class Module
{
    static partial void BowShotHook(ReducerContext ctx, uint playerId, ref BowShot shot)
    {
        // The bow shoots only from the equipped slot (Equip.cs); arrows are taken from the bag.
        InventoryItem? bow = null, arrow = null;
        if (EquippedStack(ctx) is { } held && Items.Def((Item)held.Item).BowPower > 0f) bow = held;
        if (bow is null) throw new Exception(NoWeaponMessage(ctx, "your bow"));
        foreach (var row in ctx.Db.InventoryItem.Owner.Filter(ctx.Sender))
        {
            if (!Items.IsKnown(row.Item)) continue;
            if (Items.Def((Item)row.Item).ArrowPower > 0f && (arrow is null || row.Quality > arrow.Value.Quality))
                arrow = row;
        }
        if (bow is not { } b || arrow is not { } a) return;

        RemoveFromStack(ctx, a, 1);
        WearTool(ctx, b);
        shot.BowPower = Items.Def((Item)b.Item).BowPower;
        shot.ArrowPower = Items.Def((Item)a.Item).ArrowPower;
        shot.BowQ = b.Quality / 100f;
        shot.ArrowQ = a.Quality / 100f;
        shot.Ready = true;
    }
}
