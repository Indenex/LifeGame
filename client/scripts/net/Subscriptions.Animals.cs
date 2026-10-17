using System.Collections.Generic;

namespace LifeGame;

// Animals: the herd, carcasses, hide racks, and what you wear (animals-12, read by the equipment panel, ui-10).
public static partial class Subscriptions
{
    [SubscriptionQueries]
    static IEnumerable<string> AnimalsQueries(string range, string farRange) => new[]
    {
        $"SELECT * FROM animal WHERE {range}",
        $"SELECT * FROM animal_carcass WHERE {range}",
        $"SELECT * FROM hide_rack WHERE {range}",
        $"SELECT * FROM snare_state WHERE {range}",
        "SELECT * FROM my_worn",
        "SELECT * FROM worn_item",
    };
}
