using LifeGame.Shared;
using SpacetimeDB;

// Wading and floating train Swimming (swim-1). The server watches where players stand on each
// survival tick, so a client can't claim water time it didn't spend: every Wading.WaterPerUse
// seconds of moving through water is one report of swim.wade (feet on the bed) or swim.float
// (afloat past Wading.FloatDepth), like move.run's stretches.
public static partial class Module
{
    // Seconds spent moving in water since the last report. Private.
    [SpacetimeDB.Table(Accessor = "WaterTime")]
    public partial struct WaterTime
    {
        [PrimaryKey]
        public uint PlayerId;
        public float WadeSeconds;
        public float FloatSeconds;
    }

    static void NoteSwimming(ReducerContext ctx, Player player, PlayerPos pos)
    {
        float depth = Wading.Depth(World.Height(World.CellOf(pos.X), World.CellOf(pos.Z),
            ctx.Db.TerrainEdit.CellKey.Find(World.CellKey(World.CellOf(pos.X), World.CellOf(pos.Z))) is { } edit ? edit.Depth : (short)0));
        var row = ctx.Db.WaterTime.PlayerId.Find(player.PlayerId);
        // Standing still in the water is no swimming lesson: they must have moved within the last tick or two.
        bool moving = depth > 0f && ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) is { } state
            && SecondsBetween(state.LastMoveAt, ctx.Timestamp) < SurvivalTickMillis * 0.0015f;
        if (!moving) return;

        float tick = SurvivalTickMillis / 1000f;
        var time = row ?? new WaterTime { PlayerId = player.PlayerId };
        if (Wading.Floating(depth)) time.FloatSeconds += tick; else time.WadeSeconds += tick;

        if (time.FloatSeconds >= Wading.WaterPerUse)
        {
            time.FloatSeconds -= Wading.WaterPerUse;
            OnProcessPerformed(ctx, player.PlayerId, Wading.FloatProcess, Items.RawQuality, false);
        }
        if (time.WadeSeconds >= Wading.WaterPerUse)
        {
            time.WadeSeconds -= Wading.WaterPerUse;
            OnProcessPerformed(ctx, player.PlayerId, Wading.WadeProcess, Items.RawQuality, false);
        }
        if (row == null) ctx.Db.WaterTime.Insert(time); else ctx.Db.WaterTime.PlayerId.Update(time);
    }
}
