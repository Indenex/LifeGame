using LifeGame.Shared;
using SpacetimeDB;

// How a player first learned a process.
[SpacetimeDB.Type]
public enum LearnedBy
{
    Experimenting,
    Watching,
    Reading,
}

// The discovery journal: every process a player knows, and how they learned it.
// Knowledge is never capped and never lost, not even on death.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "JournalEntry")]
    [SpacetimeDB.Index.BTree(Accessor = "ByOwnerProcess", Columns = new[] { nameof(Owner), nameof(ProcessId) })]
    public partial struct JournalEntry
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        public string ProcessId;
        public LearnedBy LearnedBy;
        // Who you watched, or who wrote the note you read. Empty when you worked it out yourself.
        public string LearnedFrom;
        public Timestamp LearnedAt;
        public uint TimesPerformed;
    }

    [SpacetimeDB.View(Accessor = "MyJournal", Public = true)]
    public static List<JournalEntry> MyJournal(ViewContext ctx) =>
        ctx.Db.JournalEntry.Owner.Filter(ctx.Sender).ToList();

    // A written note left on the ground. Anyone in reach can read it and learn the process.
    [SpacetimeDB.Table(Accessor = "JournalNote", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct JournalNote
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Author;
        public string AuthorName;
        public string ProcessId;
        public float X;
        public float Z;
        public int ChunkX;
        public int ChunkZ;
        public Timestamp WrittenAt;
    }

    // Crafting code calls this once each time a player finishes a process.
    // It records the process in their journal, teaches it to anyone watching,
    // and trains the matching skill.
    public static void PerformedProcess(ReducerContext ctx, Identity who, string processId, float x, float z)
    {
        if (FindEntry(ctx, who, processId) is { } entry)
            ctx.Db.JournalEntry.Id.Update(entry with { TimesPerformed = entry.TimesPerformed + 1 });
        else
            Learn(ctx, who, processId, LearnedBy.Experimenting, "", performed: 1);

        TeachOnlookers(ctx, who, processId, x, z);
        TrainSkill(ctx, who, processId);
    }

    public static bool KnowsProcess(ReducerContext ctx, Identity who, string processId) =>
        FindEntry(ctx, who, processId) is not null;

    // Leave a note about something you know at your feet, for others to find.
    [SpacetimeDB.Reducer]
    public static void WriteNote(ReducerContext ctx, string processId)
    {
        var player = RequirePlayer(ctx);
        if (!KnowsProcess(ctx, ctx.Sender, processId))
            throw new Exception("You can only write about what you know");
        var pos = RequirePos(ctx, player);

        var mine = ctx.Db.JournalNote.Author.Filter(ctx.Sender).OrderBy(n => n.WrittenAt.MicrosecondsSinceUnixEpoch).ToList();
        for (int i = 0; i <= mine.Count - Skills.NotesPerPlayer; i++)
            ctx.Db.JournalNote.Id.Delete(mine[i].Id);

        ctx.Db.JournalNote.Insert(new JournalNote
        {
            Id = 0,
            Author = ctx.Sender,
            AuthorName = player.Name,
            ProcessId = processId,
            X = pos.X,
            Z = pos.Z,
            ChunkX = pos.ChunkX,
            ChunkZ = pos.ChunkZ,
            WrittenAt = ctx.Timestamp,
        });
    }

    // Read a note within reach. The note stays where it is for the next reader.
    [SpacetimeDB.Reducer]
    public static void ReadNote(ReducerContext ctx, ulong noteId)
    {
        var player = RequirePlayer(ctx);
        var note = ctx.Db.JournalNote.Id.Find(noteId) ?? throw new Exception("That note is gone");
        RequireReach(RequirePos(ctx, player), note.X, note.Z);
        if (KnowsProcess(ctx, ctx.Sender, note.ProcessId))
            throw new Exception("You already know this");
        Learn(ctx, ctx.Sender, note.ProcessId, LearnedBy.Reading, note.AuthorName, performed: 0);
    }

    static JournalEntry? FindEntry(ReducerContext ctx, Identity who, string processId)
    {
        foreach (var row in ctx.Db.JournalEntry.ByOwnerProcess.Filter((who, processId)))
            return row;
        return null;
    }

    static void Learn(ReducerContext ctx, Identity who, string processId, LearnedBy how, string from, uint performed)
    {
        ctx.Db.JournalEntry.Insert(new JournalEntry
        {
            Id = 0,
            Owner = who,
            ProcessId = processId,
            LearnedBy = how,
            LearnedFrom = from,
            LearnedAt = ctx.Timestamp,
            TimesPerformed = performed,
        });
    }

    // Online players near the worker who don't know the process yet pick it up by watching.
    static void TeachOnlookers(ReducerContext ctx, Identity who, string processId, float x, float z)
    {
        var worker = ctx.Db.Player.Identity.Find(who);
        if (worker is null) return;
        int cx = World.ChunkOf(x), cz = World.ChunkOf(z);
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                foreach (var pos in ctx.Db.PlayerPos.ByChunk.Filter(((short)(cx + dx), (short)(cz + dz))).ToList())
                {
                    if (pos.PlayerId == worker.Value.PlayerId) continue;
                    float ox = pos.X - x, oz = pos.Z - z;
                    if (ox * ox + oz * oz > Skills.WatchRadius * Skills.WatchRadius) continue;
                    if (ctx.Db.Player.PlayerId.Find(pos.PlayerId) is not { Online: true } watcher) continue;
                    if (KnowsProcess(ctx, watcher.Identity, processId)) continue;
                    Learn(ctx, watcher.Identity, processId, LearnedBy.Watching, worker.Value.Name, performed: 0);
                }
    }
}
