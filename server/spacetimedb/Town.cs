using LifeGame.Shared;
using SpacetimeDB;

// claims-19: towns (Justin: "Add neighbors together via a roster everyone signs").
// A claim owner starts a roster, a town with a name. Anyone already on the roster can invite
// another claim owner, who signs to join. Everyone who signed gets builder rights (build, dig,
// pick up, gates) on every signed claim, via Claims.HasClaimRights. Stashes, tending a marker
// and the fighting rule stay with each claim's owner and members. Leaving the roster, or the
// claim falling, takes the rights away. All of it is decided here on the server (D-182).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Town", Public = true)]
    public partial struct Town
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public string Name;
        // Whoever may remove others from the roster; passes to the longest-standing signer.
        public Identity Founder;
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

    [SpacetimeDB.Reducer]
    public static void StartTown(ReducerContext ctx, string name)
    {
        var player = RequirePlayer(ctx);
        var claim = RequireOwnStandingClaim(ctx);
        if (SignerOf(ctx, ctx.Sender) != null) throw new Exception("You are already on a town roster");
        name = Social.CleanClaimName(name);
        if (name.Length == 0) throw new Exception("Give the town a name");
        var town = ctx.Db.Town.Insert(new Town { Id = 0, Name = name, Founder = ctx.Sender, StartedAt = ctx.Timestamp });
        Sign(ctx, town.Id, claim, player);
    }

    static void Sign(ReducerContext ctx, ulong townId, Claim claim, Player player) =>
        ctx.Db.TownSigner.Insert(new TownSigner
        {
            Id = 0, TownId = townId, ClaimId = claim.Id, Owner = player.Identity,
            PlayerId = player.PlayerId, OwnerName = player.Name, SignedAt = ctx.Timestamp,
        });

    // Anyone on the roster may invite another claim owner who is not on one.
    [SpacetimeDB.Reducer]
    public static void InviteToTown(ReducerContext ctx, uint playerId)
    {
        var me = RequirePlayer(ctx);
        var mine = SignerOf(ctx, ctx.Sender) ?? throw new Exception("Start or join a town first");
        var town = ctx.Db.Town.Id.Find(mine.TownId) ?? throw new Exception("That town is gone");
        var them = ctx.Db.Player.PlayerId.Find(playerId) ?? throw new Exception("No such player");
        if (them.Identity == ctx.Sender) throw new Exception("You are already on the roster");
        bool hasClaim = ctx.Db.Claim.Owner.Filter(them.Identity).Any(c => IsStanding(ctx, c));
        if (!hasClaim) throw new Exception($"{them.Name} has no claim to sign with");
        if (SignerOf(ctx, them.Identity) != null) throw new Exception($"{them.Name} is already on a town roster");
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

    // Only the founder removes someone else; anyone may leave on their own.
    [SpacetimeDB.Reducer]
    public static void RemoveFromTown(ReducerContext ctx, uint playerId)
    {
        var mine = SignerOf(ctx, ctx.Sender) ?? throw new Exception("You are not on a town roster");
        var town = ctx.Db.Town.Id.Find(mine.TownId) ?? throw new Exception("That town is gone");
        if (town.Founder != ctx.Sender) throw new Exception("Only the town's founder can remove a claim from the roster");
        var target = ctx.Db.TownSigner.ByTown.Filter(town.Id).FirstOrDefault(r => r.PlayerId == playerId);
        if (target.Id == 0) throw new Exception("They are not on this roster");
        if (target.Owner == ctx.Sender) throw new Exception("Use leave to step down yourself");
        DropSigner(ctx, target);
    }

    // The signer goes; an empty town is dissolved and a founderless one passes to the longest-signed.
    static void DropSigner(ReducerContext ctx, TownSigner row)
    {
        ctx.Db.TownSigner.Id.Delete(row.Id);
        var rest = ctx.Db.TownSigner.ByTown.Filter(row.TownId).OrderBy(r => r.SignedAt.MicrosecondsSinceUnixEpoch).ToList();
        if (rest.Count == 0)
        {
            foreach (var invite in ctx.Db.TownInvite.Iter().Where(i => i.TownId == row.TownId).ToList())
                ctx.Db.TownInvite.Id.Delete(invite.Id);
            ctx.Db.Town.Id.Delete(row.TownId);
            return;
        }
        if (ctx.Db.Town.Id.Find(row.TownId) is { } town && town.Founder == row.Owner)
            ctx.Db.Town.Id.Update(town with { Founder = rest[0].Owner });
    }

    // A claim that falls or is taken down leaves its roster.
    static void TownDropClaim(ReducerContext ctx, Claim claim)
    {
        if (ctx.Db.TownSigner.ClaimId.Find(claim.Id) is { } row) DropSigner(ctx, row);
    }
}
