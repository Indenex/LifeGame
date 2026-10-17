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
    // The item in hand: the server keeps it on PlayerEquip.Item (other players' rows too, so their
    // clients can show it); null when nothing is held.
    public static Item? Item(DbConnection? conn) =>
        conn?.Identity is { } me ? ItemOf(conn.Db.PlayerEquip.Owner.Find(me)) : null;

    // The item another (or this) player holds, from their PlayerEquip row.
    public static Item? ItemOf(PlayerEquip? equip) =>
        equip is { Item: not 0 } e && Items.IsKnown(e.Item) ? (Item)e.Item : null;

    // Whether a working tool with these tags is equipped: carried in the bag (not on the belt)
    // and not spent.
    public static bool Has(DbConnection? conn, ItemTags tags)
    {
        if (Item(conn) is not { } item || (Items.Def(item).Tags & tags) != tags) return false;
        return conn!.Db.MyInventory.Iter().Any(r => r.Count > 0 && r.Item == (ushort)item && !r.OnBelt
            && !(Durability.Wears(item) && Durability.IsSpent(r.Quality, r.Wear)));
    }
}
