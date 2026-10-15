using SpacetimeDB;

public static partial class Module
{
    // One row per account: who the player is. Changes rarely, so it is cheap to replicate.
    [SpacetimeDB.Table(Accessor = "Player", Public = true)]
    public partial struct Player
    {
        [PrimaryKey]
        public Identity Identity;
        [Unique]
        [AutoInc]
        public uint PlayerId;
        public string Name;
        public bool Online;
    }

    // Where each player is. Updated about ten times a second while walking, so the row
    // is kept small: it is the bulk of all traffic. Clients subscribe by chunk.
    [SpacetimeDB.Table(Accessor = "PlayerPos", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct PlayerPos
    {
        [PrimaryKey]
        public uint PlayerId;
        public float X;
        public float Z;
        public float Heading;
        public short ChunkX;
        public short ChunkZ;
    }

    // Latest movement a client reported, applied to PlayerPos in one batch per tick.
    // Private, so a Move call on its own sends nothing to other clients.
    [SpacetimeDB.Table(Accessor = "MoveInput")]
    public partial struct MoveInput
    {
        [PrimaryKey]
        public uint PlayerId;
        public float X;
        public float Z;
        public float Heading;
    }

    [SpacetimeDB.Table(Accessor = "MoveTickTimer", Scheduled = nameof(ApplyMoves), ScheduledAt = nameof(ScheduledAt))]
    public partial struct MoveTickTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // Server-only bookkeeping for anti-cheat checks; never sent to clients.
    [SpacetimeDB.Table(Accessor = "PlayerState")]
    public partial struct PlayerState
    {
        [PrimaryKey]
        public uint PlayerId;
        public Timestamp LastMoveAt;
        public Timestamp LastActionAt;
    }

    // Terrain is generated from the shared seed; only dug height points are stored.
    [SpacetimeDB.Table(Accessor = "TerrainEdit", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct TerrainEdit
    {
        [PrimaryKey]
        public long CellKey;
        public int CellX;
        public int CellZ;
        public int ChunkX;
        public int ChunkZ;
        public short Depth;
    }

    // Loose items lying on the ground. Item is a LifeGame.Shared.Item id.
    [SpacetimeDB.Table(Accessor = "WorldItem", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct WorldItem
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public ushort Item;
        public byte Quality;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
    }

    // What each player carries: one stack per item and quality.
    [SpacetimeDB.Table(Accessor = "InventoryItem")]
    [SpacetimeDB.Index.BTree(Accessor = "ByOwnerItem", Columns = new[] { nameof(Owner), nameof(Item) })]
    public partial struct InventoryItem
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        public ushort Item;
        public byte Quality;
        public uint Count;
    }

    // Clients read their own inventory through this view; the table itself is private.
    [SpacetimeDB.View(Accessor = "MyInventory", Public = true)]
    public static List<InventoryItem> MyInventory(ViewContext ctx) =>
        ctx.Db.InventoryItem.Owner.Filter(ctx.Sender).ToList();

    [SpacetimeDB.Table(Accessor = "RespawnTimer", Scheduled = nameof(RespawnItems), ScheduledAt = nameof(ScheduledAt))]
    public partial struct RespawnTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }
}
