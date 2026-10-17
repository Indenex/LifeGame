using SpacetimeDB;

// Reducer rate limit (bugfix-50, audit item 9 of bugfix-24, D-182). Every reducer that acts for
// a player goes through RequirePlayer, which spends one call from that player's bucket: a burst
// of CallBurst calls, refilled at CallRefillPerSecond. A hacked client that hammers PlaceStructure,
// Drop, SendChat and the like gets "Slow down" once the bucket is empty; an honest client (a held
// key repeats about once a second, the busiest UI a few calls a second) never meets it. Move has
// its own distance budget and Heartbeat is a timer, so those two skip the bucket.
//
// A refused call throws, which rolls back the call and so the bucket row too: a refusal costs
// nothing, and the bucket keeps refilling with time, so the sustained rate that gets through is
// the refill rate.
public static partial class Module
{
    public const double CallBurst = 60;
    public const double CallRefillPerSecond = 30;

    // Private: only the server reads it.
    [SpacetimeDB.Table(Accessor = "CallBucket")]
    public partial struct CallBucket
    {
        [PrimaryKey]
        public uint PlayerId;
        public double Tokens;
        public Timestamp At;
    }

    static void SpendCall(ReducerContext ctx, uint playerId)
    {
        var row = ctx.Db.CallBucket.PlayerId.Find(playerId);
        double tokens = row is { } b
            ? Math.Min(CallBurst, b.Tokens + Math.Max(0, SecondsBetween(b.At, ctx.Timestamp)) * CallRefillPerSecond)
            : CallBurst;
        if (tokens < 1) throw new Exception("Slow down");
        var next = new CallBucket { PlayerId = playerId, Tokens = tokens - 1, At = ctx.Timestamp };
        if (row is null) ctx.Db.CallBucket.Insert(next);
        else ctx.Db.CallBucket.PlayerId.Update(next);
    }
}
