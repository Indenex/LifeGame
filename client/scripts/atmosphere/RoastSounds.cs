using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Meat roasting over a fire, heard (audio-21): a soft sizzle with now and then a spit of fat.
// Browning meat sizzles busily; ready meat settles to a steady sizzle with the odd drip; meat
// left until it chars spits and crackles more. Reads the public roast table, close range only.
[AudioSounds]
static class RoastSounds
{
    const float Hearing = 9f;   // metres
    readonly record struct Piece(Vector3 Where, long PutAtMicros);

    static readonly Dictionary<ulong, Piece> pieces = new();
    static readonly Random random = new();

    public static void Connect(DbConnection c)
    {
        c.Db.Roast.OnInsert += (_, r) => Set(r);
        c.Db.Roast.OnUpdate += (_, _, r) => Set(r);
        c.Db.Roast.OnDelete += (_, r) => pieces.Remove(r.Id);
    }

    static void Set(Roast r) =>
        pieces[r.Id] = new Piece(new Vector3(r.X, 0.6f, r.Z), r.PutAt.MicrosecondsSinceUnixEpoch);

    // Each frame from Atmosphere.
    public static void Tick(double dt)
    {
        if (pieces.Count == 0) return;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        foreach (var piece in pieces.Values)
        {
            float seconds = (now - piece.PutAtMicros) / 1_000_000f;
            bool burnt = Roasting.Burnt(seconds);
            bool char_ = seconds >= Roasting.BurnSeconds - 10f;
            bool ready = Roasting.Done(seconds);
            // Sizzle breaths about once a second while browning, slower once ready; spits and drips
            // pick up as it chars.
            float sizzle = burnt ? 0.5f : ready ? 0.7f : 1.1f;
            float spit = burnt ? 1.6f : char_ ? 1.1f : ready ? 0.25f : 0.6f;
            float drip = ready && !char_ ? 0.35f : 0.15f;
            if (random.NextDouble() < sizzle * dt)
                Sfx.PlayAnimal(CueDefs.RoastSizzle, piece.Where, Hearing, 0.8f + 0.4f * (float)random.NextDouble());
            if (random.NextDouble() < spit * dt)
                Sfx.PlayAnimal(CueDefs.RoastSpit, piece.Where, Hearing, 0.7f + 0.6f * (float)random.NextDouble());
            if (random.NextDouble() < drip * dt)
                Sfx.PlayAnimal(CueDefs.RoastDrip, piece.Where, Hearing, 0.8f + 0.4f * (float)random.NextDouble());
        }
    }
}
