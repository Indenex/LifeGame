using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Decides when the red deer nearby make a sound (audio-4). Deer are quiet: a deer
// that starts to run barks once for its herd (not once per deer), running deer
// thud softly, and at dusk in autumn the stags roar now and then, heard from
// far off. Knows nothing about tables: the caller reports each deer's position,
// whether it is running and whether it is a stag, and how much roaring weather it
// is (RoarTime, 0 to 1).
public class DeerVoices
{
    const float BarkHearing = 60f;    // metres
    const float RoarHearing = 160f;
    const float HoofHearing = 25f;
    const float HerdRadius = 15f;     // a bolt within this of a recent one is the same herd
    const double BarkQuiet = 4.0;     // seconds before that herd can bark again
    const double HoofEvery = 0.32;    // seconds between a running deer's thuds

    readonly record struct Deer(Vector2 Position, bool Running, bool Stag, double NextRoar, double NextHoof);

    readonly Dictionary<ulong, Deer> deer = new();
    readonly List<(Vector2 Position, double At)> barks = new();
    readonly List<ulong> due = new();
    readonly Random random = new();
    double now;
    Vector3 lastListener, lastRight = Vector3.Right;

    // call, pan, gain, far
    public Action<SoundMix.DeerCall, float, float, float>? Cry;

    // Roaring weather, 0 to 1: Atmosphere sets it to the dusk of an autumn day.
    public float RoarTime;

    public void Seen(ulong id, float x, float z, bool running, bool stag)
    {
        var position = new Vector2(x, z);
        if (deer.TryGetValue(id, out var was))
        {
            if (running && !was.Running) Bark(position);
            deer[id] = was with { Position = position, Running = running, Stag = stag };
        }
        else
            deer[id] = new Deer(position, running, stag, now + 10 + random.NextDouble() * 60, now);
    }

    public void Gone(ulong id) => deer.Remove(id);

    // Call once a frame with where the listener is and which way is right.
    public void Tick(double delta, Vector3 listener, Vector3 right)
    {
        now += delta;
        barks.RemoveAll(b => now - b.At > BarkQuiet);
        due.Clear();
        foreach (var (id, d) in deer)
            if ((d.Running && now >= d.NextHoof) || (d.Stag && now >= d.NextRoar)) due.Add(id);
        foreach (var id in due)
        {
            var d = deer[id];
            if (d.Running && now >= d.NextHoof)
            {
                d = d with { NextHoof = now + HoofEvery * (0.8 + random.NextDouble() * 0.4) };
                Play(SoundMix.DeerCall.Hoof, d.Position, listener, right, HoofHearing, 0.7f);
            }
            if (d.Stag && now >= d.NextRoar)
            {
                // A stag roars every 40 to 100 seconds while it is roaring weather;
                // out of season the timer just runs on silently.
                d = d with { NextRoar = now + 40 + random.NextDouble() * 60 };
                if (RoarTime > 0.05f && !d.Running && random.NextDouble() < RoarTime)
                    Play(SoundMix.DeerCall.Roar, d.Position, listener, right, RoarHearing, 1f);
            }
            deer[id] = d;
        }
        lastListener = listener;
        lastRight = right;
    }

    void Bark(Vector2 position)
    {
        foreach (var b in barks)
            if (b.Position.DistanceTo(position) < HerdRadius) return;
        barks.Add((position, now));
        Play(SoundMix.DeerCall.Bark, position, lastListener, lastRight, BarkHearing, 1f);
    }

    void Play(SoundMix.DeerCall call, Vector2 position, Vector3 listener, Vector3 right, float hearing, float loudness)
    {
        var offset = new Vector3(position.X - listener.X, 0, position.Y - listener.Z);
        float distance = offset.Length();
        if (distance > hearing) return;
        float near = 1f - distance / hearing;
        float gain = loudness * Mathf.Pow(near, 1.4f);
        float pan = distance < 0.5f ? 0f : Mathf.Clamp(offset.Normalized().Dot(new Vector3(right.X, 0, right.Z).Normalized()), -1f, 1f) * 0.8f;
        Cry?.Invoke(call, pan, gain, 1f - near);
    }
}
