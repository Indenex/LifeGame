using SpacetimeDB;

// Atmosphere: everyone hears the work going on around them (D-072). Craft jobs
// are private, so this public table mirrors only who is working at what: a row
// appears when a job starts and goes when it ends, nothing per tick.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "CraftWork", Public = true)]
    public partial struct CraftWork
    {
        [PrimaryKey]
        public uint PlayerId;
        public string ProcessId;
        // Known from the start; the client plays it as a snap when the work ends.
        public bool Failed;
    }

    // Crafting calls these when a job is inserted (StartCraft) and deleted (FinishCraft).
    static partial void OnCraftStarted(ReducerContext ctx, CraftJob job);
    static partial void OnCraftEnded(ReducerContext ctx, CraftJob job);

    static partial void OnCraftStarted(ReducerContext ctx, CraftJob job)
    {
        ctx.Db.CraftWork.PlayerId.Delete(job.PlayerId);
        ctx.Db.CraftWork.Insert(new CraftWork { PlayerId = job.PlayerId, ProcessId = job.ProcessId, Failed = job.Failed });
    }

    static partial void OnCraftEnded(ReducerContext ctx, CraftJob job) => ctx.Db.CraftWork.PlayerId.Delete(job.PlayerId);
}
