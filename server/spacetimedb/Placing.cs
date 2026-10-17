using LifeGame.Shared;
using SpacetimeDB;

// Setting a made thing down where the player chooses (crafting-21). A craft always ends
// in the bag; the client then lets the player drag a placeable item out onto the ground
// or take the after-craft prompt. The server judges: reach, claim, water, slope, and
// whether a tree or boulder stands there.
public static partial class Module
{
    [SpacetimeDB.Reducer]
    public static void PlaceItem(ReducerContext ctx, ushort item, byte quality, uint wear, float x, float z, float yaw)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        if (!Items.IsKnown(item) || !Items.Def((Item)item).Placeable)
            throw new Exception("That can't be set down");
        if (!float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(yaw))
            throw new Exception("Not there");
        RequireReach(pos, x, z);
        RequireClaimAccess(ctx, x, z);

        RequireHeld(ctx, item);
        var stack = FindStack(ctx, Me(ctx), (Item)item, quality, wear) ?? throw new Exception("You are not carrying that");
        if (Placement.Check((Item)item, x, z, (px, pz) => Structures.GroundAt(px, pz, (i, j) => DepthAt(ctx, i, j))) is { } problem)
            throw new Exception(problem);
        int cx = World.CellOf(x), cz = World.CellOf(z);
        if ((Trees.GrowsAt(cx, cz) && ctx.Db.ChoppedTree.CellKey.Find(World.CellKey(cx, cz)) is null)
            || (Rocks.LiesAt(cx, cz) && ctx.Db.TerrainEdit.CellKey.Find(World.CellKey(cx, cz)) is null))
            throw new Exception("Something is in the way");

        RequireRoomForItem(ctx, World.ChunkOf(x), World.ChunkOf(z));
        RemoveFromStack(ctx, stack, 1);
        var placed = ctx.Db.WorldItem.Insert(new WorldItem
        {
            Id = 0,
            Item = item,
            Quality = quality,
            Wear = wear,
            Maker = stack.Maker,
            Placed = true,
            Yaw = yaw,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
        });
        OnPlayerDroppedItem(ctx, placed);
    }
}
