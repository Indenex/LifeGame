using LifeGame.Shared;
using SpacetimeDB;

// Sitting and lying down (Animation lane, anim-12): who is resting and how, so every
// client poses them. A row exists only while the player rests; stepping off ends it.
// Purely a pose for now: rest spots (Survival's mats and beds) can fill SpotId and the
// seat height later without changing the table.
public static partial class Module
{
    public const byte RestSit = 1, RestLie = 2;

    [SpacetimeDB.Table(Accessor = "PlayerRest", Public = true)]
    public partial struct PlayerRest
    {
        [PrimaryKey]
        public uint PlayerId;
        // RestSit or RestLie.
        public byte Kind;
        // How high the hips sit, in metres: 0 on the ground, a log's or bench's height on one.
        public float Seat;
        // The rest spot used (a laid mat or a bed), 0 for the bare ground.
        public ulong SpotId;
    }

    // Sit down, or lie down, where you stand. Not while afloat or flying.
    [SpacetimeDB.Reducer]
    public static void Rest(ReducerContext ctx, byte kind)
    {
        if (kind != RestSit && kind != RestLie) throw new Exception("No such way to rest");
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        if (IsFlyingAdmin(ctx, player.PlayerId)) throw new Exception("Not while flying");
        if (Wading.Depth(World.BaseHeight(World.CellOf(pos.X), World.CellOf(pos.Z))) > Wading.FloatDepth * 0.5f)
            throw new Exception("Too deep to rest here");
        var row = new PlayerRest { PlayerId = player.PlayerId, Kind = kind };
        if (ctx.Db.PlayerRest.PlayerId.Find(player.PlayerId) is null) ctx.Db.PlayerRest.Insert(row);
        else ctx.Db.PlayerRest.PlayerId.Update(row);
        ctx.Db.PlayerSneaking.PlayerId.Delete(player.PlayerId);
    }

    [SpacetimeDB.Reducer]
    public static void StandUp(ReducerContext ctx) => StopResting(ctx, RequirePlayer(ctx).PlayerId);

    // Moving, logging off, or anything else that gets you on your feet.
    static void StopResting(ReducerContext ctx, uint playerId) => ctx.Db.PlayerRest.PlayerId.Delete(playerId);
}
