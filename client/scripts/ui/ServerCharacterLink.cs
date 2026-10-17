using System;
using System.Collections.Generic;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The character menu's live link (ui-39) to Survival's multi-character model (survival-24):
// my_characters for the names and when each last played, my_character_looks for the
// portraits, and SelectCharacter / DeleteCharacter. The server checks every one of them.
public sealed class ServerCharacterLink : ICharacterLink
{
    readonly DbConnection conn;
    public event Action? Changed;

    public ServerCharacterLink(DbConnection conn)
    {
        this.conn = conn;
        conn.Db.MyCharacters.OnInsert += (_, _) => Changed?.Invoke();
        conn.Db.MyCharacters.OnUpdate += (_, _, _) => Changed?.Invoke();
        conn.Db.MyCharacters.OnDelete += (_, _) => Changed?.Invoke();
        conn.Db.MyCharacterLooks.OnInsert += (_, _) => Changed?.Invoke();
        conn.Db.MyCharacterLooks.OnUpdate += (_, _, _) => Changed?.Invoke();
    }

    public IReadOnlyList<CharacterCard> Characters => Me.Characters(conn).Select(c =>
    {
        var look = conn.Db.MyCharacterLooks.Iter().FirstOrDefault(l => l.PlayerId == c.PlayerId) is { } l
            ? HumanFigure.Appearance.From(l.Skin, l.Hair, l.Cut, l.Beard)
            : HumanFigure.Appearance.For(c.PlayerId);
        // Never played: LastPlayed equals Created.
        DateTime? played = c.LastPlayed.MicrosecondsSinceUnixEpoch > c.Created.MicrosecondsSinceUnixEpoch
            ? DateTimeOffset.FromUnixTimeMilliseconds(c.LastPlayed.MicrosecondsSinceUnixEpoch / 1000).LocalDateTime
            : null;
        return new CharacterCard(c.PlayerId, c.Name, "", look, played);
    }).ToList();

    public int Slots => LifeGame.Shared.Characters.MaxPerLogin;

    public void Play(ulong id)
    {
        if (Key(id) is { } key) conn.Reducers.SelectCharacter(key);
    }

    public void Delete(ulong id, string typedName)
    {
        if (Key(id) is { } key) conn.Reducers.DeleteCharacter(key, typedName);
    }

    SpacetimeDB.Identity? Key(ulong id) => Me.Characters(conn).FirstOrDefault(c => c.PlayerId == id)?.Key;
}
