// The bag's order and the tools section of the player doll (UX and UI, ui-18). Justin:
// tools set on the doll "shouldn't take up inventory space or add weight", and the bag
// gets both a Sort button and arrangement by hand. Both live on InventoryItem rows: Slot
// is a row's place (in the bag, or on the belt when OnBelt). Belt rows are still
// inventory rows, so BestTool, wear (Durability.cs), trade, stash, drop and death treat a
// tool on the belt like any other; only the carried load skips them (Survival.cs) and new
// pick-ups never stack onto them (AddToBag).
using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

public static partial class Module
{
    // What makes two things the same stack: the item, its quality, its wear and its maker
    // (crafting-25). Every place that merges, splits or moves stacks compares this and
    // nothing else, so a new part of a stack's identity is added here once. Sites:
    // AddToBag, ToBag, MoveCarried, Drop and PickUp, BasketPut/BasketTake/SpillBasket,
    // StashItem/TakeFromStash/RemoveClaim, PlaceItem, WearTool's split, RepairTool, Die.
    readonly record struct StackKey(ushort Item, byte Quality, uint Wear, uint Maker);

    static StackKey KeyOf(InventoryItem r) => new(r.Item, r.Quality, r.Wear, r.Maker);
    static StackKey KeyOf(WorldItem r) => new(r.Item, r.Quality, r.Wear, r.Maker);
    static StackKey KeyOf(BasketItem r) => new(r.Item, r.Quality, r.Wear, r.Maker);
    static StackKey KeyOf(ClaimStash r) => new(r.Item, r.Quality, r.Wear, r.Maker);

