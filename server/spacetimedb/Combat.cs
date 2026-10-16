using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// claims-3: players striking players. Open in the wild; refused on claimed land
// unless its owner allows fighting, in the waking clearing, and for newcomers.
// Being struck down is the same death as hunger or cold (Survival's KillPlayer).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "PlayerHealth", Public = true)]
    public partial struct PlayerHealth
    {
        [PrimaryKey]
        public uint PlayerId;
        // Health as of AsOf; clients work out regeneration with Social.HealthAt.
        public float Health;
        public Timestamp AsOf;
        public Timestamp LastHitAt;
        public Timestamp LastStrikeAt;
        // Newcomers can't strike or be struck until then.
        public Timestamp GraceUntil;
        public bool Fed;
        // After striking someone down, a player's name shows red to others until
        // then (D-053).
        public Timestamp MarkedUntil;
    }

    // Tells nearby clients a strike landed, so the struck player flinches.
    [SpacetimeDB.Table(Accessor = "StrikeEvent", Public = true, Event = true)]
    public partial struct StrikeEvent
    {
        public uint AttackerId;
        public uint VictimId;
        public float X;
        public float Z;
        public bool Killed;
    }

    // Playtest and test setting: how long newcomers are protected. Only the server
    // admin (Claims' ClaimConfig) can change it.
    [SpacetimeDB.Table(Accessor = "CombatConfig")]
    public partial struct CombatConfig
    {
        [PrimaryKey]
        public uint Id;
        public long GraceMicros;
    }

    static long NewcomerGrace(ReducerContext ctx) =>
        ctx.Db.CombatConfig.Id.Find(0)?.GraceMicros ?? Social.NewcomerGraceMicros;

    [SpacetimeDB.Reducer]
    public static void SetNewcomerGrace(ReducerContext ctx, long seconds)
    {
        var config = ctx.Db.ClaimConfig.Id.Find(0) ?? throw new Exception("Not set up");
        if (config.Admin != ctx.Sender) throw new Exception("Only the server admin can change this");
        var row = new CombatConfig { Id = 0, GraceMicros = Math.Max(0, seconds) * 1_000_000 };
        if (ctx.Db.CombatConfig.Id.Find(0) is null) ctx.Db.CombatConfig.Insert(row);
        else ctx.Db.CombatConfig.Id.Update(row);
    }

    // A row for each player the first time they are seen. A player who already
    // existed before fighting came in gets a short grace once, like a newcomer.
    static PlayerHealth EnsureHealth(ReducerContext ctx, uint playerId)
    {
        if (ctx.Db.PlayerHealth.PlayerId.Find(playerId) is { } h) return h;
        return ctx.Db.PlayerHealth.Insert(new PlayerHealth
        {
            PlayerId = playerId,
            Health = Social.MaxHealth,
            AsOf = ctx.Timestamp,
            LastHitAt = new Timestamp(0),
            LastStrikeAt = new Timestamp(0),
            GraceUntil = ctx.Timestamp + new TimeDuration(NewcomerGrace(ctx)),
            Fed = true,
            MarkedUntil = new Timestamp(0),
        });
    }

    static bool IsFed(ReducerContext ctx, Player player) =>
        NeedsNow(ctx, player.PlayerId) is { } n && n.food > Social.WellFed && n.water > Social.WellFed;

    // Health brought up to now.
    static PlayerHealth HealthNow(ReducerContext ctx, PlayerHealth h, bool fed)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        float hp = Social.HealthAt(h.Health, h.AsOf.MicrosecondsSinceUnixEpoch, h.LastHitAt.MicrosecondsSinceUnixEpoch, now, h.Fed);
        return h with { Health = hp, AsOf = ctx.Timestamp, Fed = fed };
    }

    [SpacetimeDB.Reducer]
    public static void StrikePlayer(ReducerContext ctx, uint targetPlayerId)
    {
        var attacker = RequirePlayer(ctx);
        if (attacker.PlayerId == targetPlayerId) throw new Exception("You can't strike yourself");
        var victim = ctx.Db.Player.PlayerId.Find(targetPlayerId) ?? throw new Exception("Nobody is there");
        if (!victim.Online) throw new Exception("Nobody is there");
        var a = RequirePos(ctx, attacker);
        var b = ctx.Db.PlayerPos.PlayerId.Find(victim.PlayerId) ?? throw new Exception("Nobody is there");

        float dx = b.X - a.X, dz = b.Z - a.Z;
        float reach = Social.StrikeReach + Social.StrikeReachSlack;
        if (dx * dx + dz * dz > reach * reach) throw new Exception("Too far away");

        if (Social.InSpawnClearing(a.X, a.Z) || Social.InSpawnClearing(b.X, b.Z))
            throw new Exception("Nobody fights in the clearing where newcomers wake");
        RequirePvpAllowed(ctx, a.X, a.Z, b.X, b.Z);

        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        var mine = EnsureHealth(ctx, attacker.PlayerId);
        var theirs = EnsureHealth(ctx, victim.PlayerId);
        if (theirs.GraceUntil.MicrosecondsSinceUnixEpoch > now)
            throw new Exception($"{victim.Name} is new here. Leave them be for now");
        if (now - mine.LastStrikeAt.MicrosecondsSinceUnixEpoch < Social.StrikeCooldownMicros)
            throw new Exception("Too soon");

        // Striking ends your own newcomer grace.
        mine = HealthNow(ctx, mine, IsFed(ctx, attacker)) with { LastStrikeAt = ctx.Timestamp };
        if (mine.GraceUntil.MicrosecondsSinceUnixEpoch > now) mine = mine with { GraceUntil = ctx.Timestamp };
        ctx.Db.PlayerHealth.PlayerId.Update(mine);

        bool Has(Item item) => ctx.Db.InventoryItem.Owner.Filter(attacker.Identity).Any(s => s.Item == (ushort)item && s.Count > 0);
        float damage = Social.StrikeDamage(Has(Item.Spear), Has(Item.HandAxe), Has(Item.Flake));

        theirs = HealthNow(ctx, theirs, IsFed(ctx, victim));
        float left = theirs.Health - damage;
        bool killed = left <= 0f;
        ctx.Db.StrikeEvent.Insert(new StrikeEvent { AttackerId = attacker.PlayerId, VictimId = victim.PlayerId, X = b.X, Z = b.Z, Killed = killed });

        if (!killed)
        {
            ctx.Db.PlayerHealth.PlayerId.Update(theirs with { Health = left, LastHitAt = ctx.Timestamp });
            return;
        }

        ctx.Db.PlayerHealth.PlayerId.Update(theirs with { Health = Social.MaxHealth, LastHitAt = new Timestamp(0) });
        if (ctx.Db.PlayerHealth.PlayerId.Find(attacker.PlayerId) is { } killer)
            ctx.Db.PlayerHealth.PlayerId.Update(killer with { MarkedUntil = ctx.Timestamp + new TimeDuration(Social.KillerMarkMicros) });
        // Survival's death screen reads "You were struck down" for this cause.
        KillPlayer(ctx, victim, "slain");
        SayNearby(ctx, b.X, b.Z, $"{victim.Name} was struck down by {attacker.Name}");
    }
}
