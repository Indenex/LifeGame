using System;
using LifeGame.Shared;
using SpacetimeDB;

// Lambs (animals-4): a lamb trails its mother, a step behind and to one side, and
// does what she does: grazes when she grazes, looks about when she stops. When it
// is grown, or its mother is gone, it joins the flock like any other sheep. Fear
// works as for every sheep, so a frightened lamb bolts with the rest.
public static partial class Module
{
    // How far a lamb lets its mother get before it trots after her.
    const float LambStrayLimit = 2.2f;

    // The lamb's mother among this tick's flock, if it still has one to follow.
    // A lamb whose mother is gone (or that was born before lambs had mothers) takes
    // to the nearest grown ewe; a grown sheep lets go.
    static Animal? AnimalFindMother(ref AnimalMind mind, Animal body, FlockMember[] flock)
    {
        if (body.Growth >= 100)
        {
            mind.MotherId = 0;
            return null;
        }
        Animal? nearest = null;
        float best = float.MaxValue;
        foreach (var m in flock)
        {
            if (m.Body.Id == mind.MotherId) return m.Body;
            float d = AnimalDist(body.X, body.Z, m.Body.X, m.Body.Z);
            if (m.Body.Growth >= 100 && d < best) (nearest, best) = (m.Body, d);
        }
        mind.MotherId = nearest?.Id ?? 0;
        return nearest;
    }

    static void LambDecide(ReducerContext ctx, ref Animal body, ref AnimalMind mind, Animal mother, long now)
    {
        const float RunAt = 0.5f, CalmAt = 0.2f;
        if (mind.Nervous > RunAt || (body.State == AnimalState.Run && mind.Nervous > CalmAt))
        {
            // Bolt, but toward wherever the mother is fleeing if she is.
            body.State = AnimalState.Run;
            float fx = mind.FleeX, fz = mind.FleeZ;
            if (mother.State == AnimalState.Run)
                (fx, fz) = (MathF.Sin(mother.Heading), MathF.Cos(mother.Heading));
            mind.TargetX = body.X + fx * 6f;
            mind.TargetZ = body.Z + fz * 6f;
            return;
        }

        // A spot a step behind her and to the lamb's own side.
        float side = (body.Id % 2 == 0) ? 1f : -1f;
        float hx = MathF.Sin(mother.Heading), hz = MathF.Cos(mother.Heading);
        float sx = mother.X - hx * 0.9f + hz * 0.55f * side;
        float sz = mother.Z - hz * 0.9f - hx * 0.55f * side;
        float fromMother = AnimalDist(body.X, body.Z, mother.X, mother.Z);

        if (body.State == AnimalState.Walk || body.State == AnimalState.Run)
        {
            // Keep re-aiming while she moves; settle once alongside.
            if (fromMother > 1.3f || AnimalDist(body.X, body.Z, sx, sz) > 0.5f)
            {
                var (tx, tz) = AnimalNearestDry(sx, sz);
                if (AnimalDist(tx, tz, mind.TargetX, mind.TargetZ) > 0.3f) (mind.TargetX, mind.TargetZ) = (tx, tz);
                body.State = AnimalState.Walk;
                return;
            }
        }
        else if (fromMother > LambStrayLimit)
        {
            var (tx, tz) = AnimalNearestDry(sx, sz);
            AnimalStartWalk(ref body, ref mind, tx, tz, ctx, now);
            return;
        }

        // Alongside her: do as she does, re-checking often so it never lags long.
        var copy = mother.State == AnimalState.Graze ? AnimalState.Graze : AnimalState.Idle;
        if (body.State != copy || now >= mind.StateUntil)
        {
            if (body.State != copy && copy == AnimalState.Idle)
                body.Heading = MathF.Atan2(mother.X - body.X, mother.Z - body.Z);
            body.State = copy;
            mind.StateUntil = now + AnimalMicros(1.5f, 4f, ctx);
        }
    }
}
