using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// What the player has equipped (crafting-29): the hotbar slot the server keeps in PlayerEquip
// (server/spacetimedb/Equip.cs). World tool actions (chopping, digging, shearing, butchering)
// work only with that tool in hand; a tool in the bag or on the tools belt does not count.
// The server checks the same rule (EquippedTool); this is only for the prompts.
public static class Equipped
{
    // The equipped slot's item, or null when no slot is equipped or the slot is empty.
    public static Item? Item(DbConnection? conn)
    {
        if (conn?.Identity is not { } me || conn.Db.PlayerEquip.Owner.Find(me) is not { } equip) return null;
        var slot = conn.Db.MyHotbar.Iter().FirstOrDefault(s => s.Index == equip.Slot);
        return slot is { } s && Items.IsKnown(s.Item) ? (Item)s.Item : null;
    }

    // Whether a working tool with these tags is equipped: carried in the bag (not on the belt)
    // and not spent.
    public static bool Has(DbConnection? conn, ItemTags tags)
    {
        if (Item(conn) is not { } item || (Items.Def(item).Tags & tags) != tags) return false;
        return conn!.Db.MyInventory.Iter().Any(r => r.Count > 0 && r.Item == (ushort)item && !r.OnBelt
            && !(Durability.Wears(item) && Durability.IsSpent(r.Quality, r.Wear)));
    }
}
