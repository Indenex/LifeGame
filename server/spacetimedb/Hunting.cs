using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// Hunting (D-039): a player with a spear or a blade can strike an animal. A struck
// sheep bolts, slower the more it is hurt, so a kill is a short chase. A dead sheep
// lies down where it fell; butchering it with a blade gives meat and a hide. A flock
// that has lost animals gains a lamb about once an in-game day. Kept calm and quiet
// on purpose (D-036): no blood, the sheep just lies down.
public static partial class Module
{
    const string HuntProcess = "animal.kill";
    const string ButcherProcess = "animal.butcher";
    // A lamb is born this grown and reaches 100 after LambGrowMicros.
    const byte LambGrowth = 35;
    const long LambGrowMicros = 2L * WorldTime.DayLengthSeconds * 1_000_000;
    const long BirthEveryMicros = 1L * WorldTime.DayLengthSeconds * 1_000_000;
    // An unbutchered carcass is gone after this.
    const long CarcassKeepMicros = 1L * WorldTime.DayLengthSeconds * 1_000_000;
    // One strike per animal per this long (about a swing), so clicks don't stack.
    const long StrikeEveryMicros = 1_000_000;
    // A struck animal leaps this far away at once, so the hunter has to give chase.
    const float StartleLeap = 1.4f;

