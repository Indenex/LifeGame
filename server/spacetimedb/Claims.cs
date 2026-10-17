using LifeGame.Shared;
using SpacetimeDB;

// Land claims: a player builds a claim marker (a structure from Building.cs, laid
// out from the build menu), and once it stands the circle of land around it is
// theirs while they keep tending it. Outsiders cannot dig there or take what lies
// on the ground, members share a stash kept at the marker, and fighting inside a
// standing claim needs the owner's permission. Left alone, the marker falls.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Claim", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct Claim
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        // The finished ClaimMarker structure this claim belongs to.
        [Unique]
        public ulong StructureId;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        public uint OwnerPlayerId;
        public string OwnerName;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public Timestamp TendedAt;
        public Timestamp ExpiresAt;
        // Whether the owner allows fighting inside the claim.
        public bool AllowPvp;
    }

    // People the owner has let in: they can dig, pick up and use the stash.
    [SpacetimeDB.Table(Accessor = "ClaimMember", Public = true)]
    public partial struct ClaimMember
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong ClaimId;
        [SpacetimeDB.Index.BTree]
        public Identity Member;
        public uint PlayerId;
        public string Name;
    }

    // What is stored at a claim's marker, one row per item and quality.
    [SpacetimeDB.Table(Accessor = "ClaimStash")]
    [SpacetimeDB.Index.BTree(Accessor = "ByClaimItem", Columns = new[] { nameof(ClaimId), nameof(Item) })]
    public partial struct ClaimStash
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong ClaimId;
        // A LifeGame.Shared.Item id.
        public ushort Item;
        public byte Quality;
        public uint Count;
        [Default(0u)]
        public uint Wear;
        // Who made it (a PlayerId; 0 = unknown or not made). Part of a stack's identity with
        // quality and wear (Inventory.cs StackKey); only the server sets it (crafting-25).
        [Default(0u)]
        public uint Maker;
    }

    // Owners and members see their claims' stashes; nobody else does.
    [SpacetimeDB.View(Accessor = "MyClaimStash", Public = true)]
    public static List<ClaimStash> MyClaimStash(ViewContext ctx)
    {
        var result = new List<ClaimStash>();
        foreach (var claim in ctx.Db.Claim.Owner.Filter(Me(ctx)))
            result.AddRange(ctx.Db.ClaimStash.ClaimId.Filter(claim.Id));
        foreach (var membership in ctx.Db.ClaimMember.Member.Filter(Me(ctx)))
            result.AddRange(ctx.Db.ClaimStash.ClaimId.Filter(membership.ClaimId));
        return result;
    }

    // Tunables the database owner can change with `spacetime call`, mainly so a
    // playtest can watch a claim decay in minutes rather than days.
    [SpacetimeDB.Table(Accessor = "ClaimConfig")]
    public partial struct ClaimConfig
    {
        [PrimaryKey]
        public uint Id;
        public Identity Admin;
        public long LifetimeSeconds;
    }

    [SpacetimeDB.Table(Accessor = "ClaimDecayTimer", Scheduled = nameof(DecayClaims), ScheduledAt = nameof(ScheduledAt))]
    public partial struct ClaimDecayTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // Called from Init. The publisher becomes the claims admin.
    static void InitClaims(ReducerContext ctx)
    {
        if (ctx.Db.ClaimConfig.Id.Find(0) is null)
            ctx.Db.ClaimConfig.Insert(new ClaimConfig { Id = 0, Admin = Me(ctx), LifetimeSeconds = Social.DefaultClaimLifetimeSeconds });
        EnsureDecayTimer(ctx);
    }

    static void EnsureDecayTimer(ReducerContext ctx)
    {
        if (ctx.Db.ClaimDecayTimer.Count > 0) return;
        ctx.Db.ClaimDecayTimer.Insert(new ClaimDecayTimer
        {
            ScheduledId = 0,
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(20)),
        });
    }

    static long ClaimLifetimeSeconds(ReducerContext ctx) =>
        ctx.Db.ClaimConfig.Id.Find(0)?.LifetimeSeconds ?? Social.DefaultClaimLifetimeSeconds;

    // --- Rules other features call -----------------------------------------

    // How many chunks out from a point a claim's marker can stand and still cover it.
    static readonly int ClaimChunkReach = (int)MathF.Ceiling(Social.ClaimRadius / (World.ChunkSize * World.CellSize));

    // The standing claim covering a point, if any. Only markers in nearby chunks can reach it.
    public static Claim? ClaimAt(ReducerContext ctx, float x, float z)
    {
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z), r = ClaimChunkReach;
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
                foreach (var claim in ctx.Db.Claim.ByChunk.Filter((cx + dx, cz + dz)))
                    if (IsStanding(ctx, claim) && Social.InClaim(claim.X, claim.Z, x, z))
                        return claim;
        return null;
    }

    // House building asks this before placing or taking down a structure.
    static partial void CheckClaimAccess(ReducerContext ctx, float x, float z) => RequireClaimAccess(ctx, x, z);

    // Builder rights on a claim: its owner, its members, and (claims-19) anyone who signed
    // the same town roster as the claim. Building, digging, picking up, gates and the like.
    public static bool HasClaimRights(ReducerContext ctx, Claim claim, Identity who) =>
        !BannedFromClaim(ctx, claim, who) && (IsOwnerOrMember(ctx, claim, who) || IsTownmate(ctx, claim, who));

    // The inner circle only: the owner and the members the owner added. Stashes, tending the
    // marker and similar stay with them, not with the town.
    public static bool IsOwnerOrMember(ReducerContext ctx, Claim claim, Identity who)
    {
        if (claim.Owner == who) return true;
        if (BannedFromClaim(ctx, claim, who)) return false;
        foreach (var m in ctx.Db.ClaimMember.ClaimId.Filter(claim.Id))
            if (m.Member == who) return true;
        return false;
    }

    // Throws unless the sender may change the land or take things at this point.
    // Anything that digs, builds or removes items in the world should call this.
    public static void RequireClaimAccess(ReducerContext ctx, float x, float z)
    {
        if (ClaimAt(ctx, x, z) is not { } claim) return;
        if (!HasClaimRights(ctx, claim, Me(ctx)))
            throw new Exception(BanRefusal(ctx, claim, Me(ctx)) ?? $"This land is claimed by {claim.OwnerName}");
        // Working a neighbour's land under a town roster does not keep their claim standing.
        if (IsOwnerOrMember(ctx, claim, Me(ctx))) TendByWork(ctx, claim);
    }

    // claims-5: owners and members keep their claim standing just by working on it.
    // At most one write a minute (or a quarter of the lifetime, if shorter), so
    // busy work doesn't churn the claim row.
    static void TendByWork(ReducerContext ctx, Claim claim)
    {
        long life = ClaimLifetimeSeconds(ctx);
        long every = Math.Min(Social.WorkTendEverySeconds, Math.Max(1, life / 4)) * 1_000_000;
        if (ctx.Timestamp.MicrosecondsSinceUnixEpoch - claim.TendedAt.MicrosecondsSinceUnixEpoch < every) return;
        ctx.Db.Claim.Id.Update(claim with { TendedAt = ctx.Timestamp, ExpiresAt = After(ctx.Timestamp, life) });
    }

    // Fighting is allowed in the wild, and inside a standing claim only when its
    // owner has allowed it. Combat code should check both players' positions.
    public static bool PvpAllowedAt(ReducerContext ctx, float x, float z) =>
        ClaimAt(ctx, x, z) is not { } claim || claim.AllowPvp;

    public static void RequirePvpAllowed(ReducerContext ctx, float attackerX, float attackerZ, float victimX, float victimZ)
    {
        if (!PvpAllowedAt(ctx, attackerX, attackerZ) || !PvpAllowedAt(ctx, victimX, victimZ))
            throw new Exception("The owner of this land does not allow fighting here");
    }

    static bool IsStanding(ReducerContext ctx, Claim claim) =>
        claim.ExpiresAt.MicrosecondsSinceUnixEpoch > ctx.Timestamp.MicrosecondsSinceUnixEpoch;

    // --- Reducers ----------------------------------------------------------

    // Rules for laying out a claim marker, on top of Building's own placement checks.
    static partial void CheckStructureSite(ReducerContext ctx, StructureKind kind, float x, float z)
    {
        if (kind != StructureKind.ClaimMarker) return;
        EnsureDecayTimer(ctx);

        float keepOut = Social.SpawnClearRadius + Social.ClaimRadius;
        if (x * x + z * z < keepOut * keepOut)
            throw new Exception("This clearing is where newcomers wake up. Claim land further out");

        int owned = 0;
        foreach (var mine in ctx.Db.Claim.Owner.Filter(Me(ctx)))
            if (IsStanding(ctx, mine)) owned++;
        foreach (var site in ctx.Db.Structure.Owner.Filter(Me(ctx)))
            if (site.Kind == StructureKind.ClaimMarker && !site.Complete) owned++;
        if (owned >= Social.MaxClaimsPerPlayer)
            throw new Exception("You already have a claim marker. Take it down first");

        // Claims may not overlap, counting markers that are still being built.
        float minGap = Social.ClaimRadius * 2f;
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z), reach = ClaimChunkReach * 2;
        for (int dx = -reach; dx <= reach; dx++)
            for (int dz = -reach; dz <= reach; dz++)
            {
                foreach (var other in ctx.Db.Claim.ByChunk.Filter((cx + dx, cz + dz)))
                    if (IsStanding(ctx, other) && Closer(other.X - x, other.Z - z, minGap))
                        throw new Exception($"Too close to {other.OwnerName}'s claim. Move {MoveAway(other.X - x, other.Z - z, minGap)} m further from their marker, or the edges just touch at {minGap:0} m");
                foreach (var site in ctx.Db.Structure.ByChunk.Filter((cx + dx, cz + dz)))
                    if (site.Kind == StructureKind.ClaimMarker && !site.Complete && Closer(site.X - x, site.Z - z, minGap))
                        throw new Exception($"Too close to another claim marker being built. Move {MoveAway(site.X - x, site.Z - z, minGap)} m further away");
            }
    }

    // The marker's last material went in: the land around it is claimed from now on.
    static partial void OnStructureComplete(ReducerContext ctx, Structure s)
    {
        if (s.Kind != StructureKind.ClaimMarker) return;
        EnsureDecayTimer(ctx);
        var owner = ctx.Db.Player.Identity.Find(s.Owner);
        ctx.Db.Claim.Insert(new Claim
        {
            Id = 0,
            StructureId = s.Id,
            Owner = s.Owner,
            OwnerPlayerId = owner?.PlayerId ?? 0,
            OwnerName = owner?.Name ?? "someone",
            X = s.X,
            Z = s.Z,
            ChunkX = World.ChunkOf(s.X),
            ChunkZ = World.ChunkOf(s.Z),
            TendedAt = ctx.Timestamp,
            ExpiresAt = After(ctx.Timestamp, ClaimLifetimeSeconds(ctx)),
            AllowPvp = false,
        });
    }

    // Dismantling the marker ends the claim.
    // claims-2: the owner of a standing claim may take down anything built inside
    // it, members' buildings included (the land is theirs). Building's Dismantle
    // asks this when the caller isn't whoever laid the building out.
    static partial void CanClearStructure(ReducerContext ctx, Structure s, ref bool allowed)
    {
        if (ClaimAt(ctx, s.X, s.Z) is { } claim && claim.Owner == Me(ctx)) allowed = true;
    }

    // Weathering (D-046): nothing inside a standing claim weathers. Building's
    // Weathering.cs asks this through InClaimHook.
    static partial void InClaimHook(ReducerContext ctx, float x, float z, ref bool inClaim);
    static partial void InClaimHook(ReducerContext ctx, float x, float z, ref bool inClaim)
    {
        if (ClaimAt(ctx, x, z) is not null) inClaim = true;
    }

    static partial void OnStructureRemoved(ReducerContext ctx, Structure s)
    {
        if (s.Kind == StructureKind.ClaimMarker && ctx.Db.Claim.StructureId.Find(s.Id) is { } claim)
            RemoveClaim(ctx, claim);
    }

    // How many whole metres further from the other marker a spot must go to be allowed.
    static int MoveAway(float dx, float dz, float distance) =>
        Math.Max(1, (int)MathF.Ceiling(distance - MathF.Sqrt(dx * dx + dz * dz)));

    static bool Closer(float dx, float dz, float distance) => dx * dx + dz * dz < distance * distance;

    // Owners and members keep a claim standing by visiting the marker.
    [SpacetimeDB.Reducer]
    public static void TendClaim(ReducerContext ctx, ulong claimId)
    {
        var player = RequirePlayer(ctx);
        var claim = RequireStandingClaim(ctx, claimId);
        if (!IsOwnerOrMember(ctx, claim, Me(ctx)))
            throw new Exception("Only the owner and members can tend this marker");
        RequireNearMarker(ctx, player, claim);

        var updated = claim with { TendedAt = ctx.Timestamp, ExpiresAt = After(ctx.Timestamp, ClaimLifetimeSeconds(ctx)) };
        // Keep the owner's name current if they renamed themselves.
        if (claim.Owner == Me(ctx)) updated = updated with { OwnerName = player.Name };
        ctx.Db.Claim.Id.Update(updated);
    }

    [SpacetimeDB.Reducer]
    public static void SetClaimPvp(ReducerContext ctx, ulong claimId, bool allow)
    {
        MeterCall(ctx);
        var claim = RequireOwnClaim(ctx, claimId);
        ctx.Db.Claim.Id.Update(claim with { AllowPvp = allow });
    }

    [SpacetimeDB.Reducer]
    public static void AddClaimMember(ReducerContext ctx, uint playerId)
    {
        MeterCall(ctx);
        var claim = RequireOwnStandingClaim(ctx);
        var who = ctx.Db.Player.PlayerId.Find(playerId) ?? throw new Exception("No such player");
        AddMember(ctx, claim, who);
    }

    // claims-4: the owner adds someone by name from the claim panel, near or far,
    // online or not. Names are not unique: if several people share one and exactly
    // one of them is online, that one is meant; otherwise invite them in person.
    [SpacetimeDB.Reducer]
    public static void AddClaimMemberByName(ReducerContext ctx, string name)
    {
        MeterCall(ctx);
        var claim = RequireOwnStandingClaim(ctx);
        name = (name ?? "").Trim();
        if (name.Length == 0) throw new Exception("Type the name of whoever you want to add");
        var matches = ctx.Db.Player.Iter()
            .Where(p => string.Equals(p.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0) throw new Exception($"Nobody called {name} has been here");
        if (matches.Count > 1)
        {
            var online = matches.Where(p => p.Online).ToList();
            if (online.Count != 1)
                throw new Exception($"More than one person goes by {name}. Invite them in person, near the marker");
            matches = online;
        }
        AddMember(ctx, claim, matches[0]);
    }

    static void AddMember(ReducerContext ctx, Claim claim, Player who)
    {
        if (who.Identity == Me(ctx)) throw new Exception("You already own this claim");
        if (IsOwnerOrMember(ctx, claim, who.Identity)) throw new Exception($"{who.Name} is already a member");
        ctx.Db.ClaimMember.Insert(new ClaimMember { Id = 0, ClaimId = claim.Id, Member = who.Identity, PlayerId = who.PlayerId, Name = who.Name });

        // A calm line by the marker, and where the new member stands if that's elsewhere.
        string owner = ctx.Db.Player.Identity.Find(claim.Owner)?.Name ?? "the owner";
        string line = $"{who.Name} now shares {owner}'s land";
        SayNearby(ctx, claim.X, claim.Z, line);
        if (who.Online && ctx.Db.PlayerPos.PlayerId.Find(who.PlayerId) is { } pos
            && (pos.X - claim.X) * (pos.X - claim.X) + (pos.Z - claim.Z) * (pos.Z - claim.Z) > Social.ChatRange * Social.ChatRange)
            SayNearby(ctx, pos.X, pos.Z, line);
    }

    [SpacetimeDB.Reducer]
    public static void RemoveClaimMember(ReducerContext ctx, uint playerId)
    {
        MeterCall(ctx);
        var claim = RequireOwnStandingClaim(ctx);
        foreach (var m in ctx.Db.ClaimMember.ClaimId.Filter(claim.Id).ToList())
            if (m.PlayerId == playerId) ctx.Db.ClaimMember.Id.Delete(m.Id);
    }

    // A member can also leave a claim on their own.
    [SpacetimeDB.Reducer]
    public static void LeaveClaim(ReducerContext ctx, ulong claimId)
    {
        MeterCall(ctx);
        foreach (var m in ctx.Db.ClaimMember.ClaimId.Filter(claimId).ToList())
            if (m.Member == Me(ctx)) ctx.Db.ClaimMember.Id.Delete(m.Id);
    }

    // Taking a marker down drops whatever was in the stash at its foot.
    // (Dismantling it from the build menu does the same, and gives the materials back.)
    [SpacetimeDB.Reducer]
    public static void AbandonClaim(ReducerContext ctx, ulong claimId)
    {
        MeterCall(ctx);
        var claim = RequireOwnClaim(ctx, claimId);
        RemoveClaim(ctx, claim);
    }

    // claims-14: move a whole stack (or part of one) in one call; the stash panel's
    // drag and drop sends the stack size. The bag's own limit still applies.
    [SpacetimeDB.Reducer]
    public static void StashItem(ReducerContext ctx, ulong claimId, ushort item, byte quality, uint count)
    {
        MeterCall(ctx);
        if (count == 0) throw new Exception("Nothing to leave");
        var claim = RequireStashAccess(ctx, claimId);
        uint carried = CarriedCount(ctx, Me(ctx), (Item)item, quality);
        if (carried == 0) throw new Exception("You are not carrying that");
        if (carried < count) throw new Exception("You are not carrying that many");
        // The most worn go first, and each keeps its wear (shared/Durability.cs).
        uint left = count;
        while (left > 0 && FindStack(ctx, Me(ctx), (Item)item, quality) is { } stack)
        {
            uint n = Math.Min(left, stack.Count);
            RemoveFromStack(ctx, stack, n);
            if (FindStash(ctx, claim.Id, item, quality, stack.Wear, stack.Maker) is { } row)
                ctx.Db.ClaimStash.Id.Update(row with { Count = row.Count + n });
            else
                ctx.Db.ClaimStash.Insert(new ClaimStash { Id = 0, ClaimId = claim.Id, Item = item, Quality = quality, Count = n, Wear = stack.Wear, Maker = stack.Maker });
            left -= n;
        }
        LogStash(ctx, claim.Id, item, (int)count);
    }

    [SpacetimeDB.Reducer]
    public static void TakeFromStash(ReducerContext ctx, ulong claimId, ushort item, byte quality, uint count)
    {
        MeterCall(ctx);
        if (count == 0) throw new Exception("Nothing to take");
        var claim = RequireStashAccess(ctx, claimId);
        uint held = 0;
        foreach (var r in ctx.Db.ClaimStash.ByClaimItem.Filter((claim.Id, item)))
            if (r.Quality == quality) held += r.Count;
        if (held == 0) throw new Exception("The stash has none of that");
        if (held < count) throw new Exception("The stash doesn't have that many");
        uint left = count;
        while (left > 0 && FindStash(ctx, claim.Id, item, quality) is { } row)
        {
            uint n = Math.Min(left, row.Count);
            if (row.Count == n)
                ctx.Db.ClaimStash.Id.Delete(row.Id);
            else
                ctx.Db.ClaimStash.Id.Update(row with { Count = row.Count - n });
            AddToInventory(ctx, Me(ctx), (Item)item, n, quality, row.Wear, row.Maker);
            left -= n;
        }
        LogStash(ctx, claim.Id, item, -(int)count);
    }

    // Database owner only: how long markers stand untended. Applies to markers
    // tended or raised from now on.
    [SpacetimeDB.Reducer]
    public static void SetClaimLifetime(ReducerContext ctx, long seconds)
    {
        MeterCall(ctx);
        var config = ctx.Db.ClaimConfig.Id.Find(0) ?? throw new Exception("Claims are not set up");
        if (config.Admin != Me(ctx)) throw new Exception("Only the server admin can change this");
        if (seconds < 30) throw new Exception("At least 30 seconds");
        ctx.Db.ClaimConfig.Id.Update(config with { LifetimeSeconds = seconds });
        // Shorten standing claims too, so a playtest can see decay without re-tending.
        foreach (var claim in ctx.Db.Claim.Iter().ToList())
        {
            var latest = After(claim.TendedAt, seconds);
            if (latest.MicrosecondsSinceUnixEpoch < claim.ExpiresAt.MicrosecondsSinceUnixEpoch)
                ctx.Db.Claim.Id.Update(claim with { ExpiresAt = latest });
        }
    }

    // Markers whose time ran out fall: the claim, its members and its stash go,
    // and the stash spills onto the ground for anyone to find.
    [SpacetimeDB.Reducer]
    public static void DecayClaims(ReducerContext ctx, ClaimDecayTimer timer)
    {
        RequireServer(ctx);
        foreach (var claim in ctx.Db.Claim.Iter().ToList())
            if (!IsStanding(ctx, claim))
                RemoveClaim(ctx, claim);
    }

    // --- Helpers -----------------------------------------------------------

    static void RemoveClaim(ReducerContext ctx, Claim claim)
    {
        foreach (var row in ctx.Db.ClaimStash.ClaimId.Filter(claim.Id).ToList())
        {
            for (uint i = 0; i < row.Count; i++)
            {
                float angle = (float)(ctx.Rng.NextDouble() * Math.PI * 2);
                float r = 0.4f + (float)ctx.Rng.NextDouble() * 1.2f;
                float x = claim.X + MathF.Cos(angle) * r, z = claim.Z + MathF.Sin(angle) * r;
                ctx.Db.WorldItem.Insert(new WorldItem { Id = 0, Item = row.Item, Quality = row.Quality, Wear = row.Wear, Maker = row.Maker, X = x, Z = z, ChunkX = World.ChunkOf(x), ChunkZ = World.ChunkOf(z) });
            }
            ctx.Db.ClaimStash.Id.Delete(row.Id);
        }
        foreach (var m in ctx.Db.ClaimMember.ClaimId.Filter(claim.Id).ToList())
            ctx.Db.ClaimMember.Id.Delete(m.Id);
        RemoveClaimName(ctx, claim.Id);
        RemoveStashLog(ctx, claim.Id);
        TownDropClaim(ctx, claim);
        ctx.Db.Claim.Id.Delete(claim.Id);
        // The marker post goes with it; what was built into it is not given back.
        if (ctx.Db.Structure.Id.Find(claim.StructureId) is { } marker)
        {
            foreach (var row in ctx.Db.StructureBuilder.StructureId.Filter(marker.Id).ToList())
                ctx.Db.StructureBuilder.Id.Delete(row.Id);
            ctx.Db.Structure.Id.Delete(marker.Id);
        }
    }

    static Claim RequireStandingClaim(ReducerContext ctx, ulong claimId)
    {
        var claim = ctx.Db.Claim.Id.Find(claimId) ?? throw new Exception("That marker is gone");
        if (!IsStanding(ctx, claim)) throw new Exception("That marker has fallen");
        return claim;
    }

    static Claim RequireOwnClaim(ReducerContext ctx, ulong claimId)
    {
        var claim = ctx.Db.Claim.Id.Find(claimId) ?? throw new Exception("That marker is gone");
        if (claim.Owner != Me(ctx)) throw new Exception("Only the owner can do that");
        return claim;
    }

    static Claim RequireOwnStandingClaim(ReducerContext ctx)
    {
        foreach (var claim in ctx.Db.Claim.Owner.Filter(Me(ctx)))
            if (IsStanding(ctx, claim)) return claim;
        throw new Exception("You do not hold a claim");
    }

    static Claim RequireStashAccess(ReducerContext ctx, ulong claimId)
    {
        var player = RequirePlayer(ctx);
        var claim = RequireStandingClaim(ctx, claimId);
        if (!IsOwnerOrMember(ctx, claim, Me(ctx))) throw new Exception("This stash is not yours to open");
        RequireNearMarker(ctx, player, claim);
        return claim;
    }

    static void RequireNearMarker(ReducerContext ctx, Player player, Claim claim)
    {
        var pos = RequirePos(ctx, player);
        float dx = claim.X - pos.X, dz = claim.Z - pos.Z;
        if (dx * dx + dz * dz > Social.MarkerReach * Social.MarkerReach)
            throw new Exception("Walk up to the marker first");
    }

    // The stash row of this item and quality; the most worn first, as FindStack does.
    static ClaimStash? FindStash(ReducerContext ctx, ulong claimId, ushort item, byte quality)
    {
        ClaimStash? found = null;
        foreach (var row in ctx.Db.ClaimStash.ByClaimItem.Filter((claimId, item)))
            if (row.Quality == quality && (found is null || Durability.Used(row.Wear) > Durability.Used(found.Value.Wear)))
                found = row;
        return found;
    }

    static ClaimStash? FindStash(ReducerContext ctx, ulong claimId, ushort item, byte quality, uint wear, uint maker)
    {
        var key = new StackKey(item, quality, wear, maker);
        foreach (var row in ctx.Db.ClaimStash.ByClaimItem.Filter((claimId, item)))
            if (KeyOf(row) == key)
                return row;
        return null;
    }

    static Timestamp After(Timestamp t, long seconds) =>
        new Timestamp(t.MicrosecondsSinceUnixEpoch + seconds * 1_000_000);
}
