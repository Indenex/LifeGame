// Clothing (Animals lane): what a player carries is what they wear. The warmest
// piece per slot (Shared Clothing) takes a share off the cold's bite, through
// Survival's ClothingWarmthHook. Picking up or dropping clothes changes the rate
// at the next survival tick.
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

public static partial class Module
{
    static partial void ClothingWarmthHook(ReducerContext ctx, uint playerId, ref float insulation)
    {
        if (ctx.Db.Player.PlayerId.Find(playerId) is not { } player) return;
        insulation += Clothing.InsulationOf(ctx.Db.InventoryItem.Owner.Filter(player.Identity).Select(i => i.Item));
    }
}