    // A dead animal lying where it fell, until butchered or it fades.
    [SpacetimeDB.Table(Accessor = "AnimalCarcass", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct AnimalCarcass
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        // The animal it was, so the client keeps its fleece color.
        public ulong AnimalId;
        public AnimalSpecies Species;
        public float X;
        public float Z;
        public float Heading;
        public int ChunkX;
        public int ChunkZ;
        public byte Fleece;
        public byte Growth;
        public long DiedAt;
    }

    [SpacetimeDB.Reducer]
    public static void StrikeAnimal(ReducerContext ctx, ulong animalId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var animal = ctx.Db.Animal.Id.Find(animalId) ?? throw new Exception("It got away");
        var mind = ctx.Db.AnimalMind.AnimalId.Find(animalId) ?? throw new Exception("It got away");

        var spear = BestTool(ctx, ItemTags.Piercing);
        var blade = BestTool(ctx, ItemTags.Cutting);
        if (spear == null && blade == null) throw new Exception("You need a spear or a blade to hunt with");

        // A spear reaches a little further; a running target gets some slack for
        // where the hunter saw it a moment ago.
        float reach = World.Reach + (spear != null ? 0.7f : 0.2f) + (animal.State == AnimalState.Run ? 1f : 0f);
        if (AnimalDist(pos.X, pos.Z, animal.X, animal.Z) > reach) throw new Exception("Too far away");

        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        if (now - mind.StruckAt < StrikeEveryMicros) return;

        // A spear takes two good strikes, a stone blade about three.
        int damage = spear is { } s ? 50 + s.Quality / 4 : 30 + blade!.Value.Quality / 8;
        damage = (int)(damage * Traits(animal.Species).StrikeScale);
        OnProcessPerformed(ctx, player.PlayerId, HuntProcess, (spear ?? blade)!.Value.Quality, false);
        if (damage >= mind.Health)
        {
            KillAnimal(ctx, animal, mind, now);
            return;
        }

        float dx = animal.X - pos.X, dz = animal.Z - pos.Z;
        float d = MathF.Max(0.01f, MathF.Sqrt(dx * dx + dz * dz));
        ctx.Db.AnimalMind.AnimalId.Update(mind with
        {
            Health = (byte)(mind.Health - damage),
            StruckAt = now,
            Nervous = 1f,
            FleeX = dx / d,
            FleeZ = dz / d,
        });
        float lx = animal.X + dx / d * StartleLeap, lz = animal.Z + dz / d * StartleLeap;
        if (!AnimalCanStand(lx, lz) || AnimalStepBlocked(ctx, animal.X, animal.Z, lx, lz)) (lx, lz) = (animal.X, animal.Z);
        ctx.Db.Animal.Id.Update(animal with
        {
            State = AnimalState.Run,
            X = lx,
            Z = lz,
            Heading = MathF.Atan2(dx, dz),
            ChunkX = World.ChunkOf(lx),
            ChunkZ = World.ChunkOf(lz),
        });
    }

    static void KillAnimal(ReducerContext ctx, Animal animal, AnimalMind mind, long now)
    {
        ctx.Db.AnimalCarcass.Insert(new AnimalCarcass
        {
            Id = 0,
            AnimalId = animal.Id,
            Species = animal.Species,
            X = animal.X,
            Z = animal.Z,
            Heading = animal.Heading,
            ChunkX = animal.ChunkX,
            ChunkZ = animal.ChunkZ,
            Fleece = animal.Fleece,
            Growth = animal.Growth,
            DiedAt = now,
        });
        ctx.Db.Animal.Id.Delete(animal.Id);
        ctx.Db.AnimalMind.AnimalId.Delete(animal.Id);

        // The rest of the flock takes fright, and someone else leads.
        // A grown sheep takes the lead if there is one; a lamb only if it's all that's left.
        var rest = ctx.Db.AnimalMind.FlockId.Filter(mind.FlockId)
            .OrderBy(m => (ctx.Db.Animal.Id.Find(m.AnimalId)?.Growth ?? 0) >= 100 ? 0 : 1).ThenBy(m => m.AnimalId).ToList();
        for (int i = 0; i < rest.Count; i++)
        {
            var mate = rest[i];
            if (mind.Leader && i == 0) mate.Leader = true;
            if (ctx.Db.Animal.Id.Find(mate.AnimalId) is { } body && AnimalDist(body.X, body.Z, animal.X, animal.Z) < 10f)
            {
                float dx = body.X - animal.X, dz = body.Z - animal.Z;
                float d = MathF.Max(0.01f, MathF.Sqrt(dx * dx + dz * dz));
                mate.Nervous = MathF.Max(mate.Nervous, 0.9f);
                mate.FleeX = dx / d;
                mate.FleeZ = dz / d;
            }
            if (!mate.Equals(rest[i])) ctx.Db.AnimalMind.AnimalId.Update(mate);
        }

        // The clock for the next lamb starts from the first loss.
        if (ctx.Db.Flock.FlockId.Find(mind.FlockId) is { } flock && now - flock.LastBirthAt > BirthEveryMicros)
            ctx.Db.Flock.FlockId.Update(flock with { LastBirthAt = now });
    }

    [SpacetimeDB.Reducer]
    public static void ButcherCarcass(ReducerContext ctx, ulong carcassId)
    {
        var player = RequirePlayer(ctx);
        var carcass = ctx.Db.AnimalCarcass.Id.Find(carcassId) ?? throw new Exception("There's nothing left there");
        RequireReach(RequirePos(ctx, player), carcass.X, carcass.Z);
        var tool = BestTool(ctx, ItemTags.Cutting) ?? throw new Exception("You need a blade to butcher with");

        byte cap = 100;
        QualityCapFor(ctx, player.PlayerId, ButcherProcess, ref cap);
        byte quality = Math.Min(tool.Quality, cap);
        OnProcessPerformed(ctx, player.PlayerId, ButcherProcess, quality, false);

        // A grown sheep gives a few cuts and a hide; a lamb gives one small cut. A deer
        // gives venison, its hide and an antler; a fawn one small cut.
        bool grown = carcass.Growth >= 80;
        bool deer = carcass.Species == AnimalSpecies.Deer;
        var meat = deer ? Item.RawVenison : Item.RawMutton;
        for (int i = 0; i < (grown ? (deer ? 2 : 3) : 1); i++) DropNear(ctx, carcass, meat, quality);
        if (grown) DropNear(ctx, carcass, deer ? Item.DeerHide : Item.SheepHide, quality);
        if (grown && deer) DropNear(ctx, carcass, Item.Antler, quality);
        ctx.Db.AnimalCarcass.Id.Delete(carcassId);
    }

    static void DropNear(ReducerContext ctx, AnimalCarcass carcass, Item item, byte quality)
    {
        float angle = (float)(ctx.Rng.NextDouble() * MathF.Tau);
        float r = 0.3f + (float)ctx.Rng.NextDouble() * 0.5f;
        float x = carcass.X + MathF.Sin(angle) * r, z = carcass.Z + MathF.Cos(angle) * r;
        ctx.Db.WorldItem.Insert(new WorldItem
        {
            Id = 0,
            Item = (ushort)item,
            Quality = quality,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
        });
    }

    static void ClearOldCarcasses(ReducerContext ctx, long now)
    {
        foreach (var c in ctx.Db.AnimalCarcass.Iter().Where(c => now - c.DiedAt > CarcassKeepMicros).ToList())
            ctx.Db.AnimalCarcass.Id.Delete(c.Id);
    }

    // A flock below its size gains lambs in spring only (D-135), at most two a
    // spring, born beside one of the adults. Lambs need two grown animals; a flock
    // down to one or none instead gets a stray adult wandering in about once a day,
    // whatever the season, so the meadows never empty for good.
    // Returns true when the flock changed and should think next tick.
    static bool FlockRegrow(ReducerContext ctx, Flock flock, System.Collections.Generic.List<FlockMember> members, long now)
    {
        if (flock.Size == 0)
        {
            // Flocks from before hunting existed: their current count is their size.
            ctx.Db.Flock.FlockId.Update(flock with { Size = (byte)Math.Max(members.Count, 2) });
            return true;
        }
        if (members.Count >= flock.Size) return false;

        var adults = members.Where(m => m.Body.Growth >= 100).ToList();
        if (adults.Count >= 2)
        {
            if (SeasonNow(ctx).Season != Season.Spring || now - flock.LastBirthAt < LambGapMicros(ctx)) return false;
            var mother = adults[ctx.Rng.Next(0, adults.Count)].Body;
            SpawnAnimal(ctx, flock, mother.X + 0.6f, mother.Z + 0.4f, false, LambGrowth, now, mother.Id);
        }
        else
        {
            if (now - flock.LastBirthAt < BirthEveryMicros) return false;
            SpawnAnimal(ctx, flock, flock.HomeX, flock.HomeZ, !members.Any(m => m.Mind.Leader), 100, now);
        }
        ctx.Db.Flock.FlockId.Update(flock with { LastBirthAt = now });
        return true;
    }

    // Two lambs fit in one spring: the gap is a little under half a season.
    static long LambGapMicros(ReducerContext ctx) =>
        ctx.Db.SeasonClock.Id.Find(0) is { } clock ? (long)(YearMicros(ctx, clock) / 4.0 * 0.45) : BirthEveryMicros / 2;

    // Lambs grow in steps of 5 (so they aren't resent every tick). A wound heals a
    // point a tick, starting 10 s after the last strike if the animal isn't running.
    static void AnimalGrowAndHeal(ref Animal body, ref AnimalMind mind, long now)
    {
        if (body.Growth < 100 && mind.BornAt > 0)
        {
            long grown = LambGrowth + (now - mind.BornAt) * (100 - LambGrowth) / LambGrowMicros;
            byte stepped = (byte)(grown >= 100 ? 100 : grown / 5 * 5);
            if (stepped > body.Growth) body.Growth = stepped;
        }
        if (mind.Health < 100 && body.State != AnimalState.Run && now - mind.StruckAt > 10_000_000)
            mind.Health = (byte)Math.Min(100, mind.Health + 1);
    }
}
