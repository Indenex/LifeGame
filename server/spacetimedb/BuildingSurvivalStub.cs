using SpacetimeDB;

// Temporary: this branch is built on crafting-core and ceramics, which don't have
// Survival yet. Survival.cs declares this hook on main; delete this file when
// main is merged in.
public static partial class Module
{
    static partial void ShelterAtHook(ReducerContext ctx, float x, float z, ref float rainCover, ref float warmth);
}
