using System;
using LifeGame.Shared;
using SpacetimeDB;

// Sheep and fire (animals-6): a sheep keeps clear of a burning fire. Within a few
// meters it grows uneasy and edges away, like a mild fright, and it never steps
// closer than FireKeepAway to the flames. A fire that has burned out is just ash.
public static partial class Module
{
    // No step ends this close to a burning fire (unless it moves the sheep away).
    const float FireKeepAway = 1.5f;
    // A burning fire unsettles sheep this close.
    const float FireUneasyRange = 4f;

    // True when a step from one spot to another would cross something a sheep
    // won't walk into: for now, the ring around a burning fire.
    static bool AnimalStepBlocked(ReducerContext ctx, float fromX, float fromZ, float toX, float toZ)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var (fx, fz) in BurningFiresNear(ctx, toX, toZ, now))
        {
            float to = AnimalDist(toX, toZ, fx, fz);
            if (to < FireKeepAway && to < AnimalDist(fromX, fromZ, fx, fz)) return true;
        }
        return false;
    }

    // A fire nearby is a mild fright: unease grows the closer the sheep is, and it
    // leans its flight away from the flames.
    static void AnimalShyFromFires(ReducerContext ctx, ref AnimalMind mind, Animal body)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        float fear = 0f, awayX = 0f, awayZ = 0f;
        foreach (var (fx, fz) in BurningFiresNear(ctx, body.X, body.Z, now))
        {
            float dx = body.X - fx, dz = body.Z - fz;
            float d = MathF.Max(MathF.Sqrt(dx * dx + dz * dz), 0.1f);
            if (d > FireUneasyRange) continue;
            float threat = 0.45f * Math.Clamp((FireUneasyRange - d) / (FireUneasyRange - 1f), 0f, 1f);
            fear += threat;
            awayX += dx / d * threat;
            awayZ += dz / d * threat;
        }
        if (fear <= 0f) return;
        mind.Nervous = Math.Clamp(mind.Nervous + fear * 0.7f, 0f, 1f);
        float fxl = mind.FleeX * 0.4f + awayX / fear * 0.6f, fzl = mind.FleeZ * 0.4f + awayZ / fear * 0.6f;
        float len = MathF.Sqrt(fxl * fxl + fzl * fzl);
        if (len > 0.01f)
        {
            mind.FleeX = fxl / len;
            mind.FleeZ = fzl / len;
        }
    }

    static System.Collections.Generic.IEnumerable<(float X, float Z)> BurningFiresNear(ReducerContext ctx, float x, float z, long now)
    {
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var fire in ctx.Db.Fire.ByChunk.Filter((cx + dx, cz + dz)))
                    if (fire.FuelUntil.MicrosecondsSinceUnixEpoch > now)
                        yield return (fire.X, fire.Z);
    }
}
