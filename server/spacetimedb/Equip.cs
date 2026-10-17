// Which hotbar slot a player has equipped (crafting-29, Justin: "for a tool to be used, it
// needs to be on your bar and equipped"). World tool actions (chopping, digging, shearing,
// butchering) work only with the tool in that slot; a tool in the bag or on the tools belt
// does not count there. Crafting recipes that need a tool keep BestTool (belt and bag count,
// D-263). The client picks the slot with a hotbar key (UX); the server only trusts this row.
using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

public static partial class Module
{
    // One row per player who has a slot equipped; no row means nothing is equipped.
    [SpacetimeDB.Table(Accessor = "PlayerEquip", Public = true)]
    public partial struct PlayerEquip
    {
        [PrimaryKey]
        public Identity Owner;
        public byte Slot;
        // The item in hand, so other players' clients can show it (they cannot read someone's
        // hotbar). Kept in step with the slot by EquipSlot and SyncEquip.
        [Default((ushort)0)]
        public ushort Item;
    }

    // Equips a hotbar slot (0 to 9): whatever sits on it comes to hand, tool or not (Justin: every
    // slot equips). The slot already equipped, an empty slot, or 255 puts it away.
    [SpacetimeDB.Reducer]
    public static void EquipSlot(ReducerContext ctx, byte index)
    {
        RequirePlayer(ctx);
        var row = ctx.Db.PlayerEquip.Owner.Find(Me(ctx));
        var held = index < HotbarSlots ? SlotAt(ctx, index) : null;
        if (held is not { Item: not 0 } || (row is { } r && r.Slot == index))
        {
            if (row is not null) ctx.Db.PlayerEquip.Owner.Delete(Me(ctx));
            return;
        }
        var next = new PlayerEquip { Owner = Me(ctx), Slot = index, Item = held.Value.Item };
        if (row is null) ctx.Db.PlayerEquip.Insert(next);
        else ctx.Db.PlayerEquip.Owner.Update(next);
    }

    // After the hotbar changes: the item in hand follows its slot (a swap or a new item), and an
    // emptied slot puts the hand away.
    static void SyncEquip(ReducerContext ctx)
    {
        if (ctx.Db.PlayerEquip.Owner.Find(Me(ctx)) is not { } equip) return;
        if (SlotAt(ctx, equip.Slot) is { Item: not 0 } slot)
        {
            if (slot.Item != equip.Item) ctx.Db.PlayerEquip.Owner.Update(equip with { Item = slot.Item });
        }
        else
            ctx.Db.PlayerEquip.Owner.Delete(Me(ctx));
    }

    // Using or setting down an item is a click on the item in hand (Justin): refuses any other item.
    static void RequireHeld(ReducerContext ctx, ushort item)
    {
        if (ctx.Db.PlayerEquip.Owner.Find(Me(ctx)) is not { } equip || equip.Item != item)
            throw new Exception("Take it in your hand first: press its number key");
    }

    // Uses the item in hand the way its Def says (eat, drink, wear): a click on the held item. The
    // item is the server's own record of what is in hand, never a client argument.
    [SpacetimeDB.Reducer]
    public static void UseHeld(ReducerContext ctx)
    {
        RequirePlayer(ctx);
        var equip = ctx.Db.PlayerEquip.Owner.Find(Me(ctx)) ?? throw new Exception("You are holding nothing");
        UseItem(ctx, equip.Item);
    }

    // The tool in the equipped slot if it carries these tags, is in the bag (not on the tools
    // belt) and still works; null when nothing fitting is equipped.
    static InventoryItem? EquippedTool(ReducerContext ctx, ItemTags tags)
    {
        if (EquippedStack(ctx) is not { } stack) return null;
        return (Items.Def((Item)stack.Item).Tags & tags) == tags ? stack : null;
    }

    // Whatever working item is in the equipped slot and in the bag (a bow has no tag, so the
    // bow shot reads it here).
    static InventoryItem? EquippedStack(ReducerContext ctx)
    {
        if (ctx.Db.PlayerEquip.Owner.Find(Me(ctx)) is not { } equip) return null;
        if (SlotAt(ctx, equip.Slot) is not { } slot || !Items.IsKnown(slot.Item)) return null;
        InventoryItem? best = null;
        foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((Me(ctx), slot.Item)))
        {
            if (row.OnBelt || row.Count == 0) continue;
            if (Durability.Wears((Item)row.Item) && Durability.IsSpent(row.Quality, row.Wear)) continue;
            if (best is null || row.Quality > best.Value.Quality) best = row;
        }
        return best;
    }

    // Why a hunting strike or a bow shot has no weapon: a spent one equipped, or none equipped.
    static string NoWeaponMessage(ReducerContext ctx, string what)
    {
        if (ctx.Db.PlayerEquip.Owner.Find(Me(ctx)) is { } equip && SlotAt(ctx, equip.Slot) is { } slot
            && Items.IsKnown(slot.Item) && Durability.Wears((Item)slot.Item))
            foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((Me(ctx), slot.Item)))
                if (!row.OnBelt && Durability.IsSpent(row.Quality, row.Wear))
                    return $"Your {Items.Def((Item)slot.Item).Name.ToLowerInvariant()} is spent. Mend it at a repair table.";
        return $"Put {what} on your bar and equip it";
    }

    // The same, or the reason there is none, for the player's message.
    static InventoryItem EquippedToolOrThrow(ReducerContext ctx, ItemTags tags, string what)
    {
        if (EquippedTool(ctx, tags) is { } tool) return tool;
        if (ctx.Db.PlayerEquip.Owner.Find(Me(ctx)) is { } equip && SlotAt(ctx, equip.Slot) is { } slot
            && Items.IsKnown(slot.Item) && (Items.Def((Item)slot.Item).Tags & tags) == tags)
        {
            // The right tool is equipped but spent, or only on the belt.
            foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((Me(ctx), slot.Item)))
                if (!row.OnBelt && Durability.Wears((Item)row.Item) && Durability.IsSpent(row.Quality, row.Wear))
                    return throwSpent(slot.Item);
            throw new Exception($"Take your {Items.Def((Item)slot.Item).Name.ToLowerInvariant()} out of the tools belt and into the bag to use it");
        }
        throw new Exception($"Put {what} on your bar and equip it");
        static InventoryItem throwSpent(ushort item) =>
            throw new Exception($"Your {Items.Def((Item)item).Name.ToLowerInvariant()} is spent. Mend it at a repair table.");
    }
}
