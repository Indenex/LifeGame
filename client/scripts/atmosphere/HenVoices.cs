using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Decides when the hens nearby make a sound (audio-13). Hens are small and soft: an idle hen
// clucks now and then, a pecking hen ticks at the ground and clucks between pecks, a hen that
// bolts squawks once with a flap, and a hen roosting at night murmurs sleepily. Knows nothing
// about tables: the caller reports each hen's position and what it is doing (Mood).
// Voices are CueDefs (hen.* in CueDefs.cs) played where the hen is (Sfx.PlayAnimal).
public class HenVoices
{
    public enum Mood { Idle, Peck, Walk, Run, Roost }

    const float CluckHearing = 18f;   // metres
    const float PeckHearing = 8f;
    const float SquawkHearing = 30f;
    const float RoostHearing = 10f;
    const double SquawkQuiet = 3.0;   // seconds before the same hen can squawk again

    sealed class Hen
    {
        public Vector2 Position;
        public Mood Mood;
        public double NextPeck, NextCluck, NextMurmur, SquawkedAt = -100;
    }

    readonly Dictionary<ulong, Hen> hens = new();
    readonly Random random = new();
    double now;

    // How much it is dusk or night, 0 to 1: roosting hens murmur louder as it gets darker.
    public float NightTime;

    public void Seen(ulong id, float x, float z, Mood mood)
    {
        var position = new Vector2(x, z);
        if (!hens.TryGetValue(id, out var hen))
        {
            hens[id] = new Hen
            {
                Position = position, Mood = mood,
                NextPeck = now + random.NextDouble(), NextCluck = now + 3 + random.NextDouble() * 15, NextMurmur = now + 2 + random.NextDouble() * 10,
            };
            return;
        }
        if (mood == Mood.Run && hen.Mood != Mood.Run && now - hen.SquawkedAt > SquawkQuiet)
        {
            hen.SquawkedAt = now;
            Play(CueDefs.HenSquawk, position, SquawkHearing, 1f);
        }
        hen.Position = position;
        hen.Mood = mood;
    }

    public void Gone(ulong id) => hens.Remove(id);

    // Call once a frame.
    public void Tick(double delta)
    {
        now += delta;
        foreach (var hen in hens.Values)
        {
            switch (hen.Mood)
            {
                case Mood.Peck:
                    if (now >= hen.NextPeck)
                    {
                        hen.NextPeck = now + 0.5 + random.NextDouble() * 1.1;
                        Play(CueDefs.HenPeck, hen.Position, PeckHearing, 0.8f + 0.4f * (float)random.NextDouble());
                    }
                    if (now >= hen.NextCluck)
                    {
                        hen.NextCluck = now + 6 + random.NextDouble() * 14;
                        Play(CueDefs.HenCluck, hen.Position, CluckHearing, 1f);
                    }
                    break;
                case Mood.Idle:
                case Mood.Walk:
                    if (now >= hen.NextCluck)
                    {
                        // Idle hens cluck more often than walking ones; sometimes a run of three.
                        hen.NextCluck = now + (hen.Mood == Mood.Idle ? 8 : 14) + random.NextDouble() * 17;
                        Play(random.NextDouble() < 0.35 ? CueDefs.HenClucks : CueDefs.HenCluck, hen.Position, CluckHearing, 1f);
                    }
                    break;
                case Mood.Roost:
                    // Murmurs only once it is getting dark; louder deep into the evening.
                    if (now >= hen.NextMurmur)
                    {
                        hen.NextMurmur = now + 10 + random.NextDouble() * 25;
                        if (NightTime > 0.1f) Play(CueDefs.HenRoost, hen.Position, RoostHearing, 0.5f + 0.5f * NightTime);
                    }
                    break;
            }
        }
    }

    static void Play(CueDef def, Vector2 position, float hearing, float loudness) =>
        Sfx.PlayAnimal(def, new Vector3(position.X, 0f, position.Y), hearing, loudness);
}
