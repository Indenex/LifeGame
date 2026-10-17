using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LifeGame;

// The queries the client subscribes to. Plain C# (no Godot) so the bot checks in
// tools/ run exactly what the game runs.
public static partial class Subscriptions
{
    // Your own player row and position, wherever you are. Subscribed first and kept
    // for the whole session, so a player saved far from the origin, or woken on the
    // rim after dying, always gets their own row and the area can follow it.
    public static readonly string[] Self =
    {
        "SELECT * FROM player WHERE identity = :sender",
        "SELECT player_pos.* FROM player_pos JOIN player ON player_pos.player_id = player.player_id WHERE player.identity = :sender",
    };

    // How far, in chunks, the slow-changing world state is subscribed: dug cells, felled trees
    // and buildings. The live things (players, animals, items, fires) stay at
    // LifeGame.Shared.World.ViewRadiusChunks. Environment's view distance sets this so far chunks draw as they
    // really are; it never goes below the near radius or above FarRadiusMax.
    public const int FarRadiusMax = 10;
    static int farRadius = LifeGame.Shared.World.ViewRadiusChunks;
    public static int FarRadius
    {
        get => farRadius;
        set => farRadius = System.Math.Clamp(value, LifeGame.Shared.World.ViewRadiusChunks, FarRadiusMax);
    }

    public static string Range(int chunkX, int chunkZ, int r) =>
        $"chunk_x >= {chunkX - r} AND chunk_x <= {chunkX + r} AND chunk_z >= {chunkZ - r} AND chunk_z <= {chunkZ + r}";

    // Everything around one chunk, plus the per-player tables: the core queries below, then each
    // lane's own (Subscriptions.<Lane>.cs, found by [SubscriptionQueries]).
    public static string[] Area(string range) => Area(range, range);

    // `range` for the live things; `farRange` (a wider Range) for the slow-changing world state.
    public static string[] Area(string range, string farRange) => Core(range, farRange).Concat(Lanes(range, farRange)).ToArray();

    // The core of the world: who is here, the ground, trees and buildings. Lanes do not add to this list;
    // they add a method marked [SubscriptionQueries] in their own file (docs/CONTENT.md).
    static IEnumerable<string> Core(string range, string farRange) => new[]
    {
        "SELECT * FROM player WHERE online = true",
        $"SELECT * FROM player_pos WHERE {range}",
        $"SELECT * FROM terrain_edit WHERE {farRange}",
        $"SELECT * FROM chopped_tree WHERE {farRange}",
        "SELECT * FROM player_mark",
        "SELECT * FROM my_admin",
        $"SELECT * FROM structure WHERE {farRange}",
    };

    // Marks a static method of Subscriptions, `IEnumerable<string> Name(string range, string farRange)`,
    // whose queries Area adds. `range` is for the live things, `farRange` for the slow-changing world state.
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubscriptionQueriesAttribute : Attribute { }

    static readonly Func<string, string, IEnumerable<string>>[] lanes = typeof(Subscriptions)
        .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(m => m.GetCustomAttribute<SubscriptionQueriesAttribute>() != null)
        .OrderBy(m => m.Name, StringComparer.Ordinal)
        .Select(m => (Func<string, string, IEnumerable<string>>)Delegate.CreateDelegate(typeof(Func<string, string, IEnumerable<string>>), m))
        .ToArray();

    static IEnumerable<string> Lanes(string range, string farRange) => lanes.SelectMany(l => l(range, farRange));
}
