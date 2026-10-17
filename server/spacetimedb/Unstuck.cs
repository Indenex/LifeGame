using LifeGame.Shared;
using SpacetimeDB;

// "I'm stuck" (bugfix-44, D-182). A player (or a check bot) who is wedged in something can ask
// the server to move them to the nearest free standing spot. The server decides everything: it
// searches outward from where they stand, then falls back to their own claim marker, then to
// the world's spawn. From free ground it still nudges (UnstuckNudge). A 60 s cooldown and the search radius keep it from being a way to travel.
public static partial class Module
{
    public const float UnstuckRadius = 12f;
    public const double UnstuckCooldownSeconds = 60;
    // A destination is kept this far from any solid footprint; a player already this clear is not stuck.
    const float UnstuckClearance = 0.45f;
    const float UnstuckStep = 0.5f;
    // The least a player who already stands on free ground is moved.
    const float UnstuckNudge = 1.5f;

    // When each player last used it. Private: only the server reads it.
    [SpacetimeDB.Table(Accessor = "UnstuckUse")]
    public partial struct UnstuckUse
    {
        [PrimaryKey]
        public uint PlayerId;
        public Timestamp At;
    }

    [SpacetimeDB.Reducer]
    public static void Unstuck(ReducerContext ctx)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var used = ctx.Db.UnstuckUse.PlayerId.Find(player.PlayerId);
        if (used is { } u)
        {
            double left = UnstuckCooldownSeconds - SecondsBetween(u.At, ctx.Timestamp);
            if (left > 0) throw new Exception($"You can shake yourself free again in {(int)Math.Ceiling(left)} s");
        }
        if (PlayerAirHeight(ctx, player.PlayerId) > 0.05f) throw new Exception("Wait until you land");
        // Mid-stride (a step not yet applied that is well away from the last position): not stuck.
        if (ctx.Db.MoveInput.PlayerId.Find(player.PlayerId) is { } pending
            && Dist(pending.X, pending.Z, pos.X, pos.Z) > 1.5f)
            throw new Exception("Stand still a moment first");

        // Most real stuck cases look free to the server (pressed against a wall's edge), so it
        // always helps: from free ground the player is nudged at least UnstuckNudge away, to the
        // spot with the most room round it.
        var (x, z) = (StandingFree(ctx, pos.X, pos.Z, PlayerBody)
                ? NudgeSpot(ctx, pos.X, pos.Z)
                : FindFreeSpot(ctx, pos.X, pos.Z, UnstuckRadius))
            ?? OwnClaimSpot(ctx)
            ?? FindFreeSpot(ctx, 0f, 0f, UnstuckRadius)
            ?? (0f, 0f);

        ctx.Db.PlayerPos.PlayerId.Update(pos with
        {
            X = x,
            Z = z,
            ChunkX = (short)World.ChunkOf(x),
            ChunkZ = (short)World.ChunkOf(z),
        });
        ctx.Db.MoveInput.PlayerId.Delete(player.PlayerId);
        if (ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) is { } state)
            ctx.Db.PlayerState.PlayerId.Update(state with { LastMoveAt = ctx.Timestamp });
        var row = new UnstuckUse { PlayerId = player.PlayerId, At = ctx.Timestamp };
        if (used is null) ctx.Db.UnstuckUse.Insert(row);
        else ctx.Db.UnstuckUse.PlayerId.Update(row);
        Log.Info($"unstuck: {player.Name} ({player.PlayerId}) from {pos.X:0.0},{pos.Z:0.0} to {x:0.0},{z:0.0}");
    }

    // The nearest free spot to the caller's own standing claim marker, if they have one.
    static (float, float)? OwnClaimSpot(ReducerContext ctx)
    {
        foreach (var claim in ctx.Db.Claim.Owner.Filter(Me(ctx)))
            if (IsStanding(ctx, claim) && FindFreeSpot(ctx, claim.X, claim.Z, 6f) is { } spot) return spot;
        return null;
    }

    // Rings outward from (cx, cz) in half-metre steps; the first free point on the nearest ring wins.
    static (float, float)? FindFreeSpot(ReducerContext ctx, float cx, float cz, float radius)
    {
        for (float r = 0f; r <= radius; r += UnstuckStep)
        {
            int points = r == 0f ? 1 : (int)MathF.Ceiling(MathF.Tau * r / UnstuckStep);
            for (int i = 0; i < points; i++)
            {
                float a = MathF.Tau * i / points;
                float x = cx + MathF.Cos(a) * r, z = cz + MathF.Sin(a) * r;
                if (StandingFree(ctx, x, z, UnstuckClearance)) return (x, z);
            }
        }
        return null;
    }

    // From free ground: of the free spots on the first rings at least UnstuckNudge away, the one
    // with the most clear room around it (so away from the nearest wall or obstacle); nearer wins a tie.
    static (float, float)? NudgeSpot(ReducerContext ctx, float cx, float cz)
    {
        (float, float)? best = null;
        float bestRoom = -1f;
        bool found = false;
        for (float r = UnstuckNudge; r <= UnstuckRadius; r += UnstuckStep)
        {
            int points = (int)MathF.Ceiling(MathF.Tau * r / UnstuckStep);
            for (int i = 0; i < points; i++)
            {
                float a = MathF.Tau * i / points;
                float x = cx + MathF.Cos(a) * r, z = cz + MathF.Sin(a) * r;
                if (!StandingFree(ctx, x, z, UnstuckClearance)) continue;
                float room = UnstuckClearance;
                while (room < 3f && StandingFree(ctx, x, z, room + UnstuckStep)) room += UnstuckStep;
                if (room > bestRoom) { bestRoom = room; best = (x, z); }
                found = true;
            }
            // Look one metre past the first ring that has a free spot, then settle.
            if (found && r >= UnstuckNudge + 1f) break;
        }
        return best;
    }

    // Whether a body of this size can stand here: ground that is dry or only wading depth, no
    // structure footprint or trunk or boulder (grown by margin), and no claim the caller has no rights in.
    static bool StandingFree(ReducerContext ctx, float x, float z, float margin)
    {
        float ground = Structures.GroundAt(x, z, (i, j) => DepthAt(ctx, i, j));
        if (Wading.Floating(Wading.Depth(ground))) return false;
        // Not in a tree trunk or boulder (shared/Obstacles.cs, the rule the server enforces on steps).
        if (Obstacles.PushOut(new ServerGround(ctx), margin, x, z, 0f) != (x, z)) return false;
        foreach (var s in StructuresNear(ctx, x, z))
        {
            // A plan that has only been marked out has nothing to bump into yet.
            if (!s.Complete && s.Stage < 1) continue;
            if (Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, x, z, margin)) return false;
        }
        return !(ClaimAt(ctx, x, z) is { } claim && !HasClaimRights(ctx, claim, Me(ctx)));
    }
}
