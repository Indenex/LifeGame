using LifeGame.Shared;
using SpacetimeDB;

// The admin item panel (admin-1). The server decides who may give themselves items:
// the admin list is private (WorldAdmin), and a client only ever sees whether it is
// itself on it (my_admin). The first admin is the publisher, who adds players with
// `spacetime call <db> make_admin '"Name"'` (docs/ADMIN.md).
public static partial class Module
{
    // A client reads only its own row of the private admin list, to know whether to show the panel.
    [SpacetimeDB.View(Accessor = "MyAdmin", Public = true)]
    public static List<WorldAdmin> MyAdmin(ViewContext ctx) =>
        ctx.Db.WorldAdmin.Identity.Find(ctx.Sender) is { } me ? new List<WorldAdmin> { me } : new List<WorldAdmin>();

    [SpacetimeDB.Reducer]
    public static void AdminGive(ReducerContext ctx, ushort item, uint count, byte quality)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can give items");
        RequirePlayer(ctx);
        if (!Items.IsKnown(item)) throw new Exception("No such item");
        if (count < 1 || count > AdminRules.GiveMax) throw new Exception($"Give between 1 and {AdminRules.GiveMax}");
        var def = Items.Def((Item)item);
        // Raw materials always come at a flat quality; made things at the quality asked, 1 to 100.
        byte made = def.HasQuality ? Math.Clamp(quality, (byte)1, (byte)100) : Items.RawQuality;
        AddToInventory(ctx, ctx.Sender, (Item)item, count, made);
        Log.Info($"admin_give: {ctx.Sender} gave themselves {count} x {def.Name} (quality {made})");
    }

    // Puts the admin's own player at a spot (admin-only; D-182). Used by the CI check bots as a last resort
    // when a walk is stuck: the tools log every use, and walking itself is tested in one place.
    [SpacetimeDB.Reducer]
    public static void AdminTeleport(ReducerContext ctx, float x, float z)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can teleport");
        if (!float.IsFinite(x) || !float.IsFinite(z) || MathF.Abs(x) > AdminRules.TeleportMax || MathF.Abs(z) > AdminRules.TeleportMax)
            throw new Exception("That spot is outside the world");
        var player = RequirePlayer(ctx);
        var state = ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) ?? throw new Exception("No state");
        var pos = RequirePos(ctx, player);
        // A step the player reported but the tick has not applied yet would pull them back.
        ctx.Db.MoveInput.PlayerId.Delete(player.PlayerId);
        ctx.Db.PlayerPos.PlayerId.Update(pos with { X = x, Z = z, ChunkX = (short)World.ChunkOf(x), ChunkZ = (short)World.ChunkOf(z) });
        ctx.Db.PlayerState.PlayerId.Update(state with { LastMoveAt = ctx.Timestamp });
        Log.Info($"admin_teleport: {ctx.Sender} moved to ({x:F1}, {z:F1})");
    }

    // Admins who are flying (/fly). Private: only the server reads it.
    [SpacetimeDB.Table(Accessor = "AdminFlyer")]
    public partial struct AdminFlyer
    {
        [PrimaryKey]
        public uint PlayerId;
    }

    [SpacetimeDB.Reducer]
    public static void AdminFly(ReducerContext ctx, bool on)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can fly");
        var player = RequirePlayer(ctx);
        bool now = ctx.Db.AdminFlyer.PlayerId.Find(player.PlayerId) is not null;
        if (on && !now) ctx.Db.AdminFlyer.Insert(new AdminFlyer { PlayerId = player.PlayerId });
        else if (!on && now) ctx.Db.AdminFlyer.PlayerId.Delete(player.PlayerId);
        Log.Info($"admin_fly: {ctx.Sender} {(on ? "took off" : "landed")}");
    }

    // A flying admin; checked again as an admin each time, so losing admin ends the exemption.
    static bool IsFlyingAdmin(ReducerContext ctx, uint playerId) =>
        ctx.Db.AdminFlyer.PlayerId.Find(playerId) is not null && IsWorldAdmin(ctx);
}
