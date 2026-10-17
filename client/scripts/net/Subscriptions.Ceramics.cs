using System.Collections.Generic;

namespace LifeGame;

// Ceramics: clay pieces, kilns, simmering pots, lamps and the clay news.
public static partial class Subscriptions
{
    [SubscriptionQueries]
    static IEnumerable<string> CeramicsQueries(string range, string farRange) => new[]
    {
        $"SELECT * FROM clay_piece WHERE {range}",
        $"SELECT * FROM kiln WHERE {range}",
        $"SELECT * FROM simmer_pot WHERE {range}",
        $"SELECT * FROM lamp WHERE {range}",
        "SELECT * FROM clay_news",
    };
}
