using System;
using LifeGame.Shared;
using SpacetimeDB;

// Sheep and fire (animals-6): a sheep keeps clear of a burning fire. Within a few
// meters it grows uneasy and edges away, like a mild fright, and it never steps
// closer than FireKeepAway to the flames. A fire that has burned out is just ash.
// A firing kiln counts too (animals-7): it is a fire with a clay dome over it, so
// the distances are measured from its wall rather than its middle.
public static partial class Module
{
    // No step ends this close to a burning fire (unless it moves the sheep away).
    const float FireKeepAway = 1.5f;
    // A burning fire unsettles sheep this close.
    const float FireUneasyRange = 4f;

    // True when a step from one spot to another would cross something a sheep
    // won't walk into: for now, the ring around a burning fire or firing kiln.
    // House building implements this (building-8, D-087): a step across a finished
    // fence, a shut gate or a wall is blocked. Unimplemented, it does nothing.
    static partial void StructureStepHook(ReducerContext ctx, float fromX, float fromZ, float toX, float toZ, ref bool blocked);

    static bool AnimalStepBlocked(ReducerContext ctx, float fromX, float fromZ, float toX, float toZ)
    {
        bool blocked = false;
        StructureStepHook(ctx, fromX, fromZ, toX, toZ, ref blocked);
        if (blocked) return true;
        if (AnimalStepIntoObstacle(ctx, fromX, fromZ, toX, toZ)) return true;
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var (fx, fz, wall) in BurningFiresNear(ctx, toX, toZ, now))
        {
            float to = AnimalDist(toX, toZ, fx, fz);
            if (to < FireKeepAway + wall && to < AnimalDist(fromX, fromZ, fx, fz)) return true;
        }
        return false;
    }

    // Tree trunks and boulders (bugfix-23): a step may not end inside one, counting the
    // animal's body, unless it moves the animal out of it. Mirrors the player rules in
    // Terrain.CollideVegetation; saplings are client-only and ignored.
    const float AnimalBody = 0.3f, TrunkRadius = 0.35f;

    static bool AnimalStepIntoObstacle(ReducerContext ctx, float fromX, float fromZ, float toX, float toZ)
    {
        int cx = (int)MathF.Round(toX), cz = (int)MathF.Round(toZ);
        for (int i = cx - 1; i <= cx + 1; i++)
            for (int j = cz - 1; j <= cz + 1; j++)
            {
                float ox, oz, r;
                if (Rocks.LiesAt(i, j) && TreeStands(ctx, i, j))
                {
                    (ox, oz) = Rocks.Center(i, j);
                    r = Rocks.Radius(i, j) + AnimalBody;
                }
                else if (Trees.GrowsAt(i, j) && TreeStands(ctx, i, j))
                {
                    (ox, oz) = Trees.Trunk(i, j);
                    r = TrunkRadius + AnimalBody;
                }
                else continue;
                float to = AnimalDist(toX, toZ, ox, oz);
                if (to < r && to < AnimalDist(fromX, fromZ, ox, oz)) return true;
            }
        return false;
    }

    // A fire nearby is a mild fright: unease grows the closer the sheep is, and it
    // leans its flight away from the flames.
    static void AnimalShyFromFires(ReducerContext ctx, ref AnimalMind mind, Animal body)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        float fear = 0f, awayX = 0f, awayZ = 0f;
        foreach (var (fx, fz, wall) in BurningFiresNear(ctx, body.X, body.Z, now))
        {
            float dx = body.X - fx, dz = body.Z - fz;
            float center = MathF.Max(MathF.Sqrt(dx * dx + dz * dz), 0.1f);
            float d = center - wall;
            if (d > FireUneasyRange) continue;
            float threat = 0.45f * Math.Clamp((FireUneasyRange - d) / (FireUneasyRange - 1f), 0f, 1f);
            fear += threat;
            awayX += dx / center * threat;
            awayZ += dz / center * threat;
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

    // A kiln's outer wall, out from its middle (the client's KilnModel.Radius).
    const float KilnWall = 0.8f;

    // Burning fires (pit firings included: they need a fueled fire) and firing
    // kilns in the 3x3 chunks around a spot, with how far their edge is from the middle.
    static System.Collections.Generic.IEnumerable<(float X, float Z, float Wall)> BurningFiresNear(ReducerContext ctx, float x, float z, long now)
    {
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                foreach (var fire in ctx.Db.Fire.ByChunk.Filter((cx + dx, cz + dz)))
                    if (fire.FuelUntil.MicrosecondsSinceUnixEpoch > now)
                        yield return (fire.X, fire.Z, 0f);
                foreach (var kiln in ctx.Db.Kiln.ByChunk.Filter((cx + dx, cz + dz)))
                    if (kiln.State == (byte)KilnState.Firing && kiln.FireFinish.MicrosecondsSinceUnixEpoch > now)
                        yield return (kiln.X, kiln.Z, KilnWall);
            }
    }
}
