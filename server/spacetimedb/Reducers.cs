using LifeGame.Shared;
using SpacetimeDB;

public static partial class Module
{
    static readonly Item[] GroundItems = { Item.Stick, Item.Stick, Item.Stone, Item.Fiber };

    [SpacetimeDB.Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        int r = World.SpawnRadiusChunks;
        for (int cx = -r; cx < r; cx++)
            for (int cz = -r; cz < r; cz++)
                for (int i = 0; i < World.ItemsPerChunkTarget; i++)
                    SpawnGroundItem(ctx, cx, cz);

        ctx.Db.RespawnTimer.Insert(new RespawnTimer
        {
            ScheduledId = 0,
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(30)),
        });
        ctx.Db.MoveTickTimer.Insert(new MoveTickTimer
        {
            ScheduledId = 0,
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(World.MoveTickMillis)),
        });
        EnsureWorldClock(ctx);
    }

    [SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
    public static void PlayerConnected(ReducerContext ctx)
    {
        EnsureWorldClock(ctx);
        if (ctx.Db.Player.Identity.Find(ctx.Sender) is { } existing)
        {
            ctx.Db.Player.Identity.Update(existing with { Online = true });
            if (ctx.Db.PlayerState.PlayerId.Find(existing.PlayerId) is { } state)
                ctx.Db.PlayerState.PlayerId.Update(state with { LastMoveAt = ctx.Timestamp });
            SurvivalConnected(ctx, existing with { Online = true });
            return;
        }

        var player = ctx.Db.Player.Insert(new Player
        {
            Identity = ctx.Sender,
            PlayerId = 0,
            Name = $"Wanderer-{ctx.Rng.Next(1000, 10000)}",
            Online = true,
        });

        // New players wake up naked near the middle of the seeded area.
        float x = (float)(ctx.Rng.NextDouble() * 16 - 8);
        float z = (float)(ctx.Rng.NextDouble() * 16 - 8);
        ctx.Db.PlayerPos.Insert(new PlayerPos
        {
            PlayerId = player.PlayerId,
            X = x,
            Z = z,
            Heading = 0,
            ChunkX = (short)World.ChunkOf(x),
            ChunkZ = (short)World.ChunkOf(z),
        });
        ctx.Db.PlayerState.Insert(new PlayerState
        {
            PlayerId = player.PlayerId,
            LastMoveAt = ctx.Timestamp,
            LastActionAt = ctx.Timestamp,
        });
        SurvivalConnected(ctx, player);
    }

    [SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)]
    public static void PlayerDisconnected(ReducerContext ctx)
    {
        if (ctx.Db.Player.Identity.Find(ctx.Sender) is { } player)
        {
            ctx.Db.Player.Identity.Update(player with { Online = false });
            SurvivalDisconnected(ctx, player);
        }
    }

    [SpacetimeDB.Reducer]
    public static void SetName(ReducerContext ctx, string name)
    {
        name = name.Trim();
        if (name.Length < 1 || name.Length > 24)
            throw new Exception("Name must be 1 to 24 characters");
        var player = RequirePlayer(ctx);
        ctx.Db.Player.Identity.Update(player with { Name = name });
    }

    // Clients predict movement locally and report where they are about ten times a second.
    // The server clamps each step to what the player could have walked since the last one,
    // then holds it until the next tick so all movement goes out in one batch.
    [SpacetimeDB.Reducer]
    public static void Move(ReducerContext ctx, float x, float z, float heading)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(heading))
            throw new Exception("Invalid position");
        var player = RequirePlayer(ctx);
        var state = ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) ?? throw new Exception("No state");
        var pending = ctx.Db.MoveInput.PlayerId.Find(player.PlayerId);
        var pos = RequirePos(ctx, player);
        float fromX = pending?.X ?? pos.X, fromZ = pending?.Z ?? pos.Z;

        float dt = Math.Min(SecondsBetween(state.LastMoveAt, ctx.Timestamp), 1f);
        float allowed = World.MaxSpeed * dt + World.MoveSlack;
        float dx = x - fromX, dz = z - fromZ;
        float dist = MathF.Sqrt(dx * dx + dz * dz);
        if (dist > allowed)
        {
            x = fromX + dx / dist * allowed;
            z = fromZ + dz / dist * allowed;
        }

        var input = new MoveInput { PlayerId = player.PlayerId, X = x, Z = z, Heading = heading };
        if (pending is not null)
            ctx.Db.MoveInput.PlayerId.Update(input);
        else
            ctx.Db.MoveInput.Insert(input);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastMoveAt = ctx.Timestamp });
    }

    [SpacetimeDB.Reducer]
    public static void ApplyMoves(ReducerContext ctx, MoveTickTimer timer)
    {
        foreach (var input in ctx.Db.MoveInput.Iter().ToList())
        {
            ctx.Db.PlayerPos.PlayerId.Update(new PlayerPos
            {
                PlayerId = input.PlayerId,
                X = input.X,
                Z = input.Z,
                Heading = input.Heading,
                ChunkX = (short)World.ChunkOf(input.X),
                ChunkZ = (short)World.ChunkOf(input.Z),
            });
            ctx.Db.MoveInput.PlayerId.Delete(input.PlayerId);
        }
    }

    // Dig with bare hands at a terrain height point. Clay ground yields a lump of clay.
    [SpacetimeDB.Reducer]
    public static void Dig(ReducerContext ctx, int cellX, int cellZ)
    {
        var player = RequirePlayer(ctx);
        RequireReach(RequirePos(ctx, player), cellX * World.CellSize, cellZ * World.CellSize);
        var state = RequireCooldown(ctx, player);

        long key = World.CellKey(cellX, cellZ);
        var edit = ctx.Db.TerrainEdit.CellKey.Find(key);
        short depth = edit?.Depth ?? 0;
        if (depth >= World.MaxDigDepth)
            throw new Exception("Too deep to dig by hand");

        if (edit is { } existing)
            ctx.Db.TerrainEdit.CellKey.Update(existing with { Depth = (short)(depth + 1) });
        else
            ctx.Db.TerrainEdit.Insert(new TerrainEdit
            {
                CellKey = key,
                CellX = cellX,
                CellZ = cellZ,
                ChunkX = World.ChunkOfCell(cellX),
                ChunkZ = World.ChunkOfCell(cellZ),
                Depth = 1,
            });

        // A digging stick loosens twice the clay a bare hand does.
        if (World.IsClay(cellX, cellZ))
            AddToInventory(ctx, ctx.Sender, Item.Clay, BestTool(ctx, ItemTags.Digging) is null ? 1u : 2u, Items.RawQuality);

        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    [SpacetimeDB.Reducer]
    public static void PickUp(ReducerContext ctx, ulong itemId)
    {
        var player = RequirePlayer(ctx);
        var item = ctx.Db.WorldItem.Id.Find(itemId) ?? throw new Exception("That item is gone");
        var pos = RequirePos(ctx, player);
        RequireReach(pos, item.X, item.Z);

        ctx.Db.WorldItem.Id.Delete(itemId);
        AddToInventory(ctx, ctx.Sender, (Item)item.Item, 1, item.Quality);
        // With a basket you scoop up every loose one of the same kind within reach.
        if (BestTool(ctx, ItemTags.Container) is not null)
            foreach (var other in WorldItemsNear(ctx, pos.X, pos.Z, World.Reach).Where(o => o.Item == item.Item).ToList())
            {
                ctx.Db.WorldItem.Id.Delete(other.Id);
                AddToInventory(ctx, ctx.Sender, (Item)other.Item, 1, other.Quality);
            }
    }

    static IEnumerable<WorldItem> WorldItemsNear(ReducerContext ctx, float x, float z, float reach)
    {
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var w in ctx.Db.WorldItem.ByChunk.Filter((cx + dx, cz + dz)))
                    if ((w.X - x) * (w.X - x) + (w.Z - z) * (w.Z - z) <= reach * reach)
                        yield return w;
    }

    [SpacetimeDB.Reducer]
    public static void Drop(ReducerContext ctx, ushort item, byte quality)
    {
        var pos = RequirePos(ctx, RequirePlayer(ctx));
        var stack = FindStack(ctx, ctx.Sender, (Item)item, quality) ?? throw new Exception("You are not carrying that");
        RemoveFromStack(ctx, stack, 1);

        ctx.Db.WorldItem.Insert(new WorldItem
        {
            Id = 0,
            Item = item,
            Quality = quality,
            X = pos.X,
            Z = pos.Z,
            ChunkX = pos.ChunkX,
            ChunkZ = pos.ChunkZ,
        });
    }

    // Tops up loose sticks, stones and fiber in the seeded area.
    [SpacetimeDB.Reducer]
    public static void RespawnItems(ReducerContext ctx, RespawnTimer timer)
    {
        int r = World.SpawnRadiusChunks;
        for (int cx = -r; cx < r; cx++)
            for (int cz = -r; cz < r; cz++)
            {
                int count = ctx.Db.WorldItem.ByChunk.Filter((cx, cz)).Count();
                if (count < World.ItemsPerChunkTarget)
                    SpawnGroundItem(ctx, cx, cz);
            }
    }

    static void SpawnGroundItem(ReducerContext ctx, int chunkX, int chunkZ)
    {
        float span = World.ChunkSize * World.CellSize;
        float x = (chunkX + (float)ctx.Rng.NextDouble()) * span;
        float z = (chunkZ + (float)ctx.Rng.NextDouble()) * span;
        ctx.Db.WorldItem.Insert(new WorldItem
        {
            Id = 0,
            Item = (ushort)GroundItems[ctx.Rng.Next(0, GroundItems.Length)],
            Quality = Items.RawQuality,
            X = x,
            Z = z,
            ChunkX = chunkX,
            ChunkZ = chunkZ,
        });
    }

    static Player RequirePlayer(ReducerContext ctx) =>
        ctx.Db.Player.Identity.Find(ctx.Sender) ?? throw new Exception("No player for this connection");

    static PlayerPos RequirePos(ReducerContext ctx, Player player) =>
        ctx.Db.PlayerPos.PlayerId.Find(player.PlayerId) ?? throw new Exception("No position");

    static void RequireReach(PlayerPos pos, float x, float z)
    {
        float dx = x - pos.X, dz = z - pos.Z;
        if (dx * dx + dz * dz > World.Reach * World.Reach)
            throw new Exception("Out of reach");
    }

    static PlayerState RequireCooldown(ReducerContext ctx, Player player)
    {
        var state = ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) ?? throw new Exception("No state");
        long elapsed = ctx.Timestamp.MicrosecondsSinceUnixEpoch - state.LastActionAt.MicrosecondsSinceUnixEpoch;
        if (elapsed < World.DigCooldownMicros)
            throw new Exception("Still working");
        return state;
    }

    static float SecondsBetween(Timestamp a, Timestamp b) =>
        (b.MicrosecondsSinceUnixEpoch - a.MicrosecondsSinceUnixEpoch) / 1_000_000f;

    static InventoryItem? FindStack(ReducerContext ctx, Identity owner, Item item, byte quality)
    {
        foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((owner, (ushort)item)))
            if (row.Quality == quality)
                return row;
        return null;
    }

    static void AddToInventory(ReducerContext ctx, Identity owner, Item item, uint count, byte quality)
    {
        if (FindStack(ctx, owner, item, quality) is { } stack)
            ctx.Db.InventoryItem.Id.Update(stack with { Count = stack.Count + count });
        else
            ctx.Db.InventoryItem.Insert(new InventoryItem { Id = 0, Owner = owner, Item = (ushort)item, Quality = quality, Count = count });
    }

    static void RemoveFromStack(ReducerContext ctx, InventoryItem stack, uint count)
    {
        if (stack.Count <= count)
            ctx.Db.InventoryItem.Id.Delete(stack.Id);
        else
            ctx.Db.InventoryItem.Id.Update(stack with { Count = stack.Count - count });
    }
}
