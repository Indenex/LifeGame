using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// What G would do right now, offered by one lane's handler. Prompt is the verb shown
// next to the key ("Pick berries"). Repeat > 0 makes it a hold action that runs again
// every Repeat seconds while G stays down (adding materials to a blueprint).
// Target, when given, is where the thing is, so the one you face can win (see Best).
// Again > 0 marks an action you can repeat by holding the key: after a short delay it runs
// again each time the last one is done and at least Again seconds have passed, for as long
// as the same offer stays valid (HoldRepeat); the server still checks every one.
public sealed record UseOffer(string Prompt, float Distance, Action Use, float Repeat = 0f, Vector3? Target = null, float Again = 0f);

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

    // The offer the G prompt shows this frame (set by UseKey), so a lane can tell
    // whether its own offer won (bugfix-19).
    public static UseOffer? Shown { get; set; }

    // Whether the last action is still going (a craft job or minigame), so a held key waits
    // for it rather than starting another. Set by Main.
    public static Func<bool> Busy { get; set; } = () => false;

    // When a server reducer last failed (Godot ticks, ms); a held key stops on it (HoldRepeat).
    public static ulong FailedAtMsec { get; private set; }
    public static void Failed() => FailedAtMsec = Time.GetTicksMsec();

    // Least time between held actions that don't set their own, a little over the server's short cooldowns.
    public const float HoldInterval = 0.7f;

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

    // The other things G could do right now, best first (everything Best passed over), so the prompt can
    // show a second line ("also: Chop tree") when two things compete, as loot and a tree do.
    public static List<UseOffer> Others(Vector3 at, Vector3 facing, UseOffer? best)
    {
        var ranked = new List<(UseOffer Offer, int Tier, int Priority)>();
        foreach (var handler in handlers.Values)
            if (handler.Find(at) is { } offer && !ReferenceEquals(offer, best) && (best == null || offer.Prompt != best.Prompt))
                ranked.Add((offer, Tier(offer, at, facing), handler.Priority));
        ranked.Sort((x, y) =>
        {
            int c = y.Tier.CompareTo(x.Tier);
            if (c == 0) c = y.Priority.CompareTo(x.Priority);
            return c != 0 ? c : x.Offer.Distance.CompareTo(y.Offer.Distance);
        });
        return ranked.ConvertAll(r => r.Offer);
    }

    // Hints: a quiet line for when G has nothing to do but there is something to learn ("Equip an axe to
    // chop"). A handler returns the text or null. The dispatcher shows each text now and then, not every
    // time you look: at most once per HintEvery seconds, for HintShown seconds.
    static readonly Dictionary<string, Func<Vector3, string?>> hints = new();
    static readonly Dictionary<string, double> hintNextAt = new();
    public const float HintEvery = 30f, HintShown = 4f;

    public static void RegisterHint(string id, Func<Vector3, string?> find) => hints[id] = find;

    public static void UnregisterHint(string id) => hints.Remove(id);

    // The hint to show now, or null. `now` is seconds on any steady clock.
    public static string? Hint(Vector3 at, double now)
    {
        foreach (var find in hints.Values)
        {
            if (find(at) is not { } text) continue;
            if (hintNextAt.TryGetValue(text, out double next) && now < next) continue;
            hintNextAt[text] = now + HintEvery;
            return text;
        }
        return null;
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
