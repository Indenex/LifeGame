using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Wild rabbits heard (audio-32), for Animals' D-323: a foot-thump alarm when a rabbit bolts, a
// small squeak when one is hurt, and quiet rustling while one grazes close by. Keys on the
// voice id (AnimalDef.Voice), not the model, and has no bleat. Reads the public animal table and
// the AnimalHit event in its own subscription.
[AudioSounds]
static class RabbitSounds
{
    const float ThumpHearing = 30f;      // metres
    const float SqueakHearing = 25f;
    const float RustleHearing = 7f;

    sealed class Rabbit
    {
        public Vector3 Where;
        public AnimalState State;
        public double NextRustle;
    }

    static readonly Dictionary<ulong, Rabbit> rabbits = new();
    static readonly Random random = new();
    static double now;

    public static void Connect(DbConnection c)
    {
        c.Db.Animal.OnInsert += (_, a) => Seen(a, announce: false);
        c.Db.Animal.OnUpdate += (_, _, a) => Seen(a, announce: true);
        c.Db.Animal.OnDelete += (_, a) => rabbits.Remove(a.Id);
        c.Db.AnimalHit.OnInsert += (_, h) =>
        {
            if (rabbits.ContainsKey(h.AnimalId))
                Sfx.PlayAnimal(CueDefs.RabbitSqueak, new Vector3(h.X, 0.2f, h.Z), SqueakHearing);
        };
    }

    static void Seen(Animal a, bool announce)
    {
        if (AnimalDefs.Get((byte)a.Species).Voice != "rabbit") return;
        if (!rabbits.TryGetValue(a.Id, out var r))
            rabbits[a.Id] = r = new Rabbit { State = a.State, NextRustle = now + 1 + random.NextDouble() * 3 };
        r.Where = new Vector3(a.X, 0.1f, a.Z);
        // It bolts: the thump that warns the warren.
        if (announce && a.State == AnimalState.Run && r.State != AnimalState.Run)
            Sfx.PlayAnimal(CueDefs.RabbitThump, r.Where, ThumpHearing);
        r.State = a.State;
    }

    // Each frame from AudioHooks.
    public static void Tick(double dt)
    {
        now += dt;
        foreach (var r in rabbits.Values)
        {
            if (r.State != AnimalState.Graze || now < r.NextRustle) continue;
            r.NextRustle = now + 1.5 + random.NextDouble() * 3.5;
            Sfx.PlayAnimal(CueDefs.RabbitRustle, r.Where, RustleHearing, 0.8f + 0.4f * (float)random.NextDouble());
        }
    }
}
