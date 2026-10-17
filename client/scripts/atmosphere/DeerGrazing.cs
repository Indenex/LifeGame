using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Deer grazing heard (audio-30): while a deer is in the Graze state it tears and chews grass now
// and then, and lifts its head with a snort every so often. Quiet and close; the animals' daily
// rhythm (dawn and dusk grazing) decides when, from the animal's state. Reads the public animal
// table in its own subscription, so it adds nothing to Atmosphere.
[AudioSounds]
static class DeerGrazing
{
    const float CrunchHearing = 10f;   // metres
    const float SnortHearing = 22f;

    sealed class Grazer
    {
        public Vector3 Where;
        public bool Grazing;
        public double NextCrunch, NextSnort;
    }

    static readonly Dictionary<ulong, Grazer> deer = new();
    static readonly Random random = new();
    static double now;

    public static void Connect(DbConnection c)
    {
        c.Db.Animal.OnInsert += (_, a) => Seen(a);
        c.Db.Animal.OnUpdate += (_, _, a) => Seen(a);
        c.Db.Animal.OnDelete += (_, a) => deer.Remove(a.Id);
    }

    static void Seen(Animal a)
    {
        if (AnimalDefs.Get((byte)a.Species).Voice != "deer") return;
        if (!deer.TryGetValue(a.Id, out var d))
            deer[a.Id] = d = new Grazer { NextCrunch = now + 1 + random.NextDouble() * 2, NextSnort = now + 15 + random.NextDouble() * 40 };
        d.Where = new Vector3(a.X, 0.4f, a.Z);
        d.Grazing = a.State == AnimalState.Graze;
    }

    // Each frame from AudioHooks.
    public static void Tick(double dt)
    {
        now += dt;
        foreach (var d in deer.Values)
        {
            if (!d.Grazing) continue;
            if (now >= d.NextCrunch)
            {
                d.NextCrunch = now + 1.4 + random.NextDouble() * 2.6;
                Sfx.PlayAnimal(CueDefs.DeerCrunch, d.Where, CrunchHearing, 0.8f + 0.4f * (float)random.NextDouble());
            }
            if (now >= d.NextSnort)
            {
                d.NextSnort = now + 25 + random.NextDouble() * 40;
                Sfx.PlayAnimal(CueDefs.DeerSnort, d.Where, SnortHearing);
            }
        }
    }
}
