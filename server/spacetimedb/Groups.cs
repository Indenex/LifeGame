using LifeGame.Shared;
using SpacetimeDB;

// ui-38: groups, a shell (Justin: "just build a shell right now"). Invite from the right-click
// player menu; the invite waits Groups.InviteMicros for an answer; accepting forms a group (or
// joins the inviter's). No leader: anyone leaves any time, a member offline for a day drops out
// (SweepGroups), and a group of one ends. Members see each other through MyGroup; nobody else
// sees who is grouped. Group chat ("/g ...") is delivered to members only (Chat.cs).
// Perks are data for later (Groups.Perks); nothing here changes for them.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "PlayerGroup")]
    public partial struct PlayerGroup
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public Timestamp FormedAt;
    }

    [SpacetimeDB.Table(Accessor = "GroupMember")]
    public partial struct GroupMember
    {
        [PrimaryKey]
        public Identity Member;
        [Unique]
        public uint PlayerId;
        [SpacetimeDB.Index.BTree]
        public ulong GroupId;
        public string Name;
        public Timestamp JoinedAt;
    }

    [SpacetimeDB.Table(Accessor = "GroupInvite")]
    public partial struct GroupInvite
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Invitee;
        [SpacetimeDB.Index.BTree]
        public Identity From;
        public uint InviteeId;
        public uint FromId;
        public string FromName;
        public Timestamp At;
    }

    [SpacetimeDB.Table(Accessor = "GroupSweepTimer", Scheduled = nameof(SweepGroups), ScheduledAt = nameof(ScheduledAt))]
    public partial struct GroupSweepTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // Everyone in your group, you included; empty when you are in none.
    [SpacetimeDB.View(Accessor = "MyGroup", Public = true)]
    public static List<GroupMember> MyGroup(ViewContext ctx) =>
        ctx.Db.GroupMember.Member.Find(Me(ctx)) is { } me ? ctx.Db.GroupMember.GroupId.Filter(me.GroupId).ToList() : new List<GroupMember>();

    // Invitations to you, and the ones you sent that wait for an answer.
    [SpacetimeDB.View(Accessor = "MyGroupInvites", Public = true)]
    public static List<GroupInvite> MyGroupInvites(ViewContext ctx)
    {
        var result = ctx.Db.GroupInvite.Invitee.Filter(Me(ctx)).ToList();
        result.AddRange(ctx.Db.GroupInvite.From.Filter(Me(ctx)));
        return result;
    }

    static void InitGroups(ReducerContext ctx)
    {
        if (ctx.Db.GroupSweepTimer.Count > 0) return;
        ctx.Db.GroupSweepTimer.Insert(new GroupSweepTimer
        {
            ScheduledId = 0,
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(30)),
        });
    }

    static bool InviteLapsed(ReducerContext ctx, GroupInvite invite) =>
        ctx.Timestamp.MicrosecondsSinceUnixEpoch - invite.At.MicrosecondsSinceUnixEpoch > Groups.InviteMicros;

    [SpacetimeDB.Reducer]
    public static void InviteToGroup(ReducerContext ctx, uint playerId)
    {
        InitGroups(ctx);
        var me = RequirePlayer(ctx);
        var myPos = RequirePos(ctx, me);
        var them = ctx.Db.Player.PlayerId.Find(playerId) ?? throw new Exception("No such player");
        if (them.Identity == Me(ctx)) throw new Exception("You can't invite yourself");
        if (!them.Online) throw new Exception($"{them.Name} isn't here");
        var theirPos = ctx.Db.PlayerPos.PlayerId.Find(playerId) ?? throw new Exception($"{them.Name} isn't here");
        float dx = theirPos.X - myPos.X, dz = theirPos.Z - myPos.Z;
        if (dx * dx + dz * dz > Groups.InviteReach * Groups.InviteReach) throw new Exception($"Walk closer to {them.Name} to invite them");
        if (ctx.Db.GroupMember.Member.Find(them.Identity) is { } theirs)
            throw new Exception(ctx.Db.GroupMember.Member.Find(Me(ctx)) is { } mine && mine.GroupId == theirs.GroupId
                ? $"{them.Name} is already in your group"
                : $"{them.Name} is already in a group");
        if (ctx.Db.GroupMember.Member.Find(Me(ctx)) is { } my && ctx.Db.GroupMember.GroupId.Filter(my.GroupId).Count() >= Groups.MaxMembers)
            throw new Exception($"A group holds at most {Groups.MaxMembers}");
        foreach (var old in ctx.Db.GroupInvite.From.Filter(Me(ctx)).Where(i => i.Invitee == them.Identity).ToList())
            ctx.Db.GroupInvite.Id.Delete(old.Id);
        ctx.Db.GroupInvite.Insert(new GroupInvite
        {
            Id = 0, Invitee = them.Identity, From = Me(ctx), InviteeId = them.PlayerId, FromId = me.PlayerId,
            FromName = me.Name, At = ctx.Timestamp,
        });
    }

    [SpacetimeDB.Reducer]
    public static void AnswerGroup(ReducerContext ctx, ulong inviteId, bool accept)
    {
        InitGroups(ctx);
        var me = RequirePlayer(ctx);
        var invite = ctx.Db.GroupInvite.Id.Find(inviteId) ?? throw new Exception("That invitation is gone");
        if (invite.Invitee != Me(ctx)) throw new Exception("That invitation isn't yours");
        ctx.Db.GroupInvite.Id.Delete(invite.Id);
        if (!accept) return;
        if (InviteLapsed(ctx, invite)) throw new Exception("That invitation has expired. Ask for a new one");
        if (ctx.Db.GroupMember.Member.Find(Me(ctx)) != null) throw new Exception("Leave your group first");
        var inviter = ctx.Db.Player.Identity.Find(invite.From) ?? throw new Exception("They're gone");

        ulong groupId;
        if (ctx.Db.GroupMember.Member.Find(invite.From) is { } theirs)
        {
            groupId = theirs.GroupId;
            if (ctx.Db.GroupMember.GroupId.Filter(groupId).Count() >= Groups.MaxMembers)
                throw new Exception($"{inviter.Name}'s group is full");
        }
        else
        {
            groupId = ctx.Db.PlayerGroup.Insert(new PlayerGroup { Id = 0, FormedAt = ctx.Timestamp }).Id;
            ctx.Db.GroupMember.Insert(new GroupMember
            {
                Member = inviter.Identity, PlayerId = inviter.PlayerId, GroupId = groupId, Name = inviter.Name, JoinedAt = ctx.Timestamp,
            });
        }
        ctx.Db.GroupMember.Insert(new GroupMember
        {
            Member = Me(ctx), PlayerId = me.PlayerId, GroupId = groupId, Name = me.Name, JoinedAt = ctx.Timestamp,
        });
        // One group at a time: other invitations to you lapse.
        foreach (var other in ctx.Db.GroupInvite.Invitee.Filter(Me(ctx)).ToList())
            ctx.Db.GroupInvite.Id.Delete(other.Id);
    }

    [SpacetimeDB.Reducer]
    public static void LeaveGroup(ReducerContext ctx)
    {
        MeterCall(ctx);
        var mine = ctx.Db.GroupMember.Member.Find(Me(ctx)) ?? throw new Exception("You're not in a group");
        RemoveFromGroup(ctx, mine);
    }

    // Takes one member out, and ends the group when one is left.
    static void RemoveFromGroup(ReducerContext ctx, GroupMember member)
    {
        ctx.Db.GroupMember.Member.Delete(member.Member);
        foreach (var sent in ctx.Db.GroupInvite.From.Filter(member.Member).ToList())
            ctx.Db.GroupInvite.Id.Delete(sent.Id);
        var left = ctx.Db.GroupMember.GroupId.Filter(member.GroupId).ToList();
        if (left.Count > 1) return;
        foreach (var last in left) ctx.Db.GroupMember.Member.Delete(last.Member);
        ctx.Db.PlayerGroup.Id.Delete(member.GroupId);
    }

    // Lapsed invitations go; members away for a day drop out.
    [SpacetimeDB.Reducer]
    public static void SweepGroups(ReducerContext ctx, GroupSweepTimer timer)
    {
        RequireServer(ctx);
        foreach (var invite in ctx.Db.GroupInvite.Iter().Where(i => InviteLapsed(ctx, i)).ToList())
            ctx.Db.GroupInvite.Id.Delete(invite.Id);
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var member in ctx.Db.GroupMember.Iter().ToList())
        {
            if (ctx.Db.GroupMember.Member.Find(member.Member) is null) continue;
            var player = ctx.Db.Player.Identity.Find(member.Member);
            if (player is { Online: true }) continue;
            long seen = player is { } p && ctx.Db.PlayerSeen.PlayerId.Find(p.PlayerId) is { } s ? s.At.MicrosecondsSinceUnixEpoch : 0;
            if (now - seen > Groups.DropAfterMicros) RemoveFromGroup(ctx, member);
        }
    }

    // "/g hi": one row per online member, the speaker too, wherever they stand.
    static void SayToGroup(ReducerContext ctx, Player speaker, string text)
    {
        var mine = ctx.Db.GroupMember.Member.Find(speaker.Identity) ?? throw new Exception("You're not in a group. /g talks to your group");
        foreach (var member in ctx.Db.GroupMember.GroupId.Filter(mine.GroupId).ToList())
        {
            if (ctx.Db.Player.Identity.Find(member.Member) is not { Online: true } listener) continue;
            var at = ctx.Db.PlayerPos.PlayerId.Find(listener.PlayerId);
            ctx.Db.ChatHeard.Insert(new ChatHeard
            {
                Id = 0, Recipient = listener.Identity, SenderId = speaker.PlayerId, SenderName = speaker.Name, Text = text,
                X = at?.X ?? 0f, Z = at?.Z ?? 0f, SentAt = ctx.Timestamp,
            });
        }
    }
}
