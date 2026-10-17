using System.Collections.Generic;

namespace LifeGame;

// Atmosphere: smoke signals seen from afar. The clock, weather and seasons have their own subscription
// (Subscriptions.Atmosphere, the string array in Subscriptions.cs).
public static partial class Subscriptions
{
    [SubscriptionQueries]
    static IEnumerable<string> AtmosphereQueries(string range, string farRange) => new[]
    {
        // World-wide, a tiny table: only fires stoked past 75% of their cap (SignalFire).
        "SELECT * FROM signal_fire",
    };
}
