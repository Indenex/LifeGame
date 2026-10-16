using LifeGame.Shared;
using SpacetimeDB;

// Proximity chat: a message is spoken where the speaker stands, and clients near
// that spot hear it. Messages are kept for a couple of minutes, then cleared.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ChatMessage", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct ChatMessage
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public uint SenderId;
        public string SenderName;
        public string Text;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        [SpacetimeDB.Index.BTree]
        public Timestamp SentAt;
    }

    // Server-only: when each player last spoke, to keep chat from being flooded.
    [SpacetimeDB.Table(Accessor = "ChatState")]
    public partial struct ChatState
    {
        [PrimaryKey]
        public uint PlayerId;
        public Timestamp LastSentAt;
    }

    [SpacetimeDB.Table(Accessor = "ChatPruneTimer", Scheduled = nameof(PruneChat), ScheduledAt = nameof(ScheduledAt))]
    public partial struct ChatPruneTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // Called from Init, and again on first use for databases created before chat existed.
    static void InitChat(ReducerContext ctx)
    {
        if (ctx.Db.ChatPruneTimer.Count > 0) return;
        ctx.Db.ChatPruneTimer.Insert(new ChatPruneTimer
        {
            ScheduledId = 0,
            ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(15)),
        });
    }

    // A line from the world rather than a person (SenderId 0), heard within chat range.
    static void SayNearby(ReducerContext ctx, float x, float z, string text)
    {
        InitChat(ctx);
        ctx.Db.ChatMessage.Insert(new ChatMessage
        {
            Id = 0,
            SenderId = 0,
            SenderName = "",
            Text = text,
            X = x,
            Z = z,
            ChunkX = World.ChunkOf(x),
            ChunkZ = World.ChunkOf(z),
            SentAt = ctx.Timestamp,
        });
    }

    [SpacetimeDB.Reducer]
    public static void SendChat(ReducerContext ctx, string text)
    {
        InitChat(ctx);
        text = text.Trim();
        if (text.Length == 0) return;
        if (text.Length > Social.ChatMaxLength) text = text[..Social.ChatMaxLength];

        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);

        var state = ctx.Db.ChatState.PlayerId.Find(player.PlayerId);
        if (state is { } s && ctx.Timestamp.MicrosecondsSinceUnixEpoch - s.LastSentAt.MicrosecondsSinceUnixEpoch < Social.ChatCooldownMicros)
            throw new Exception("Slow down a little");
        var next = new ChatState { PlayerId = player.PlayerId, LastSentAt = ctx.Timestamp };
        if (state is null) ctx.Db.ChatState.Insert(next);
        else ctx.Db.ChatState.PlayerId.Update(next);

        ctx.Db.ChatMessage.Insert(new ChatMessage
        {
            Id = 0,
            SenderId = player.PlayerId,
            SenderName = player.Name,
            Text = text,
            X = pos.X,
            Z = pos.Z,
            ChunkX = pos.ChunkX,
            ChunkZ = pos.ChunkZ,
            SentAt = ctx.Timestamp,
        });
    }

    [SpacetimeDB.Reducer]
    public static void PruneChat(ReducerContext ctx, ChatPruneTimer timer)
    {
        var cutoff = new Timestamp(ctx.Timestamp.MicrosecondsSinceUnixEpoch - Social.ChatKeepMicros);
        foreach (var message in ctx.Db.ChatMessage.SentAt.Filter((new Timestamp(0), cutoff)).ToList())
            ctx.Db.ChatMessage.Id.Delete(message.Id);
    }
}
