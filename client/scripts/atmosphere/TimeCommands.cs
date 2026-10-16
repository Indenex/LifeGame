using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// Chat commands for the world's admins: /day, /night and /time 0 to 1. The server
// moves the clock; every client glides to the new time over a second or two.
public static class TimeCommands
{
    const float Morning = 0.35f, Night = 0.92f;

    public static readonly (string Keys, string Help)[] Help =
    {
        ("/day", "Admin: set the time to mid-morning"),
        ("/night", "Admin: set the time to night"),
        ("/time 0-1", "Admin: set the time (0.25 sunrise, 0.5 noon, 0.75 sunset)"),
    };

    // Handles the text if it is a time command; returns false to let chat send it.
    public static bool Try(DbConnection conn, string text, Action<string> note)
    {
        string t = text.Trim().ToLowerInvariant();
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

    public static bool IsAdmin(DbConnection? conn) =>
        conn?.Identity is { } me && conn.Db.WorldAdmin.Identity.Find(me) is not null;

    public static IEnumerable<(string Keys, string Help)> HelpFor(DbConnection? conn) =>
        IsAdmin(conn) ? Help : Array.Empty<(string, string)>();
}
