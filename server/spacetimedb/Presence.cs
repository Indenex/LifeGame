using SpacetimeDB;

public static partial class Module
{
    // A client that is killed or loses power may never trigger ClientDisconnected, which
    // leaves its player "Online" for ever. Clients send Heartbeat every HeartbeatSeconds;
    // the 30 s tick marks anyone silent for StaleSeconds offline, as a disconnect would.
    const int StaleSeconds = 75;

    [SpacetimeDB.Table(Accessor = "PlayerSeen")]
    public partial struct PlayerSeen
    {
        [PrimaryKey]
        public uint PlayerId;
        public Timestamp At;
    }

    static void MarkSeen(ReducerContext ctx, uint playerId)
    {
        var row = new PlayerSeen { PlayerId = playerId, At = ctx.Timestamp };
        if (ctx.Db.PlayerSeen.PlayerId.Find(playerId) != null) ctx.Db.PlayerSeen.PlayerId.Update(row);
        else ctx.Db.PlayerSeen.Insert(row);
    }

    [SpacetimeDB.Reducer]
    public static void Heartbeat(ReducerContext ctx)
    {
        var player = RequirePlayer(ctx);
        MarkSeen(ctx, player.PlayerId);
        if (!player.Online)
        {
            // Swept while the connection was still alive (long stall): come back as a connect would.
            var back = player with { Online = true };
            ctx.Db.Player.Identity.Update(back);
            SurvivalConnected(ctx, back);
        }
    }

    static void SweepPresence(ReducerContext ctx)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var player in ctx.Db.Player.Iter().Where(p => p.Online).ToList())
        {
            if (ctx.Db.PlayerSeen.PlayerId.Find(player.PlayerId) is not { } seen)
            {
                MarkSeen(ctx, player.PlayerId); // online before this existed: grace period
                continue;
            }
            if (now - seen.At.MicrosecondsSinceUnixEpoch < StaleSeconds * 1_000_000L) continue;
            ctx.Db.Player.Identity.Update(player with { Online = false });
            SurvivalDisconnected(ctx, player);
            StanceDisconnected(ctx, player);
            Log.Info($"Presence: {player.Name} went quiet, marked offline");
        }
    }
}
