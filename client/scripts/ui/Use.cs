using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// What G would do right now, offered by one lane's handler. Prompt is the verb shown
// next to the key ("Pick berries"). Repeat > 0 makes it a hold action that runs again
// every Repeat seconds while G stays down (adding materials to a blueprint).
// Target, when given, is where the thing is, so the one you face can win (see Best).
public sealed record UseOffer(string Prompt, float Distance, Action Use, float Repeat = 0f, Vector3? Target = null);

// A lane's Use action. Find looks at the player's position and returns an offer, or
// null when there is nothing to do. When several lanes offer, a thing you face wins
// (see Use.Best), then the highest Priority, then the nearest.
public sealed class UseHandler
{
    public required string Id { get; init; }
    public int Priority { get; init; }
    public required Func<Vector3, UseOffer?> Find { get; init; }
}

// The shared registry behind the G key (D-025). Lanes register from their own files:
//
//     Use.Register(new UseHandler
//     {
//         Id = "survival.forage",
//         Priority = 10,
//         Find = at => NearestRipeBush() is { } bush
//             ? new UseOffer("Pick berries", Distance(at, bush), () => conn.Reducers.Forage(bush.Id))
//             : null,
//     });
public static class Use
{
    static readonly Dictionary<string, UseHandler> handlers = new();

    // Registering the same Id again replaces the old handler.
    public static void Register(UseHandler handler) => handlers[handler.Id] = handler;

    public static void Unregister(string id) => handlers.Remove(id);

    // Within this angle of the way you face, a thing counts as in front of you.
    const float FrontAngle = 60f;

    // G uses what's in front of you: an offer with a Target you face beats one with no
    // Target, which beats one with a Target off to the side or behind; within that, the
    // highest Priority wins, then the nearest. Facing is the player's flat forward
    // direction (zero ignores facing).
    public static UseOffer? Best(Vector3 at, Vector3 facing = default)
    {
        UseOffer? best = null;
        (int tier, int priority) bestRank = (int.MinValue, int.MinValue);
        foreach (var handler in handlers.Values)
        {
            if (handler.Find(at) is not { } offer) continue;
            var rank = (Tier(offer, at, facing), handler.Priority);
            if (rank.CompareTo(bestRank) > 0 || (rank == bestRank && offer.Distance < best!.Distance))
            {
                best = offer;
                bestRank = rank;
            }
        }
        return best;
    }

    static int Tier(UseOffer offer, Vector3 at, Vector3 facing)
    {
        if (offer.Target is not { } target || facing == Vector3.Zero) return 1;
        var to = new Vector3(target.X - at.X, 0, target.Z - at.Z);
        if (to.LengthSquared() < 0.04f) return 2;
        var forward = new Vector3(facing.X, 0, facing.Z).Normalized();
        return Mathf.RadToDeg(forward.AngleTo(to)) <= FrontAngle ? 2 : 0;
    }
}
