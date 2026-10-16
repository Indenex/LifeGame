using LifeGame.Shared;
using SpacetimeDB;

// Clay and ceramics (tier 3). Shaped clay is set down in the world to dry; rain
// slows it and a nearby fire speeds it up. A dry pot is fired in a campfire, or
// picked up and carried to one. A pot fired while still damp cracks into sherds,
// which crush into grog for the next batch. A fired pot can be filled with water.
public static partial class Module
{
    // A piece of worked clay sitting in the world: drying, or in a fire.
    // Drying is stored as the moisture at a moment plus a rate (see ClayRules), so
    // the row only changes when the weather or a nearby fire does.
    [SpacetimeDB.Table(Accessor = "ClayPiece", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct ClayPiece
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public byte Form;        // ClayForm
        public byte Stage;       // ClayStage: Drying or Firing
        public byte Quality;
        [SpacetimeDB.Index.BTree]
        public uint MadeBy;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public float Moisture;
        public Timestamp MoistureAt;
        public float DryRate;
        // Set while firing.
        public ulong FireId;
        public Timestamp FireStart;
        public Timestamp FireFinish;
        public float FiredMoisture;
    }

    // Rechecks rain and fires around drying pieces.
    [SpacetimeDB.Table(Accessor = "ClayDryTimer", Scheduled = nameof(DryClay), ScheduledAt = nameof(ScheduledAt))]
    public partial struct ClayDryTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    [SpacetimeDB.Table(Accessor = "FiringTimer", Scheduled = nameof(FinishFiring), ScheduledAt = nameof(ScheduledAt))]
    public partial struct FiringTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
        public ulong PieceId;
    }

    // A line of news for one player: how a firing came out, and so on.
    [SpacetimeDB.Table(Accessor = "ClayNews", Public = true, Event = true)]
    public partial struct ClayNews
    {
        public uint PlayerId;
        public string Text;
    }

    // How hard it is raining at a spot, 0 to 1. The weather system implements
    // this; until it does, it never rains on drying clay.
    static partial void RainAt(ReducerContext ctx, float x, float z, ref float rain);

    const float DryCheckSeconds = 15f;

    // Shaping a pot or molding a brick sets it down here to dry.
    static partial void OnPlaced(ReducerContext ctx, CraftJob job, ProcessDef def)
    {
        if (ClayRules.FormFor(def.Id) is not { } form) return;
        if (ctx.Db.ClayPiece.MadeBy.Filter(job.PlayerId).Count() >= ClayRules.MaxPiecesPerPlayer)
        {
            // No room: hand the clay back rather than lose it.
            foreach (var input in def.Consumes)
                AddToInventory(ctx, job.Owner, input.Item, input.Count, Items.RawQuality);
            News(ctx, job.PlayerId, "You have too much clay drying already. The clay goes back in your bag.");
            return;
        }
        // Set down a step in front of the crafter, beside earlier pieces rather than
        // on them, and never in the flames: at a fire, round its edge instead.
        float x = job.X, z = job.Z;
        if (NearestBurningFire(ctx, job.X, job.Z, ClayRules.FireEdge) is { } fire)
        {
            var crafter = ctx.Db.PlayerPos.PlayerId.Find(job.PlayerId);
            float toward = crafter is { } c ? MathF.Atan2(c.X - fire.X, c.Z - fire.Z) : 0f;
            (x, z) = FreeSpot(ctx, fire.X, fire.Z, ClayRules.FireEdge, toward);
        }
        else if (!SpotFree(ctx, x, z))
            (x, z) = FreeSpot(ctx, job.X, job.Z, 0.45f, 0f);
        ctx.Db.ClayPiece.Insert(new ClayPiece
        {
            Id = 0,
            Form = (byte)form,
            Stage = (byte)ClayStage.Drying,
            Quality = job.Quality,
            MadeBy = job.PlayerId,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            Moisture = 1f,
            MoistureAt = ctx.Timestamp,
            DryRate = DryRateAt(ctx, x, z),
        });
        EnsureDryTimer(ctx);
    }

