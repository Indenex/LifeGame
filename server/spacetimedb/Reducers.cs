using LifeGame.Shared;
using SpacetimeDB;

public static partial class Module
{
    static readonly Item[] GroundItems = { Item.Stick, Item.Stick, Item.Stone, Item.Fiber };

    [SpacetimeDB.Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        // A mis-appended structure kind fails the first publish (CI load test) instead of breaking a world.
        var enumKinds = Enum.GetValues<StructureKind>().Select(k => (int)k).OrderBy(k => k);
        if (!enumKinds.SequenceEqual(Structures.Kinds))
            throw new InvalidOperationException(
                "StructureKind enum and the StructureDefs in shared/structures/ disagree: enum "
                + string.Join(",", enumKinds) + " defs " + string.Join(",", Structures.Kinds));
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
        InitChat(ctx);
        InitClaims(ctx);
        EnsureWorldClock(ctx);
        EnsureServerInfo(ctx);
    }

    [SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
    public static void PlayerConnected(ReducerContext ctx)
    {
        EnsureWorldClock(ctx);
        EnsureServerInfo(ctx);
        EnsureAnimals(ctx);
        BackfillCharacter(ctx, ctx.Sender);
        // A login with no character yet gets its first, named for now; it picks a real name
        // and look at the creator. A login with characters waits at the menu, unless its
        // character is still in the world from a connection that was lost.
        if (!ctx.Db.Character.Login.Filter(ctx.Sender).Any())
        {
            var first = MakeCharacter(ctx, ctx.Sender, ctx.Sender, $"Wanderer-{ctx.Rng.Next(1000, 10000)}");
            SetActive(ctx, ctx.Sender, first.Identity);
            EnterWorld(ctx, first);
        }
        else if (ActivePlayer(ctx, ctx.Sender) is { Online: true } resumed)
            EnterWorld(ctx, resumed);
    }

    [SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)]
    public static void PlayerDisconnected(ReducerContext ctx)
    {
        if (ActivePlayer(ctx, ctx.Sender) is { } player) LeaveWorld(ctx, player);
    }

    [SpacetimeDB.Reducer]
    public static void SetName(ReducerContext ctx, string name)
    {
        // The picker at first join and /name later (ui-31). The rules are shared (Names), so
        // the picker's field says the same thing before asking; the reason is the error.
        name = Names.Normalize(name);
        if (Names.Problem(name) is { } problem) throw new Exception(problem);
        var player = RequirePlayer(ctx);
        RequirePace(ctx, player.PlayerId, Pace.ChangeName, 1.0, "One moment before you change your name again");
        RequireNameFree(ctx, name, player.PlayerId);
        ctx.Db.Player.Identity.Update(player with { Name = name });
        if (ctx.Db.Character.Key.Find(player.Identity) is { } character) ctx.Db.Character.Key.Update(character with { Name = name });
    }

    // How much walking a player has banked (World.MoveAllowance). Private.
    [SpacetimeDB.Table(Accessor = "MoveBank")]
    public partial struct MoveBank
    {
        [PrimaryKey]
        public uint PlayerId;
        public float Meters;
    }

