using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// Roosting (animals-20, D-269): at dusk hens walk to the foot of their coop's ramp, go in and settle on the
// roost bar; at dawn they come out. With no coop they settle at the nearest finished roofed building, or
// where they stand. A coop becomes the flock's home, so by day the hens scratch about around it.
public static partial class Module
{
    // Hens roost when the light falls below RoostBelow and get up when it is back above WakeAbove.
    const float RoostBelow = 0.25f, WakeAbove = 0.4f;
    // How far from where she is a hen looks for a coop, and for a roofed building if there is no coop.
    const float CoopSeek = 40f, RoofSeek = 25f;
    // Close enough to a roosting spot to settle, and to the coop's entry to go on in.
    const float RoostArrive = 0.6f, EntryArrive = 0.8f;
    // Hens sit side by side along the roost bar (local X), this far apart.
    const float RoostSpacing = 0.25f;

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

        var spot = RoostSpot(ctx, body, flock);
        if (AnimalDist(body.X, body.Z, spot.RoostX, spot.RoostZ) <= RoostArrive)
        {
            body.State = AnimalState.Roost;
            mind.StateUntil = now;
            if (spot.Coop is { } coop && AnimalDist(flock.HomeX, flock.HomeZ, coop.X, coop.Z) > 1f)
                ctx.Db.Flock.FlockId.Update(flock with { HomeX = coop.X, HomeZ = coop.Z });
            return true;
        }
        if (body.State == AnimalState.Walk) return true;
        // Via the foot of the ramp first, so a hen doesn't try to walk through the wall.
        bool atDoor = AnimalDist(body.X, body.Z, spot.EntryX, spot.EntryZ) <= EntryArrive
            || AnimalDist(body.X, body.Z, spot.RoostX, spot.RoostZ) < AnimalDist(spot.EntryX, spot.EntryZ, spot.RoostX, spot.RoostZ);
        var (tx, tz) = atDoor ? (spot.RoostX, spot.RoostZ) : (spot.EntryX, spot.EntryZ);
        (tx, tz) = AnimalNearestDry(tx, tz);
        AnimalStartWalk(ref body, ref mind, tx, tz, ctx, now);
        return true;
    }

    readonly record struct RoostPoint(float EntryX, float EntryZ, float RoostX, float RoostZ, Structure? Coop);

    // Where a hen spends the night: her place on the roost bar of the nearest finished coop, else the nearest
    // finished roofed building, else where she is.
    static RoostPoint RoostSpot(ReducerContext ctx, Animal body, Flock flock)
    {
        Structure? best = null;
        float bestD = float.MaxValue;
        bool bestCoop = false;
        foreach (var s in StructuresNear(ctx, body.X, body.Z).Concat(StructuresNear(ctx, flock.HomeX, flock.HomeZ)))
        {
            if (!s.Complete) continue;
            bool coop = s.Kind == StructureKind.HenCoop;
            if (!coop && Structures.Shape((int)s.Kind).RainCover <= 0f) continue;
            float d = AnimalDist(body.X, body.Z, s.X, s.Z);
            if (d > (coop ? CoopSeek : RoofSeek)) continue;
            // Any coop beats any roof.
            if (best == null || (coop && !bestCoop) || (coop == bestCoop && d < bestD))
                (best, bestD, bestCoop) = (s, d, coop);
        }
        if (best is not { } b) return new RoostPoint(body.X, body.Z, body.X, body.Z, null);
        if (!bestCoop) return new RoostPoint(b.X, b.Z, b.X, b.Z, null);
        var shape = Structures.Shape((int)b.Kind);
        var (ex, ez) = Structures.ToWorld(b.X, b.Z, b.Yaw, shape.Entry.X, shape.Entry.Z);
        float along = ((int)(body.Id % 4) - 1.5f) * RoostSpacing;
        var (rx, rz) = Structures.ToWorld(b.X, b.Z, b.Yaw, shape.Roost.X + along, shape.Roost.Z);
        return new RoostPoint(ex, ez, rx, rz, b);
    }
}
