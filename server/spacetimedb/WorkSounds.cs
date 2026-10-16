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

    // Crafting calls these when a job starts (StartCraft) and ends (FinishCraft,
    // which also runs for a player who disconnected mid-job). Crafting declares them
    // in Crafting.cs on crafting-12 (branch crafting-work-hooks); until that merges
    // they are declared here too. DELETE THESE TWO LINES when merging after it.
    static partial void OnCraftStarted(ReducerContext ctx, CraftJob job);
    static partial void OnCraftEnded(ReducerContext ctx, CraftJob job);

    static partial void OnCraftStarted(ReducerContext ctx, CraftJob job)
    {
        ctx.Db.CraftWork.PlayerId.Delete(job.PlayerId);
        ctx.Db.CraftWork.Insert(new CraftWork { PlayerId = job.PlayerId, ProcessId = job.ProcessId, Failed = job.Failed });
    }

    static partial void OnCraftEnded(ReducerContext ctx, CraftJob job) => ctx.Db.CraftWork.PlayerId.Delete(job.PlayerId);
}