    // The one clay action, on a piece within reach. A dry brick or pot is picked
    // up; a pot beside a burning fire goes into it. Firing a damp pot cracks it,
    // so the client asks first and passes evenIfDamp on the second press.
    [SpacetimeDB.Reducer]
    public static void TendClay(ReducerContext ctx, ulong pieceId, bool evenIfDamp)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var piece = ctx.Db.ClayPiece.Id.Find(pieceId) ?? throw new Exception("That clay is gone");
        RequireReach(pos, piece.X, piece.Z);
        RequireClaimAccess(ctx, piece.X, piece.Z);
        if (piece.Stage != (byte)ClayStage.Drying)
            throw new Exception("It is in the fire");

        var form = (ClayForm)piece.Form;
        float moisture = MoistureNow(ctx, piece);
        // A dry pot the fire can't take (too little fuel, or a full ring) is picked up
        // instead, so a pot shaped by a fire can still be carried away (bugfix-18).
        if (form != ClayForm.Brick && !ClayRules.KilnOnly(form) && NearestBurningFire(ctx, piece.X, piece.Z, ClayRules.FireReach) is { } fire
            && (ClayRules.TooWetToFire(moisture) || FireCanTakePot(ctx, fire)))
        {
            if (ClayRules.TooWetToFire(moisture) && !evenIfDamp)
                throw new Exception($"The pot is still {ClayRules.Describe(moisture)}. Fired now, it will crack.");
            StartFiring(ctx, piece, fire, moisture);
            return;
        }
        if (ClayRules.TooWetToFire(moisture))
            throw new Exception($"Still {ClayRules.Describe(moisture)}. It turns pale when it is dry.");

