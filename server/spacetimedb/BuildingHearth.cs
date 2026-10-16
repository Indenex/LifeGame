using LifeGame.Shared;
using SpacetimeDB;

// Raised hearths (building-9, D-132). A ring of stones with a clay surround, built
// inside a finished hut or house. Lighting a fire within reach of one sets it on
// the hearth, fuel on it lasts HearthFuelScale times as long, and a fire on a
// hearth warms a whole clay hut, as any fire already warms a brick house. Crafting
// declares the two fire hooks (Hearths.cs FireSpot, LightFire and FeedFire).
public static partial class Module
{
    // The finished hut or house whose walls hold this spot, at least gap inside them.
    static Structure? HomeAround(ReducerContext ctx, float x, float z, float gap)
    {
        foreach (var s in StructuresNear(ctx, x, z))
            if (s.Complete && Structures.IsHome((int)s.Kind) && Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, x, z, -gap))
                return s;
        return null;
    }

    // Finished hearths around a point, within reach of their middle.
    static IEnumerable<Structure> HearthsNear(ReducerContext ctx, float x, float z, float reach)
    {
        foreach (var s in StructuresNear(ctx, x, z))
            if (s.Complete && Structures.IsIndoor((int)s.Kind) && Dist(s.X, s.Z, x, z) <= reach)
                yield return s;
    }

    // A fire lit within reach of a cold, finished hearth goes onto it, nearest first.
    static partial void BuiltHearthSpotHook(ReducerContext ctx, float x, float z, ref (float X, float Z)? spot)
    {
        Structure? best = null;
        float bestD = float.MaxValue;
        foreach (var h in HearthsNear(ctx, x, z, World.Reach))
        {
            if (BurningFireNear(ctx, h.X, h.Z, Structures.HearthRadius) is not null) continue;
            float d = Dist(h.X, h.Z, x, z);
            if (d < bestD)
            {
                bestD = d;
                best = h;
            }
        }
        if (best is { } hearth) spot = (hearth.X, hearth.Z);
    }

    // Fuel on a hearth lasts longer: the clay holds the heat and the ring keeps the draft steady.
    static partial void FireFuelScaleHook(ReducerContext ctx, float x, float z, ref float scale)
    {
        foreach (var _ in HearthsNear(ctx, x, z, Structures.HearthRadius))
        {
            scale = MathF.Max(scale, Structures.HearthFuelScale);
            return;
        }
    }

    // A fire burning on a finished hearth inside this hut's walls.
    static bool HearthFireIn(ReducerContext ctx, Structure home)
    {
        foreach (var h in HearthsNear(ctx, home.X, home.Z, Structures.Shape((int)home.Kind).Radius))
            if (Structures.Contains((int)home.Kind, home.X, home.Z, home.Yaw, h.X, h.Z)
                && BurningFireNear(ctx, h.X, h.Z, Structures.HearthRadius) is not null)
                return true;
        return false;
    }
}
