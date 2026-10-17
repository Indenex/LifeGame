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
        InventoryItem? bow = null, arrow = null;
        foreach (var row in ctx.Db.InventoryItem.Owner.Filter(Me(ctx)))
        {
            if (!Items.IsKnown(row.Item)) continue;
            var def = Items.Def((Item)row.Item);
            if (def.BowPower > 0f)
            {
                if (Durability.Wears((Item)row.Item) && Durability.IsSpent(row.Quality, row.Wear)) continue;
                if (bow is null || row.Quality > bow.Value.Quality) bow = row;
            }
            else if (def.ArrowPower > 0f && (arrow is null || row.Quality > arrow.Value.Quality))
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
