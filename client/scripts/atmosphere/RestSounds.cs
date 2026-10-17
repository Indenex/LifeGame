using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// Sitting and lying down heard (audio-29): the rustle of settling and of getting up, for
// everyone nearby. Reads the public player_rest table (a row exists only while a player rests;
// Animation, anim-12). Only changes made by a reducer sound, so a player already resting when you
// arrive stays silent.
static class RestSounds
{
    const float Hearing = 14f;   // metres
    const byte Sit = 1, Lie = 2; // Module.RestSit, Module.RestLie

    public static void Connect(DbConnection c)
    {
        c.Db.PlayerRest.OnInsert += (ctx, r) => Settle(c, ctx, r);
        c.Db.PlayerRest.OnUpdate += (ctx, was, r) =>
        {
            if (was.Kind != r.Kind) Settle(c, ctx, r);
        };
        c.Db.PlayerRest.OnDelete += (ctx, r) =>
        {
            if (Where(c, ctx, r.PlayerId) is { } at) Sfx.PlayAnimal(CueDefs.RestUp, at, Hearing);
        };
    }

    static void Settle(DbConnection c, EventContext ctx, PlayerRest r)
    {
        if (Where(c, ctx, r.PlayerId) is not { } at) return;
        var def = r.Kind == Lie ? (r.SpotId != 0 ? CueDefs.RestLieMat : CueDefs.RestLie) : CueDefs.RestSit;
        Sfx.PlayAnimal(def, at, Hearing);
    }

    // Where the player is, if this change was made by a reducer (not a row arriving with a chunk).
    static Vector3? Where(DbConnection c, EventContext ctx, uint playerId)
    {
        if (ctx.Event is not SpacetimeDB.Event<Reducer>.Reducer) return null;
        return c.Db.PlayerPos.PlayerId.Find(playerId) is { } p ? new Vector3(p.X, 0.2f, p.Z) : null;
    }
}
