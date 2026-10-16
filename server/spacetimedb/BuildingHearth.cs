using LifeGame.Shared;
using SpacetimeDB;

// Raised hearths (building-9, D-132). A ring of stones with a clay surround, built
// inside a finished hut or house. Lighting a fire within reach of one sets it on
// the hearth, fuel on it lasts HearthFuelScale times as long, and a fire on a
// hearth warms a whole clay hut, as any fire already warms a brick house. A fire on
// a hearth also feeds itself from baskets and jars close by, so it never needs
// tending (D-149). Crafting declares the fire hooks (Hearths.cs, LightFire,
// FeedFire and BurnOutFires).
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

    // A hearth fire tops itself up from stores this close to the hearth (D-149).
    const float HearthStoreReach = 3f;
    // It reaches for more fuel once this little is left.
    const float HearthTopUpSeconds = 60f;
    // What it burns, in the order it takes them: logs last longest, so fewer trips.
    static readonly Item[] HearthFuels = { Item.Log, Item.Stick };

    // Crafting's fire tick asks this of every fire. A fire on a finished hearth that
    // is running low takes one log, or else one stick, from a store near the hearth.
    static partial void FireTopUpHook(ReducerContext ctx, Fire fire, ref Timestamp fuelUntil)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        long left = fuelUntil.MicrosecondsSinceUnixEpoch - now;
        if (left <= 0 || left > (long)(HearthTopUpSeconds * 1_000_000)) return;
        if (!HearthsNear(ctx, fire.X, fire.Z, Structures.HearthRadius).Any()) return;

        foreach (var fuel in HearthFuels)
            foreach (var store in StructuresNear(ctx, fire.X, fire.Z))
            {
                if (!store.Complete || !Structures.IsStore((int)store.Kind) || Dist(store.X, store.Z, fire.X, fire.Z) > HearthStoreReach) continue;
                foreach (var row in ctx.Db.BasketItem.StructureId.Filter(store.Id))
                {
                    if (row.Item != (ushort)fuel || row.Count == 0) continue;
                    // Same cap as feeding it by hand (D-121), as if whoever lit it were feeding it.
                    long add = (long)(Items.Def(fuel).FuelSeconds * Structures.HearthFuelScale * 1_000_000);
                    long cap = now + (long)(Fires.MaxSecondsFor(SkillRank(ctx, fire.LitBy, "fire.hand_drill")) * Structures.HearthFuelScale * 1_000_000);
                    long until = Math.Min(fuelUntil.MicrosecondsSinceUnixEpoch + add, cap);
                    if (until <= fuelUntil.MicrosecondsSinceUnixEpoch) return;
                    if (row.Count == 1) ctx.Db.BasketItem.Id.Delete(row.Id);
                    else ctx.Db.BasketItem.Id.Update(row with { Count = row.Count - 1 });
                    fuelUntil = new Timestamp(until);
                    return;
                }
            }
    }
}
