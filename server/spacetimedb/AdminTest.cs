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

    // A dry hide hanging free-standing at a spot, as if the player madeBy had hung it (0 for nobody).
    [SpacetimeDB.Reducer]
    public static void AdminHangHide(ReducerContext ctx, float x, float z, uint madeBy)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can do that");
        RequirePlayer(ctx);
        if (!float.IsFinite(x) || !float.IsFinite(z)) throw new Exception("Invalid position");
        ctx.Db.HideRack.Insert(new HideRack
        {
            Id = 0,
            MadeBy = madeBy,
            X = x,
            Z = z,
            Heading = 0f,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            Quality = 50,
            Moisture = 0f,
            MoistureAt = ctx.Timestamp,
            DryRate = 0f,
            StationId = 0,
            Peg = 0,
        });
        Log.Info($"admin_hang_hide: {Me(ctx)} hung a dry hide at ({x:F1}, {z:F1})");
    }
}
