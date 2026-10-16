using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// Leather (animals-5): a sheep hide is scraped clean with a blade (process
// hide.scrape), then stretched on a little stick rack to dry (hide.stretch, set
// down in front of the crafter). It dries like clay: slower in rain, faster by a
// fire. When dry, G takes it off the rack as leather, at the quality of the scraping.
public static partial class Module
{
    // A hide on a rack takes this long to dry in fair weather, away from a fire.
    const float HideDrySeconds = 360f;
    // Racks re-read the weather this often.
    const long HideCheckMicros = 15_000_000;
    // A player can have this many hides drying at once.
    const int MaxRacksPerPlayer = 4;

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
    }

    // Called by the crafting Placed outcome for hide.stretch.
    static void PlaceHideRack(ReducerContext ctx, CraftJob job, ProcessDef def)
    {
        if (ctx.Db.HideRack.MadeBy.Filter(job.PlayerId).Count() >= MaxRacksPerPlayer)
        {
            // No room: hand everything back rather than lose it.
            foreach (var input in def.Consumes)
                AddToInventory(ctx, job.Owner, input.Item, input.Count, input.Item == Item.ScrapedHide ? job.Quality : Items.RawQuality);
            News(ctx, job.PlayerId, $"You already have {MaxRacksPerPlayer} hides drying. The hide goes back in your bag.");
            return;
        }
        var crafter = ctx.Db.PlayerPos.PlayerId.Find(job.PlayerId);
        float heading = crafter is { } c ? MathF.Atan2(job.X - c.X, job.Z - c.Z) : 0f;
        ctx.Db.HideRack.Insert(new HideRack
        {
            Id = 0,
            MadeBy = job.PlayerId,
            X = job.X,
            Z = job.Z,
            Heading = heading,
            ChunkX = World.ChunkOf(job.X),
            ChunkZ = World.ChunkOf(job.Z),
            Quality = job.Quality,
            Moisture = 1f,
            MoistureAt = ctx.Timestamp,
            DryRate = HideDryRateAt(ctx, job.X, job.Z),
        });
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
        AddToInventory(ctx, ctx.Sender, Item.Leather, 1, rack.Quality);
        OnProcessPerformed(ctx, player.PlayerId, "hide.dry", rack.Quality, false);
        ctx.Db.HideRack.Id.Delete(rackId);
        // The rack comes apart; the sticks are worth keeping, the cord is worn through.
        AddToInventory(ctx, ctx.Sender, Item.Stick, 2, Items.RawQuality);
    }
}
