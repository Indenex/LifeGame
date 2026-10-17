using System;
using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// Clay oil lamps heard (audio-26): a lit lamp breathes a faint flutter now and then and ticks
// as the wick burns, only audible close by (cosy, not a chore). The wick catching and the flame
// going out each make a soft sound. Knows the lamps from the public lamp table: lit while
// BurnsUntil is in the future.
[AudioSounds]
static class LampSounds
{
    const float Hearing = 9f;   // metres

    sealed class State
    {
        public Vector3 Where;
        public long BurnsUntilMicros;
        public bool WasLit;
        public double NextFlutter, NextTick;
    }

    static readonly Dictionary<ulong, State> lamps = new();
    static readonly Random random = new();
    static double clock;

    static long NowMicros => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

    public static void Connect(DbConnection c)
    {
        c.Db.Lamp.OnInsert += (ctx, l) => Set(l, announce: ctx.Event is SpacetimeDB.Event<Reducer>.Reducer);
        c.Db.Lamp.OnUpdate += (ctx, _, l) => Set(l, announce: ctx.Event is SpacetimeDB.Event<Reducer>.Reducer);
        c.Db.Lamp.OnDelete += (_, l) => lamps.Remove(l.Id);
    }

    static void Set(Lamp l, bool announce)
    {
        long until = l.BurnsUntil.MicrosecondsSinceUnixEpoch;
        bool lit = until > NowMicros;
        if (!lamps.TryGetValue(l.Id, out var state))
        {
            state = new State
            {
                Where = new Vector3(l.X, 0.25f, l.Z), WasLit = false,
                NextFlutter = clock + 1 + random.NextDouble() * 4, NextTick = clock + 2 + random.NextDouble() * 6,
            };
            lamps[l.Id] = state;
        }
        state.Where = new Vector3(l.X, 0.25f, l.Z);
        state.BurnsUntilMicros = until;
        // Lit by someone just now (a reducer, not a lamp arriving with the chunk): the wick catches.
        if (announce && lit && !state.WasLit) Play(CueDefs.LampLight, state.Where, 1f);
        state.WasLit = lit;
    }

    // Each frame from AudioHooks.
    public static void Tick(double dt)
    {
        clock += dt;
        if (lamps.Count == 0) return;
        long now = NowMicros;
        foreach (var lamp in lamps.Values)
        {
            bool lit = lamp.BurnsUntilMicros > now;
            if (lamp.WasLit && !lit) Play(CueDefs.LampOut, lamp.Where, 1f);
            lamp.WasLit = lit;
            if (!lit) continue;
            if (clock >= lamp.NextFlutter)
            {
                lamp.NextFlutter = clock + 3 + random.NextDouble() * 5;
                Play(CueDefs.LampFlutter, lamp.Where, 0.7f + 0.6f * (float)random.NextDouble());
            }
            if (clock >= lamp.NextTick)
            {
                lamp.NextTick = clock + 5 + random.NextDouble() * 9;
                Play(CueDefs.LampTick, lamp.Where, 0.8f + 0.4f * (float)random.NextDouble());
            }
        }
    }

    static void Play(CueDef def, Vector3 where, float loudness) => Sfx.PlayAnimal(def, where, Hearing, loudness);
}
