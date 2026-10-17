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
        // The tool in hand (an item id), 0 for none, so other players see the exact tool.
        [Default((ushort)0)]
        public ushort Tool;
    }

    // Crafting declares and calls these (Crafting.cs, crafting-12) when a job starts
    // (StartCraft) and ends (FinishCraft, which also runs for a player who
    // disconnected mid-job).

    static partial void OnCraftStarted(ReducerContext ctx, CraftJob job)
    {
        ctx.Db.CraftWork.PlayerId.Delete(job.PlayerId);
        ctx.Db.CraftWork.Insert(new CraftWork { PlayerId = job.PlayerId, ProcessId = job.ProcessId, Failed = job.Failed, Tool = job.Tool });
    }

    static partial void OnCraftEnded(ReducerContext ctx, CraftJob job) => ctx.Db.CraftWork.PlayerId.Delete(job.PlayerId);
}
