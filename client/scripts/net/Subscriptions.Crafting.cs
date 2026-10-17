using System.Collections.Generic;

namespace LifeGame;

// Crafting: what lies on the ground, fires and hearths, roasts, your bag and hotbar, and your craft job.
public static partial class Subscriptions
{
    [SubscriptionQueries]
    static IEnumerable<string> CraftingQueries(string range, string farRange) => new[]
    {
        $"SELECT * FROM world_item WHERE {range}",
        $"SELECT * FROM fire WHERE {range}",
        $"SELECT * FROM cold_hearth WHERE {range}",
        $"SELECT * FROM roast WHERE {range}",
        "SELECT * FROM my_inventory",
        "SELECT * FROM my_hotbar",
        "SELECT * FROM player_equip",
        "SELECT * FROM my_craft_job",
        "SELECT * FROM device_status",
        "SELECT * FROM device_output",
    };
}
