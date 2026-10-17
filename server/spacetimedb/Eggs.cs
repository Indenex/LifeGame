using LifeGame.Shared;
using SpacetimeDB;

// Eggs (animals-19): a grown hen lays an egg every EggDays game days and holds up to EggCap
// until someone takes them. Nothing to feed or tend (D-148); a flock far from any player is
// not simulated, so the laid count catches up from the timestamp when someone comes near.
public static partial class Module
{
    // Eggs are a flat, middling quality; there is nothing to do better.
    const byte EggQuality = 50;

    [SpacetimeDB.Reducer]
    public static void CollectEggs(ReducerContext ctx, ulong animalId)
    {
        var player = RequirePlayer(ctx);
        var animal = ctx.Db.Animal.Id.Find(animalId) ?? throw new Exception("That hen has wandered off");
        if (AnimalDefs.Get((byte)animal.Species).Egg is not { } egg) throw new Exception("That animal doesn't lay eggs");
        RequireReach(RequirePos(ctx, player), animal.X, animal.Z);
        // Hens in a claim are the claim's: its members (the Claims lane's builder rights) take their eggs; out in the open, anyone may.
        if (ClaimAt(ctx, animal.X, animal.Z) is { } claim && !HasClaimRights(ctx, claim, Me(ctx)))
            throw new Exception($"These hens belong to {claim.OwnerName}'s yard. Ask them for an egg");
        var mind = ctx.Db.AnimalMind.AnimalId.Find(animalId) ?? throw new Exception("That hen has wandered off");
        if (animal.Eggs == 0) throw new Exception("No egg to take just now");
        if (animal.State == AnimalState.Run || mind.Nervous > ShearNervousLimit)
            throw new Exception("The hen is flustered. Let her settle");

        AddToInventory(ctx, Me(ctx), egg, animal.Eggs, EggQuality);
        OnProcessPerformed(ctx, player.PlayerId, Skills.Eggs, EggQuality, false);
        // Taking the eggs restarts the clock for the next one.
        ctx.Db.Animal.Id.Update(animal with { Eggs = 0 });
        ctx.Db.AnimalMind.AnimalId.Update(mind with { EggAt = ctx.Timestamp.MicrosecondsSinceUnixEpoch });
    }

    static void LayEggs(ref Animal body, ref AnimalMind mind, long now)
    {
        var def = AnimalDefs.Get((byte)body.Species);
        if (def.Egg == null || body.Growth < 100) return;
        if (mind.EggAt == 0) mind.EggAt = now;
        if (body.Eggs >= def.EggCap) return;
        long period = (long)(def.EggDays * WorldTime.DayLengthSeconds * 1_000_000);
        long due = (now - mind.EggAt) / period;
        if (due <= 0) return;
        body.Eggs = (byte)Math.Min(def.EggCap, body.Eggs + due);
        mind.EggAt = body.Eggs >= def.EggCap ? now : mind.EggAt + due * period;
    }
}