    // Clients predict movement locally and report where they are about ten times a second.
    // The server clamps each step to what the player could have walked since the last one,
    // then holds it until the next tick so all movement goes out in one batch.
    [SpacetimeDB.Reducer]
    public static void Move(ReducerContext ctx, float x, float z, float heading)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(heading))
            throw new Exception("Invalid position");
        var player = RequirePlayerUnmetered(ctx);
        var state = ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) ?? throw new Exception("No state");
        var pending = ctx.Db.MoveInput.PlayerId.Find(player.PlayerId);
        var pos = RequirePos(ctx, player);
        float fromX = pending?.X ?? pos.X, fromZ = pending?.Z ?? pos.Z;

        // An admin who is flying (/fly, admin-1) is exempt from the walking pace; nobody else is.
        bool flyingNow = IsFlyingAdmin(ctx, player.PlayerId);
        if (!flyingNow)
        {
            float speed = World.MaxSpeed * CarryWalkFactor(ctx, Me(ctx));
            var bank = ctx.Db.MoveBank.PlayerId.Find(player.PlayerId);
            float allowed = World.MoveAllowance(bank?.Meters ?? float.MaxValue, speed, SecondsBetween(state.LastMoveAt, ctx.Timestamp));
            float dx = x - fromX, dz = z - fromZ;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            if (dist > allowed)
            {
                if (dist - allowed > SuspectMoveExcess) NoteSuspicion(ctx, player.PlayerId, Suspect.MoveOverBudget);
                x = fromX + dx / dist * allowed;
                z = fromZ + dz / dist * allowed;
                dist = allowed;
            }
            var left = new MoveBank { PlayerId = player.PlayerId, Meters = allowed - dist };
            if (bank is null) ctx.Db.MoveBank.Insert(left);
            else ctx.Db.MoveBank.PlayerId.Update(left);
        }

        // Walls, shut gates, fences, solid things and trunks stop a step (PlayerCollision.cs).
        if (!flyingNow && PlayerStepBlocked(ctx, player.PlayerId, fromX, fromZ, x, z))
        {
            // Honest players bump into things all the time; only a step that lands deep inside one counts.
            if (!StandingFree(ctx, x, z, PlayerBody * 0.5f)) NoteSuspicion(ctx, player.PlayerId, Suspect.MoveInsideObstacle);
            x = fromX;
            z = fromZ;
        }

        // Stepping off gets a sitting or lying player up (Resting.cs).
        if ((x - fromX) * (x - fromX) + (z - fromZ) * (z - fromZ) > 0.02f * 0.02f) StopResting(ctx, player.PlayerId);

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
        RequireServer(ctx);
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
            if (ctx.Db.Player.PlayerId.Find(input.PlayerId) is { } mover) CheckJobReach(ctx, mover, input.X, input.Z);
        }
    }

    // Dig with bare hands at a terrain height point. Clay ground yields a lump of clay.
    [SpacetimeDB.Reducer]
    public static void Dig(ReducerContext ctx, int cellX, int cellZ)
    {
        var player = RequirePlayer(ctx);
        RequireReach(RequirePos(ctx, player), cellX * World.CellSize, cellZ * World.CellSize);
        RequireClaimAccess(ctx, cellX * World.CellSize, cellZ * World.CellSize);
        // The equipped digging tool reaches one level deeper than hands (D-041), and a spade works faster than a stick (Equip.cs).
        var stickTool = EquippedTool(ctx, ItemTags.Digging);
        float toolSpeed = stickTool is { } held ? Items.Def((Item)held.Item).DigSpeed : 1f;
        // Skilled diggers work faster.
        var state = RequireCooldown(ctx, player, (long)(World.DigCooldownMicros / toolSpeed * DurationScale(ctx, player.PlayerId, Skills.Dig)));

        long key = World.CellKey(cellX, cellZ);
        var edit = ctx.Db.TerrainEdit.CellKey.Find(key);
        short depth = edit?.Depth ?? 0;
        bool stick = stickTool is not null;
        if (depth >= World.MaxDigDepth + (stick ? 1 : 0))
            throw new Exception(stick ? "Too deep even for a digging stick" : "Too deep to dig by hand");

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

        // The extra level a digging stick reaches holds finer clay (D-041).
        if (World.IsClay(cellX, cellZ))
            GiveDugClay(ctx, player, depth);

        if (stickTool is { } digger) WearTool(ctx, digger);
        PerformedProcess(ctx, player.PlayerId, Skills.Dig, cellX * World.CellSize, cellZ * World.CellSize);
        NoteAct(ctx, player.PlayerId, WorkMotion.Dig, stickTool is { } dug ? (Item)dug.Item : 0);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // Lets a lane refuse a pick-up by throwing (Traps.cs: a snare holding a catch).
    static partial void PickUpGuard(ReducerContext ctx, WorldItem item);

    [SpacetimeDB.Reducer]
    public static void PickUp(ReducerContext ctx, ulong itemId)
    {
        var player = RequirePlayer(ctx);
        var item = ctx.Db.WorldItem.Id.Find(itemId) ?? throw new Exception("That item is gone");
        RequireReach(RequirePos(ctx, player), item.X, item.Z);
        RequireClaimAccess(ctx, item.X, item.Z);
        PickUpGuard(ctx, item);

        ctx.Db.WorldItem.Id.Delete(itemId);
        AddToInventory(ctx, Me(ctx), (Item)item.Item, 1, item.Quality, item.Wear, item.Maker);
        PerformedProcess(ctx, player.PlayerId, Skills.Forage, item.X, item.Z);
        NoteAct(ctx, player.PlayerId, WorkMotion.PickUp, (Item)item.Item);
    }

    [SpacetimeDB.Reducer]
    public static void Drop(ReducerContext ctx, ushort item, byte quality)
    {
        var pos = RequirePos(ctx, RequirePlayer(ctx));
        RequireRoomForItem(ctx, pos.ChunkX, pos.ChunkZ);
        var stack = FindStack(ctx, Me(ctx), (Item)item, quality) ?? throw new Exception("You are not carrying that");
        RemoveFromStack(ctx, stack, 1);

        var dropped = ctx.Db.WorldItem.Insert(new WorldItem
        {
            Id = 0,
            Item = item,
            Quality = quality,
            Wear = stack.Wear,
            Maker = stack.Maker,
            X = pos.X,
            Z = pos.Z,
            ChunkX = pos.ChunkX,
            ChunkZ = pos.ChunkZ,
        });
        OnPlayerDroppedItem(ctx, dropped);
    }

    // Tops up loose sticks, stones and fiber in the seeded area.
    [SpacetimeDB.Reducer]
    public static void RespawnItems(ReducerContext ctx, RespawnTimer timer)
    {
        RequireServer(ctx);
        SweepPresence(ctx);
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
        // Only on dry ground: a stick dropped in a pond or stream bed can't be reached,
        // and it would still count toward the chunk's target (bugfix-21). A chunk that
        // is all water gets nothing this round.
        float x = 0, z = 0;
        bool found = false;
        for (int tries = 0; tries < 8 && !found; tries++)
        {
            x = (chunkX + (float)ctx.Rng.NextDouble()) * span;
            z = (chunkZ + (float)ctx.Rng.NextDouble()) * span;
            found = IsDry(x, z);
        }
        if (!found) return;
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

    // Scheduled reducers are public, so a client could call them with a made-up timer row.
    // Only the server's own scheduler may.
    static void RequireServer(ReducerContext ctx)
    {
        if (!ctx.SenderAuth.IsInternal) throw new Exception("Only the server runs this");
    }

    // The character the caller is playing; refused at the character menu (not in the world).
    static Player RequirePlayer(ReducerContext ctx)
    {
        var p = RequirePlayerUnmetered(ctx);
        SpendCall(ctx, p.PlayerId);
        return p;
    }

    // Move (its own distance budget) and Heartbeat (a timer) skip the call bucket (RateLimit.cs).
    static Player RequirePlayerUnmetered(ReducerContext ctx) =>
        ctx.Db.Player.Identity.Find(Me(ctx)) is { Online: true } p ? p : throw new Exception("No player for this connection");

    static PlayerPos RequirePos(ReducerContext ctx, Player player) =>
        ctx.Db.PlayerPos.PlayerId.Find(player.PlayerId) ?? throw new Exception("No position");

    static void RequireReach(PlayerPos pos, float x, float z)
    {
        float dx = x - pos.X, dz = z - pos.Z;
        if (dx * dx + dz * dz > World.Reach * World.Reach)
            throw new Exception("Out of reach");
    }

    static PlayerState RequireCooldown(ReducerContext ctx, Player player, long cooldownMicros = World.DigCooldownMicros)
    {
        var state = ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) ?? throw new Exception("No state");
        long elapsed = ctx.Timestamp.MicrosecondsSinceUnixEpoch - state.LastActionAt.MicrosecondsSinceUnixEpoch;
        if (elapsed < cooldownMicros)
            throw new Exception("Still working");
        return state;
    }

    static float SecondsBetween(Timestamp a, Timestamp b) =>
        (b.MicrosecondsSinceUnixEpoch - a.MicrosecondsSinceUnixEpoch) / 1_000_000f;

    // The stack of this item and quality to move or use. Tools of the same quality can
    // differ in wear (shared/Durability.cs); the most worn goes first, so a player
    // never loses their best tool by dropping, storing or trading "a stone axe".
    static InventoryItem? FindStack(ReducerContext ctx, Identity owner, Item item, byte quality)
    {
        InventoryItem? found = null;
        foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((owner, (ushort)item)))
            if (row.Quality == quality && (found is null || Durability.Used(row.Wear) > Durability.Used(found.Value.Wear)))
                found = row;
        return found;
    }

    static InventoryItem? FindStack(ReducerContext ctx, Identity owner, Item item, byte quality, uint wear)
    {
        foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((owner, (ushort)item)))
            if (row.Quality == quality && row.Wear == wear)
                return row;
        return null;
    }

    // How many of an item and quality are carried, whatever their wear.
    static uint CarriedCount(ReducerContext ctx, Identity owner, Item item, byte quality)
    {
        uint n = 0;
        foreach (var row in ctx.Db.InventoryItem.ByOwnerItem.Filter((owner, (ushort)item)))
            if (row.Quality == quality) n += row.Count;
        return n;
    }

    // Moves carried items from one player to another, the most worn first, wear and all.
    static void MoveCarried(ReducerContext ctx, Identity from, Identity to, Item item, byte quality, uint count)
    {
        while (count > 0 && FindStack(ctx, from, item, quality) is { } stack)
        {
            uint n = Math.Min(count, stack.Count);
            RemoveFromStack(ctx, stack, n);
            AddToInventory(ctx, to, item, n, quality, stack.Wear, stack.Maker);
            count -= n;
        }
    }

    // Into the bag: onto a matching bag stack, never onto the tools belt (Inventory.cs, ui-18).
    static void AddToInventory(ReducerContext ctx, Identity owner, Item item, uint count, byte quality, uint wear = 0, uint maker = 0) =>
        AddToBag(ctx, owner, item, count, quality, wear, maker);

    static void RemoveFromStack(ReducerContext ctx, InventoryItem stack, uint count)
    {
        if (stack.Count <= count)
            ctx.Db.InventoryItem.Id.Delete(stack.Id);
        else
            ctx.Db.InventoryItem.Id.Update(stack with { Count = stack.Count - count });
        ClearHandIfGone(ctx, stack.Owner, stack.Item);
    }
}
