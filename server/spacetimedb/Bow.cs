using System;
using LifeGame.Shared;
using SpacetimeDB;

// Shooting (animals-15): the client picks the nearest huntable animal in a cone in
// front of the player, and one reducer resolves the shot at once: no projectile is
// simulated, the client just animates the arrow. Numbers are option A of
// tickets/animals-15.md until Justin picks.
public static partial class Module
{
    // What Crafting's bow and arrows are worth. Crafting implements the hook: it finds
    // the player's best bow and an arrow, takes the arrow out of the quiver, and fills
    // this in with Ready = true. Qualities are 0 to 1. Unimplemented, nobody can shoot.
    public struct BowShot
    {
        public bool Ready;
        public float BowPower, ArrowPower, BowQ, ArrowQ;
    }

    static partial void BowShotHook(ReducerContext ctx, uint playerId, ref BowShot shot);

    const float BowMaxRange = 40f;
    // Half the angle of the cone the client aims in, a little wider here for lag.
    const float BowConeRadians = 0.7f;
    // Full damage out to this far, then it falls to half at BowMaxRange.
    const float BowFullDamageRange = 15f;
    const long BowEveryMicros = 1_200_000;
    const string ShootProcess = "animal.shoot";

    [SpacetimeDB.Reducer]
    public static void ShootAnimal(ReducerContext ctx, ulong animalId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var animal = ctx.Db.Animal.Id.Find(animalId) ?? throw new Exception("It got away");
        var mind = ctx.Db.AnimalMind.AnimalId.Find(animalId) ?? throw new Exception("It got away");

        float dx = animal.X - pos.X, dz = animal.Z - pos.Z;
        float d = MathF.Sqrt(dx * dx + dz * dz);
        if (d > BowMaxRange) throw new Exception("Too far away");
        float aim = MathF.Atan2(dx, dz) - pos.Heading;
        aim = MathF.Abs(MathF.IEEERemainder(aim, MathF.Tau));
        if (d > 1f && aim > BowConeRadians) throw new Exception("Face the animal to shoot");

        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        if (now - mind.StruckAt < BowEveryMicros) return;

        var shot = new BowShot();
        BowShotHook(ctx, player.PlayerId, ref shot);
        if (!shot.Ready) throw new Exception("You need a bow and an arrow");

        var t = Traits(animal.Species);
        float speed = animal.State == AnimalState.Run ? t.Run * (0.3f + 0.7f * mind.Health / 100f)
            : animal.State == AnimalState.Walk ? t.Walk : 0f;
        float hit = BowHitChance(d, speed);
        OnProcessPerformed(ctx, player.PlayerId, ShootProcess, (byte)Math.Clamp((int)(shot.BowQ * 100f), 1, 100), false);

        if (ctx.Rng.NextDouble() >= hit)
        {
            // A miss still frightens it: the arrow lands close by.
            ctx.Db.AnimalMind.AnimalId.Update(mind with { Nervous = 1f, FleeX = dx / MathF.Max(d, 0.01f), FleeZ = dz / MathF.Max(d, 0.01f), StruckAt = now });
            ctx.Db.Animal.Id.Update(animal with { State = AnimalState.Run });
            return;
        }

        float damage = BowDamage(shot, d, ctx.Rng.NextDouble());
        HurtAnimal(ctx, animal, mind, Math.Max(1, (int)MathF.Round(damage)), pos.X, pos.Z, 0f, now);
    }

    // 0.95 close up, falling 0.012 per meter beyond 5 m to a floor of 0.3, then the
    // target's speed takes up to 65% off (a deer at a full run: about 0.55).
    public static float BowHitChance(float distance, float speed) =>
        Math.Clamp(0.95f - 0.012f * (distance - 5f), 0.3f, 0.95f) * Math.Clamp(1f - 0.07f * speed, 0.35f, 1f);

    // bowPower x arrowPower x (0.7 + 0.3 bowQ) x (0.7 + 0.3 arrowQ) x rangeFactor x rand(0.85, 1.15).
    public static float BowDamage(BowShot shot, float distance, double roll)
    {
        float range = distance <= BowFullDamageRange ? 1f
            : 1f - 0.5f * Math.Clamp((distance - BowFullDamageRange) / (BowMaxRange - BowFullDamageRange), 0f, 1f);
        return shot.BowPower * shot.ArrowPower * (0.7f + 0.3f * shot.BowQ) * (0.7f + 0.3f * shot.ArrowQ) * range * (0.85f + 0.3f * (float)roll);
    }
}
