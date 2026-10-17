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

    // A floor, not an exact count: the number of schedulers when this check was added. Reflection can
    // find too few (assembly trimming, load order), which would silence sounds without any error.
    // Never lower it; adding a scheduler needs no change here.
    const int AtLeast = 10;

    // The schedulers found, for the startup log line and for tests.
    public static IReadOnlyList<string> Found => sounds.Value.Select(x => x.Name).ToArray();

    public static void Connect(DbConnection conn)
    {
        var found = Found;
        if (found.Count == 0)
            GD.PushError("Audio: no [AudioSounds] schedulers found, so no sound will be hooked up (reflection failed?)");
        else if (found.Count < AtLeast)
            GD.PushError($"Audio: only {found.Count} [AudioSounds] schedulers found, expected at least {AtLeast}: {string.Join(", ", found)}");
        GD.Print($"Audio: {found.Count} sound schedulers hooked up: {string.Join(", ", found)}");
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
