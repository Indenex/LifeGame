using SpacetimeDB;

// Heavy-reducer pacing and the loose-item cap (bugfix-51, audit item 9 of bugfix-24, D-182). The
// call bucket in RateLimit.cs caps how many calls a player makes; these cover the few reducers
// where even a few calls a second would flood the database or everyone's screen. Refusals are
// friendly (nobody is punished) and, as in RateLimit.cs, cost nothing: a refused call rolls back.
public static partial class Module
{
    // Chunks are 16 x 16 cells, so this is more than one loose item to every square metre.
    public const int MaxItemsPerChunk = 400;

    public enum Pace : byte { PlaceStructure = 1, Dismantle = 2, ChangeName = 3, ChangeLook = 4 }

    // When each player last did each paced thing. Private: only the server reads it.
    [SpacetimeDB.Table(Accessor = "PaceMark")]
    public partial struct PaceMark
    {
        [PrimaryKey]
        public ulong Key; // player id * 256 + the Pace
        public Timestamp At;
    }

    // Refuses (gently) a player who does this paced thing again before `seconds` have passed.
    static void RequirePace(ReducerContext ctx, uint playerId, Pace what, double seconds, string message)
    {
        ulong key = (ulong)playerId * 256UL + (ulong)what;
        var row = ctx.Db.PaceMark.Key.Find(key);
        if (row is { } last && SecondsBetween(last.At, ctx.Timestamp) < seconds) throw new Exception(message);
        var next = new PaceMark { Key = key, At = ctx.Timestamp };
        if (row is null) ctx.Db.PaceMark.Insert(next);
        else ctx.Db.PaceMark.Key.Update(next);
    }

    // Room for one more loose item in the chunk? Natural spawns, drops and set-down things all lie
    // in WorldItem; a chunk with a pile this size is full, and the player is told how to make room.
    static void RequireRoomForItem(ReducerContext ctx, int chunkX, int chunkZ)
    {
        int n = 0;
        foreach (var _ in ctx.Db.WorldItem.ByChunk.Filter((chunkX, chunkZ)))
            if (++n >= MaxItemsPerChunk)
                throw new Exception("The ground here is crowded; pick some things up or try a basket");
    }
}
