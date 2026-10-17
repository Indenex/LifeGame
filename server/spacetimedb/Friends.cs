using LifeGame.Shared;
using SpacetimeDB;

// claims-32: friends. A character marks another as a friend (AddFriend) or stops (RemoveFriend).
// A one-way mark shows only names; once both have marked each other, each sees the other's
// online state and a rounded spot (FriendSpot, refreshed by SweepFriends every few seconds), so
// the client can say "Mara  40 m north" at any distance. Nobody else sees who is friends with whom.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Friend")]
    public partial struct Friend
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        // The character who marked (Me), and whom they marked.
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        public uint OwnerId;
        public string OwnerName;
        [SpacetimeDB.Index.BTree]
        public uint FriendId;
        public string FriendName;
        public Timestamp At;
    }

    // Where a marked friend roughly is, rounded to Friends.SpotStep, and whether they are online.
    [SpacetimeDB.Table(Accessor = "FriendSpot")]
    public partial struct FriendSpot
    {
        [PrimaryKey]
        public uint PlayerId;
        public bool Online;
        public float X;
        public float Z;
    }

    [SpacetimeDB.Table(Accessor = "FriendSweepTimer", Scheduled = nameof(SweepFriends), ScheduledAt = nameof(ScheduledAt))]
    public partial struct FriendSweepTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // The friends you marked, and the people who marked you (so you can mark them back).
    [SpacetimeDB.View(Accessor = "MyFriends", Public = true)]
    public static List<Friend> MyFriends(ViewContext ctx)
    {
        var result = ctx.Db.Friend.Owner.Filter(Me(ctx)).ToList();
        if (ctx.Db.Player.Identity.Find(Me(ctx)) is { } me)
            result.AddRange(ctx.Db.Friend.FriendId.Filter(me.PlayerId));
        return result;
    }

    // Online state and rounded spot of each friend who has marked you back.
    [SpacetimeDB.View(Accessor = "MyFriendSpots", Public = true)]
    public static List<FriendSpot> MyFriendSpots(ViewContext ctx)
    {
        var result = new List<FriendSpot>();
        if (ctx.Db.Player.Identity.Find(Me(ctx)) is not { } me) return result;
        foreach (var mine in ctx.Db.Friend.Owner.Filter(Me(ctx)))
            if (ctx.Db.Friend.FriendId.Filter(me.PlayerId).Any(back => back.OwnerId == mine.FriendId)
                && ctx.Db.FriendSpot.PlayerId.Find(mine.FriendId) is { } spot)
                result.Add(spot);
        return result;
    }

    [SpacetimeDB.Reducer]
    public static void AddFriend(ReducerContext ctx, uint playerId)
    {
        MeterCall(ctx);
        InitFriends(ctx);
        var me = RequirePlayer(ctx);
        var them = ctx.Db.Player.PlayerId.Find(playerId) ?? throw new Exception("No such player");
        if (them.PlayerId == me.PlayerId) throw new Exception("You are already your own friend");
        var mine = ctx.Db.Friend.Owner.Filter(Me(ctx)).ToList();
        if (mine.Any(f => f.FriendId == playerId)) throw new Exception($"{them.Name} is already your friend");
        if (mine.Count >= Friends.Max) throw new Exception($"You can keep at most {Friends.Max} friends");
        ctx.Db.Friend.Insert(new Friend
        {
            Id = 0, Owner = Me(ctx), OwnerId = me.PlayerId, OwnerName = me.Name,
            FriendId = them.PlayerId, FriendName = them.Name, At = ctx.Timestamp,
        });
        NoteFriendSpot(ctx, them.PlayerId);
        NoteFriendSpot(ctx, me.PlayerId);
    }

    [SpacetimeDB.Reducer]
    public static void RemoveFriend(ReducerContext ctx, uint playerId)
    {
        MeterCall(ctx);
        RequirePlayer(ctx);
        var row = ctx.Db.Friend.Owner.Filter(Me(ctx)).FirstOrDefault(f => f.FriendId == playerId);
        if (row.Id == 0) throw new Exception("They aren't on your friends list");
        ctx.Db.Friend.Id.Delete(row.Id);
    }

    static void InitFriends(ReducerContext ctx)
    {
        if (ctx.Db.FriendSweepTimer.Count > 0) return;
        ctx.Db.FriendSweepTimer.Insert(new FriendSweepTimer
        {
            ScheduledId = 0,
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(Friends.SweepSeconds)),
        });
    }

    // Notes where everyone someone has marked is, rounded; forgets those nobody marks any more.
    [SpacetimeDB.Reducer]
    public static void SweepFriends(ReducerContext ctx, FriendSweepTimer timer)
    {
        RequireServer(ctx);
        var marked = new HashSet<uint>();
        foreach (var f in ctx.Db.Friend.Iter())
        {
            marked.Add(f.FriendId);
            marked.Add(f.OwnerId);
        }
        foreach (var id in marked) NoteFriendSpot(ctx, id);
        foreach (var spot in ctx.Db.FriendSpot.Iter().Where(s => !marked.Contains(s.PlayerId)).ToList())
            ctx.Db.FriendSpot.PlayerId.Delete(spot.PlayerId);
    }

    // Writes a player's rounded spot, only when it changed, so friends' views stay quiet.
    static void NoteFriendSpot(ReducerContext ctx, uint playerId)
    {
        if (ctx.Db.Player.PlayerId.Find(playerId) is not { } p) return;
        var pos = ctx.Db.PlayerPos.PlayerId.Find(playerId);
        var old = ctx.Db.FriendSpot.PlayerId.Find(playerId);
        float x = pos is { } at ? MathF.Round(at.X / Friends.SpotStep) * Friends.SpotStep : old?.X ?? 0f;
        float z = pos is { } at2 ? MathF.Round(at2.Z / Friends.SpotStep) * Friends.SpotStep : old?.Z ?? 0f;
        var spot = new FriendSpot { PlayerId = playerId, Online = p.Online, X = x, Z = z };
        if (old is not { } was) ctx.Db.FriendSpot.Insert(spot);
        else if (was.Online != spot.Online || was.X != spot.X || was.Z != spot.Z) ctx.Db.FriendSpot.PlayerId.Update(spot);
    }
}
