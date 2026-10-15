using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// What G would do right now, offered by one lane's handler. Prompt is the verb shown
// next to the key ("Pick berries"). Repeat > 0 makes it a hold action that runs again
// every Repeat seconds while G stays down (adding materials to a blueprint).
public sealed record UseOffer(string Prompt, float Distance, Action Use, float Repeat = 0f);

// A lane's Use action. Find looks at the player's position and returns an offer, or
// null when there is nothing to do. When several lanes offer, the highest Priority
// wins, then the nearest.
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

    public static UseOffer? Best(Vector3 at)
    {
        UseOffer? best = null;
        int bestPriority = int.MinValue;
        foreach (var handler in handlers.Values)
        {
            if (handler.Find(at) is not { } offer) continue;
            if (handler.Priority > bestPriority || (handler.Priority == bestPriority && offer.Distance < best!.Distance))
            {
                best = offer;
                bestPriority = handler.Priority;
            }
        }
        return best;
    }
}
