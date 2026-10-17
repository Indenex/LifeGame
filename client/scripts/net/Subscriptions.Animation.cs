using System.Collections.Generic;

namespace LifeGame;

// Animation: what other players are doing (sneaking, looks, jumps, acts).
public static partial class Subscriptions
{
    [SubscriptionQueries]
    static IEnumerable<string> AnimationQueries(string range, string farRange) => new[]
    {
        "SELECT * FROM player_sneaking",
        "SELECT * FROM player_look",
        "SELECT * FROM player_jump",
        "SELECT * FROM player_act",
    };
}
