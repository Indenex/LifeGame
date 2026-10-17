using LifeGame.Shared;
using SpacetimeDB;

// Sleeping mats: a woven mat laid inside a finished hut or house you have a right
// to (you placed it, helped build it, or belong to the claim it stands in). You
// wake on your mat if you chose it (survival-23). One mat per player; laying a new one rolls up the
// old. If the mat's building is taken down or weathers away, the mat is left on
// the ground. Whether you wake on it after dying is your choice (survival-23, RestSpots.cs).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "SleepingMat", Public = true)]
    public partial struct SleepingMat
    {
        [PrimaryKey]
        public uint PlayerId;
        [SpacetimeDB.Index.BTree]
        public ulong StructureId;
        public float X;
        public float Z;
        public float Yaw;
    }

    // Lays the carried mat out where you stand, inside a finished hut or house.
    [SpacetimeDB.Reducer]
    public static void LayMat(ReducerContext ctx, ulong structureId)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var s = ctx.Db.Structure.Id.Find(structureId) ?? throw new Exception("That building is gone");
        if (!s.Complete || !Structures.HasDoorway((int)s.Kind))
            throw new Exception("A mat goes inside a finished hut or house");
        if (!Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, pos.X, pos.Z, -0.4f))
            throw new Exception("Step inside to lay it out");
        if (!MayLiveIn(ctx, s))
            throw new Exception("This isn't your home: only its builders or the claim's members can sleep here");
        if (CountOf(ctx, Item.SleepingMat) < 1)
            throw new Exception("You need a sleeping mat");
        Take(ctx, Item.SleepingMat, 1);
        if (ctx.Db.SleepingMat.PlayerId.Find(player.PlayerId) is not null)
        {
            ctx.Db.SleepingMat.PlayerId.Delete(player.PlayerId);
            AddToInventory(ctx, Me(ctx), Item.SleepingMat, 1, Items.RawQuality);
        }
        ctx.Db.SleepingMat.Insert(new SleepingMat { PlayerId = player.PlayerId, StructureId = s.Id, X = pos.X, Z = pos.Z, Yaw = pos.Heading });
        TouchStructure(ctx, s.Id);
    }

    // Rolls your mat back up and carries it.
    [SpacetimeDB.Reducer]
    public static void RollUpMat(ReducerContext ctx)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var mat = ctx.Db.SleepingMat.PlayerId.Find(player.PlayerId) ?? throw new Exception("You have no mat laid out");
        RequireReach(pos, mat.X, mat.Z);
        ctx.Db.SleepingMat.PlayerId.Delete(player.PlayerId);
        AddToInventory(ctx, Me(ctx), Item.SleepingMat, 1, Items.RawQuality);
    }

    static bool MayLiveIn(ReducerContext ctx, Structure s)
    {
        if (s.Owner == Me(ctx)) return true;
        if (ctx.Db.StructureBuilder.StructureId.Filter(s.Id).Any(b => b.Builder == Me(ctx))) return true;
        return ClaimAt(ctx, s.X, s.Z) is { } claim && HasClaimRights(ctx, claim, Me(ctx));
    }

    // A building is gone: its mats are left lying where they were.
    static void DropMats(ReducerContext ctx, Structure s)
    {
        foreach (var mat in ctx.Db.SleepingMat.StructureId.Filter(s.Id).ToList())
        {
            ctx.Db.SleepingMat.PlayerId.Delete(mat.PlayerId);
            var dropped = ctx.Db.WorldItem.Insert(new WorldItem
            {
                Id = 0,
                Item = (ushort)Item.SleepingMat,
                Quality = Items.RawQuality,
                X = mat.X,
                Z = mat.Z,
                ChunkX = World.ChunkOf(mat.X),
                ChunkZ = World.ChunkOf(mat.Z),
            });
            TouchGroundItem(ctx, dropped);
        }
    }

    // Survival asks whether a player's laid-out rest spot (their mat) is still there (RestSpots.cs).
    static partial void LaidRestSpotHook(ReducerContext ctx, uint playerId, RestSpotDef spot, ulong structureId, ref float x, ref float z, ref bool stands)
    {
        if (spot.Item != Item.SleepingMat) return;
        if (ctx.Db.SleepingMat.PlayerId.Find(playerId) is not { } mat || mat.StructureId != structureId) return;
        x = mat.X;
        z = mat.Z;
        stands = true;
    }
}
