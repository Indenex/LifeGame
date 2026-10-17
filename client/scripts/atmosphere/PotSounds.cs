using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Pots of broth and stew simmering by fires, heard (audio-19): soft bubbles rising and popping.
// Broth is thin and busy, small bubbles and pops; stew is thick and slow, with big gloops. A pot
// that is done goes on bubbling, only gentler. Knows the pots from the public simmer_pot table.
static class PotSounds
{
    const float Hearing = 14f;   // metres
    readonly record struct Pot(Vector3 Where, bool Stew, long DoneAtMicros);

    static readonly Dictionary<ulong, Pot> pots = new();
    static readonly Random random = new();

    public static void Connect(DbConnection c)
    {
        c.Db.SimmerPot.OnInsert += (_, p) => Set(p);
        c.Db.SimmerPot.OnUpdate += (_, _, p) => Set(p);
        c.Db.SimmerPot.OnDelete += (_, p) => pots.Remove(p.Id);
    }

    static void Set(SimmerPot p)
    {
        var dish = (Item)p.Dish;
        long done = p.PutAt.MicrosecondsSinceUnixEpoch + (long)(ClayRules.SimmerSeconds(dish) * 1_000_000f);
        pots[p.Id] = new Pot(new Vector3(p.X, 0f, p.Z), dish == Item.PotOfStew, done);
    }

    // Each frame from Atmosphere.
    public static void Tick(double dt)
    {
        if (pots.Count == 0) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        foreach (var pot in pots.Values)
        {
            // Bubbles a second per pot, fewer for stew; a pot that is done settles to half.
            float rate = (pot.Stew ? 0.9f : 1.8f) * (now >= pot.DoneAtMicros ? 0.5f : 1f);
            if (random.NextDouble() >= rate * dt) continue;
            double roll = random.NextDouble();
            var def = pot.Stew
                ? (roll < 0.55 ? CueDefs.BubbleBig : roll < 0.85 ? CueDefs.BubbleSmall : CueDefs.BubblePop)
                : (roll < 0.6 ? CueDefs.BubbleSmall : roll < 0.9 ? CueDefs.BubblePop : CueDefs.BubbleBig);
            Sfx.PlayAnimal(def, pot.Where, Hearing, 0.8f + 0.4f * (float)random.NextDouble());
        }
    }
}
