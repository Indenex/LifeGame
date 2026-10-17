using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Mending worn tools at a repair table (crafting-19, D-155). Stand at the table with a
// tool that has started to show wear and G offers to mend it; no panel, no minigame.
// The server takes the lashing and decides the new quality.
public static class Mending
{
    public static void Register(DbConnection conn)
    {
        Use.Register(new UseHandler
        {
            Id = "crafting.mend",
            Priority = 20,
            Find = at =>
            {
                var table = Stations.NearestOf(conn, StationKind.Repair, at);
                if (table == null) return null;
                var tool = Worst(conn);
                if (tool == null) return null;
                float d = new Vector2(table.X - at.X, table.Z - at.Z).Length();
                var t = tool;
                return new UseOffer($"Mend your {ItemLook.Label((Item)t.Item, t.Quality, 1, ItemLook.Condition(t.Item, t.Quality, t.Wear)).ToLowerInvariant()}", d,
                    () => conn.Reducers.RepairTool(t.Item, t.Quality, t.Wear));
            },
        });
    }

    // The carried tool most in need of mending: a spent one first, then the most worn.
    static InventoryItem? Worst(DbConnection conn)
    {
        InventoryItem? worst = null;
        foreach (var row in conn.Db.MyInventory.Iter())
        {
            if (!Items.IsKnown(row.Item) || !Durability.Wears((Item)row.Item)) continue;
            if (ItemLook.Condition(row.Item, row.Quality, row.Wear) == "") continue;
            if (worst == null || Durability.Left(row.Quality, row.Wear) < Durability.Left(worst.Quality, worst.Wear))
                worst = row;
        }
        return worst;
    }
}
