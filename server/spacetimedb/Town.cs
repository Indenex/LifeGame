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

    // A proposal to join two towns into one. It needs a majority of each town's signers.
    [SpacetimeDB.Table(Accessor = "TownMerge", Public = true)]
    public partial struct TownMerge
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ulong FromTownId;
        public ulong ToTownId;
        public string FromName;
        public string ToName;
        public Timestamp ExpiresAt;
    }

    [SpacetimeDB.Table(Accessor = "TownMergeVote", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByMerge", Columns = new[] { nameof(MergeId) })]
    public partial struct TownMergeVote
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ulong MergeId;
        public ulong TownId;
        public Identity Voter;
        public bool Yes;
    }

    // Private: when each town's Herald (or Mayor) last spoke.
    [SpacetimeDB.Table(Accessor = "TownVoice")]
    public partial struct TownVoice
    {
        [PrimaryKey]
        public ulong TownId;
        public Timestamp LastAt;
    }

    // Private: lets the server admin shorten terms for a playtest (and for the SocialTest).
    [SpacetimeDB.Table(Accessor = "TownConfig")]
    public partial struct TownConfig
    {
        [PrimaryKey]
        public byte Id;
        public long TermMicros;
        public long VacancyMicros;
    }

    static long TermMicros(ReducerContext ctx) => ctx.Db.TownConfig.Id.Find(0)?.TermMicros ?? Social.TownTermMicros;
    static long VacancyMicros(ReducerContext ctx) => ctx.Db.TownConfig.Id.Find(0)?.VacancyMicros ?? Social.TownVacancyMicros;

    // Database owner only: how long an office term (and a vacant seat's wait) lasts, in seconds.
    // 0 puts the defaults back. Applies to seats filled from now on.
    [SpacetimeDB.Reducer]
    public static void SetTownTerm(ReducerContext ctx, long seconds)
    {
        MeterCall(ctx);
        var config = ctx.Db.ClaimConfig.Id.Find(0) ?? throw new Exception("Claims are not set up");
        if (config.Admin != Me(ctx)) throw new Exception("Only the server admin can change this");
        if (seconds < 0) throw new Exception("Seconds cannot be negative");
        var row = new TownConfig { Id = 0, TermMicros = seconds == 0 ? Social.TownTermMicros : seconds * 1_000_000, VacancyMicros = seconds == 0 ? Social.TownVacancyMicros : seconds * 1_000_000 };
        if (ctx.Db.TownConfig.Id.Find(0) == null) ctx.Db.TownConfig.Insert(row); else ctx.Db.TownConfig.Id.Update(row);
    }

    // Database owner only: settle the terms that have ended now rather than at the next hourly pass.
    [SpacetimeDB.Reducer]
    public static void SettleTownTermsNow(ReducerContext ctx)
    {
        var config = ctx.Db.ClaimConfig.Id.Find(0) ?? throw new Exception("Claims are not set up");
        if (config.Admin != Me(ctx)) throw new Exception("Only the server admin can do this");
        SettleTerms(ctx);
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
    public static List<TownInvite> MyTownInvites(ViewContext ctx) => ctx.Db.TownInvite.Invitee.Filter(Me(ctx)).ToList();

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
        if (SignerOf(ctx, Me(ctx)) != null) throw new Exception("You are already on a town roster");
        name = Social.CleanClaimName(name);
        if (name.Length == 0) throw new Exception("Give the town a name");
        EnsureTownTimer(ctx);
        var town = ctx.Db.Town.Insert(new Town { Id = 0, Name = name, StartedAt = ctx.Timestamp });
        Sign(ctx, town.Id, claim, player);
        TownLogLine(ctx, town.Id, $"{player.Name} started the town");
        // Invitations to other towns no longer apply once you have started your own.
        foreach (var invite in ctx.Db.TownInvite.Invitee.Filter(Me(ctx)).ToList()) ctx.Db.TownInvite.Id.Delete(invite.Id);
        // The founder is the first Mayor until the signers vote.
        ctx.Db.TownOffice.Insert(new TownOffice
        {
            Id = 0, TownId = town.Id, Office = Social.OfficeMayor, Holder = player.Identity, HolderId = player.PlayerId,
            HolderName = player.Name, TermEndsAt = ctx.Timestamp + new TimeDuration(TermMicros(ctx)),
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
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("Start or join a town first");
        var town = ctx.Db.Town.Id.Find(mine.TownId) ?? throw new Exception("That town is gone");
        var them = ctx.Db.Player.PlayerId.Find(playerId) ?? throw new Exception("No such player");
        if (them.Identity == Me(ctx)) throw new Exception("You are already on the roster");
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
        var invite = ctx.Db.TownInvite.ByTownInvitee.Filter((townId, Me(ctx))).FirstOrDefault();
        if (invite.Id == 0) throw new Exception("You haven't been invited to sign this roster");
        if (ctx.Timestamp.MicrosecondsSinceUnixEpoch - invite.At.MicrosecondsSinceUnixEpoch > Social.TownInviteMicros)
        {
            ctx.Db.TownInvite.Id.Delete(invite.Id);
            throw new Exception("That invitation has expired. Ask for a new one");
        }
        var claim = RequireOwnStandingClaim(ctx);
        if (SignerOf(ctx, Me(ctx)) != null) throw new Exception("You are already on a town roster");
        if (!NearTown(ctx, town.Id, claim))
            throw new Exception($"Your claim is too far from the town: within {Social.TownNearMetres:0} m of a signed marker, please");
        if (ctx.Db.TownSigner.ByTown.Filter(town.Id).Count() >= Social.TownMaxClaims)
            throw new Exception($"A town holds at most {Social.TownMaxClaims} claims");
        Sign(ctx, town.Id, claim, player);
        TownLogLine(ctx, town.Id, $"{player.Name} signed the roster");
        foreach (var mine in ctx.Db.TownInvite.Invitee.Filter(Me(ctx)).ToList())
            ctx.Db.TownInvite.Id.Delete(mine.Id);
    }

    [SpacetimeDB.Reducer]
    public static void DeclineTown(ReducerContext ctx, ulong townId)
    {
        MeterCall(ctx);
        foreach (var invite in ctx.Db.TownInvite.ByTownInvitee.Filter((townId, Me(ctx))).ToList())
            ctx.Db.TownInvite.Id.Delete(invite.Id);
    }

    [SpacetimeDB.Reducer]
    public static void LeaveTown(ReducerContext ctx)
    {
        MeterCall(ctx);
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        DropSigner(ctx, mine);
    }

    // Only the Mayor removes someone else; anyone may leave on their own.
    [SpacetimeDB.Reducer]
    public static void RemoveFromTown(ReducerContext ctx, uint playerId)
    {
        MeterCall(ctx);
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Remove))
            throw new Exception("Only the town's Mayor can remove a claim from the roster");
        var target = ctx.Db.TownSigner.ByTown.Filter(mine.TownId).FirstOrDefault(r => r.PlayerId == playerId);
        if (target.Id == 0) throw new Exception("They are not on this roster");
        if (target.Owner == Me(ctx)) throw new Exception("Use leave to step down yourself");
        DropSigner(ctx, target);
    }

    // Whether `who` holds an office with this power, or stands in for one that is vacant.
    static bool HasTownPower(ReducerContext ctx, ulong townId, Identity who, TownPower power)
    {
        var seats = ctx.Db.TownOffice.ByTown.Filter(townId).ToList();
        foreach (var seat in seats)
        {
            if (seat.HolderId == 0 || seat.Holder != who || Towns.Get(seat.Office) is not { } def) continue;
            if ((def.Powers & power) != 0) return true;
            // A stand-in (the Deputy) has the powers of the office it covers while that seat is empty.
            if (def.StandsInFor != TownOfficeDef.NoOffice && Towns.Get(def.StandsInFor) is { } covered
                && (covered.Powers & power) != 0 && !seats.Any(o => o.Office == def.StandsInFor && o.HolderId != 0))
                return true;
        }
        return false;
    }

    static IEnumerable<Identity> HoldersWith(ReducerContext ctx, ulong townId, TownPower power) =>
        ctx.Db.TownOffice.ByTown.Filter(townId).ToList().OrderBy(o => o.Office)
            .Where(o => o.HolderId != 0 && Towns.Get(o.Office) is { } d && (d.Powers & power) != 0).Select(o => o.Holder);

    [SpacetimeDB.Reducer]
    public static void RenameTown(ReducerContext ctx, string name)
    {
        MeterCall(ctx);
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Rename)) throw new Exception("Only the Mayor can rename the town");
        name = Social.CleanClaimName(name);
        if (name.Length == 0) throw new Exception("Give the town a name");
        var town = ctx.Db.Town.Id.Find(mine.TownId) ?? throw new Exception("That town is gone");
        ctx.Db.Town.Id.Update(town with { Name = name });
        TownLogLine(ctx, mine.TownId, $"{mine.OwnerName} renamed the town {name}");
    }

    // The Herald (or Mayor) sends one word to every signer, not more than once a minute.
    [SpacetimeDB.Reducer]
    public static void AnnounceToTown(ReducerContext ctx, string text)
    {
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Announce)) throw new Exception("Only the Herald or the Mayor can speak for the town");
        text = Social.StripControl(text).Trim();
        if (text.Length == 0) return;
        if (text.Length > Towns.AnnounceMax) text = text[..Towns.AnnounceMax];
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        var voice = ctx.Db.TownVoice.TownId.Find(mine.TownId);
        if (voice is { } v && now - v.LastAt.MicrosecondsSinceUnixEpoch < Towns.AnnounceEveryMicros) throw new Exception("Give the town a moment before the next word");
        var row = new TownVoice { TownId = mine.TownId, LastAt = ctx.Timestamp };
        if (voice == null) ctx.Db.TownVoice.Insert(row); else ctx.Db.TownVoice.TownId.Update(row);
        string line = $"{mine.OwnerName} for {ctx.Db.Town.Id.Find(mine.TownId)?.Name ?? "the town"}: {text}";
        foreach (var signer in ctx.Db.TownSigner.ByTown.Filter(mine.TownId).ToList()) News(ctx, signer.PlayerId, line);
    }

    // The Warden (or Mayor) takes down a building in a signed claim whose builder has left the town.
    [SpacetimeDB.Reducer]
    public static void ClearTownRuin(ReducerContext ctx, ulong structureId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.ClearRuins)) throw new Exception("Only the Warden or the Mayor can clear ruins");
        var s = ctx.Db.Structure.Id.Find(structureId) ?? throw new Exception("That building is gone");
        float dx = s.X - pos.X, dz = s.Z - pos.Z;
        if (dx * dx + dz * dz > Towns.ClearRuinReach * Towns.ClearRuinReach) throw new Exception("Walk up to it first");
        if (s.Kind == StructureKind.ClaimMarker) throw new Exception("A claim marker is not a ruin");
        var claim = ClaimAt(ctx, s.X, s.Z);
        if (claim is not { } c || ctx.Db.TownSigner.ClaimId.Find(c.Id) is not { } signed || signed.TownId != mine.TownId)
            throw new Exception("That is not on a claim of this town");
        // Not the work of anyone still on the roster.
        if (ctx.Db.TownSigner.ByTown.Filter(mine.TownId).Any(r => r.Owner == s.Owner)) throw new Exception("Its builder is still on the roster");
        foreach (var row in ctx.Db.BasketItem.StructureId.Filter(s.Id).ToList()) ctx.Db.BasketItem.Id.Delete(row.Id);
        RemoveStructure(ctx, s);
        TownLogLine(ctx, mine.TownId, $"{mine.OwnerName} cleared a ruin");
    }

    // Two towns join when a majority of each town's signers say yes within three days.
    [SpacetimeDB.Reducer]
    public static void ProposeTownMerge(ReducerContext ctx, ulong otherTownId)
    {
        MeterCall(ctx);
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("You are not on a town roster");
        if (!HasTownPower(ctx, mine.TownId, Me(ctx), TownPower.Merge)) throw new Exception("Only the Mayor can propose a merge");
        if (otherTownId == mine.TownId) throw new Exception("That is your own town");
        var from = ctx.Db.Town.Id.Find(mine.TownId) ?? throw new Exception("That town is gone");
        var to = ctx.Db.Town.Id.Find(otherTownId) ?? throw new Exception("That town is gone");
        if (!TownsNear(ctx, from.Id, to.Id)) throw new Exception($"The towns need claims within {Social.TownNearMetres:0} m of each other");
        foreach (var m in ctx.Db.TownMerge.Iter().Where(m => (m.FromTownId == from.Id && m.ToTownId == to.Id) || (m.FromTownId == to.Id && m.ToTownId == from.Id)).ToList())
            DeleteMerge(ctx, m);
        ctx.Db.TownMerge.Insert(new TownMerge
        {
            Id = 0, FromTownId = from.Id, ToTownId = to.Id, FromName = from.Name, ToName = to.Name,
            ExpiresAt = ctx.Timestamp + new TimeDuration(Towns.MergeWindowMicros),
        });
    }

    [SpacetimeDB.Reducer]
    public static void VoteTownMerge(ReducerContext ctx, ulong mergeId, bool yes)
    {
        MeterCall(ctx);
        var mine = SignerOf(ctx, Me(ctx)) ?? throw new Exception("Only signers vote");
        var merge = ctx.Db.TownMerge.Id.Find(mergeId) ?? throw new Exception("That proposal is gone");
        if (mine.TownId != merge.FromTownId && mine.TownId != merge.ToTownId) throw new Exception("That is not your town's to vote on");
        if (merge.ExpiresAt.MicrosecondsSinceUnixEpoch <= ctx.Timestamp.MicrosecondsSinceUnixEpoch) { DeleteMerge(ctx, merge); throw new Exception("That proposal has run out"); }
        foreach (var old in ctx.Db.TownMergeVote.ByMerge.Filter(mergeId).Where(v => v.Voter == Me(ctx)).ToList())
            ctx.Db.TownMergeVote.Id.Delete(old.Id);
        ctx.Db.TownMergeVote.Insert(new TownMergeVote { Id = 0, MergeId = mergeId, TownId = mine.TownId, Voter = Me(ctx), Yes = yes });
        bool Majority(ulong townId) =>
            ctx.Db.TownMergeVote.ByMerge.Filter(mergeId).Count(v => v.TownId == townId && v.Yes) * 2 > ctx.Db.TownSigner.ByTown.Filter(townId).Count();
        if (Majority(merge.FromTownId) && Majority(merge.ToTownId)) ExecuteMerge(ctx, merge);
    }

    static bool TownsNear(ReducerContext ctx, ulong townA, ulong townB)
    {
        foreach (var a in ctx.Db.TownSigner.ByTown.Filter(townA))
            if (ctx.Db.Claim.Id.Find(a.ClaimId) is { } ca && ctx.Db.TownSigner.ByTown.Filter(townB).Any(b =>
                    ctx.Db.Claim.Id.Find(b.ClaimId) is { } cb && Social.TownNear(ca.X, ca.Z, cb.X, cb.Z)))
                return true;
        return false;
    }

    static void DeleteMerge(ReducerContext ctx, TownMerge merge)
    {
        foreach (var v in ctx.Db.TownMergeVote.ByMerge.Filter(merge.Id).ToList()) ctx.Db.TownMergeVote.Id.Delete(v.Id);
        ctx.Db.TownMerge.Id.Delete(merge.Id);
    }

    // The smaller town (the older, on a tie) joins the larger one: it keeps the larger town's name
    // and its signers' rights, and the offices start afresh with an empty Mayor's seat.
    static void ExecuteMerge(ReducerContext ctx, TownMerge merge)
    {
        var a = ctx.Db.Town.Id.Find(merge.FromTownId); var b = ctx.Db.Town.Id.Find(merge.ToTownId);
        if (a is not { } ta || b is not { } tb) { DeleteMerge(ctx, merge); return; }
        int na = ctx.Db.TownSigner.ByTown.Filter(ta.Id).Count(), nb = ctx.Db.TownSigner.ByTown.Filter(tb.Id).Count();
        var keep = na > nb || (na == nb && ta.StartedAt.MicrosecondsSinceUnixEpoch <= tb.StartedAt.MicrosecondsSinceUnixEpoch) ? ta : tb;
        var gone = keep.Id == ta.Id ? tb : ta;
        foreach (var signer in ctx.Db.TownSigner.ByTown.Filter(gone.Id).ToList())
            ctx.Db.TownSigner.Id.Update(signer with { TownId = keep.Id });
        foreach (var town in new[] { keep, gone })
        {
            foreach (var seat in ctx.Db.TownOffice.ByTown.Filter(town.Id).ToList()) ctx.Db.TownOffice.Id.Delete(seat.Id);
            foreach (var vote in ctx.Db.TownVote.ByTown.Filter(town.Id).ToList()) ctx.Db.TownVote.Id.Delete(vote.Id);
        }
        foreach (var invite in ctx.Db.TownInvite.Iter().Where(i => i.TownId == gone.Id).ToList()) ctx.Db.TownInvite.Id.Delete(invite.Id);
        if (ctx.Db.TownVoice.TownId.Find(gone.Id) != null) ctx.Db.TownVoice.TownId.Delete(gone.Id);
        foreach (var m in ctx.Db.TownMerge.Iter().Where(m => m.FromTownId == gone.Id || m.ToTownId == gone.Id || m.FromTownId == keep.Id || m.ToTownId == keep.Id).ToList())
            DeleteMerge(ctx, m);
        foreach (var ban in ctx.Db.TownBan.ByTown.Filter(gone.Id).ToList()) ctx.Db.TownBan.Id.Update(ban with { TownId = keep.Id });
        foreach (var line in ctx.Db.TownLog.ByTown.Filter(gone.Id).ToList()) ctx.Db.TownLog.Id.Update(line with { TownId = keep.Id });
        ctx.Db.Town.Id.Delete(gone.Id);
        TownLogLine(ctx, keep.Id, $"{gone.Name} joined {keep.Name}");
        ctx.Db.TownOffice.Insert(new TownOffice { Id = 0, TownId = keep.Id, Office = Social.OfficeMayor, Holder = default, HolderId = 0, HolderName = "", TermEndsAt = ctx.Timestamp + new TimeDuration(VacancyMicros(ctx)) });
    }

    // Vote for who should hold an office. Open ballot: one vote per signer per office, changeable.
    // A majority of the signers decides at once; otherwise the term's end does (TownTerms).
    [SpacetimeDB.Reducer]
    public static void VoteTownOffice(ReducerContext ctx, byte office, uint candidatePlayerId)
    {
        MeterCall(ctx);
        if (Towns.Get(office) == null) throw new Exception("There is no such office");
        var me = SignerOf(ctx, Me(ctx)) ?? throw new Exception("Only signers vote");
        // An office nobody holds yet gets a vacant seat on its first vote, so its term can end.
        if (OfficeOf(ctx, me.TownId, office) == null)
            ctx.Db.TownOffice.Insert(new TownOffice { Id = 0, TownId = me.TownId, Office = office, Holder = default, HolderId = 0, HolderName = "", TermEndsAt = ctx.Timestamp + new TimeDuration(VacancyMicros(ctx)) });
        var candidate = ctx.Db.TownSigner.ByTown.Filter(me.TownId).FirstOrDefault(r => r.PlayerId == candidatePlayerId);
        if (candidate.Id == 0) throw new Exception("They are not on this roster");
        foreach (var old in ctx.Db.TownVote.ByTown.Filter(me.TownId).Where(v => v.Office == office && v.Voter == Me(ctx)).ToList())
            ctx.Db.TownVote.Id.Delete(old.Id);
        ctx.Db.TownVote.Insert(new TownVote { Id = 0, TownId = me.TownId, Office = office, Voter = Me(ctx), VoterId = me.PlayerId, CandidateId = candidatePlayerId });
        int signers = ctx.Db.TownSigner.ByTown.Filter(me.TownId).Count();
        var leader = ctx.Db.TownVote.ByTown.Filter(me.TownId).Where(v => v.Office == office)
            .GroupBy(v => v.CandidateId).Select(g => (id: g.Key, n: g.Count())).OrderByDescending(g => g.n).First();
        if (leader.n * 2 > signers) SeatOffice(ctx, me.TownId, office, ctx.Db.TownSigner.ByTown.Filter(me.TownId).First(r => r.PlayerId == leader.id));
    }

    // The winner takes the seat for a full term; the ballot starts afresh.
    static void SeatOffice(ReducerContext ctx, ulong townId, byte office, TownSigner winner)
    {
        var term = ctx.Timestamp + new TimeDuration(TermMicros(ctx));
        if (OfficeOf(ctx, townId, office) is not { } held || held.HolderId != winner.PlayerId)
            TownLogLine(ctx, townId, $"{winner.OwnerName} became {Towns.Get(office)?.Name ?? "an officer"}");
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
        SettleTerms(ctx);
    }

    static void SettleTerms(ReducerContext ctx)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        ExpireBans(ctx);
        foreach (var merge in ctx.Db.TownMerge.Iter().Where(m => m.ExpiresAt.MicrosecondsSinceUnixEpoch <= now).ToList())
            DeleteMerge(ctx, merge);
        foreach (var seat in ctx.Db.TownOffice.Iter().Where(o => o.TermEndsAt.MicrosecondsSinceUnixEpoch <= now).ToList())
        {
            var signers = ctx.Db.TownSigner.ByTown.Filter(seat.TownId).ToList();
            var tally = ctx.Db.TownVote.ByTown.Filter(seat.TownId).Where(v => v.Office == seat.Office)
                .GroupBy(v => v.CandidateId).Select(g => (id: g.Key, n: g.Count())).ToList();
            if (tally.Count > 0)
            {
                int best = tally.Max(t => t.n);
                var tied = signers.Where(sg => tally.Any(t => t.id == sg.PlayerId && t.n == best)).ToList();
                // A tie goes to whoever the Judge (or, with no Judge voting, the Elder) voted for, else to the longest-standing signer.
                var judgeVote = HoldersWith(ctx, seat.TownId, TownPower.BreakTies)
                    .Select(j => ctx.Db.TownVote.ByTown.Filter(seat.TownId).FirstOrDefault(v => v.Office == seat.Office && v.Voter == j))
                    .FirstOrDefault(v => v.Id != 0);
                var winner = tied.Count > 1 && judgeVote.Id != 0 && tied.FirstOrDefault(sg => sg.PlayerId == judgeVote.CandidateId) is { Id: not 0 } judged
                    ? judged
                    : tied.OrderBy(sg => sg.SignedAt.MicrosecondsSinceUnixEpoch).FirstOrDefault();
                if (winner.Id != 0) { SeatOffice(ctx, seat.TownId, seat.Office, winner); continue; }
            }
            long wait = seat.HolderId == 0 ? VacancyMicros(ctx) : TermMicros(ctx);
            ctx.Db.TownOffice.Id.Update(seat with { TermEndsAt = ctx.Timestamp + new TimeDuration(wait) });
        }
    }

    // The signer goes, with the votes they cast and received and the offices they held. An empty
    // town is closed.
    static void DropSigner(ReducerContext ctx, TownSigner row)
    {
        ctx.Db.TownSigner.Id.Delete(row.Id);
        TownLogLine(ctx, row.TownId, $"{row.OwnerName} left the roster");
        foreach (var v in ctx.Db.TownVote.ByTown.Filter(row.TownId).Where(v => v.Voter == row.Owner || v.CandidateId == row.PlayerId).ToList())
            ctx.Db.TownVote.Id.Delete(v.Id);
        if (!ctx.Db.TownSigner.ByTown.Filter(row.TownId).Any())
        {
            foreach (var invite in ctx.Db.TownInvite.Iter().Where(i => i.TownId == row.TownId).ToList())
                ctx.Db.TownInvite.Id.Delete(invite.Id);
            foreach (var seat in ctx.Db.TownOffice.ByTown.Filter(row.TownId).ToList())
                ctx.Db.TownOffice.Id.Delete(seat.Id);
            foreach (var m in ctx.Db.TownMerge.Iter().Where(m => m.FromTownId == row.TownId || m.ToTownId == row.TownId).ToList()) DeleteMerge(ctx, m);
            if (ctx.Db.TownVoice.TownId.Find(row.TownId) != null) ctx.Db.TownVoice.TownId.Delete(row.TownId);
            foreach (var ban in ctx.Db.TownBan.ByTown.Filter(row.TownId).ToList()) ctx.Db.TownBan.Id.Delete(ban.Id);
            foreach (var line in ctx.Db.TownLog.ByTown.Filter(row.TownId).ToList()) ctx.Db.TownLog.Id.Delete(line.Id);
            ctx.Db.Town.Id.Delete(row.TownId);
            return;
        }
        // A seat the leaver held falls vacant for a day, while the signers vote.
        foreach (var seat in ctx.Db.TownOffice.ByTown.Filter(row.TownId).Where(o => o.HolderId == row.PlayerId).ToList())
            ctx.Db.TownOffice.Id.Update(seat with { Holder = default, HolderId = 0, HolderName = "", TermEndsAt = ctx.Timestamp + new TimeDuration(VacancyMicros(ctx)) });
    }

    // A claim that falls or is taken down leaves its roster.
    static void TownDropClaim(ReducerContext ctx, Claim claim)
    {
        if (ctx.Db.TownSigner.ClaimId.Find(claim.Id) is { } row) DropSigner(ctx, row);
    }
}
