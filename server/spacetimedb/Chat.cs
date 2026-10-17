using LifeGame.Shared;
using SpacetimeDB;

// Proximity chat: a message is spoken where the speaker stands, and clients near
// that spot hear it. Messages are kept for a couple of minutes, then cleared.
// claims-13 (D-182): the server decides who hears. Each message is stored once per
// listener in range, in a private table, and clients read only their own rows through
// the MyChat view, so a whisper never reaches anyone out of earshot.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ChatMessage")]
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

    // What one player heard: a copy of a message per listener in range, the speaker too.
    // Private; clients read their own rows through the MyChat view. (ChatMessage above is
    // the older public table: nothing writes it any more and it is kept so existing
    // databases upgrade without a wipe.)
    [SpacetimeDB.Table(Accessor = "ChatHeard")]
    [SpacetimeDB.Index.BTree(Accessor = "ByRecipient", Columns = new[] { nameof(Recipient) })]
    public partial struct ChatHeard
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public Identity Recipient;
        public uint SenderId;
        public string SenderName;
        public string Text;
        public float X;
        public float Z;
        [SpacetimeDB.Index.BTree]
        public Timestamp SentAt;
    }

    [SpacetimeDB.View(Accessor = "MyChat", Public = true)]
    public static List<ChatHeard> MyChat(ViewContext ctx) => ctx.Db.ChatHeard.ByRecipient.Filter(ctx.Sender).ToList();

    // Stores one row per online player (the speaker included) within range of (x, z).
    static void Deliver(ReducerContext ctx, uint senderId, string senderName, string text, float x, float z, float range)
    {
        foreach (var listener in ctx.Db.Player.Iter().Where(p => p.Online).ToList())
        {
            if (ctx.Db.PlayerPos.PlayerId.Find(listener.PlayerId) is not { } at) continue;
            float dx = at.X - x, dz = at.Z - z;
            if (listener.PlayerId != senderId && dx * dx + dz * dz > range * range) continue;
            ctx.Db.ChatHeard.Insert(new ChatHeard
            {
                Id = 0,
                Recipient = listener.Identity,
                SenderId = senderId,
                SenderName = senderName,
                Text = text,
                X = x,
                Z = z,
                SentAt = ctx.Timestamp,
            });
        }
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
        Deliver(ctx, 0, "", text, x, z, Social.ChatRange);
    }

    [SpacetimeDB.Reducer]
    public static void SendChat(ReducerContext ctx, string text)
    {
        InitChat(ctx);
        text = Social.StripControl(text).Trim();
        if (text.Length == 0) return;
        if (text.Length > Social.ChatMaxLength) text = text[..Social.ChatMaxLength];
        // claims-6: "/me" on its own says nothing; "/ME waves" is stored as "/me waves".
        if (Social.IsEmote(text))
        {
            string body = Social.EmoteBody(text);
            if (body.Length == 0) return;
            text = "/me " + body;
        }
        // claims-10: "/W hi" is stored as "/w hi"; a bare "/w" or "/s" says nothing.
        else if (Social.VoiceOf(text) is var voice && voice != Social.Voice.Say)
        {
            string body = Social.VoiceBody(text);
            if (body.Length == 0) return;
            text = (voice == Social.Voice.Whisper ? "/w " : "/s ") + body;
        }

        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);

        var state = ctx.Db.ChatState.PlayerId.Find(player.PlayerId);
        if (state is { } s && ctx.Timestamp.MicrosecondsSinceUnixEpoch - s.LastSentAt.MicrosecondsSinceUnixEpoch < Social.ChatCooldownMicros)
            throw new Exception("Slow down a little");
        var next = new ChatState { PlayerId = player.PlayerId, LastSentAt = ctx.Timestamp };
        if (state is null) ctx.Db.ChatState.Insert(next);
        else ctx.Db.ChatState.PlayerId.Update(next);

        Deliver(ctx, player.PlayerId, player.Name, text, pos.X, pos.Z, Social.RangeOf(Social.VoiceOf(text)));
    }

    [SpacetimeDB.Reducer]
    public static void PruneChat(ReducerContext ctx, ChatPruneTimer timer)
    {
        RequireServer(ctx);
        var cutoff = new Timestamp(ctx.Timestamp.MicrosecondsSinceUnixEpoch - Social.ChatKeepMicros);
        foreach (var message in ctx.Db.ChatHeard.SentAt.Filter((new Timestamp(0), cutoff)).ToList())
            ctx.Db.ChatHeard.Id.Delete(message.Id);
        foreach (var message in ctx.Db.ChatMessage.SentAt.Filter((new Timestamp(0), cutoff)).ToList())
            ctx.Db.ChatMessage.Id.Delete(message.Id);
    }
}
