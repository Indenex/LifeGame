using LifeGame.Shared;
using SpacetimeDB;

// Where fires go and what they leave behind. A new fire is set on clear, level
// ground near the spot in front of the crafter, never on a boulder or against a
// trunk. A fire that burns out leaves a cold hearth of stones and ash, and anyone
// nearby hears that it has burned down. Lighting a fire by a cold hearth rekindles it.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ColdHearth", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct ColdHearth
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        // The stones are scattered and the ash blown away after this.
        public Timestamp Until;
    }

    // Players this close hear that a fire has burned down.
    const float BurnedDownHearing = 20f;

    // Where a new fire goes: a cold hearth within reach is rekindled in place;
    // otherwise the nearest clear, level spot to the one a step in front.
    // House building's raised hearth (D-132): a fire lit within reach goes on it, and
    // fuel there burns longer (scale > 1). Building implements both.
    static partial void BuiltHearthSpotHook(ReducerContext ctx, float x, float z, ref (float X, float Z)? spot);
    static partial void FireFuelScaleHook(ReducerContext ctx, float x, float z, ref float scale);
    // Before a fire is checked for burning out, a hearth fire may top up its fuel (D-149).
    static partial void FireTopUpHook(ReducerContext ctx, Fire fire, ref Timestamp fuelUntil);

    static (float X, float Z) FireSpot(ReducerContext ctx, PlayerPos pos)
    {
        (float X, float Z)? built = null;
        BuiltHearthSpotHook(ctx, pos.X, pos.Z, ref built);
        if (built is { } b) return b;
        if (NearestColdHearth(ctx, pos.X, pos.Z, World.Reach) is { } hearth)
            return (hearth.X, hearth.Z);
        float fx = pos.X + MathF.Sin(pos.Heading) * 1.2f, fz = pos.Z + MathF.Cos(pos.Heading) * 1.2f;
        // Rings of candidates around the front spot, nearest first.
        for (int ring = 0; ring <= 5; ring++)
        {
            float r = ring * 0.35f;
            int steps = ring == 0 ? 1 : 16;
            for (int k = 0; k < steps; k++)
            {
                float a = pos.Heading + k * MathF.Tau / steps;
                float x = fx + MathF.Sin(a) * r, z = fz + MathF.Cos(a) * r;
                float dp = MathF.Sqrt((x - pos.X) * (x - pos.X) + (z - pos.Z) * (z - pos.Z));
                if (dp < 0.8f || dp > World.Reach + 0.5f) continue;
                if (ClearForFire(ctx, x, z)) return (x, z);
            }
        }
        throw new Exception("There's no clear, level ground here for a fire");
    }

    // Level ground above the water with no boulder, trunk, fire or hearth under the stone ring.
    static bool ClearForFire(ReducerContext ctx, float x, float z)
    {
        int cx = World.CellOf(x), cz = World.CellOf(z);
        float lo = float.MaxValue, hi = float.MinValue;
        for (int i = cx - 1; i <= cx + 1; i++)
            for (int j = cz - 1; j <= cz + 1; j++)
            {
                float h = World.Height(i, j, DepthAt(ctx, i, j));
                lo = Math.Min(lo, h);
                hi = Math.Max(hi, h);
            }
        if (lo < World.WaterLevel + 0.02f || hi - lo > 0.4f) return false;

        for (int i = cx - 2; i <= cx + 2; i++)
            for (int j = cz - 2; j <= cz + 2; j++)
            {
                if (DepthAt(ctx, i, j) != 0) continue;
                // Test the cheap shared lookups first; a building clears the ground under it
                // (the client draws no boulder or tree there), checked only where one stands.
                if (Rocks.LiesAt(i, j))
                {
                    if (UnderStructure(ctx, i, j)) continue;
                    var (rx, rz) = Rocks.Center(i, j);
                    if (Dist(rx, rz, x, z) < Rocks.Radius(i, j) + Fires.RingRadius) return false;
                }
                else if (Trees.GrowsAt(i, j))
                {
                    if (UnderStructure(ctx, i, j)) continue;
                    var (tx, tz) = Trees.Trunk(i, j);
                    if (Dist(tx, tz, x, z) < 0.4f + Fires.RingRadius) return false;
                }
            }

        int chx = World.ChunkOf(x), chz = World.ChunkOf(z);
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                foreach (var fire in ctx.Db.Fire.ByChunk.Filter((chx + dx, chz + dz)))
                    if (Dist(fire.X, fire.Z, x, z) < Fires.Spacing) return false;
                foreach (var hearth in ctx.Db.ColdHearth.ByChunk.Filter((chx + dx, chz + dz)))
                    if (Dist(hearth.X, hearth.Z, x, z) < Fires.Spacing) return false;
            }
        return true;
    }

    // Whether a building's footprint covers this cell, as the client clears it (BuildingSystem.SetCleared).
    static bool UnderStructure(ReducerContext ctx, int i, int j) =>
        StructuresNear(ctx, i, j).Any(o => Structures.Contains((int)o.Kind, o.X, o.Z, o.Yaw, i, j, 0.4f));

    static ColdHearth? NearestColdHearth(ReducerContext ctx, float x, float z, float reach)
    {
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        ColdHearth? best = null;
        float bestD = reach;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var hearth in ctx.Db.ColdHearth.ByChunk.Filter((cx + dx, cz + dz)))
                {
                    float d = Dist(hearth.X, hearth.Z, x, z);
                    if (d <= bestD) { best = hearth; bestD = d; }
                }
        return best;
    }

    // A fire has run out of fuel: its stones and ash stay as a cold hearth, and
    // everyone close by is told.
    static void LeaveColdHearth(ReducerContext ctx, Fire fire)
    {
        ctx.Db.ColdHearth.Insert(new ColdHearth
        {
            Id = 0,
            X = fire.X,
            Z = fire.Z,
            ChunkX = fire.ChunkX,
            ChunkZ = fire.ChunkZ,
            Until = ctx.Timestamp + new TimeDuration((long)(Fires.ColdSeconds * 1_000_000)),
        });
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var pos in ctx.Db.PlayerPos.ByChunk.Filter(((short)(fire.ChunkX + dx), (short)(fire.ChunkZ + dz))))
                    if (Dist(pos.X, pos.Z, fire.X, fire.Z) <= BurnedDownHearing
                        && ctx.Db.Player.PlayerId.Find(pos.PlayerId) is { Online: true })
                        News(ctx, pos.PlayerId, pos.PlayerId == fire.LitBy ? "Your fire has burned down" : "A fire nearby has burned down");
    }

    // Rekindling a cold hearth puts the fire back where the hearth was.
    static void RekindleAt(ReducerContext ctx, float x, float z)
    {
        if (NearestColdHearth(ctx, x, z, 0.05f) is { } hearth)
            ctx.Db.ColdHearth.Id.Delete(hearth.Id);
    }

    // A cold hearth in the open weathers away after Fires.ColdSeconds. Inside a
    // standing claim it stays as long as the claim does (D-109).
    static void ClearOldHearths(ReducerContext ctx)
    {
        foreach (var hearth in ctx.Db.ColdHearth.Iter().ToList())
            if (hearth.Until.MicrosecondsSinceUnixEpoch < ctx.Timestamp.MicrosecondsSinceUnixEpoch
                && ClaimAt(ctx, hearth.X, hearth.Z) is null)
                ctx.Db.ColdHearth.Id.Delete(hearth.Id);
    }

    static float Dist(float ax, float az, float bx, float bz) =>
        MathF.Sqrt((ax - bx) * (ax - bx) + (az - bz) * (az - bz));
}
