using LifeGame.Shared;
using SpacetimeDB;

// claims-21: what the Marshal, Gatekeeper and Scribe can do for a town (Justin's picks, D-295).
// Marshal: a timed ban from the town's claims. Gatekeeper: bars and unbars the gates on them.
// Scribe: keeps the town's log. The Elder only breaks ties (TownPower.BreakTies in Town.cs).
// Everything is decided on the server (D-182).
public static partial class Module
{
    // A player kept off a town's claims until `Until`. Their own claim stays theirs.
    [SpacetimeDB.Table(Accessor = "TownBan", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByTown", Columns = new[] { nameof(TownId) })]
    public partial struct TownBan
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ulong TownId;
        [SpacetimeDB.Index.BTree]
        public Identity Banned;
        public uint BannedId;
        public string BannedName;
        public string ByName;
        public Timestamp Until;
    }

    [SpacetimeDB.Table(Accessor = "TownLog", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByTown", Columns = new[] { nameof(TownId) })]
    public partial struct TownLog
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ulong TownId;
        public Timestamp At;
        public string Text;
        public bool Note;      // written by the Scribe, not by the town's own doings
        public uint WriterId;
    }

    // Appends a line to the town's log, dropping the oldest beyond Towns.LogKeep.
    static void TownLogLine(ReducerContext ctx, ulong townId, string text, bool note = false, uint writerId = 0)
    {
        ctx.Db.TownLog.Insert(new TownLog { Id = 0, TownId = townId, At = ctx.Timestamp, Text = text, Note = note, WriterId = writerId });
        var all = ctx.Db.TownLog.ByTown.Filter(townId).OrderBy(l => l.Id).ToList();
        for (int i = 0; i < all.Count - Towns.LogKeep; i++) ctx.Db.TownLog.Id.Delete(all[i].Id);
    }

    // Whether this player is under a live ban in the town that signed `claim`, and the claim is not their own.
    static bool BannedFromClaim(ReducerContext ctx, Claim claim, Identity who)
    {
        if (claim.Owner == who) return false;
        if (ctx.Db.TownSigner.ClaimId.Find(claim.Id) is not { } there) return false;
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var ban in ctx.Db.TownBan.Banned.Filter(who))
            if (ban.TownId == there.TownId && ban.Until.MicrosecondsSinceUnixEpoch > now) return true;
        return false;
    }

    static void ExpireBans(ReducerContext ctx)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var ban in ctx.Db.TownBan.Iter().Where(b => b.Until.MicrosecondsSinceUnixEpoch <= now).ToList())
            ctx.Db.TownBan.Id.Delete(ban.Id);
    }

    // The Marshal keeps a player off the town's claims for whole in-game days (default 1, at most 7).
    [SpacetimeDB.Reducer]
    public static void BanFromTown(ReducerContext ctx, uint playerId, int days)
    {
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Ban)) throw new Exception("Only the town's Marshal can ban someone");
        if (days <= 0) days = Towns.BanDaysDefault;
        days = Math.Min(days, Towns.BanDaysMax);
        var target = ctx.Db.Player.PlayerId.Find(playerId) ?? throw new Exception("Nobody by that name");
        if (target.Identity == Me(ctx)) throw new Exception("You cannot ban yourself");
        foreach (var seat in ctx.Db.TownOffice.ByTown.Filter(mine.TownId))
            if (seat.HolderId == playerId && seat.Office == Social.OfficeMayor) throw new Exception("The Mayor cannot be banned");
        long len = days * (long)WorldTime.DayLengthSeconds * 1_000_000;
        var until = ctx.Timestamp + new TimeDuration(len);
        var existing = ctx.Db.TownBan.Banned.Filter(target.Identity).FirstOrDefault(b => b.TownId == mine.TownId);
        if (existing.Id != 0) ctx.Db.TownBan.Id.Update(existing with { Until = until, ByName = mine.OwnerName });
        else ctx.Db.TownBan.Insert(new TownBan { Id = 0, TownId = mine.TownId, Banned = target.Identity, BannedId = playerId, BannedName = target.Name, ByName = mine.OwnerName, Until = until });
        TownLogLine(ctx, mine.TownId, $"{mine.OwnerName} kept {target.Name} off the town's claims for {days} {(days == 1 ? "day" : "days")}");
        News(ctx, playerId, $"You are kept off {ctx.Db.Town.Id.Find(mine.TownId)?.Name ?? "the town"}'s claims for {days} {(days == 1 ? "day" : "days")}");
    }

    [SpacetimeDB.Reducer]
    public static void LiftTownBan(ReducerContext ctx, uint playerId)
    {
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Ban)) throw new Exception("Only the town's Marshal can lift a ban");
        var ban = ctx.Db.TownBan.ByTown.Filter(mine.TownId).FirstOrDefault(b => b.BannedId == playerId);
        if (ban.Id == 0) throw new Exception("They are not banned");
        ctx.Db.TownBan.Id.Delete(ban.Id);
        TownLogLine(ctx, mine.TownId, $"{mine.OwnerName} lifted the ban on {ban.BannedName}");
        News(ctx, playerId, "Your ban from the town's claims is lifted");
    }

    // The Scribe writes a note into the log, not more than once every ten seconds.
    [SpacetimeDB.Reducer]
    public static void WriteTownLog(ReducerContext ctx, string text)
    {
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Log)) throw new Exception("Only the town's Scribe can write in its log");
        text = Social.StripControl(text).Trim();
        if (text.Length == 0) return;
        if (text.Length > Towns.LogNoteMax) text = text[..Towns.LogNoteMax];
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        var last = ctx.Db.TownLog.ByTown.Filter(mine.TownId).Where(l => l.Note).OrderByDescending(l => l.Id).FirstOrDefault();
        if (last.Id != 0 && now - last.At.MicrosecondsSinceUnixEpoch < Towns.LogNoteEveryMicros) throw new Exception("Give the pen a moment");
        TownLogLine(ctx, mine.TownId, $"{mine.OwnerName}: {text}", true, mine.PlayerId);
    }

    // Gate at `gate` on a claim of a town: barring it is for the claim's owner and members, and the Gatekeeper.
    static bool TownGateKeeperMay(ReducerContext ctx, Claim claim)
    {
        if (ctx.Db.TownSigner.ClaimId.Find(claim.Id) is not { } there) return false;
        return HasTownPower(ctx, there.TownId, Me(ctx), TownPower.Gates);
    }
}
