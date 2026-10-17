using System.Collections.Generic;

namespace LifeGame;

// Bug fixes: the in-game bug report (bugfix-42). A player reads their own reports, an admin all of them.
public static partial class Subscriptions
{
    [SubscriptionQueries]
    static IEnumerable<string> BugFixesQueries(string range, string farRange) => new[]
    {
        "SELECT * FROM my_bug_reports",
    };
}
