using LifeGame.Shared;
using SpacetimeDB;

// claims-11: who put what in a claim's stash and who took it, so a group can trust
// a shared stash. Owners and members see the last few entries in the claim panel.
// Quick repeats (the same person, item and direction within a minute) fold into
// one entry: "Tam took 3 sticks".
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ClaimStashLog")]
    public partial struct ClaimStashLog
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong ClaimId;
        public uint PlayerId;
        public string PlayerName;
        public ushort Item;
        // Positive: left in the stash. Negative: taken out.
        public int Count;
        public Timestamp At;
    }

    [SpacetimeDB.View(Accessor = "MyClaimStashLog", Public = true)]
    public static List<ClaimStashLog> MyClaimStashLog(ViewContext ctx)
    {
        var result = new List<ClaimStashLog>();
        foreach (var claim in ctx.Db.Claim.Owner.Filter(Me(ctx)))
            result.AddRange(ctx.Db.ClaimStashLog.ClaimId.Filter(claim.Id));
        foreach (var membership in ctx.Db.ClaimMember.Member.Filter(Me(ctx)))
            result.AddRange(ctx.Db.ClaimStashLog.ClaimId.Filter(membership.ClaimId));
        return result;
    }

    // Called by StashItem (+1) and TakeFromStash (-1).
    static void LogStash(ReducerContext ctx, ulong claimId, ushort item, int count)
    {
        var player = RequirePlayer(ctx);
        var entries = ctx.Db.ClaimStashLog.ClaimId.Filter(claimId).OrderBy(e => e.At.MicrosecondsSinceUnixEpoch).ToList();
        if (entries.Count > 0 && entries[^1] is var last && last.PlayerId == player.PlayerId && last.Item == item
            && Math.Sign(last.Count) == Math.Sign(count)
            && ctx.Timestamp.MicrosecondsSinceUnixEpoch - last.At.MicrosecondsSinceUnixEpoch < Social.StashLogFoldMicros)
        {
            ctx.Db.ClaimStashLog.Id.Update(last with { Count = last.Count + count, At = ctx.Timestamp });
            return;
        }
        ctx.Db.ClaimStashLog.Insert(new ClaimStashLog
        {
            Id = 0,
            ClaimId = claimId,
            PlayerId = player.PlayerId,
            PlayerName = player.Name,
            Item = item,
            Count = count,
            At = ctx.Timestamp,
        });
        for (int i = 0; i + Social.StashLogKeep <= entries.Count; i++)
            ctx.Db.ClaimStashLog.Id.Delete(entries[i].Id);
    }

    static void RemoveStashLog(ReducerContext ctx, ulong claimId)
    {
        foreach (var e in ctx.Db.ClaimStashLog.ClaimId.Filter(claimId).ToList())
            ctx.Db.ClaimStashLog.Id.Delete(e.Id);
    }
}
