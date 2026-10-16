using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Chat commands for admins that give things (admin-1): /give opens the item menu;
// /give <item words> [n] gives straight away. The server checks the admin (admin_give).
public static class AdminCommands
{
    // The quality made things come at when the command doesn't say.
    const byte DefaultQuality = 80;

    public static AdminPanel? Menu;

    // Handles the text if it is an admin item command; returns false to let chat go on.
    public static bool Try(DbConnection conn, string text, Action<string> note)
    {
        string t = text.Trim();
        string lower = t.ToLowerInvariant();
        if (lower != "/give" && !lower.StartsWith("/give ")) return false;
        if (!TimeCommands.IsAdmin(conn))
        {
            note("Only the world's admin can give items");
            return true;
        }
        string args = t.Length > 5 ? t[5..].Trim() : "";
        if (args.Length == 0)
        {
            Menu?.Show();
            return true;
        }
        // "stone knife 3": the last word is the count when it is a number.
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        uint count = 1;
        if (words.Count > 1 && uint.TryParse(words[^1], out uint n)) { count = n; words.RemoveAt(words.Count - 1); }
        if (count < 1 || count > AdminRules.GiveMax)
        {
            note($"Give between 1 and {AdminRules.GiveMax}");
            return true;
        }
        string name = string.Join(' ', words);
        var matches = Find(name);
        if (matches.Count == 0)
        {
            note($"No item called \"{name}\". /give alone opens the list");
            return true;
        }
        if (matches.Count > 1)
        {
            Menu?.Show(name);
            note($"{matches.Count} items match \"{name}\": " + string.Join(", ", matches.Take(5).Select(d => d.Name)) + (matches.Count > 5 ? "..." : "") + ". Pick one in the list");
            return true;
        }
        var def = matches[0];
        conn.Reducers.AdminGive((ushort)def.Id, count, DefaultQuality);
        note($"Gave yourself {count} x {(count == 1 ? def.Name : def.Plural)}");
        return true;
    }

    // An exact name (or plural) wins; otherwise every item whose name contains the words.
    public static System.Collections.Generic.List<ItemDef> Find(string name)
    {
        var all = Items.All.Values;
        var exact = all.Where(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || d.Plural.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count > 0) return exact;
        return all.Where(d => d.Name.Contains(name, StringComparison.OrdinalIgnoreCase) || d.Plural.Contains(name, StringComparison.OrdinalIgnoreCase)).OrderBy(d => d.Name).ToList();
    }
}
