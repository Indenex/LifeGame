using LifeGame.Shared;
using SpacetimeDB;

// Waking places (survival-23). Dying wakes you at the wilderness edge, unless you chose a rest
// spot (a laid sleeping mat today, beds later: shared/RestSpots.cs) as your waking place. Laying
// a mat no longer sets it silently; the player is asked (the client offers "Wake here after a
// fall?") and SetRespawn records it. Server-validated (D-182): you must have a right to the
// building (owner, builder or claim member), be within reach of the spot, and the spot must stand.
// If it is gone when you die, you wake at the edge and are told so. No penalty, no cooldown (D-081).
public static partial class Module
{
    // A player's chosen waking place. Private; the client reads its own row through my_respawn_point.
    [SpacetimeDB.Table(Accessor = "RespawnPoint")]
    public partial struct RespawnPoint
    {
        [PrimaryKey]
        public uint PlayerId;
        public ushort Spot;
        public ulong StructureId;
        public float X;
        public float Z;
    }

    [SpacetimeDB.View(Accessor = "MyRespawnPoint", Public = true)]
    public static List<RespawnPoint> MyRespawnPoint(ViewContext ctx) =>
        ctx.Db.Player.Identity.Find(Me(ctx)) is { } p && ctx.Db.RespawnPoint.PlayerId.Find(p.PlayerId) is { } r
            ? new List<RespawnPoint> { r }
            : new List<RespawnPoint>();

    // House building implements this: whether the player's laid-out item rest spot (their mat) is
    // still there at that building and place.
    static partial void LaidRestSpotHook(ReducerContext ctx, uint playerId, RestSpotDef spot, ulong structureId, ref float x, ref float z, ref bool stands);

    // Where a rest spot stands now, or false if it is gone.
    static bool RestSpotStands(ReducerContext ctx, uint playerId, RestSpotDef spot, ulong structureId, out float x, out float z)
    {
        x = z = 0f;
        if (ctx.Db.Structure.Id.Find(structureId) is not { Complete: true } s) return false;
        if (spot.Item != null)
        {
            bool stands = false;
            LaidRestSpotHook(ctx, playerId, spot, structureId, ref x, ref z, ref stands);
            return stands;
        }
        if ((int)s.Kind != spot.StructureKind) return false;
        x = s.X;
        z = s.Z;
        return true;
    }

    // The rest spot at this building that the player may use and is close enough to: its definition and
    // where it stands. Shared by choosing a waking place and lying down on it.
    static RestSpotDef RequireRestSpot(ReducerContext ctx, Player player, PlayerPos pos, ushort spotId, ulong structureId, out float x, out float z)
    {
        var spot = RestSpots.Find(spotId) ?? throw new Exception("That is not somewhere to sleep");
        var s = ctx.Db.Structure.Id.Find(structureId) ?? throw new Exception("That building is gone");
        if (!MayLiveIn(ctx, s))
            throw new Exception("This isn't your home: only its builders or the claim's members can wake here");
        if (!RestSpotStands(ctx, player.PlayerId, spot, structureId, out x, out z))
            throw new Exception($"There is no {spot.Name} here to wake on");
        RequireReach(pos, x, z);
        return spot;
    }

    // Chooses a rest spot as your waking place. Replaces any earlier one.
    [SpacetimeDB.Reducer]
    public static void SetRespawn(ReducerContext ctx, ushort spotId, ulong structureId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        RequireRestSpot(ctx, player, pos, spotId, structureId, out float x, out float z);
        var row = new RespawnPoint { PlayerId = player.PlayerId, Spot = spotId, StructureId = structureId, X = x, Z = z };
        if (ctx.Db.RespawnPoint.PlayerId.Find(player.PlayerId) is not null) ctx.Db.RespawnPoint.PlayerId.Update(row);
        else ctx.Db.RespawnPoint.Insert(row);
    }

    // Lie down on a rest spot you may use (survival-27): the same pose as Rest(RestLie), but on your mat, so
    // everyone sees you asleep there. Walking off stands you up, as ever. Just a pose (Justin's pick).
    [SpacetimeDB.Reducer]
    public static void LieOnRestSpot(ReducerContext ctx, ushort spotId, ulong structureId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        RequireRestSpot(ctx, player, pos, spotId, structureId, out _, out _);
        if (IsFlyingAdmin(ctx, player.PlayerId)) throw new Exception("Not while flying");
        var row = new PlayerRest { PlayerId = player.PlayerId, Kind = RestLie, SpotId = structureId };
        if (ctx.Db.PlayerRest.PlayerId.Find(player.PlayerId) is null) ctx.Db.PlayerRest.Insert(row);
        else ctx.Db.PlayerRest.PlayerId.Update(row);
        ctx.Db.PlayerSneaking.PlayerId.Delete(player.PlayerId);
    }

    // Where a player wakes after dying: their chosen rest spot while it stands, else Survival's default.
    // Returns how they woke (RestSpots.WokeAt...). A spot that is gone is forgotten here.
    static byte ChooseWake(ReducerContext ctx, uint playerId, ref float x, ref float z)
    {
        if (ctx.Db.RespawnPoint.PlayerId.Find(playerId) is not { } point) return RestSpots.WokeAtEdge;
        if (RestSpots.Find(point.Spot) is { } spot && RestSpotStands(ctx, playerId, spot, point.StructureId, out float sx, out float sz))
        {
            x = sx;
            z = sz;
            return RestSpots.WokeAtRestSpot;
        }
        ctx.Db.RespawnPoint.PlayerId.Delete(playerId);
        return RestSpots.WokeBedGone;
    }
}