    // Adds to the bag: onto a matching bag stack, or as a new stack in the first free place.
    static void AddToBag(ReducerContext ctx, Identity owner, Item item, uint count, byte quality, uint wear, uint maker)
    {
        var key = new StackKey((ushort)item, quality, wear, maker);
        foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((owner, (ushort)item)))
            if (!row.OnBelt && KeyOf(row) == key)
            {
                ctx.Db.InventoryItem.Id.Update(row with { Count = row.Count + count });
                return;
            }
        ctx.Db.InventoryItem.Insert(new InventoryItem
        {
            Id = 0, Owner = owner, Item = (ushort)item, Quality = quality, Count = count, Wear = wear, Maker = maker,
            Slot = FreeBagPlace(ctx, owner),
        });
    }

    static ushort FreeBagPlace(ReducerContext ctx, Identity owner)
    {
        var taken = ctx.Db.InventoryItem.Owner.Filter(owner).Where(r => !r.OnBelt).Select(r => r.Slot).ToHashSet();
        for (ushort i = 0; i < Bag.Places; i++)
            if (!taken.Contains(i)) return i;
        return (ushort)(Bag.Places - 1);
    }

    static InventoryItem MyRow(ReducerContext ctx, ulong rowId) =>
        ctx.Db.InventoryItem.Id.Find(rowId) is { } row && row.Owner == ctx.Sender
            ? row
            : throw new Exception("You are not carrying that");

    // Moves a bag stack to a place; a stack already there takes the moved one's old place.
    [SpacetimeDB.Reducer]
    public static void MoveInBag(ReducerContext ctx, ulong rowId, ushort place)
    {
        RequirePlayer(ctx);
        var row = MyRow(ctx, rowId);
        if (row.OnBelt) throw new Exception("Take it off the tools first");
        if (place >= Bag.Places) throw new Exception("No such place");
        // Rows from before the bag kept an order all sit at place 0: spread them out first.
        if (Spread(ctx)) row = MyRow(ctx, rowId);
        if (row.Slot == place) return;
        foreach (var other in ctx.Db.InventoryItem.Owner.Filter(ctx.Sender).Where(r => !r.OnBelt && r.Slot == place && r.Id != row.Id).ToList())
            ctx.Db.InventoryItem.Id.Update(other with { Slot = row.Slot });
        ctx.Db.InventoryItem.Id.Update(row with { Slot = place });
    }

    // Gives bag rows that share a place their own places, keeping their order. True if any moved.
    static bool Spread(ReducerContext ctx)
    {
        var rows = ctx.Db.InventoryItem.Owner.Filter(ctx.Sender).Where(r => !r.OnBelt).OrderBy(r => r.Slot).ThenBy(r => r.Id).ToList();
        if (rows.Select(r => r.Slot).Distinct().Count() == rows.Count) return false;
        var used = new System.Collections.Generic.HashSet<ushort>();
        foreach (var r in rows)
        {
            ushort to = r.Slot;
            while (used.Contains(to)) to++;
            used.Add(to);
            if (to != r.Slot) ctx.Db.InventoryItem.Id.Update(r with { Slot = to });
        }
        return true;
    }

    // Puts the bag in order: food and drink, tools, clothes, then the rest, each by item.
    [SpacetimeDB.Reducer]
    public static void SortBag(ReducerContext ctx)
    {
        RequirePlayer(ctx);
        var rows = ctx.Db.InventoryItem.Owner.Filter(ctx.Sender).Where(r => !r.OnBelt)
            .OrderBy(r => Bag.Group((Item)r.Item)).ThenBy(r => r.Item).ThenByDescending(r => r.Quality).ThenBy(r => r.Wear)
            .ToList();
        for (int i = 0; i < rows.Count; i++)
            if (rows[i].Slot != i) ctx.Db.InventoryItem.Id.Update(rows[i] with { Slot = (ushort)i });
    }

    // Puts one tool on a place of the tools section (255: the first free one). A tool already
    // there goes back to the bag. Only made tools (Bag.BeltOk), one per place.
    [SpacetimeDB.Reducer]
    public static void PutOnBelt(ReducerContext ctx, ulong rowId, byte place)
    {
        RequirePlayer(ctx);
        var row = MyRow(ctx, rowId);
        if (!Bag.BeltOk((Item)row.Item)) throw new Exception("Only tools go there");
        var belt = ctx.Db.InventoryItem.Owner.Filter(ctx.Sender).Where(r => r.OnBelt && r.Id != row.Id).ToList();
        if (place == 255)
        {
            var taken = belt.Select(r => r.Slot).ToHashSet();
            place = (byte)Enumerable.Range(0, Bag.BeltSlots).FirstOrDefault(i => !taken.Contains((ushort)i), Bag.BeltSlots);
            if (place >= Bag.BeltSlots) throw new Exception("Your tools are full");
        }
        if (place >= Bag.BeltSlots) throw new Exception("No such place");
        foreach (var other in belt.Where(r => r.Slot == place))
            ToBag(ctx, other, row.OnBelt ? row.Slot : null);
        if (row.Count > 1)
        {
            ctx.Db.InventoryItem.Id.Update(row with { Count = row.Count - 1 });
            ctx.Db.InventoryItem.Insert(row with { Id = 0, Count = 1, OnBelt = true, Slot = place });
        }
        else
            ctx.Db.InventoryItem.Id.Update(row with { OnBelt = true, Slot = place });
    }

    // Takes a tool off the tools section into the bag (a place, or 65535 for the first free one).
    [SpacetimeDB.Reducer]
    public static void TakeOffBelt(ReducerContext ctx, ulong rowId, ushort place)
    {
        RequirePlayer(ctx);
        var row = MyRow(ctx, rowId);
        if (!row.OnBelt) return;
        ToBag(ctx, row, null, place < Bag.Places ? place : null);
    }

    // A belt row back into the bag: onto a matching stack, or to a place (or a belt place
    // swap, or the first free one).
    static void ToBag(ReducerContext ctx, InventoryItem row, ushort? beltSwap, ushort? place = null)
    {
        if (beltSwap is { } b)
        {
            ctx.Db.InventoryItem.Id.Update(row with { Slot = b });
            return;
        }
        foreach (var same in ctx.Db.InventoryItem.ByOwnerItem.Filter((row.Owner, row.Item)))
            if (!same.OnBelt && KeyOf(same) == KeyOf(row))
            {
                ctx.Db.InventoryItem.Id.Update(same with { Count = same.Count + row.Count });
                ctx.Db.InventoryItem.Id.Delete(row.Id);
                return;
            }
        ushort to = place is { } p && !ctx.Db.InventoryItem.Owner.Filter(row.Owner).Any(r => !r.OnBelt && r.Slot == p)
            ? p : FreeBagPlace(ctx, row.Owner);
        ctx.Db.InventoryItem.Id.Update(row with { OnBelt = false, Slot = to });
    }
}