        ctx.Db.ClayPiece.Id.Delete(piece.Id);
        if (form == ClayForm.Brick)
            AddToInventory(ctx, ctx.Sender, Item.Brick, 1, Items.RawQuality);
        else
            AddToInventory(ctx, ctx.Sender, ClayRules.UnfiredItem(form), 1, piece.Quality);
    }

    // Sets every unfired pot you carry into a fire within reach, as many as fit.
    [SpacetimeDB.Reducer]
    public static void FirePots(ReducerContext ctx, ulong fireId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var fire = ctx.Db.Fire.Id.Find(fireId) ?? throw new Exception("That fire is out");
        RequireReach(pos, fire.X, fire.Z);
        RequireClaimAccess(ctx, fire.X, fire.Z);
        RequireFuelForFiring(ctx, fire);

        var pots = ctx.Db.InventoryItem.Owner.Filter(ctx.Sender)
            .Where(i => Items.IsKnown(i.Item) && ClayRules.FormOfUnfired((Item)i.Item) is not null)
            .ToList();
        if (pots.Count == 0)
            throw new Exception("You have no dry pots to fire");
        // A storage jar is too big for an open fire; it waits for a kiln.
        pots = pots.Where(i => !ClayRules.KilnOnly(ClayRules.FormOfUnfired((Item)i.Item)!.Value)).ToList();
        if (pots.Count == 0)
            throw new Exception("A storage jar is too big for a campfire. Fire it in a kiln.");
        if (ctx.Db.ClayPiece.MadeBy.Filter(player.PlayerId).Count() >= ClayRules.MaxPiecesPerPlayer)
            throw new Exception("You have too much clay out already");

        int placed = 0;
        foreach (var stack in pots)
        {
            uint used = 0;
            while (used < stack.Count && FreeFireSlot(ctx, fire) is not null)
            {
                var piece = ctx.Db.ClayPiece.Insert(new ClayPiece
                {
                    Id = 0,
                    Form = (byte)ClayRules.FormOfUnfired((Item)stack.Item)!.Value,
                    Stage = (byte)ClayStage.Drying,
                    Quality = stack.Quality,
                    MadeBy = player.PlayerId,
                    X = fire.X,
                    Z = fire.Z,
                    ChunkX = fire.ChunkX,
                    ChunkZ = fire.ChunkZ,
                    Moisture = 0f,
                    MoistureAt = ctx.Timestamp,
                });
                StartFiring(ctx, piece, fire, 0f);
                used++;
                placed++;
            }
            if (used > 0) RemoveFromStack(ctx, stack, used);
        }
        if (placed == 0)
            throw new Exception("That fire is full of pots already");
    }

    // Fills every empty fired pot you carry, standing at the water's edge.
    [SpacetimeDB.Reducer]
    public static void FillPots(ReducerContext ctx)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        if (!NearWater(ctx, pos.X, pos.Z))
            throw new Exception("Stand at the water's edge to fill a pot");

        var empty = ctx.Db.InventoryItem.Owner.Filter(ctx.Sender)
            .Where(i => Items.IsKnown(i.Item) && ClayRules.FilledItem((Item)i.Item) is not null)
            .ToList();
        if (empty.Count == 0)
            throw new Exception("You have no fired pot to fill");
        foreach (var stack in empty)
        {
            RemoveFromStack(ctx, stack, stack.Count);
            AddToInventory(ctx, ctx.Sender, ClayRules.FilledItem((Item)stack.Item)!.Value, stack.Count, stack.Quality);
            OnProcessPerformed(ctx, player.PlayerId, ClayRules.FillProcess, stack.Quality, false);
        }
    }

    // Rechecks the drying rate of every drying piece, and turns dry bricks into
    // bricks on the ground.
    [SpacetimeDB.Reducer]
    public static void DryClay(ReducerContext ctx, ClayDryTimer timer)
    {
        int drying = 0;
        foreach (var piece in ctx.Db.ClayPiece.Iter().ToList())
        {
            if (piece.Stage != (byte)ClayStage.Drying) continue;
            drying++;
            float moisture = MoistureNow(ctx, piece);
            if (piece.Form == (byte)ClayForm.Brick && moisture <= 0f)
            {
                ctx.Db.ClayPiece.Id.Delete(piece.Id);
                ctx.Db.WorldItem.Insert(new WorldItem
                {
                    Id = 0, Item = (ushort)Item.Brick, Quality = Items.RawQuality,
                    X = piece.X, Z = piece.Z, ChunkX = piece.ChunkX, ChunkZ = piece.ChunkZ,
                });
                continue;
            }
            if (moisture <= 0f && piece.Moisture <= 0f) continue;
            float rate = DryRateAt(ctx, piece.X, piece.Z);
            if (MathF.Abs(rate - piece.DryRate) > 1e-6f)
                ctx.Db.ClayPiece.Id.Update(piece with { Moisture = moisture, MoistureAt = ctx.Timestamp, DryRate = rate });
        }
        // Nothing left drying: stop ticking until something is shaped again.
        if (drying == 0)
            foreach (var t in ctx.Db.ClayDryTimer.Iter().ToList())
                ctx.Db.ClayDryTimer.ScheduledId.Delete(t.ScheduledId);
    }

    // A firing is done: the pot comes out whole, or in pieces.
    [SpacetimeDB.Reducer]
    public static void FinishFiring(ReducerContext ctx, FiringTimer timer)
    {
        if (ctx.Db.ClayPiece.Id.Find(timer.PieceId) is not { } piece) return;
        ctx.Db.ClayPiece.Id.Delete(piece.Id);
        var form = (ClayForm)piece.Form;

        bool steam = ClayRules.TooWetToFire(piece.FiredMoisture);
        bool cracked = steam || ctx.Rng.NextDouble() < ClayRules.CrackChance(FireKind.Pit, piece.Quality);
        OnProcessPerformed(ctx, piece.MadeBy, ClayRules.PitFireProcess, piece.Quality, cracked);

        if (cracked)
        {
            for (int i = 0; i < ClayRules.SherdsPerCrack; i++)
                DropNear(ctx, piece, Item.Potsherd, Items.RawQuality, i);
            News(ctx, piece.MadeBy, steam
                ? $"Crack! Your {ClayRules.FormName(form)} was still damp and burst in the fire. Crush the sherds into grog."
                : $"Your {ClayRules.FormName(form)} cracked in the fire. Better tempered, better shaped pots crack less.");
        }
        else
        {
            DropNear(ctx, piece, ClayRules.FiredItem(form), piece.Quality, 0);
            News(ctx, piece.MadeBy, $"Your {ClayRules.FormName(form)} is fired, and whole. {{use}} picks it up.");
        }
    }

    // A spot on a circle round a center, as close to the given direction as possible
    // without sitting on another piece of clay.
    static (float, float) FreeSpot(ReducerContext ctx, float cx, float cz, float radius, float angle)
    {
        for (int i = 0; i < 14; i++)
        {
            float a = angle + (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 0.45f;
            float x = cx + MathF.Sin(a) * radius, z = cz + MathF.Cos(a) * radius;
            if (SpotFree(ctx, x, z)) return (x, z);
        }
        return (cx + MathF.Sin(angle) * radius, cz + MathF.Cos(angle) * radius);
    }

    static bool SpotFree(ReducerContext ctx, float x, float z)
    {
        const float Gap = 0.4f;
        int chunkX = World.ChunkOf(x), chunkZ = World.ChunkOf(z);
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var p in ctx.Db.ClayPiece.ByChunk.Filter((chunkX + dx, chunkZ + dz)))
                    if ((p.X - x) * (p.X - x) + (p.Z - z) * (p.Z - z) < Gap * Gap) return false;
        return true;
    }

    static void StartFiring(ReducerContext ctx, ClayPiece piece, Fire fire, float moisture)
    {
        RequireFuelForFiring(ctx, fire);
        var slot = FreeFireSlot(ctx, fire) ?? throw new Exception("That fire is full of pots already");
        float angle = slot * MathF.Tau / ClayRules.MaxPotsPerFire + 0.4f;
        float x = fire.X + MathF.Cos(angle) * ClayRules.FireRing, z = fire.Z + MathF.Sin(angle) * ClayRules.FireRing;
        var finish = ctx.Timestamp + new TimeDuration((long)(ClayRules.PitFireSeconds * 1_000_000));
        ctx.Db.ClayPiece.Id.Update(piece with
        {
            Stage = (byte)ClayStage.Firing,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            Moisture = moisture,
            MoistureAt = ctx.Timestamp,
            DryRate = 0f,
            FireId = fire.Id,
            FireStart = ctx.Timestamp,
            FireFinish = finish,
            FiredMoisture = moisture,
        });
        ctx.Db.FiringTimer.Insert(new FiringTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(finish), PieceId = piece.Id });
    }

    static bool FireCanTakePot(ReducerContext ctx, Fire fire) => ClayRules.FireCanTakePot(
        (fire.FuelUntil.MicrosecondsSinceUnixEpoch - ctx.Timestamp.MicrosecondsSinceUnixEpoch) / 1_000_000f,
        FreeFireSlot(ctx, fire) is null ? ClayRules.MaxPotsPerFire : 0);

    // A pit firing needs a fire with fuel for the whole firing.
    static void RequireFuelForFiring(ReducerContext ctx, Fire fire)
    {
        float left = (fire.FuelUntil.MicrosecondsSinceUnixEpoch - ctx.Timestamp.MicrosecondsSinceUnixEpoch) / 1_000_000f;
        if (left < ClayRules.PitFireSeconds)
        {
            int sticks = ClayRules.SticksForFiring(left, Items.Def(Item.Stick).FuelSeconds);
            throw new Exception($"Feed the fire first: {sticks} more {(sticks == 1 ? "stick" : "sticks")} to last the {ClayRules.PitFireSeconds / 60f:0} minute firing");
        }
    }

    // The first free place in a fire's ring of pots, or null when it is full.
    static int? FreeFireSlot(ReducerContext ctx, Fire fire)
    {
        var taken = new HashSet<int>();
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var p in ctx.Db.ClayPiece.ByChunk.Filter((fire.ChunkX + dx, fire.ChunkZ + dz)))
                    if (p.Stage == (byte)ClayStage.Firing && p.FireId == fire.Id)
                    {
                        float angle = MathF.Atan2(p.Z - fire.Z, p.X - fire.X) - 0.4f;
                        int slot = (int)MathF.Round(angle / (MathF.Tau / ClayRules.MaxPotsPerFire));
                        taken.Add(((slot % ClayRules.MaxPotsPerFire) + ClayRules.MaxPotsPerFire) % ClayRules.MaxPotsPerFire);
                    }
        for (int i = 0; i < ClayRules.MaxPotsPerFire; i++)
            if (!taken.Contains(i)) return i;
        return null;
    }

    static Fire? NearestBurningFire(ReducerContext ctx, float x, float z, float reach)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        Fire? best = null;
        float bestDist = reach;
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var fire in ctx.Db.Fire.ByChunk.Filter((cx + dx, cz + dz)))
                {
                    if (fire.FuelUntil.MicrosecondsSinceUnixEpoch <= now) continue;
                    float d = MathF.Sqrt((fire.X - x) * (fire.X - x) + (fire.Z - z) * (fire.Z - z));
                    if (d <= bestDist) { best = fire; bestDist = d; }
                }
        return best;
    }

    static float MoistureNow(ReducerContext ctx, ClayPiece piece) =>
        ClayRules.MoistureAt(piece.Moisture, piece.DryRate, SecondsBetween(piece.MoistureAt, ctx.Timestamp));

    static float DryRateAt(ReducerContext ctx, float x, float z)
    {
        float rain = 0f;
        RainAt(ctx, x, z, ref rain);
        return ClayRules.DryRate(rain, WarmthAt(ctx, x, z));
    }

    // True when any ground within reach is under water, dug holes included.
    static bool NearWater(ReducerContext ctx, float x, float z)
    {
        int r = (int)MathF.Ceiling(World.Reach);
        int cx = World.CellOf(x), cz = World.CellOf(z);
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                int gx = cx + dx, gz = cz + dz;
                if ((gx - x) * (gx - x) + (gz - z) * (gz - z) > World.Reach * World.Reach) continue;
                short depth = ctx.Db.TerrainEdit.CellKey.Find(World.CellKey(gx, gz))?.Depth ?? 0;
                if (World.Height(gx, gz, depth) < World.WaterLevel) return true;
            }
        return false;
    }

    // Drops an item on the ground where a piece was, nudged so several don't stack.
    static void DropNear(ReducerContext ctx, ClayPiece piece, Item item, byte quality, int index)
    {
        float x = piece.X + index * 0.18f, z = piece.Z - index * 0.12f;
        ctx.Db.WorldItem.Insert(new WorldItem
        {
            Id = 0, Item = (ushort)item, Quality = quality,
            X = x, Z = z, ChunkX = World.ChunkOf(x), ChunkZ = World.ChunkOf(z),
        });
    }

    // "{use}" in the text becomes the player's Use key on their screen.
    static void News(ReducerContext ctx, uint playerId, string text) =>
        ctx.Db.ClayNews.Insert(new ClayNews { PlayerId = playerId, Text = text });

    static void EnsureDryTimer(ReducerContext ctx)
    {
        if (ctx.Db.ClayDryTimer.Count > 0) return;
        ctx.Db.ClayDryTimer.Insert(new ClayDryTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(DryCheckSeconds)) });
    }
}
