using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// Roosting (animals-20, D-269): at dusk hens walk into their coop and settle; at dawn they come out. With
// no coop they settle at the nearest finished roofed building, or where they stand. A coop becomes the
// flock's home, so by day the hens scratch about around it.
public static partial class Module
{
    // Hens roost when the light falls below RoostBelow and get up when it is back above WakeAbove.
    const float RoostBelow = 0.25f, WakeAbove = 0.4f;
    // How far from where she is a hen looks for a coop, and for a roofed building if there is no coop.
    const float CoopSeek = 40f, RoofSeek = 25f;
    // Close enough to the roosting spot to settle.
    const float RoostArrive = 1.2f;

    // Returns true when the animal is roosting (or walking to roost) and the usual decisions should be skipped.
    static bool RoostDecide(ReducerContext ctx, ref Animal body, ref AnimalMind mind, Flock flock, long now)
    {
        float daylight = WorldTime.Daylight(TimeOfDay(ctx));
        bool roosting = body.State == AnimalState.Roost;
        if (daylight > (roosting ? WakeAbove : RoostBelow))
        {
            if (!roosting) return false;
            // Dawn: get up, stagger the hens a little so they don't all go at once.
            body.State = AnimalState.Idle;
            mind.StateUntil = now + AnimalMicros(1f, 14f, ctx);
            return true;
        }
        if (roosting) return true;

        var (sx, sz, coop) = RoostSpot(ctx, body.X, body.Z, flock);
        if (AnimalDist(body.X, body.Z, sx, sz) <= RoostArrive)
        {
            body.State = AnimalState.Roost;
            mind.StateUntil = now;
            if (coop && AnimalDist(flock.HomeX, flock.HomeZ, sx, sz) > 1f)
                ctx.Db.Flock.FlockId.Update(flock with { HomeX = sx, HomeZ = sz });
            return true;
        }
        if (body.State == AnimalState.Walk) return true;
        var (tx, tz) = AnimalNearestDry(sx, sz);
        AnimalStartWalk(ref body, ref mind, tx, tz, ctx, now);
        return true;
    }

    // Where a hen spends the night: the nearest finished coop, else the nearest finished roofed building,
    // else where she is.
    static (float X, float Z, bool Coop) RoostSpot(ReducerContext ctx, float x, float z, Flock flock)
    {
        Structure? best = null;
        float bestD = float.MaxValue;
        bool bestCoop = false;
        foreach (var s in StructuresNear(ctx, x, z).Concat(StructuresNear(ctx, flock.HomeX, flock.HomeZ)))
        {
            if (!s.Complete) continue;
            var shape = Structures.Shape((int)s.Kind);
            bool coop = shape.Coop;
            if (!coop && shape.RainCover <= 0f) continue;
            float d = AnimalDist(x, z, s.X, s.Z);
            if (d > (coop ? CoopSeek : RoofSeek)) continue;
            // Any coop beats any roof.
            if (best == null || (coop && !bestCoop) || (coop == bestCoop && d < bestD))
                (best, bestD, bestCoop) = (s, d, coop);
        }
        return best is { } b ? (b.X, b.Z, bestCoop) : (x, z, false);
    }
}
