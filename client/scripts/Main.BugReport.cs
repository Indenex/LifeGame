using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The in-game bug report (bugfix-42): the panel and the game facts it puts in a report.
public partial class Main
{
    BugReportPanel? bugReport;

    void SetUpBugReport()
    {
        bugReport = new BugReportPanel { Conn = () => conn, Facts = BugFacts };
        AddChild(bugReport);
    }

    IEnumerable<string> BugFacts()
    {
        var p = me.Position;
        string build = Arg("build") ?? OS.GetEnvironment("LIFEGAME_BUILD");
        yield return $"Build: {(string.IsNullOrEmpty(build) ? "dev" : build)}";
        var player = conn?.Db.Player.PlayerId.Find(myId);
        yield return $"Player: {player?.Name ?? "?"} (id {myId})";
        yield return $"Position: {p.X:0.0}, {p.Y:0.0}, {p.Z:0.0} facing {Mathf.RadToDeg(me.Rotation.Y):0} deg";
        yield return $"Time: {atmosphere.TimeOfDay * 24f:0.0} h, {atmosphere.Season}";
        if (conn?.Db.Weather.Iter().FirstOrDefault() is { } w)
            yield return $"Weather: cloud {w.Cloud:0.00}, rain {w.Rain:0.00}, wind {w.Wind:0.00}";
        yield return $"Panel: {(Panels.Open() ? "a window is open" : "none")}";
        if (conn != null)
        {
            var bag = conn.Db.MyInventory.Iter().GroupBy(i => i.Item)
                .Select(g => $"{Items.Def((Item)g.Key).Name} x{g.Sum(i => (long)i.Count)}");
            yield return $"Bag: {string.Join(", ", bag)}";
        }
    }
}
