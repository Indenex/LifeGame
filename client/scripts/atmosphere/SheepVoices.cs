using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Decides when the sheep nearby bleat. Grazing and wandering sheep bleat softly
// now and then; when a flock bolts, one startled bleat goes up for the flock
// rather than one per sheep. Knows nothing about tables: the caller reports
// each sheep's position, whether it is running, and whether it is a lamb.
public class SheepVoices
{
    const float Hearing = 35f;      // metres; quieter with distance, silent beyond
    const float FlockRadius = 12f;  // a bolt within this of a recent one is the same flock
    const double StartleQuiet = 2.5; // seconds before that flock can startle again

    readonly record struct Sheep(Vector2 Position, bool Running, bool Lamb, double NextBleat);

    readonly Dictionary<ulong, Sheep> sheep = new();
    readonly List<(Vector2 Position, double At)> startles = new();
    readonly List<ulong> due = new();
    readonly Random random = new();
    double now;
    Vector3 lastListener, lastRight = Vector3.Right;

    public Action<float, float, bool, bool>? Bleat; // pan, gain, startled, lamb

    public void Seen(ulong id, float x, float z, bool running, bool lamb = false)
    {
        var position = new Vector2(x, z);
        if (sheep.TryGetValue(id, out var was))
        {
            if (running && !was.Running) Startle(position, lamb);
            sheep[id] = was with { Position = position, Running = running, Lamb = lamb };
        }
        else
            sheep[id] = new Sheep(position, running, lamb, now + 3 + random.NextDouble() * 15);
    }

    public void Gone(ulong id) => sheep.Remove(id);

    // Call once a frame with where the listener is and which way is right.
    public void Tick(double delta, Vector3 listener, Vector3 right)
    {
        now += delta;
        startles.RemoveAll(s => now - s.At > StartleQuiet);
        due.Clear();
        foreach (var (id, s) in sheep)
            if (now >= s.NextBleat) due.Add(id);
        foreach (var id in due)
        {
            var s = sheep[id];
            // Each calm sheep bleats every 15 to 45 seconds, so a flock is heard
            // every few seconds; running sheep are busy fleeing.
            sheep[id] = s with { NextBleat = now + 15 + random.NextDouble() * 30 };
            if (!s.Running) Play(s.Position, listener, right, 0.55f, false, s.Lamb);
        }
        lastListener = listener;
        lastRight = right;
    }

    void Startle(Vector2 position, bool lamb)
    {
        foreach (var s in startles)
            if (s.Position.DistanceTo(position) < FlockRadius) return;
        startles.Add((position, now));
        Play(position, lastListener, lastRight, 1f, true, lamb);
    }

    void Play(Vector2 position, Vector3 listener, Vector3 right, float loudness, bool startled, bool lamb)
    {
        var offset = new Vector3(position.X - listener.X, 0, position.Y - listener.Z);
        float distance = offset.Length();
        if (distance > Hearing) return;
        float gain = loudness * Mathf.Pow(1f - distance / Hearing, 1.6f);
        float pan = distance < 0.5f ? 0f : Mathf.Clamp(offset.Normalized().Dot(new Vector3(right.X, 0, right.Z).Normalized()), -1f, 1f) * 0.8f;
        Bleat?.Invoke(pan, gain, startled, lamb);
    }
}
