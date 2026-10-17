using LifeGame.Shared;
using SpacetimeDB;

// Crafting stations (D-098, D-112). A station is a finished structure whose shape
// names a StationKind; it stands inside a claim. A process with a Station is made
// within reach of one, and draws its ingredients from the bag first, then from
// storage within StationInfo.StorageReach of the station, then the claim's stash.
public static partial class Module
{
    // Stations go up only inside a claim the builder has rights to, except the wild crafting table (building-13).
    static void CheckStationSite(ReducerContext ctx, StructureKind kind, float x, float z)
    {
        int k = (int)kind;
        var station = Structures.Shape(k).Station;
        if (station == StationKind.None || Structures.IsWild(k)) return;
        if (ClaimAt(ctx, x, z) is not { } claim || !HasClaimRights(ctx, claim, ctx.Sender))
            throw new Exception($"A {StationInfo.Name(station)} has to stand inside your claim");
    }

    // How far a station's middle is from its edge, for reach.
    static float StationRadius(Structure s)
    {
        var shape = Structures.Shape((int)s.Kind);
        return shape.Round ? shape.Radius : MathF.Max(shape.HalfWidth, shape.HalfDepth);
    }

    // The nearest finished station of this kind the player can work at from here.
    static Structure? StationNear(ReducerContext ctx, StationKind kind, float x, float z)
    {
        Structure? best = null;
        float bestD = float.MaxValue;
        foreach (var s in StructuresNear(ctx, x, z))
        {
            if (!s.Complete || Structures.Shape((int)s.Kind).Station != kind) continue;
            float d = Dist(s.X, s.Z, x, z);
            if (d - StationRadius(s) > Structures.WorkReach || d >= bestD) continue;
            // A wild station (the crafting table) can stand on open ground; in a claim only its members may use it.
            if (ClaimAt(ctx, s.X, s.Z) is { } claim && !HasClaimRights(ctx, claim, ctx.Sender)) continue;
            if (ClaimAt(ctx, s.X, s.Z) is null && !Structures.IsWild((int)s.Kind)) continue;
            best = s;
            bestD = d;
        }
        return best;
    }

    // Finished storage near a station that the player may take from.
    static List<Structure> StoresNear(ReducerContext ctx, Structure station)
    {
        var stores = new List<Structure>();
        foreach (var s in StructuresNear(ctx, station.X, station.Z))
            if (s.Complete && Structures.IsStore((int)s.Kind)
                && Dist(s.X, s.Z, station.X, station.Z) <= StationInfo.StorageReach
                && (ClaimAt(ctx, s.X, s.Z) is not { } claim || HasClaimRights(ctx, claim, ctx.Sender)))
                stores.Add(s);
        return stores;
    }

    // How many of an item the player can use: the bag, plus a station's storage and stash.
    static uint CountFor(ReducerContext ctx, Structure? station, Item item)
    {
        uint n = CountOf(ctx, item);
        if (station is not { } st) return n;
        foreach (var store in StoresNear(ctx, st))
            foreach (var row in ctx.Db.BasketItem.StructureId.Filter(store.Id))
                if (row.Item == (ushort)item) n += row.Count;
        if (ClaimAt(ctx, st.X, st.Z) is { } claim)
            foreach (var row in ctx.Db.ClaimStash.ByClaimItem.Filter((claim.Id, (ushort)item)))
                n += row.Count;
        return n;
    }

    // Takes items for a station process: the bag first, then nearby storage, then the
    // claim's stash, best quality first within each. Returns the summed quality taken.
    static float TakeFor(ReducerContext ctx, Structure? station, Item item, uint count)
    {
        uint inBag = Math.Min(count, CountOf(ctx, item));
        float sum = Take(ctx, item, inBag);
        count -= inBag;
        if (count == 0 || station is not { } st) return sum;

        var stored = new List<BasketItem>();
        foreach (var store in StoresNear(ctx, st))
            foreach (var row in ctx.Db.BasketItem.StructureId.Filter(store.Id))
                if (row.Item == (ushort)item) stored.Add(row);
        stored.Sort((a, b) => b.Quality.CompareTo(a.Quality));
        foreach (var row in stored)
        {
            if (count == 0) break;
            uint n = Math.Min(count, row.Count);
            if (n == row.Count) ctx.Db.BasketItem.Id.Delete(row.Id);
            else ctx.Db.BasketItem.Id.Update(row with { Count = row.Count - n });
            sum += n * row.Quality;
            count -= n;
        }
        if (count == 0 || ClaimAt(ctx, st.X, st.Z) is not { } claim) return sum;

        var stash = ctx.Db.ClaimStash.ByClaimItem.Filter((claim.Id, (ushort)item)).ToList();
        stash.Sort((a, b) => b.Quality.CompareTo(a.Quality));
        foreach (var row in stash)
        {
            if (count == 0) break;
            uint n = Math.Min(count, row.Count);
            if (n == row.Count) ctx.Db.ClaimStash.Id.Delete(row.Id);
            else ctx.Db.ClaimStash.Id.Update(row with { Count = row.Count - n });
            sum += n * row.Quality;
            count -= n;
        }
        return sum;
    }

    // Where a placed thing made at a station is set down: beside the station, on the
    // player's side, so a camp's work collects there.
    static (float X, float Z) BesideStation(Structure station, float px, float pz)
    {
        float dx = px - station.X, dz = pz - station.Z, d = MathF.Sqrt(dx * dx + dz * dz);
        if (d < 0.01f) (dx, dz, d) = (0f, 1f, 1f);
        float r = StationRadius(station) + 0.4f;
        return (station.X + dx / d * r, station.Z + dz / d * r);
    }
}
