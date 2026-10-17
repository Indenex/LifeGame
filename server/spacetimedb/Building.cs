using LifeGame.Shared;
using SpacetimeDB;

// Each value is the kind's fixed id in shared/structures/ (its StructureDef); never renumber or reuse one.
[SpacetimeDB.Type]
public enum StructureKind
{
    LeanTo,
    ClayHut,
    ClaimMarker,
    Kiln,
    Basket,
    BrickHouse,
    StorageJar,
    Fence,
    Gate,
    CraftingTable,
    Hearth,
    // Animals stations (D-098, animals-11); numbered by merge order: always at the end.
    SpindleStation,
    DryingRack,
    PottersBench,
    LogCabin,
    RepairTable,
    TiledHouse,
}

// One material a structure's current stage still needs.
[SpacetimeDB.Type]
public partial struct MaterialNeed
{
    public ushort Item;
    public uint Have;
    public uint Need;
}

// Shelter a spot gives, from 0 (open sky) to 1.
public readonly record struct Shelter(float RainCover, float Warmth, ulong StructureId)
{
    public bool Sheltered => RainCover > 0f;
}

// Building works like a slow craft done in place: lay out a footprint, then anyone
// nearby adds materials one at a time, stage by stage, until it stands.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Structure", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct Structure
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public StructureKind Kind;
        public float X;
        public float Z;
        // Radians about +Y; the footprint's +Z side is its open front or door.
        public float Yaw;
        public int ChunkX;
        public int ChunkZ;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        // Index into the shape's stage list; equals the stage count once finished.
        public byte Stage;
        public List<MaterialNeed> Needs;
        public bool Complete;
        // How many different people have added to it.
        public byte Builders;
        public Timestamp PlacedAt;
    }

    // Quality bookkeeping, kept beside Structure so its columns stay as they were
    // (no world wipe). Public so clients can show how well something is built.
    [SpacetimeDB.Table(Accessor = "StructureProgress", Public = true)]
    public partial struct StructureProgress
    {
        [PrimaryKey]
        public ulong StructureId;
        // 1 to 100: the average quality of its finished stages so far.
        public byte Quality;
        // The stage being built: summed quality of what went in, and who helped.
        public uint StageQualitySum;
        public uint StageItems;
        public List<uint> StageHelpers;
    }

    // Who added what to which structure. Private: credit for later (journal, claims).
    [SpacetimeDB.Table(Accessor = "StructureBuilder")]
    public partial struct StructureBuilder
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong StructureId;
        public Identity Builder;
        public uint Added;
    }

    // What each stage takes, by kind and stage: shared/Structures.cs, so the client can show it.
    static (Item Item, uint Count)[][] StageCosts(int kind) => Structures.Costs(kind);

    // Lays out a footprint. The structure exists from here on, at stage 0 with nothing added.
    [SpacetimeDB.Reducer]
    public static void PlaceStructure(ReducerContext ctx, StructureKind kind, float x, float z, float yaw)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z) || !float.IsFinite(yaw))
            throw new Exception("Invalid position");
        int k = (int)kind;
        if (!Structures.Exists(k))
            throw new Exception("Unknown structure");
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        float dx = x - pos.X, dz = z - pos.Z;
        if (dx * dx + dz * dz > Structures.PlaceRange * Structures.PlaceRange)
            throw new Exception("Too far away to lay out");

        int open = ctx.Db.Structure.Owner.Filter(ctx.Sender).Count(s => !s.Complete);
        if (open >= Structures.MaxOpenSites)
            throw new Exception("Finish one of your building sites first");

        foreach (var other in StructuresNear(ctx, x, z))
            if (Structures.TooClose(k, x, z, (int)other.Kind, other.X, other.Z))
                throw new Exception("Too close to another building");

        if (Structures.IsIndoor(k) && HomeAround(ctx, x, z, Structures.HearthWallGap) is null)
            throw new Exception("A hearth goes inside a finished hut or house");
        CheckClaimOnly(ctx, k, x, z);
        CheckClaimAccess(ctx, x, z);
        CheckStructureSite(ctx, kind, x, z);
        CheckStationSite(ctx, kind, x, z);
        yaw = MathF.IEEERemainder(yaw, MathF.Tau);
        if (Structures.GroundProblem(k, x, z, yaw, (px, pz) => Structures.GroundAt(px, pz, (i, j) => DepthAt(ctx, i, j))) is { } problem)
            throw new Exception(problem);

        var placed = ctx.Db.Structure.Insert(new Structure
        {
            Id = 0,
            Kind = kind,
            X = x,
            Z = z,
            Yaw = yaw,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            Owner = ctx.Sender,
            Stage = 0,
            Needs = NeedsFor(k, 0),
            Complete = false,
            Builders = 0,
            PlacedAt = ctx.Timestamp,
        });
        TouchStructure(ctx, placed.Id);
    }

    // Only basic survival buildings may stand outside a claim (building-13, Justin): anything else
    // has to be inside a claim the builder belongs to, so cities grow where players cooperate.
    static void CheckClaimOnly(ReducerContext ctx, int kind, float x, float z)
    {
        if (Structures.IsWild(kind)) return;
        string what = Structures.Shape(kind).Name.ToLowerInvariant();
        if (ClaimAt(ctx, x, z) is not { } claim)
            throw new Exception($"A {what} has to stand inside your claim. Raise a claim marker first (build menu), away from the waking clearing");
        if (!HasClaimRights(ctx, claim, ctx.Sender))
            throw new Exception($"This land is claimed by {claim.OwnerName}. Ask them to add you, or build a {what} on your own claim");
    }

    // Adds one carried item the current stage still needs. Anyone in reach can help.
    [SpacetimeDB.Reducer]
    public static void Build(ReducerContext ctx, ulong structureId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var s = ctx.Db.Structure.Id.Find(structureId) ?? throw new Exception("That building is gone");
        if (s.Complete)
            throw new Exception("Already finished");
        if (!Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, pos.X, pos.Z, Structures.WorkReach))
            throw new Exception("Out of reach");
        var state = RequireCooldown(ctx, player);

        int slot = -1;
        for (int i = 0; i < s.Needs.Count; i++)
            if (s.Needs[i].Have < s.Needs[i].Need && CountOf(ctx, (Item)s.Needs[i].Item) > 0)
            {
                slot = i;
                break;
            }
        if (slot < 0)
            throw new Exception("Needs " + string.Join(", ", s.Needs
                .Where(n => n.Have < n.Need)
                .Select(n => $"{n.Need - n.Have} {Items.Def((Item)n.Item).Count(n.Need - n.Have).ToLowerInvariant()}")));

        var need = s.Needs[slot];
        float quality = Take(ctx, (Item)need.Item, 1);
        var needs = new List<MaterialNeed>(s.Needs);
        needs[slot] = need with { Have = need.Have + 1 };
        var p = ProgressOf(ctx, s.Id);
        var helpers = new List<uint>(p.StageHelpers);
        if (!helpers.Contains(player.PlayerId)) helpers.Add(player.PlayerId);
        p = p with
        {
            StageQualitySum = p.StageQualitySum + (uint)quality,
            StageItems = p.StageItems + 1,
            StageHelpers = helpers,
        };
        s = s with { Needs = needs };
        if (needs.All(n => n.Have >= n.Need))
            (s, p) = FinishStage(ctx, s, p);
        ctx.Db.StructureProgress.StructureId.Update(p);

        if (RecordBuilder(ctx, structureId))
            s = s with { Builders = (byte)Math.Min(255, s.Builders + 1) };
        ctx.Db.Structure.Id.Update(s);
        TouchStructure(ctx, s.Id);
        if (s.Complete) FinishGate(ctx, s);
        if (s.Complete) OnStructureComplete(ctx, s);
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }

    // The owner takes it down. Everything that went into it is left on the ground around it.
    [SpacetimeDB.Reducer]
    public static void Dismantle(ReducerContext ctx, ulong structureId)
    {
        var s = ctx.Db.Structure.Id.Find(structureId) ?? throw new Exception("That building is gone");
        bool allowed = s.Owner == ctx.Sender;
        if (!allowed) CanClearStructure(ctx, s, ref allowed);
        if (!allowed)
            throw new Exception("Only whoever laid it out, or the land's owner, can take it down");
        CheckClaimAccess(ctx, s.X, s.Z);
        var pos = RequirePos(ctx, RequirePlayer(ctx));
        if (!Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, pos.X, pos.Z, Structures.WorkReach))
            throw new Exception("Out of reach");

        var refund = new List<Item>();
        var costs = StageCosts((int)s.Kind);
        for (int stage = 0; stage < s.Stage && stage < costs.Length; stage++)
            foreach (var (item, count) in costs[stage])
                for (uint i = 0; i < count; i++) refund.Add(item);
        foreach (var need in s.Needs)
            for (uint i = 0; i < need.Have; i++) refund.Add((Item)need.Item);
        byte built = ctx.Db.StructureProgress.StructureId.Find(s.Id)?.Quality ?? 0;
        byte refundQuality = built > 0 ? built : Items.RawQuality;

        var shape = Structures.Shape((int)s.Kind);
        float spread = (shape.Round ? shape.Radius : MathF.Max(shape.HalfWidth, shape.HalfDepth)) * 0.8f;
        foreach (var item in refund)
        {
            float a = (float)(ctx.Rng.NextDouble() * MathF.Tau), r = (float)ctx.Rng.NextDouble() * spread;
            float ix = s.X + MathF.Cos(a) * r, iz = s.Z + MathF.Sin(a) * r;
            var dropped = ctx.Db.WorldItem.Insert(new WorldItem
            {
                Id = 0,
                Item = (ushort)item,
                Quality = refundQuality,
                X = ix,
                Z = iz,
                ChunkX = World.ChunkOf(ix),
                ChunkZ = World.ChunkOf(iz),
            });
            TouchGroundItem(ctx, dropped);
        }

        SpillBasket(ctx, s);
        RemoveStructure(ctx, s);
    }

    // Deletes a structure and everything hanging off it (builders, door, weathering clock).
    static void RemoveStructure(ReducerContext ctx, Structure s)
    {
        foreach (var row in ctx.Db.StructureBuilder.StructureId.Filter(s.Id).ToList())
            ctx.Db.StructureBuilder.Id.Delete(row.Id);
        ctx.Db.StructureProgress.StructureId.Delete(s.Id);
        ctx.Db.StructureDoor.StructureId.Delete(s.Id);
        ctx.Db.StructureTouch.StructureId.Delete(s.Id);
        DropMats(ctx, s);
        ctx.Db.Structure.Id.Delete(s.Id);
        OnStructureRemoved(ctx, s);
        CeramicsStructureRemoved(ctx, s);
        AnimalsStructureRemoved(ctx, s);
    }

    // Ceramics implements this to hand back what was loaded in a kiln that is taken down.
    // Partial methods allow one implementation each, so every lane that needs it gets its own hook.
    static partial void CeramicsStructureRemoved(ReducerContext ctx, Structure s);
    // Animals implements this to drop the hides hanging on a drying rack that is taken down.
    static partial void AnimalsStructureRemoved(ReducerContext ctx, Structure s);

    // Claims implements this to refuse placing or taking down buildings inside a
    // claim the sender has no access to (throw to refuse). Adding materials is
    // never gated: anyone can help build.
    static partial void CheckClaimAccess(ReducerContext ctx, float x, float z);

    // Claims implements these for the claim marker: extra placement rules (throw to
    // refuse), raising the claim when the marker is finished, and dropping it when
    // the marker is taken down.
    static partial void CheckStructureSite(ReducerContext ctx, StructureKind kind, float x, float z);
    static partial void OnStructureComplete(ReducerContext ctx, Structure s);
    static partial void OnStructureRemoved(ReducerContext ctx, Structure s);
    static partial void CanClearStructure(ReducerContext ctx, Structure s, ref bool allowed);

    // For other systems (survival needs, weather): how sheltered a spot is. Only
    // finished structures shelter, and only when you are well inside.
    public static Shelter ShelterAt(ReducerContext ctx, float x, float z)
    {
        var best = new Shelter(0f, 0f, 0);
        foreach (var s in StructuresNear(ctx, x, z))
        {
            if (!s.Complete || !Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, x, z, -0.2f)) continue;
            var shape = Structures.Shape((int)s.Kind);
            float warmth = Structures.HasDoorway((int)s.Kind) && DoorClosed(ctx, s.Id) ? MathF.Max(shape.Warmth, shape.ShutWarmth) : shape.Warmth;
            if (shape.Room && FireInside(ctx, s)) warmth = 1f;
            if (s.Kind == StructureKind.ClayHut && HearthFireIn(ctx, s)) warmth = 1f;
            if (shape.RainCover > best.RainCover || warmth > best.Warmth)
                best = new Shelter(MathF.Max(best.RainCover, shape.RainCover), MathF.Max(best.Warmth, warmth), s.Id);
        }
        return best;
    }

    // Survival reads shelter through this hook.
    static partial void ShelterAtHook(ReducerContext ctx, float x, float z, ref float rainCover, ref float warmth)
    {
        var shelter = ShelterAt(ctx, x, z);
        rainCover = MathF.Max(rainCover, shelter.RainCover);
        warmth = MathF.Max(warmth, shelter.Warmth);
    }

    public static Shelter ShelterFor(ReducerContext ctx, uint playerId) =>
        ctx.Db.PlayerPos.PlayerId.Find(playerId) is { } pos ? ShelterAt(ctx, pos.X, pos.Z) : new Shelter(0f, 0f, 0);

    // Structures in the chunk around a point and its neighbors; footprints are far smaller than a chunk.
    static IEnumerable<Structure> StructuresNear(ReducerContext ctx, float x, float z)
    {
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        for (int i = cx - 1; i <= cx + 1; i++)
            for (int j = cz - 1; j <= cz + 1; j++)
                foreach (var s in ctx.Db.Structure.ByChunk.Filter((i, j)))
                    yield return s;
    }

    static List<MaterialNeed> NeedsFor(int kind, int stage) =>
        StageCosts(kind)[stage].Select(c => new MaterialNeed { Item = (ushort)c.Item, Have = 0, Need = c.Count }).ToList();

    // A stage is done: everyone who helped with it practices that building skill,
    // and the work's quality (materials, capped by each builder's skill) is folded in.
    static (Structure, StructureProgress) FinishStage(ReducerContext ctx, Structure s, StructureProgress p)
    {
        string process = Structures.StageProcess((int)s.Kind, s.Stage);
        float materials = p.StageItems > 0 ? p.StageQualitySum / (float)p.StageItems : Items.RawQuality;
        float sum = 0;
        foreach (uint helper in p.StageHelpers)
        {
            byte cap = 100;
            QualityCapFor(ctx, helper, process, ref cap);
            byte q = Processes.Quality(materials, Processes.DefaultScore, cap);
            OnProcessPerformed(ctx, helper, process, q, false);
            sum += q;
        }
        byte stageQuality = p.StageHelpers.Count > 0 ? (byte)Math.Clamp(MathF.Round(sum / p.StageHelpers.Count), 1, 100) : Items.RawQuality;
        byte overall = (byte)Math.Clamp(MathF.Round((p.Quality * s.Stage + stageQuality) / (float)(s.Stage + 1)), 1, 100);

        int stages = StageCosts((int)s.Kind).Length;
        byte next = (byte)(s.Stage + 1);
        p = p with { Quality = overall, StageQualitySum = 0, StageItems = 0, StageHelpers = new List<uint>() };
        return next >= stages
            ? (s with { Stage = (byte)stages, Needs = new List<MaterialNeed>(), Complete = true }, p)
            : (s with { Stage = next, Needs = NeedsFor((int)s.Kind, next) }, p);
    }

    // Structures from before StructureProgress existed get a fresh row on first use.
    static StructureProgress ProgressOf(ReducerContext ctx, ulong structureId)
    {
        if (ctx.Db.StructureProgress.StructureId.Find(structureId) is { } p) return p;
        return ctx.Db.StructureProgress.Insert(new StructureProgress
        {
            StructureId = structureId,
            Quality = 0,
            StageQualitySum = 0,
            StageItems = 0,
            StageHelpers = new List<uint>(),
        });
    }

    static short DepthAt(ReducerContext ctx, int x, int z) =>
        ctx.Db.TerrainEdit.CellKey.Find(World.CellKey(x, z))?.Depth ?? 0;

    // True when this is the first thing this player has added to the structure.
    static bool RecordBuilder(ReducerContext ctx, ulong structureId)
    {
        foreach (var row in ctx.Db.StructureBuilder.StructureId.Filter(structureId))
            if (row.Builder == ctx.Sender)
            {
                ctx.Db.StructureBuilder.Id.Update(row with { Added = row.Added + 1 });
                return false;
            }
        ctx.Db.StructureBuilder.Insert(new StructureBuilder { Id = 0, StructureId = structureId, Builder = ctx.Sender, Added = 1 });
        return true;
    }
}
