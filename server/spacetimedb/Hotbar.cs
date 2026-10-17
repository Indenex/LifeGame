// The hotbar (UX and UI, ui-19): ten slots along the bottom of the screen, each
// remembering a kind of item, and UseItem, which does what that item's Def says
// (Items.UseOf): eat, drink or wear. Tools need no key (Justin): they work from the
// bag or the tools section. Placing goes through PlaceItem, because the client first
// shows where. A slot keeps its item while you have none, so it fills
// again when you pick more up.
using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

public static partial class Module
{
    public const byte HotbarSlots = 10;

    [SpacetimeDB.Table(Accessor = "HotbarSlot")]
    [SpacetimeDB.Index.BTree(Accessor = "ByOwnerIndex", Columns = new[] { nameof(Owner), nameof(Index) })]
    public partial struct HotbarSlot
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        public byte Index;
        public ushort Item;
    }

    [SpacetimeDB.View(Accessor = "MyHotbar", Public = true)]
    public static System.Collections.Generic.List<HotbarSlot> MyHotbar(ViewContext ctx) =>
        ctx.Db.HotbarSlot.Owner.Filter(Me(ctx)).ToList();

    // Puts a kind of item on a slot (0 clears it). If that item sat on another slot,
    // the two slots swap, so an item is only ever on one slot.
    [SpacetimeDB.Reducer]
    public static void SetHotbarSlot(ReducerContext ctx, byte index, ushort item)
    {
        RequirePlayer(ctx);
        if (index >= HotbarSlots) throw new Exception("No such slot");
        if (item != 0 && !Items.IsKnown(item)) throw new Exception("Unknown item");
        var here = SlotAt(ctx, index);
        var there = item == 0 ? null
            : ctx.Db.HotbarSlot.Owner.Filter(Me(ctx)).Where(s => s.Item == item).Cast<HotbarSlot?>().FirstOrDefault();
        if (there is { } t)
        {
            if (t.Index == index) return;
            if (here is { } h) ctx.Db.HotbarSlot.Id.Update(h with { Index = t.Index });
            ctx.Db.HotbarSlot.Id.Update(t with { Index = index });
            return;
        }
        if (here is { } old)
        {
            if (item == 0) ctx.Db.HotbarSlot.Id.Delete(old.Id);
            else ctx.Db.HotbarSlot.Id.Update(old with { Item = item });
        }
        else if (item != 0)
            ctx.Db.HotbarSlot.Insert(new HotbarSlot { Id = 0, Owner = Me(ctx), Index = index, Item = item });
    }

    // Swaps what two slots hold (dragging one slot onto another).
    [SpacetimeDB.Reducer]
    public static void MoveHotbarSlot(ReducerContext ctx, byte from, byte to)
    {
        RequirePlayer(ctx);
        if (from >= HotbarSlots || to >= HotbarSlots) throw new Exception("No such slot");
        if (from == to) return;
        var a = SlotAt(ctx, from);
        var b = SlotAt(ctx, to);
        if (a is { } x) ctx.Db.HotbarSlot.Id.Update(x with { Index = to });
        if (b is { } y) ctx.Db.HotbarSlot.Id.Update(y with { Index = from });
    }

    // Uses one carried item the way its Def says. Each rule's own reducer checks the rest.
    [SpacetimeDB.Reducer]
    public static void UseItem(ReducerContext ctx, ushort item)
    {
        RequirePlayer(ctx);
        if (!Items.IsKnown(item)) throw new Exception("Unknown item");
        var stack = BestStack(ctx, (Item)item) ?? throw new Exception("You are not carrying that");
        switch (Items.UseOf((Item)item))
        {
            case UseAction.Eat: Eat(ctx, item); break;
            case UseAction.Drink: DrinkFrom(ctx, item); break;
            case UseAction.Wear: Wear(ctx, item, stack.Quality); break;
            case UseAction.Tool: throw new Exception("Tools work by themselves while you carry them");
            default: throw new Exception("You can't use that on its own");
        }
    }

    static HotbarSlot? SlotAt(ReducerContext ctx, byte index) =>
        ctx.Db.HotbarSlot.ByOwnerIndex.Filter((Me(ctx), index)).Cast<HotbarSlot?>().FirstOrDefault();

    // The best carried stack of an item.
    static InventoryItem? BestStack(ReducerContext ctx, Item item) =>
        ctx.Db.InventoryItem.ByOwnerItem.Filter((Me(ctx), (ushort)item))
            .Where(s => s.Count > 0).OrderByDescending(s => s.Quality).Cast<InventoryItem?>().FirstOrDefault();
}
