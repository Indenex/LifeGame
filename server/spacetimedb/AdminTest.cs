using LifeGame.Shared;
using SpacetimeDB;

// Test hooks for the check bots (animals-29), admins only and gated exactly like AdminGive
// and AdminDie. They put an animal exactly where a test needs it. The animal belongs to no
// flock, so the animal tick leaves it standing where it was placed.
public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void AdminSpawnAnimal(ReducerContext ctx, byte species, float x, float z, byte eggs)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can do that");
        RequirePlayer(ctx);
        if (species >= AnimalDefs.All.Length) throw new Exception("No such animal");
        if (!float.IsFinite(x) || !float.IsFinite(z)) throw new Exception("Invalid position");
        var def = AnimalDefs.Get(species);
        var animal = ctx.Db.Animal.Insert(new Animal
        {
            Id = 0,
            Species = (AnimalSpecies)species,
            X = x,
            Z = z,
            Heading = 0f,
            State = AnimalState.Graze,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            Fleece = 100,
            Growth = 100,
            Eggs = Math.Min(eggs, def.EggCap),
        });
        ctx.Db.AnimalMind.Insert(new AnimalMind
        {
            AnimalId = animal.Id,
            FlockId = 0,
            Leader = true,
            TargetX = x,
            TargetZ = z,
            Health = 100,
        });
        Log.Info($"admin_spawn_animal: {Me(ctx)} placed a {def.Name} at ({x:F1}, {z:F1})");
    }
}
