using System.Collections.Generic;
using LifeGame.Shared;

namespace LifeGame;

// What the hands hold while a craft job runs: the tool the process needs in the right
// hand, and for knapping, carving, lashing and sewing the piece being worked in the
// left. All from the ProcessDef, so new processes need nothing here.
public static class HeldTools
{
    // Your own tool is the one the server works with (Crafting's BestTool: the best
    // quality item carrying every needed tag). Other players show the tool craft_work
    // names (AvatarWork); without one, the plainest item with those tags.
    public static (Item? right, Item? left) For(string? processId, IEnumerable<(ushort item, byte quality)>? inventory)
    {
        if (string.IsNullOrEmpty(processId) || Processes.Find(processId) is not { } def) return (null, null);
        Item? right = def.NeedsTool == ItemTags.None ? null : Best(def.NeedsTool, inventory) ?? Plainest(def.NeedsTool);
        Item? left = null;
        var motion = WorkMotions.For(processId);
        if ((motion == WorkMotion.Knap || motion == WorkMotion.Carve || motion == WorkMotion.Wrap) && def.Consumes.Length > 0)
            left = def.Consumes[0].Item;
        // Lashing and sewing (anim-27): with no tool, the right hand winds the cord or yarn.
        if (motion == WorkMotion.Wrap && right == null && def.Consumes.Length > 1
            && def.Consumes[^1].Item is Item.Cordage or Item.Yarn)
            right = def.Consumes[^1].Item;
        return (right, left);
    }

    static Item? Best(ItemTags tags, IEnumerable<(ushort item, byte quality)>? inventory)
    {
        if (inventory == null) return null;
        Item? best = null;
        int bestQuality = -1;
        foreach (var (item, quality) in inventory)
            if (Items.IsKnown(item) && (Items.Def((Item)item).Tags & tags) == tags && quality > bestQuality)
                (best, bestQuality) = ((Item)item, quality);
        return best;
    }

    static readonly Dictionary<ItemTags, Item?> plainest = new();

    static Item? Plainest(ItemTags tags)
    {
        if (plainest.TryGetValue(tags, out var found)) return found;
        Item? best = null;
        foreach (var (id, def) in Items.All)
            if ((def.Tags & tags) == tags && (best == null || id < best)) best = id;
        return plainest[tags] = best;
    }
}
