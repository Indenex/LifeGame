using SpacetimeDB;

// Suspicion log (bugfix-52, audit item 14 of bugfix-24, D-182). Where the server already accepts a
// call and clamps what is wrong with it, it also counts that here, per player and kind, so the host
// can see who is cheating. A refused call throws and rolls everything back, so refusals are never
// logged; only the tolerated ones are. The table is private (the host reads it with
// `spacetime sql`) and an admin can read it through the suspicion_report view. Nothing is
// shown to other players and nothing here changes what a call does.
public static partial class Module
{
    public enum Suspect : byte
    {
        // A Move that asked for more distance than the walking budget allows; clamped (bugfix-24 item 1).
        MoveOverBudget = 1,
        // A Move that lands well inside a wall, trunk or the like (a teleport or noclip, not a bump);
        // sent back to where it came from (PlayerCollision.cs).
        MoveInsideObstacle = 2,
        // A minigame score sent with no real play time behind it; capped (CraftScore.cs).
        QuickScore = 3,
        // A perfect score that the streak cap pulled down (CraftScore.cs).
        PerfectStreak = 4,
    }

    // Only a Move that asks for this much more than the budget is counted; lag alone stays under it.
    const float SuspectMoveExcess = 0.75f;

    [SpacetimeDB.Table(Accessor = "Suspicion")]
    public partial struct Suspicion
    {
        // PlayerId * 256 + kind.
        [PrimaryKey]
        public ulong Key;
        public uint PlayerId;
        public byte Kind;
        public uint Count;
        public Timestamp First;
        public Timestamp Last;
        // Always true; views may only read by index, so admins read by this.
        [SpacetimeDB.Index.BTree]
        public bool Logged;
    }

    static void NoteSuspicion(ReducerContext ctx, uint playerId, Suspect kind)
    {
        ulong key = (ulong)playerId * 256 + (byte)kind;
        if (ctx.Db.Suspicion.Key.Find(key) is { } row)
        {
            row.Count++;
            row.Last = ctx.Timestamp;
            ctx.Db.Suspicion.Key.Update(row);
        }
        else
            ctx.Db.Suspicion.Insert(new Suspicion
            {
                Key = key, PlayerId = playerId, Kind = (byte)kind, Count = 1,
                First = ctx.Timestamp, Last = ctx.Timestamp, Logged = true,
            });
    }

    // Every row for an admin, none for anyone else.
    [SpacetimeDB.View(Accessor = "SuspicionReport", Public = true)]
    public static List<Suspicion> SuspicionReport(ViewContext ctx) =>
        ctx.Db.WorldAdmin.Identity.Find(Me(ctx)) is not null
            ? ctx.Db.Suspicion.Logged.Filter(true).ToList()
            : new List<Suspicion>();
}
