using LifeGame.Shared;
using SpacetimeDB;

// The jump (bugfix-25). The client plays your own jump at once; this row tells everyone
// else so they see the pose and hear it, and it is where the server will check a jump
// later (stamina, arc, collision: bugfix-24, D-182).
public static partial class Module
{
    // One row per player; Seq counts their jumps so every jump is an update.
    [SpacetimeDB.Table(Accessor = "PlayerJump", Public = true)]
    public partial struct PlayerJump
    {
        [PrimaryKey]
        public uint PlayerId;
        public uint Seq;
        public Timestamp At;
    }

    [SpacetimeDB.Reducer]
    public static void Jump(ReducerContext ctx)
    {
        var player = RequirePlayer(ctx);
        if (ctx.Db.PlayerSneaking.PlayerId.Find(player.PlayerId) is not null)
            throw new Exception("You can't jump while sneaking");
        var last = ctx.Db.PlayerJump.PlayerId.Find(player.PlayerId);
        // A little under the client's cooldown, so a lagging client isn't refused.
        if (last is { } l && SecondsBetween(l.At, ctx.Timestamp) < Jumping.CooldownSeconds * 0.8f)
            throw new Exception("Too soon");
        var row = new PlayerJump { PlayerId = player.PlayerId, Seq = (last?.Seq ?? 0) + 1, At = ctx.Timestamp };
        if (last is null) ctx.Db.PlayerJump.Insert(row);
        else ctx.Db.PlayerJump.PlayerId.Update(row);
    }
}
