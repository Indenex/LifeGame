using LifeGame.Shared;
using SpacetimeDB;

// survival-37: a shared pot. A finished broth or stew pot on a fire holds Needs.PotBowls
// bowls. Its owner, and anyone in the owner's group within reach, can ladle one: a share of
// the dish's food and water, and a good meal (well fed). The owner's empty pot goes back to
// them after the last bowl. Nothing to tend (D-148): the pot keeps simmering as before.
public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void LadlePot(ReducerContext ctx, ulong potId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var pot = ctx.Db.SimmerPot.Id.Find(potId) ?? throw new Exception("That pot is gone");
        if ((Item)pot.Dish is not (Item.PotOfBroth or Item.PotOfStew))
            throw new Exception("There is nothing to ladle from that");
        RequireReach(pos, pot.X, pot.Z);
        if (pot.Owner != Me(ctx) && !SameGroup(ctx, pot.Owner, Me(ctx)))
            throw new Exception("That pot is not shared with you");
        float left = ClayRules.SimmerSeconds((Item)pot.Dish) - SecondsBetween(pot.PutAt, ctx.Timestamp);
        if (left > 0)
            throw new Exception($"Still simmering, about {MathF.Ceiling(left):0} s left");
        var v = RequireVitals(ctx, player);
        if (v.Food >= 1f)
            throw new Exception("You are full");
        var state = RequireCooldown(ctx, player);

        var (food, water) = Needs.Nourishment((Item)pot.Dish);
        ctx.Db.Vitals.PlayerId.Update(v with
        {
            Food = Math.Min(1f, v.Food + food / Needs.PotBowls),
            Water = Math.Min(1f, v.Water + water / Needs.PotBowls),
            WellFedUntil = ctx.Timestamp.MicrosecondsSinceUnixEpoch + Stamina.WellFedMicros,
        });

        byte served = (byte)(pot.Served + 1);
        if (served >= Needs.PotBowls)
        {
            ctx.Db.SimmerPot.Id.Delete(pot.Id);
            AddToInventory(ctx, pot.Owner, Item.CoilPot, 1, pot.Quality, pot.PotWear, pot.PotMaker);
        }
        else
            ctx.Db.SimmerPot.Id.Update(pot with { Served = served });
        NoteAct(ctx, player.PlayerId, WorkMotion.Eat, (Item)pot.Dish);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // Test hook for the check bots (SocialTest): the server admin (the database owner, through
    // the CLI) puts a finished pot on the ground beside a player (offsets from where they stand,
    // kept non-negative by callers so the CLI doesn't read them as flags), with some bowls
    // already ladled. Cooking one for real takes minutes of a fire and a fired pot.
    [SpacetimeDB.Reducer]
    public static void AdminPlaceSimmerPot(ReducerContext ctx, uint playerId, float dx, float dz, ushort dish, byte served)
    {
        var config = ctx.Db.ClaimConfig.Id.Find(0) ?? throw new Exception("Not set up");
        if (config.Admin != Me(ctx)) throw new Exception("Only the server admin can do that");
        var owner = ctx.Db.Player.PlayerId.Find(playerId) ?? throw new Exception("No such player");
        var at = ctx.Db.PlayerPos.PlayerId.Find(playerId) ?? throw new Exception("They are not in the world");
        float x = at.X + dx, z = at.Z + dz;
        if (!float.IsFinite(x) || !float.IsFinite(z) || (Item)dish is not (Item.PotOfBroth or Item.PotOfStew)) throw new Exception("Invalid pot");
        ctx.Db.SimmerPot.Insert(new SimmerPot
        {
            Id = 0,
            FireId = 0,
            Owner = owner.Identity,
            PlayerId = playerId,
            Dish = dish,
            Quality = 50,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            PutAt = ctx.Timestamp - new TimeDuration(10L * 60 * 1_000_000),
            Served = served,
        });
    }

    static bool SameGroup(ReducerContext ctx, Identity a, Identity b) =>
        ctx.Db.GroupMember.Member.Find(a) is { } ma && ctx.Db.GroupMember.Member.Find(b) is { } mb && ma.GroupId == mb.GroupId;
}
