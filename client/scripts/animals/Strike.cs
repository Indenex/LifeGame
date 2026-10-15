using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// What a strike (left click / Z, D-039) would hit right now, offered by one lane:
// the target's distance and what to call on the server. The nearest offer wins.
public sealed record StrikeOffer(float Distance, Action Strike);

// The shared registry behind the strike key, like Use is for G. Animals offers the
// nearest animal; other lanes (e.g. Claims, for players) register their own targets.
// Find gets the player's position and facing direction (flat, unit length) and
// returns null when that lane has nothing in reach. Herd reads the input, checks a
// weapon is carried and the key isn't blocked, then runs the nearest offer.
//
//     Strike.Register("claims.player", (at, facing) =>
//         NearestPlayerInFront(at, facing, 1.6f) is { } p
//             ? new StrikeOffer(Distance(at, p), () => conn.Reducers.StrikePlayer(p.PlayerId))
//             : null);
public static class Strike
{
    static readonly Dictionary<string, Func<Vector3, Vector3, StrikeOffer?>> finders = new();

    // Registering the same id again replaces the old finder.
    public static void Register(string id, Func<Vector3, Vector3, StrikeOffer?> find) => finders[id] = find;

    public static void Unregister(string id) => finders.Remove(id);

    public static StrikeOffer? Best(Vector3 at, Vector3 facing)
    {
        StrikeOffer? best = null;
        foreach (var find in finders.Values)
            if (find(at, facing) is { } offer && (best == null || offer.Distance < best.Distance))
                best = offer;
        return best;
    }
}
