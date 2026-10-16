using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// Chat commands for the world's admins: /day, /night and /time 0 to 1, /season
// (a name or 0 to 1) and /season-speed (in-game days per season). The server moves
// the clock; every client glides to the new time over a second or two.
public static class TimeCommands
{
    const float Morning = 0.35f, Night = 0.92f;

    public static readonly (string Keys, string Help)[] Help =
    {
        ("/day", "Admin: set the time to mid-morning"),
        ("/night", "Admin: set the time to night"),
        ("/time 0-1", "Admin: set the time (0.25 sunrise, 0.5 noon, 0.75 sunset)"),
        ("/season name", "Admin: jump to spring, summer, autumn or winter (or 0-1 through the year)"),
        ("/season-speed days", "Admin: in-game days each season lasts"),
    };

    // Handles the text if it is a time command; returns false to let chat send it.
    public static bool Try(DbConnection conn, string text, Action<string> note)
    {
        string t = text.Trim().ToLowerInvariant();
        if (t == "/season-speed" || t.StartsWith("/season-speed ")) return SeasonSpeed(conn, t, note);
        if (t == "/season" || t.StartsWith("/season ")) return Season(conn, t, note);
        float? to = null;
        if (t == "/day") to = Morning;
        else if (t == "/night") to = Night;
        else if (t == "/time" || t.StartsWith("/time "))
        {
            if (!float.TryParse(t.Length > 6 ? t[6..] : "", NumberStyles.Float, CultureInfo.InvariantCulture, out float f) || f < 0f || f > 1f)
            {
                note("Use /time with 0 to 1: 0 midnight, 0.25 sunrise, 0.5 noon, 0.75 sunset");
                return true;
            }
            to = f;
        }
        if (to is not { } value) return false;
        if (!IsAdmin(conn))
        {
            note("Only the world's admin can change the time");
            return true;
        }
        conn.Reducers.SetTimeOfDay(value);
        note(value == Morning ? "Morning comes" : value == Night ? "Night falls" : "The time changes");
        return true;
    }

    static bool Season(DbConnection conn, string t, Action<string> note)
    {
        string arg = t.Length > 8 ? t[8..].Trim() : "";
        float? year = arg switch
        {
            "spring" => LifeGame.Shared.Seasons.Middle(LifeGame.Shared.Season.Spring),
            "summer" => LifeGame.Shared.Seasons.Middle(LifeGame.Shared.Season.Summer),
            "autumn" or "fall" => LifeGame.Shared.Seasons.Middle(LifeGame.Shared.Season.Autumn),
            "winter" => LifeGame.Shared.Seasons.Middle(LifeGame.Shared.Season.Winter),
            _ => float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) && f >= 0f && f <= 1f ? f : null,
        };
        if (year is not { } y)
        {
            note("Use /season spring, summer, autumn or winter, or 0 to 1 through the year");
            return true;
        }
        if (!IsAdmin(conn))
        {
            note("Only the world's admin can change the season");
            return true;
        }
        conn.Reducers.SetSeason(y);
        note($"It turns {LifeGame.Shared.Seasons.Name(LifeGame.Shared.Seasons.SeasonOf(y))}");
        return true;
    }

    static bool SeasonSpeed(DbConnection conn, string t, Action<string> note)
    {
        string arg = t.Length > 14 ? t[14..].Trim() : "";
        if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float days) || days < 0.05f || days > 365f)
        {
            note("Use /season-speed with in-game days per season, 0.05 to 365 (1 is a day per season)");
            return true;
        }
        if (!IsAdmin(conn))
        {
            note("Only the world's admin can change the length of the seasons");
            return true;
        }
        conn.Reducers.SetDaysPerSeason(days);
        note($"Each season now lasts {days.ToString(CultureInfo.InvariantCulture)} in-game day{(days == 1f ? "" : "s")}");
        return true;
    }

    public static bool IsAdmin(DbConnection? conn) =>
        conn != null && conn.Db.MyAdmin.Iter().Any();

    public static IEnumerable<(string Keys, string Help)> HelpFor(DbConnection? conn) =>
        IsAdmin(conn) ? Help : Array.Empty<(string, string)>();
}
