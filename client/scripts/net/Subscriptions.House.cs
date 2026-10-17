using System.Collections.Generic;

namespace LifeGame;

// House building: whether a sundial stands in one of your claims (building-19).
public static partial class Subscriptions
{
    [SubscriptionQueries]
    static IEnumerable<string> HouseQueries(string range, string farRange) => new[]
    {
        "SELECT * FROM my_sundial",
    };
}
