using LifeGame.Shared;
using SpacetimeDB;

// Things you do with a finished building: hang, swing and lock a hut's door, and
// keep items in a storage basket. Anyone can open an unlocked door (D-046); a
// locked one opens only for whoever hung it or, inside a claim, the claim's
// members, and they lock and unlock it (D-048). Anyone can put into
// a basket; taking out is open too, except inside a claim, where only those with
// access to the claim can (Claims' CheckClaimAccess decides).
public static partial class Module
{
    // A finished clay hut's hurdle door, once someone has hung it.
    [SpacetimeDB.Table(Accessor = "StructureDoor", Public = true)]
    public partial struct StructureDoor
    {
        [PrimaryKey]
        public ulong StructureId;
        public bool Closed;
        public bool Locked;
        // Whoever hung it; they can always lock and unlock it.
        public Identity HungBy;
    }

    // One stack inside a storage basket.
    [SpacetimeDB.Table(Accessor = "BasketItem", Public = true)]
    public partial struct BasketItem
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong StructureId;
        public ushort Item;
        public byte Quality;
        public uint Count;
    }

    // Weaves a door into a finished clay hut's doorway from carried sticks and fiber.
    [SpacetimeDB.Reducer]
    public static void HangDoor(ReducerContext ctx, ulong structureId)
    {
        var (s, player) = RequireFinishedInReach(ctx, structureId);
        if (!Structures.HasDoorway((int)s.Kind))
            throw new Exception("That has no doorway");
        if (ctx.Db.StructureDoor.StructureId.Find(structureId) is not null)
            throw new Exception("It already has a door");
        if (CountOf(ctx, Item.Stick) < Structures.DoorSticks || CountOf(ctx, Item.Fiber) < Structures.DoorFiber)
            throw new Exception($"A door takes {Structures.DoorSticks} sticks and {Structures.DoorFiber} fiber");
        var state = RequireCooldown(ctx, player);
        float materials = (Take(ctx, Item.Stick, Structures.DoorSticks) + Take(ctx, Item.Fiber, Structures.DoorFiber))
            / (Structures.DoorSticks + Structures.DoorFiber);
        // Hanging a door practices building like a stage does.
        byte cap = 100;
        QualityCapFor(ctx, player.PlayerId, Structures.DoorProcess, ref cap);
        OnProcessPerformed(ctx, player.PlayerId, Structures.DoorProcess, Processes.Quality(materials, Processes.DefaultScore, cap), false);
        ctx.Db.StructureDoor.Insert(new StructureDoor { StructureId = structureId, Closed = true, Locked = false, HungBy = ctx.Sender });
        TouchStructure(ctx, structureId);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // Opens or shuts a hut's door. Anyone can, unless it is locked.
    [SpacetimeDB.Reducer]
    public static void ToggleDoor(ReducerContext ctx, ulong structureId)
    {
        var (s, _) = RequireFinishedInReach(ctx, structureId);
        var door = ctx.Db.StructureDoor.StructureId.Find(structureId) ?? throw new Exception("There's no door");
        if (door.Locked && !CanLock(ctx, s, door))
            throw new Exception(Structures.IsGate((int)s.Kind) ? "The gate is barred" : "The door is barred");
        // Opening a locked door unbars it; it stays unbarred until someone locks it again.
        ctx.Db.StructureDoor.StructureId.Update(door with { Closed = !door.Closed, Locked = false });
        TouchStructure(ctx, structureId);
    }

    // Bars or unbars a hut's door: whoever hung it, or a member of the claim it stands in.
    // Barring shuts it.
    [SpacetimeDB.Reducer]
    public static void ToggleLock(ReducerContext ctx, ulong structureId)
    {
        var (s, _) = RequireFinishedInReach(ctx, structureId);
        var door = ctx.Db.StructureDoor.StructureId.Find(structureId) ?? throw new Exception("There's no door");
        if (!CanLock(ctx, s, door))
            throw new Exception("Only whoever hung the door, or the claim's members, can bar it");
        ctx.Db.StructureDoor.StructureId.Update(door with { Locked = !door.Locked, Closed = door.Closed || !door.Locked });
        TouchStructure(ctx, structureId);
    }

    static bool CanLock(ReducerContext ctx, Structure s, StructureDoor door)
    {
        if (door.HungBy == ctx.Sender) return true;
        return ClaimAt(ctx, s.X, s.Z) is { } claim && HasClaimRights(ctx, claim, ctx.Sender);
    }

    // Puts carried items of one kind and quality into a basket.
    [SpacetimeDB.Reducer]
    public static void BasketPut(ReducerContext ctx, ulong structureId, ushort item, byte quality, uint count)
    {
        var (s, _) = RequireFinishedInReach(ctx, structureId);
        if (!Structures.IsStore((int)s.Kind))
            throw new Exception("You can't store things in that");
        var stack = FindStack(ctx, ctx.Sender, (Item)item, quality) ?? throw new Exception("You are not carrying that");
        count = Math.Min(count, stack.Count);
        if (count == 0) return;

        var inside = ctx.Db.BasketItem.StructureId.Filter(structureId).ToList();
        var same = inside.Where(b => b.Item == item && b.Quality == quality).Select(b => (BasketItem?)b).FirstOrDefault();
        if (same is { } match)
            ctx.Db.BasketItem.Id.Update(match with { Count = match.Count + count });
        else if (inside.Count >= Structures.Shape((int)s.Kind).StoreStacks)
            throw new Exception($"The {Structures.Shape((int)s.Kind).Name.ToLowerInvariant()} is full");
        else
            ctx.Db.BasketItem.Insert(new BasketItem { Id = 0, StructureId = structureId, Item = item, Quality = quality, Count = count });
        RemoveFromStack(ctx, stack, count);
        TouchStructure(ctx, structureId);
    }

    // Takes items out of a basket.
    [SpacetimeDB.Reducer]
    public static void BasketTake(ReducerContext ctx, ulong basketItemId, uint count)
    {
        var row = ctx.Db.BasketItem.Id.Find(basketItemId) ?? throw new Exception("That's gone");
        var (s, _) = RequireFinishedInReach(ctx, row.StructureId);
        CheckClaimAccess(ctx, s.X, s.Z);
        count = Math.Min(count, row.Count);
        if (count == 0) return;
        if (count == row.Count) ctx.Db.BasketItem.Id.Delete(row.Id);
        else ctx.Db.BasketItem.Id.Update(row with { Count = row.Count - count });
        AddToInventory(ctx, ctx.Sender, (Item)row.Item, count, row.Quality);
        TouchStructure(ctx, s.Id);
    }

    // A burning fire within a brick house's walls: the whole room is warm.
    static bool FireInside(ReducerContext ctx, Structure s)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        for (int cx = World.ChunkOf(s.X - 3f); cx <= World.ChunkOf(s.X + 3f); cx++)
        for (int cz = World.ChunkOf(s.Z - 3f); cz <= World.ChunkOf(s.Z + 3f); cz++)
        foreach (var fire in ctx.Db.Fire.ByChunk.Filter((cx, cz)))
        {
            if (fire.FuelUntil.MicrosecondsSinceUnixEpoch <= now) continue;
            var (lx, lz) = Structures.ToLocal(s.X, s.Z, s.Yaw, fire.X, fire.Z);
            if (MathF.Abs(lx) < Structures.HouseWallX && MathF.Abs(lz) < Structures.HouseWallZ) return true;
        }
        return false;
    }

    static bool DoorClosed(ReducerContext ctx, ulong structureId) =>
        ctx.Db.StructureDoor.StructureId.Find(structureId) is { Closed: true };

    // A basket's contents end up on the ground when it is taken down.
    static void SpillBasket(ReducerContext ctx, Structure s)
    {
        foreach (var row in ctx.Db.BasketItem.StructureId.Filter(s.Id).ToList())
        {
            for (uint i = 0; i < row.Count; i++)
            {
                float a = (float)(ctx.Rng.NextDouble() * MathF.Tau), r = 0.3f + (float)ctx.Rng.NextDouble() * 0.6f;
                float x = s.X + MathF.Cos(a) * r, z = s.Z + MathF.Sin(a) * r;
                var dropped = ctx.Db.WorldItem.Insert(new WorldItem
                {
                    Id = 0,
                    Item = row.Item,
                    Quality = row.Quality,
                    X = x,
                    Z = z,
                    ChunkX = World.ChunkOf(x),
                    ChunkZ = World.ChunkOf(z),
                });
                TouchGroundItem(ctx, dropped);
            }
            ctx.Db.BasketItem.Id.Delete(row.Id);
        }
    }

    static (Structure, Player) RequireFinishedInReach(ReducerContext ctx, ulong structureId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var s = ctx.Db.Structure.Id.Find(structureId) ?? throw new Exception("That building is gone");
        if (!s.Complete)
            throw new Exception("It isn't finished yet");
        if (!Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, pos.X, pos.Z, Structures.WorkReach))
            throw new Exception("Out of reach");
        return (s, player);
    }
}
