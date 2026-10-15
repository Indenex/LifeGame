using LifeGame.Shared;
using SpacetimeDB;

// Weathering (D-046): outside claims, anything players made that nobody has touched
// for a while weathers away, so abandoned things don't pile up on the server. It
// starts after Structures.WeatherAfterHours untouched and is gone WeatherForHours
// later. Any interaction resets the clock; inside a claim nothing weathers.
// One sweep an hour keeps this cheap.
public static partial class Module
{
    // When a structure was last built on, opened or used. Public so clients can show weathering.
    [SpacetimeDB.Table(Accessor = "StructureTouch", Public = true)]
    public partial struct StructureTouch
    {
        [PrimaryKey]
        public ulong StructureId;
        public Timestamp LastTouched;
    }

    // Ground items that came from players (dropped, refunded, spilled); natural
    // sticks and stones are not tracked and never weather.
    [SpacetimeDB.Table(Accessor = "GroundItemTouch")]
    public partial struct GroundItemTouch
    {
        [PrimaryKey]
        public ulong ItemId;
        public Timestamp LastTouched;
    }

    [SpacetimeDB.Table(Accessor = "WeatherSweepTimer", Scheduled = nameof(WeatherSweep), ScheduledAt = nameof(ScheduledAt))]
    public partial struct WeatherSweepTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    const double WeatherSweepMinutes = 60;

    static void TouchStructure(ReducerContext ctx, ulong structureId)
    {
        EnsureWeatherSweep(ctx);
        var row = new StructureTouch { StructureId = structureId, LastTouched = ctx.Timestamp };
        if (ctx.Db.StructureTouch.StructureId.Find(structureId) is null) ctx.Db.StructureTouch.Insert(row);
        else ctx.Db.StructureTouch.StructureId.Update(row);
    }

    static void TouchGroundItem(ReducerContext ctx, WorldItem item)
    {
        EnsureWeatherSweep(ctx);
        var row = new GroundItemTouch { ItemId = item.Id, LastTouched = ctx.Timestamp };
        if (ctx.Db.GroundItemTouch.ItemId.Find(item.Id) is null) ctx.Db.GroundItemTouch.Insert(row);
        else ctx.Db.GroundItemTouch.ItemId.Update(row);
    }

    // Crafting calls this from Drop with the new WorldItem row, so dropped things can weather.
    static partial void OnPlayerDroppedItem(ReducerContext ctx, WorldItem item);
    static partial void OnPlayerDroppedItem(ReducerContext ctx, WorldItem item) => TouchGroundItem(ctx, item);

    // Claims implements this: true when the point is inside any claim.
    static partial void InClaimHook(ReducerContext ctx, float x, float z, ref bool inClaim);

    static bool InClaim(ReducerContext ctx, float x, float z)
    {
        bool inClaim = false;
        InClaimHook(ctx, x, z, ref inClaim);
        return inClaim;
    }

    static void EnsureWeatherSweep(ReducerContext ctx)
    {
        if (ctx.Db.WeatherSweepTimer.Count > 0) return;
        ctx.Db.WeatherSweepTimer.Insert(new WeatherSweepTimer
        {
            ScheduledId = 0,
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMinutes(WeatherSweepMinutes)),
        });
    }

    [SpacetimeDB.Reducer]
    public static void WeatherSweep(ReducerContext ctx, WeatherSweepTimer timer)
    {
        if (ctx.Sender != ctx.Identity)
            throw new Exception("Only the server weathers things");
        SweepWeathering(ctx, ctx.Timestamp);
    }

    // Removes what has fully weathered; things inside a claim have their clock reset instead.
    static void SweepWeathering(ReducerContext ctx, Timestamp now)
    {
        long goneAfter = (long)((Structures.WeatherAfterHours + Structures.WeatherForHours) * 3600e6);
        long nowMicros = now.MicrosecondsSinceUnixEpoch;

        // Structures from before weathering existed start their clock now.
        foreach (var s in ctx.Db.Structure.Iter().ToList())
            if (ctx.Db.StructureTouch.StructureId.Find(s.Id) is null)
                ctx.Db.StructureTouch.Insert(new StructureTouch { StructureId = s.Id, LastTouched = now });

        foreach (var touch in ctx.Db.StructureTouch.Iter().ToList())
        {
            if (ctx.Db.Structure.Id.Find(touch.StructureId) is not { } s)
            {
                ctx.Db.StructureTouch.StructureId.Delete(touch.StructureId);
                continue;
            }
            if (InClaim(ctx, s.X, s.Z))
            {
                ctx.Db.StructureTouch.StructureId.Update(touch with { LastTouched = now });
                continue;
            }
            if (nowMicros - touch.LastTouched.MicrosecondsSinceUnixEpoch < goneAfter) continue;
            foreach (var row in ctx.Db.BasketItem.StructureId.Filter(s.Id).ToList())
                ctx.Db.BasketItem.Id.Delete(row.Id);
            RemoveStructure(ctx, s);
        }

        foreach (var touch in ctx.Db.GroundItemTouch.Iter().ToList())
        {
            if (ctx.Db.WorldItem.Id.Find(touch.ItemId) is not { } item)
            {
                ctx.Db.GroundItemTouch.ItemId.Delete(touch.ItemId);
                continue;
            }
            if (InClaim(ctx, item.X, item.Z))
            {
                ctx.Db.GroundItemTouch.ItemId.Update(touch with { LastTouched = now });
                continue;
            }
            if (nowMicros - touch.LastTouched.MicrosecondsSinceUnixEpoch < goneAfter) continue;
            ctx.Db.WorldItem.Id.Delete(item.Id);
            ctx.Db.GroundItemTouch.ItemId.Delete(touch.ItemId);
        }
    }
}
