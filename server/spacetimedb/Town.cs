using LifeGame.Shared;
using SpacetimeDB;

// claims-19: towns (Justin: "Add neighbors together via a roster everyone signs").
// A claim owner starts a roster, a town with a name, and is its first Mayor. Anyone on the
// roster can invite another claim owner whose marker lies near a signed claim; they sign to join.
// Everyone who signed gets builder rights (build, dig, pick up, gates) on every signed claim, via
// Claims.HasClaimRights. Stashes, tending a marker and the fighting rule stay with each claim's
// owner and members. The signers vote for offices (the Mayor first, who can remove signers).
// Leaving the roster, or the claim falling, takes the rights away. All of it is decided on the
// server (D-182).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Town", Public = true)]
    public partial struct Town
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public string Name;
        public Timestamp StartedAt;
    }

    // One row per signed claim. A claim is on at most one roster, and a player has one claim.
    [SpacetimeDB.Table(Accessor = "TownSigner", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByTown", Columns = new[] { nameof(TownId) })]
    public partial struct TownSigner
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ulong TownId;
        [Unique]
        public ulong ClaimId;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        public uint PlayerId;
        public string OwnerName;
        public Timestamp SignedAt;
    }

    // An office in a town (Social.OfficeMayor, ...). HolderId 0 means the seat is vacant.
    [SpacetimeDB.Table(Accessor = "TownOffice", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByTown", Columns = new[] { nameof(TownId) })]
    public partial struct TownOffice
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ulong TownId;
        public byte Office;
        public Identity Holder;
        public uint HolderId;
        public string HolderName;
        public Timestamp TermEndsAt;
    }

    // An open ballot: who each signer is voting for, for each office.
    [SpacetimeDB.Table(Accessor = "TownVote", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByTown", Columns = new[] { nameof(TownId) })]
    public partial struct TownVote
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ulong TownId;
        public byte Office;
        public Identity Voter;
        public uint VoterId;
        public uint CandidateId;
    }

    // Private; each invitee reads their own through MyTownInvites.
    [SpacetimeDB.Table(Accessor = "TownInvite")]
    [SpacetimeDB.Index.BTree(Accessor = "ByTownInvitee", Columns = new[] { nameof(TownId), nameof(Invitee) })]
    public partial struct TownInvite
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ulong TownId;
        public string TownName;
        [SpacetimeDB.Index.BTree]
        public Identity Invitee;
        public string InvitedBy;
        public Timestamp At;
    }

    [SpacetimeDB.Table(Accessor = "TownTermTimer", Scheduled = nameof(TownTerms), ScheduledAt = nameof(ScheduledAt))]
    public partial struct TownTermTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    [SpacetimeDB.View(Accessor = "MyTownInvites", Public = true)]
    public static List<TownInvite> MyTownInvites(ViewContext ctx) => ctx.Db.TownInvite.Invitee.Filter(ctx.Sender).ToList();

    // Whether `who` signed the same roster as this claim (and is not just its owner or a member).
    static bool IsTownmate(ReducerContext ctx, Claim claim, Identity who)
    {
        if (ctx.Db.TownSigner.ClaimId.Find(claim.Id) is not { } there) return false;
        foreach (var mine in ctx.Db.TownSigner.Owner.Filter(who))
            if (mine.TownId == there.TownId) return true;
        return false;
    }

    static TownSigner? SignerOf(ReducerContext ctx, Identity who)
    {
        foreach (var row in ctx.Db.TownSigner.Owner.Filter(who)) return row;
        return null;
    }

    static void EnsureTownTimer(ReducerContext ctx)
    {
        if (ctx.Db.TownTermTimer.Count > 0) return;
        ctx.Db.TownTermTimer.Insert(new TownTermTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromHours(1)) });
    }

    // Whether a claim's marker is near enough to a claim the town has signed.
    static bool NearTown(ReducerContext ctx, ulong townId, Claim claim)
    {
        foreach (var signer in ctx.Db.TownSigner.ByTown.Filter(townId))
            if (ctx.Db.Claim.Id.Find(signer.ClaimId) is { } other && Social.TownNear(claim.X, claim.Z, other.X, other.Z))
                return true;
        return false;
    }

    static TownOffice? OfficeOf(ReducerContext ctx, ulong townId, byte office)
    {
        foreach (var row in ctx.Db.TownOffice.ByTown.Filter(townId))
            if (row.Office == office) return row;
        return null;
    }

    [SpacetimeDB.Reducer]
    public static void StartTown(ReducerContext ctx, string name)
    {
        var player = RequirePlayer(ctx);
        var claim = RequireOwnStandingClaim(ctx);
        if (SignerOf(ctx, ctx.Sender) != null) throw new Exception("You are already on a town roster");
        name = Social.CleanClaimName(name);
        if (name.Length == 0) throw new Exception("Give the town a name");
        EnsureTownTimer(ctx);
        var town = ctx.Db.Town.Insert(new Town { Id = 0, Name = name, StartedAt = ctx.Timestamp });
        Sign(ctx, town.Id, claim, player);
        // The founder is the first Mayor until the signers vote.
        ctx.Db.TownOffice.Insert(new TownOffice
        {
            Id = 0, TownId = town.Id, Office = Social.OfficeMayor, Holder = player.Identity, HolderId = player.PlayerId,
            HolderName = player.Name, TermEndsAt = ctx.Timestamp + new TimeDuration(Social.TownTermMicros),
        });
    }

    static void Sign(ReducerContext ctx, ulong townId, Claim claim, Player player) =>
        ctx.Db.TownSigner.Insert(new TownSigner
        {
            Id = 0, TownId = townId, ClaimId = claim.Id, Owner = player.Identity,
            PlayerId = player.PlayerId, OwnerName = player.Name, SignedAt = ctx.Timestamp,
        });

    // Anyone on the roster may invite a claim owner who is not on one and whose marker is near.
    [SpacetimeDB.Reducer]
    public static void InviteToTown(ReducerContext ctx, uint playerId)
    {
        var me = RequirePlayer(ctx);
        var mine = SignerOf(ctx, ctx.Sender) ?? throw new Exception("Start or join a town first");
        var town = ctx.Db.Town.Id.Find(mine.TownId) ?? throw new Exception("That town is gone");
        var them = ctx.Db.Player.PlayerId.Find(playerId) ?? throw new Exception("No such player");
        if (them.Identity == ctx.Sender) throw new Exception("You are already on the roster");
        var theirClaim = ctx.Db.Claim.Owner.Filter(them.Identity).FirstOrDefault(c => IsStanding(ctx, c));
        if (theirClaim.Id == 0) throw new Exception($"{them.Name} has no claim to sign with");
        if (SignerOf(ctx, them.Identity) != null) throw new Exception($"{them.Name} is already on a town roster");
        if (!NearTown(ctx, town.Id, theirClaim))
            throw new Exception($"{them.Name}'s claim is too far from the town: within {Social.TownNearMetres:0} m of a signed marker, please");
        if (ctx.Db.TownSigner.ByTown.Filter(town.Id).Count() >= Social.TownMaxClaims)
            throw new Exception($"A town holds at most {Social.TownMaxClaims} claims");
        foreach (var old in ctx.Db.TownInvite.ByTownInvitee.Filter((town.Id, them.Identity)).ToList())
            ctx.Db.TownInvite.Id.Delete(old.Id);
        ctx.Db.TownInvite.Insert(new TownInvite
        {
            Id = 0, TownId = town.Id, TownName = town.Name, Invitee = them.Identity, InvitedBy = me.Name, At = ctx.Timestamp,
        });
    }

    [SpacetimeDB.Reducer]
    public static void SignTown(ReducerContext ctx, ulong townId)
    {
        var player = RequirePlayer(ctx);
        var town = ctx.Db.Town.Id.Find(townId) ?? throw new Exception("That town is gone");
        var invite = ctx.Db.TownInvite.ByTownInvitee.Filter((townId, ctx.Sender)).FirstOrDefault();
        if (invite.Id == 0) throw new Exception("You haven't been invited to sign this roster");
        if (ctx.Timestamp.MicrosecondsSinceUnixEpoch - invite.At.MicrosecondsSinceUnixEpoch > Social.TownInviteMicros)
        {
            ctx.Db.TownInvite.Id.Delete(invite.Id);
            throw new Exception("That invitation has expired. Ask for a new one");
        }
        var claim = RequireOwnStandingClaim(ctx);
        if (SignerOf(ctx, ctx.Sender) != null) throw new Exception("You are already on a town roster");
        if (!NearTown(ctx, town.Id, claim))
            throw new Exception($"Your claim is too far from the town: within {Social.TownNearMetres:0} m of a signed marker, please");
        if (ctx.Db.TownSigner.ByTown.Filter(town.Id).Count() >= Social.TownMaxClaims)
            throw new Exception($"A town holds at most {Social.TownMaxClaims} claims");
        Sign(ctx, town.Id, claim, player);
        foreach (var mine in ctx.Db.TownInvite.Invitee.Filter(ctx.Sender).ToList())
            ctx.Db.TownInvite.Id.Delete(mine.Id);
    }

    [SpacetimeDB.Reducer]
    public static void DeclineTown(ReducerContext ctx, ulong townId)
    {
        foreach (var invite in ctx.Db.TownInvite.ByTownInvitee.Filter((townId, ctx.Sender)).ToList())
            ctx.Db.TownInvite.Id.Delete(invite.Id);
    }

    [SpacetimeDB.Reducer]
    public static void LeaveTown(ReducerContext ctx)
    {
        var mine = SignerOf(ctx, ctx.Sender) ?? throw new Exception("You are not on a town roster");
        DropSigner(ctx, mine);
    }

    // Only the Mayor removes someone else; anyone may leave on their own.
    [SpacetimeDB.Reducer]
    public static void RemoveFromTown(ReducerContext ctx, uint playerId)
    {
        var mine = SignerOf(ctx, ctx.Sender) ?? throw new Exception("You are not on a town roster");
        var mayor = OfficeOf(ctx, mine.TownId, Social.OfficeMayor);
        if (mayor is not { } m || m.HolderId == 0 || m.Holder != ctx.Sender)
            throw new Exception("Only the town's Mayor can remove a claim from the roster");
        var target = ctx.Db.TownSigner.ByTown.Filter(mine.TownId).FirstOrDefault(r => r.PlayerId == playerId);
        if (target.Id == 0) throw new Exception("They are not on this roster");
        if (target.Owner == ctx.Sender) throw new Exception("Use leave to step down yourself");
        DropSigner(ctx, target);
    }

    // Vote for who should hold an office. Open ballot: one vote per signer per office, changeable.
    // A majority of the signers decides at once; otherwise the term's end does (TownTerms).
    [SpacetimeDB.Reducer]
    public static void VoteTownOffice(ReducerContext ctx, byte office, uint candidatePlayerId)
    {
        if (office != Social.OfficeMayor) throw new Exception("There is no such office yet");
        var me = SignerOf(ctx, ctx.Sender) ?? throw new Exception("Only signers vote");
        var candidate = ctx.Db.TownSigner.ByTown.Filter(me.TownId).FirstOrDefault(r => r.PlayerId == candidatePlayerId);
        if (candidate.Id == 0) throw new Exception("They are not on this roster");
        foreach (var old in ctx.Db.TownVote.ByTown.Filter(me.TownId).Where(v => v.Office == office && v.Voter == ctx.Sender).ToList())
            ctx.Db.TownVote.Id.Delete(old.Id);
        ctx.Db.TownVote.Insert(new TownVote { Id = 0, TownId = me.TownId, Office = office, Voter = ctx.Sender, VoterId = me.PlayerId, CandidateId = candidatePlayerId });
        int signers = ctx.Db.TownSigner.ByTown.Filter(me.TownId).Count();
        var leader = ctx.Db.TownVote.ByTown.Filter(me.TownId).Where(v => v.Office == office)
            .GroupBy(v => v.CandidateId).Select(g => (id: g.Key, n: g.Count())).OrderByDescending(g => g.n).First();
        if (leader.n * 2 > signers) SeatOffice(ctx, me.TownId, office, ctx.Db.TownSigner.ByTown.Filter(me.TownId).First(r => r.PlayerId == leader.id));
    }

    // The winner takes the seat for a full term; the ballot starts afresh.
    static void SeatOffice(ReducerContext ctx, ulong townId, byte office, TownSigner winner)
    {
        var term = ctx.Timestamp + new TimeDuration(Social.TownTermMicros);
        if (OfficeOf(ctx, townId, office) is { } seat)
            ctx.Db.TownOffice.Id.Update(seat with { Holder = winner.Owner, HolderId = winner.PlayerId, HolderName = winner.OwnerName, TermEndsAt = term });
        else
            ctx.Db.TownOffice.Insert(new TownOffice { Id = 0, TownId = townId, Office = office, Holder = winner.Owner, HolderId = winner.PlayerId, HolderName = winner.OwnerName, TermEndsAt = term });
        foreach (var v in ctx.Db.TownVote.ByTown.Filter(townId).Where(v => v.Office == office).ToList())
            ctx.Db.TownVote.Id.Delete(v.Id);
    }

    // Hourly: a term that has ended goes to the most votes (a tie to the longest-standing signer);
    // with no votes the holder stays on, and a vacant seat waits another day.
    [SpacetimeDB.Reducer]
    public static void TownTerms(ReducerContext ctx, TownTermTimer timer)
    {
        RequireServer(ctx);
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var seat in ctx.Db.TownOffice.Iter().Where(o => o.TermEndsAt.MicrosecondsSinceUnixEpoch <= now).ToList())
        {
            var signers = ctx.Db.TownSigner.ByTown.Filter(seat.TownId).ToList();
            var tally = ctx.Db.TownVote.ByTown.Filter(seat.TownId).Where(v => v.Office == seat.Office)
                .GroupBy(v => v.CandidateId).Select(g => (id: g.Key, n: g.Count())).ToList();
            if (tally.Count > 0)
            {
                int best = tally.Max(t => t.n);
                var winner = signers.Where(sg => tally.Any(t => t.id == sg.PlayerId && t.n == best))
                    .OrderBy(sg => sg.SignedAt.MicrosecondsSinceUnixEpoch).FirstOrDefault();
                if (winner.Id != 0) { SeatOffice(ctx, seat.TownId, seat.Office, winner); continue; }
            }
            long wait = seat.HolderId == 0 ? Social.TownVacancyMicros : Social.TownTermMicros;
            ctx.Db.TownOffice.Id.Update(seat with { TermEndsAt = ctx.Timestamp + new TimeDuration(wait) });
        }
    }

    // The signer goes, with the votes they cast and received and the offices they held. An empty
    // town is closed.
    static void DropSigner(ReducerContext ctx, TownSigner row)
    {
        ctx.Db.TownSigner.Id.Delete(row.Id);
        foreach (var v in ctx.Db.TownVote.ByTown.Filter(row.TownId).Where(v => v.Voter == row.Owner || v.CandidateId == row.PlayerId).ToList())
            ctx.Db.TownVote.Id.Delete(v.Id);
        if (!ctx.Db.TownSigner.ByTown.Filter(row.TownId).Any())
        {
            foreach (var invite in ctx.Db.TownInvite.Iter().Where(i => i.TownId == row.TownId).ToList())
                ctx.Db.TownInvite.Id.Delete(invite.Id);
            foreach (var seat in ctx.Db.TownOffice.ByTown.Filter(row.TownId).ToList())
                ctx.Db.TownOffice.Id.Delete(seat.Id);
            ctx.Db.Town.Id.Delete(row.TownId);
            return;
        }
        // A seat the leaver held falls vacant for a day, while the signers vote.
        foreach (var seat in ctx.Db.TownOffice.ByTown.Filter(row.TownId).Where(o => o.HolderId == row.PlayerId).ToList())
            ctx.Db.TownOffice.Id.Update(seat with { Holder = default, HolderId = 0, HolderName = "", TermEndsAt = ctx.Timestamp + new TimeDuration(Social.TownVacancyMicros) });
    }

    // A claim that falls or is taken down leaves its roster.
    static void TownDropClaim(ReducerContext ctx, Claim claim)
    {
        if (ctx.Db.TownSigner.ClaimId.Find(claim.Id) is { } row) DropSigner(ctx, row);
    }
}
