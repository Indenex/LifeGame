using System.Linq;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// Which character this connection plays (survival-24). Everything the server stores for a
// player (inventory, claims, structures, ...) is owned by the character's key, which is
// not conn.Identity once a login has more than one character. Use Me.Key(conn) wherever the
// code used to compare with conn.Identity ("is this mine"). Plain C#, no Godot, so the bot
// checks in tools/ use it too.
public static class Me
{
    // The key of the character you play: the active one, or your login until the server
    // has said (a login's first character is keyed by the login itself).
    public static Identity? Key(DbConnection? c)
    {
        if (c?.Identity is not { } login) return null;
        foreach (var a in c.Db.MyActiveCharacter.Iter()) return a.Key;
        return login;
    }

    // Your character's Player row, online or not (null before the server has sent it).
    public static Player? Player(DbConnection? c) => c?.Db.MyPlayer.Iter().FirstOrDefault();

    // Your character's player id, 0 before the server has sent it.
    public static uint PlayerId(DbConnection? c) => Player(c)?.PlayerId ?? 0;

    // True once your character stands in the world; false at the character menu.
    public static bool InWorld(DbConnection? c) => Player(c) is { Online: true };

    // Your characters, in the order they were made.
    public static Character[] Characters(DbConnection? c) =>
        c == null ? System.Array.Empty<Character>() : c.Db.MyCharacters.Iter().OrderBy(x => x.PlayerId).ToArray();
}
