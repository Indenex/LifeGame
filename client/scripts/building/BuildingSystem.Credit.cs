using System.Collections.Generic;
using System.Linq;
using SpacetimeDB.Types;

namespace LifeGame;

// Friends' names on what they build together (building-27): a site's line names who has
// helped and how much, and standing in a finished home says who built it. From the public
// structure_credit rows; names from the player table, "you" for yourself.
public partial class BuildingSystem
{
    // Builders of a structure, most added first.
    List<(string Name, uint Added, bool Me)> Credits(ulong structureId)
    {
        var list = new List<(string, uint, bool)>();
        if (conn() is not { } c) return list;
        var me = MyIdentity();
        foreach (var row in c.Db.StructureCredit.StructureId.Filter(structureId))
        {
            bool mine = me != null && row.Builder == me;
            string name = mine ? "you" : c.Db.Player.Identity.Find(row.Builder)?.Name ?? "someone";
            list.Add((name, row.Added, mine));
        }
        return list.OrderByDescending(x => x.Item2).ThenBy(x => x.Item3).ToList();
    }

    // "Ana 12, Bo 8, you 6", or null when only you (or nobody yet) have added anything.
    string? HelpersLine(ulong structureId)
    {
        var credits = Credits(structureId);
        if (credits.Count == 0 || credits.All(x => x.Me)) return null;
        return "Built so far by " + string.Join(", ", credits.Select(x => $"{x.Name} {x.Added}"));
    }

    // "built by Ana, Bo and you", or null when nobody is recorded (older buildings).
    string? BuiltBy(ulong structureId)
    {
        var names = Credits(structureId).Select(x => x.Name).ToList();
        if (names.Count == 0) return null;
        return "built by " + (names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1]);
    }
}
