using SpacetimeDB;

public static partial class Module
{
    // Who is sneaking right now, so other players see them crouch. A row exists only
    // while the player sneaks; clients set it when Shift changes, never per tick.
    [SpacetimeDB.Table(Accessor = "PlayerSneaking", Public = true)]
    public partial struct PlayerSneaking
    {
        [PrimaryKey]
        public uint PlayerId;
    }

    [SpacetimeDB.Reducer]
    public static void SetSneaking(ReducerContext ctx, bool sneaking)
    {
        var player = RequirePlayer(ctx);
        var row = ctx.Db.PlayerSneaking.PlayerId.Find(player.PlayerId);
        if (sneaking && row == null)
            ctx.Db.PlayerSneaking.Insert(new PlayerSneaking { PlayerId = player.PlayerId });
        else if (!sneaking && row != null)
            ctx.Db.PlayerSneaking.PlayerId.Delete(player.PlayerId);
    }

    // A player who logs off mid-sneak, sitting or lying comes back standing.
    static void StanceDisconnected(ReducerContext ctx, Player player)
    {
        ctx.Db.PlayerSneaking.PlayerId.Delete(player.PlayerId);
        StopResting(ctx, player.PlayerId);
    }
}
