using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// Marks a static class of the Audio lane as a sound scheduler (audio-34). It may have
// `static void Connect(DbConnection)`, called once when the connection is made, and
// `static void Tick(double dt)`, called every frame. AudioHooks finds them by reflection,
// like [SubscriptionQueries], so a new sound is one new file and touches no shared one.
[AttributeUsage(AttributeTargets.Class)]
sealed class AudioSoundsAttribute : Attribute { }

// The Audio lane's hooks into Atmosphere: Atmosphere.cs has one Connect and one Tick line for them.
static class AudioHooks
{
    static readonly Lazy<(string Name, Action<DbConnection>? Connect, Action<double>? Tick)[]> sounds = new(() =>
        typeof(AudioHooks).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<AudioSoundsAttribute>() != null)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => (t.Name, Hook<Action<DbConnection>>(t, "Connect"), Hook<Action<double>>(t, "Tick")))
            .ToArray());

    static T? Hook<T>(Type type, string name) where T : Delegate =>
        type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is { } method
            ? (T)Delegate.CreateDelegate(typeof(T), method)
            : null;

    public static void Connect(DbConnection conn)
    {
        foreach (var (name, connect, _) in sounds.Value)
        {
            try { connect?.Invoke(conn); }
            catch (Exception e) { GD.PushError($"Audio: {name}.Connect failed: {e}"); }
        }
    }

    // A sound whose Tick throws is reported once and left out, so one fault is not logged every frame.
    static readonly HashSet<string> failed = new();

    public static void Tick(double dt)
    {
        foreach (var (name, _, tick) in sounds.Value)
        {
            if (tick == null || failed.Contains(name)) continue;
            try { tick(dt); }
            catch (Exception e)
            {
                failed.Add(name);
                GD.PushError($"Audio: {name}.Tick failed and is switched off: {e}");
            }
        }
    }
}
