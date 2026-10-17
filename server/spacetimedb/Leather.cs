using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// Leather (animals-5, animals-11): a sheep hide is scraped clean with a blade at a
// leather drying rack (process hide.scrape), then laced onto the rack's bar to dry
// (hide.stretch), up to StationHides at once (D-098). It dries like clay: slower
// in rain, faster by a fire. When dry, G takes it down as leather, at the quality
// of the scraping. Hides set on the little A-frame racks of before the stations
// (StationId 0) keep drying where they stand.
public static partial class Module
{
    // A hide on a rack takes this long to dry in fair weather, away from a fire.
    const float HideDrySeconds = 360f;
    // Racks re-read the weather this often.
    const long HideCheckMicros = 15_000_000;
    // A drying rack station holds this many hides along its bar, this far apart.
    public const int StationHides = 3;
    const float HideSpacing = 0.75f;

    [SpacetimeDB.Table(Accessor = "HideRack", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct HideRack
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public uint MadeBy;
        public float X;
        public float Z;
        public float Heading;
        public int ChunkX;
        public int ChunkZ;
        public byte Quality;
        // 1 is fresh and wet, 0 is dry leather. Stored as the value at MoistureAt
        // plus a rate per second, so clients can show it drying smoothly.
        public float Moisture;
        public Timestamp MoistureAt;
        public float DryRate;
        // The drying rack station it hangs on, or 0 for a free-standing A-frame
        // rack from before the stations (animals-11).
        [Default(0UL)]
        public ulong StationId;
        // Its place along the station's bar, 0 to StationHides - 1.
        [Default((byte)0)]
        public byte Peg;
    }

    // The crafting Placed outcome (hide.stretch) laces the hide onto the drying
    // rack it was made at, on the first free peg.
    static partial void AnimalsOnPlaced(ReducerContext ctx, CraftJob job, ProcessDef def)
    {
        if (!def.Id.StartsWith("hide.")) return;
        var station = DryingRackNear(ctx, job.X, job.Z);
        int peg = -1;
        if (station is { } st)
        {
            var used = ctx.Db.HideRack.Iter().Where(r => r.StationId == st.Id).Select(r => (int)r.Peg).ToHashSet();
            for (int i = 0; i < StationHides && peg < 0; i++)
                if (!used.Contains(i)) peg = i;
        }
        if (station is not { } rack || peg < 0)
        {
            // No room: hand everything back rather than lose it.
            foreach (var input in def.Consumes)
                AddToInventory(ctx, job.Owner, input.Item, input.Count, input.Item == Item.ScrapedHide ? job.Quality : Items.RawQuality);
            News(ctx, job.PlayerId, $"The drying rack is full: it holds {StationHides} hides. The hide goes back in your bag.");
            return;
        }
        // Along the bar, which runs along the station's local X.
        float along = (peg - (StationHides - 1) / 2f) * HideSpacing;
        float c = MathF.Cos(rack.Yaw), sn = MathF.Sin(rack.Yaw);
        float x = rack.X + along * c, z = rack.Z - along * sn;
        ctx.Db.HideRack.Insert(new HideRack
        {
            Id = 0,
            MadeBy = job.PlayerId,
            X = x,
            Z = z,
            Heading = rack.Yaw,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            Quality = job.Quality,
            Moisture = 1f,
            MoistureAt = ctx.Timestamp,
            DryRate = HideDryRateAt(ctx, x, z),
            StationId = rack.Id,
            Peg = (byte)peg,
        });
    }

    // A drying rack taken down lets its hides fall: leather if dry, else the scraped hide.
    static partial void AnimalsStructureRemoved(ReducerContext ctx, Structure s)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var hide in ctx.Db.HideRack.Iter().Where(r => r.StationId == s.Id).ToList())
        {
            var item = HideMoisture(hide, now) > 0f ? Item.ScrapedHide : Item.Leather;
            ctx.Db.WorldItem.Insert(new WorldItem
            {
                Id = 0,
                Item = (ushort)item,
                Quality = hide.Quality,
                X = hide.X,
                Z = hide.Z,
                ChunkX = hide.ChunkX,
                ChunkZ = hide.ChunkZ,
            });
            ctx.Db.HideRack.Id.Delete(hide.Id);
        }
    }

    // The finished drying rack station nearest a spot (the job is set down beside it).
    static Structure? DryingRackNear(ReducerContext ctx, float x, float z)
    {
        Structure? best = null;
        float bestD = 3f;
        foreach (var s in StructuresNear(ctx, x, z))
        {
            if (!s.Complete || Structures.Shape((int)s.Kind).Station != StationKind.DryingRack) continue;
            float d = Dist(s.X, s.Z, x, z);
            if (d < bestD) (best, bestD) = (s, d);
        }
        return best;
    }

    static float HideDryRateAt(ReducerContext ctx, float x, float z) =>
        DryRateAt(ctx, x, z) * ClayRules.BaseDrySeconds / HideDrySeconds;

    static float HideMoisture(HideRack rack, long now) =>
        ClayRules.MoistureAt(rack.Moisture, rack.DryRate, (now - rack.MoistureAt.MicrosecondsSinceUnixEpoch) / 1e6f);

    // From the animal tick: every so often each wet hide re-reads rain and fire warmth.
    static void DryHides(ReducerContext ctx, long now)
    {
        foreach (var rack in ctx.Db.HideRack.Iter().ToList())
        {
            if (now - rack.MoistureAt.MicrosecondsSinceUnixEpoch < HideCheckMicros) continue;
            if (rack.Moisture <= 0f) continue;
            float moisture = HideMoisture(rack, now);
            float rate = HideDryRateAt(ctx, rack.X, rack.Z);
            ctx.Db.HideRack.Id.Update(rack with { Moisture = moisture, MoistureAt = ctx.Timestamp, DryRate = rate });
        }
    }

    [SpacetimeDB.Reducer]
    public static void TakeLeather(ReducerContext ctx, ulong rackId)
    {
        var player = RequirePlayer(ctx);
        var rack = ctx.Db.HideRack.Id.Find(rackId) ?? throw new Exception("That rack is gone");
        RequireReach(RequirePos(ctx, player), rack.X, rack.Z);
        if (HideMoisture(rack, ctx.Timestamp.MicrosecondsSinceUnixEpoch) > 0f)
            throw new Exception("The hide is still damp. Leave it to dry");
        AddToInventory(ctx, Me(ctx), Item.Leather, 1, rack.Quality);
        OnProcessPerformed(ctx, player.PlayerId, "hide.dry", rack.Quality, false);
        ctx.Db.HideRack.Id.Delete(rackId);
        // An old A-frame rack comes apart; the sticks are worth keeping, the cord is
        // worn through. A station's bar stays up for the next hide.
        if (rack.StationId == 0) AddToInventory(ctx, Me(ctx), Item.Stick, 2, Items.RawQuality);
    }
}
