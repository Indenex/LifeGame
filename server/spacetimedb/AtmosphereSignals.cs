using System;
using System.Collections.Generic;
using LifeGame.Shared;
using SpacetimeDB;

// Smoke signals seen from afar (atmosphere-32). A fire fed past three quarters of the
// most it can hold puts up a tall column of smoke; near fires the client sees from
// the fire table, but that only reaches about 160 m. This small, world-wide table
// lists just the signalling fires, so a column can be seen from right across the land.
//
// The server decides who signals (D-182): only fires in the open or on an outdoor
// raised hearth, never one inside a home (Justin), and only above 75% of the fire's
// own cap. Each row says until when the fire stays above that line, so the client
// lets the column fade on time without the server touching the row again; the weather
// tick refreshes the list.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "SignalFire", Public = true)]
    public partial struct SignalFire
    {
        [PrimaryKey]
        public ulong FireId;
        public float X;
        public float Z;
        // The fire signals until this moment, when its fuel drops below the line.
        public Timestamp Until;
    }

    // The share of its cap a fire must hold to signal.
    const float SignalShare = 0.75f;

    // From the weather tick: bring the list in line with the fires.
    static void SweepSignalFires(ReducerContext ctx)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        var keep = new HashSet<ulong>();
        foreach (var fire in ctx.Db.Fire.Iter())
        {
            long until = fire.FuelUntil.MicrosecondsSinceUnixEpoch - (long)(SignalShare * FireCapSeconds(fire) * 1_000_000);
            if (until <= now || FireIndoors(ctx, fire)) continue;
            keep.Add(fire.Id);
            var row = new SignalFire { FireId = fire.Id, X = fire.X, Z = fire.Z, Until = new Timestamp(until) };
            if (ctx.Db.SignalFire.FireId.Find(fire.Id) is { } old)
            {
                if (old.Until.MicrosecondsSinceUnixEpoch != until) ctx.Db.SignalFire.FireId.Update(row);
            }
            else ctx.Db.SignalFire.Insert(row);
        }
        var gone = new List<ulong>();
        foreach (var row in ctx.Db.SignalFire.Iter())
            if (!keep.Contains(row.FireId)) gone.Add(row.FireId);
        foreach (var id in gone) ctx.Db.SignalFire.FireId.Delete(id);
    }

    // The most fuel this fire can hold, in seconds, hearth included: Crafting keeps it
    // per fire (the lighter's cap, or a feeder's if higher). 0 marks a fire from before.
    static float FireCapSeconds(Fire fire) => fire.FuelCapSeconds > 0f ? fire.FuelCapSeconds : Fires.MaxSeconds;

    // Whether the fire sits inside a finished home (a building with a doorway).
    static bool FireIndoors(ReducerContext ctx, Fire fire)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var s in ctx.Db.Structure.ByChunk.Filter((fire.ChunkX + dx, fire.ChunkZ + dz)))
                    if (s.Complete && Structures.HasDoorway((int)s.Kind) && Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, fire.X, fire.Z))
                        return true;
        return false;
    }
}
