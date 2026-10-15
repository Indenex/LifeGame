using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Something the Use key (G) can act on right now: a bush to pick, a fire to feed,
// a site to add materials to. Prompt is the verb phrase shown after the key,
// such as "Pick berries".
public sealed record UseTarget(Vector3 At, string Prompt, Action Use);

// A lane's handler for the Use key. Each frame it offers the target it would act
// on from where the player stands, or null. Register it once with UseTargets.
public interface IUsable
{
    // Breaks near-ties between handlers: higher wins when two targets are about
    // equally close and in front of the player.
    int Priority { get; }

    UseTarget? Find(Vector3 player, float heading);
}

// The one registry behind the Use key. The key picks the nearest target in front
// of the player, so pressing G does the obvious thing and nothing else.
public static class UseTargets
{
    static readonly List<IUsable> handlers = new();

    public static void Register(IUsable handler)
    {
        if (!handlers.Contains(handler)) handlers.Add(handler);
    }

    public static void Unregister(IUsable handler) => handlers.Remove(handler);

    public static UseTarget? Best(Vector3 player, float heading)
    {
        var facing = new Vector2(Mathf.Sin(heading), Mathf.Cos(heading));
        UseTarget? best = null;
        float bestScore = float.MaxValue;
        foreach (var handler in handlers)
        {
            if (handler.Find(player, heading) is not { } target) continue;
            var to = new Vector2(target.At.X - player.X, target.At.Z - player.Z);
            float dist = to.Length();
            // Something behind you counts as twice as far as the same thing in front.
            float ahead = dist > 0.01f ? facing.Dot(to / dist) : 1f;
            float score = dist * (1.5f - 0.5f * ahead) - handler.Priority * 0.25f;
            if (score < bestScore)
            {
                best = target;
                bestScore = score;
            }
        }
        return best;
    }
}

// A simple handler from two functions, for lanes that don't need a class.
public sealed class Usable : IUsable
{
    readonly Func<Vector3, float, UseTarget?> find;

    public Usable(int priority, Func<Vector3, float, UseTarget?> find)
    {
        Priority = priority;
        this.find = find;
    }

    public int Priority { get; }

    public UseTarget? Find(Vector3 player, float heading) => find(player, heading);
}
