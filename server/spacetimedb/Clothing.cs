// Clothing (Animals lane): what a player wears, one piece per slot (D-074). A worn
// piece leaves the inventory and goes back into it when taken off; worn clothes
// don't count toward the carry load. Only worn pieces keep the cold off, through
// Survival's ClothingWarmthHook, and they fall with everything else on death.
using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

public static partial class Module
{
    // Public so other players' clients can show what you wear (Modeling lane).
    [SpacetimeDB.Table(Accessor = "WornItem", Public = true)]
    public partial struct WornItem
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        public uint PlayerId;
        public byte Slot;
        public ushort Item;
        public byte Quality;
    }

    [SpacetimeDB.View(Accessor = "MyWorn", Public = true)]
    public static System.Collections.Generic.List<WornItem> MyWorn(ViewContext ctx) =>
        ctx.Db.WornItem.Owner.Filter(Me(ctx)).ToList();

    // Puts on one carried piece; whatever was in its slot goes back into the inventory.
    [SpacetimeDB.Reducer]
    public static void Wear(ReducerContext ctx, ushort item, byte quality)
    {
        var player = RequirePlayer(ctx);
        var piece = Clothing.Find(item) ?? throw new Exception("You can't wear that");
        var stack = FindStack(ctx, Me(ctx), (Item)item, quality) ?? throw new Exception("You are not carrying that");
        RemoveFromStack(ctx, stack, 1);
        if (WornIn(ctx, Me(ctx), (byte)piece.Slot) is { } old)
        {
            AddToInventory(ctx, Me(ctx), (Item)old.Item, 1, old.Quality);
            ctx.Db.WornItem.Id.Delete(old.Id);
        }
        ctx.Db.WornItem.Insert(new WornItem
        {
            Id = 0,
            Owner = Me(ctx),
            PlayerId = player.PlayerId,
            Slot = (byte)piece.Slot,
            Item = item,
            Quality = quality,
        });
    }

    [SpacetimeDB.Reducer]
    public static void TakeOff(ReducerContext ctx, byte slot)
    {
        RequirePlayer(ctx);
        var worn = WornIn(ctx, Me(ctx), slot) ?? throw new Exception("You aren't wearing anything there");
        AddToInventory(ctx, Me(ctx), (Item)worn.Item, 1, worn.Quality);
        ctx.Db.WornItem.Id.Delete(worn.Id);
    }

    static WornItem? WornIn(ReducerContext ctx, Identity owner, byte slot) =>
        ctx.Db.WornItem.Owner.Filter(owner).Where(w => w.Slot == slot).Cast<WornItem?>().FirstOrDefault();

    static partial void ClothingWarmthHook(ReducerContext ctx, uint playerId, ref float insulation)
    {
        if (ctx.Db.Player.PlayerId.Find(playerId) is not { } player) return;
        insulation += Clothing.InsulationOf(ctx.Db.WornItem.Owner.Filter(player.Identity).Select(w => (w.Item, w.Quality)));
    }

    // Before a death drops the inventory: worn clothes go back into it, so they fall too.
    static partial void BeforeDeathDrop(ReducerContext ctx, Player player)
    {
        foreach (var worn in ctx.Db.WornItem.Owner.Filter(player.Identity).ToList())
        {
            AddToInventory(ctx, player.Identity, (Item)worn.Item, 1, worn.Quality);
            ctx.Db.WornItem.Id.Delete(worn.Id);
        }
    }
}
