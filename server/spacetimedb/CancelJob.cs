using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// Stopping a job in progress (crafting-31). A job runs on a timer, so the server ends it
// itself when its conditions stop holding: the player walks out of reach of its target,
// or stops holding the tool it needs. The player can also stop it (CancelCraft), which the
// client does at once on any move, jump, panel or change of hand. Nothing lands after a
// cancel: the timer is removed, the work sounds and animation end with the job row, and the
// inputs come back to the bag. One place for every job (chop, craft, station, fire, roast).
public static partial class Module
{
    // Walking this far past reach of a job's target ends it. Reach plus the same slack the
    // reducers allow for lag (RequireReach).
    const float JobLeash = World.Reach + 0.5f;

    // The player ends their own job; a no-op when there is none.
    [SpacetimeDB.Reducer]
    public static void CancelCraft(ReducerContext ctx)
    {
        RequirePlayer(ctx);
        CancelJob(ctx, Me(ctx));
    }

    // Ends a job now: removes its timer, returns what it took, and tells the work hooks.
    // Returns whether there was a job.
    static bool CancelJob(ReducerContext ctx, Identity owner)
    {
        if (ctx.Db.CraftJob.Owner.Find(owner) is not { } job) return false;
        ctx.Db.CraftJob.Owner.Delete(owner);
        foreach (var timer in ctx.Db.CraftTimer.Iter().Where(t => t.Owner == owner).ToList())
            ctx.Db.CraftTimer.ScheduledId.Delete(timer.ScheduledId);
        OnCraftEnded(ctx, job);
        if (Processes.Find(job.ProcessId) is { } def)
            foreach (var input in def.Consumes)
            {
                var item = Items.Def(input.Item);
                byte quality = ClayRules.IsVessel(input.Item) ? job.VesselQuality : item.HasQuality ? job.Quality : Items.RawQuality;
                AddToInventory(ctx, owner, input.Item, input.Count, quality);
            }
        return true;
    }

    // After a player's position changes: a job whose target they have left behind stops.
    static void CheckJobReach(ReducerContext ctx, Player player, float x, float z)
    {
        if (ctx.Db.CraftJob.Owner.Find(player.Identity) is not { } job) return;
        float dx = job.X - x, dz = job.Z - z;
        if (MathF.Sqrt(dx * dx + dz * dz) > JobLeash) CancelJob(ctx, player.Identity);
    }

    // After the hand changes (EquipSlot, SyncEquip): a chop needs the axe still in hand.
    static void CheckJobTool(ReducerContext ctx)
    {
        if (ctx.Db.CraftJob.Owner.Find(Me(ctx)) is not { } job) return;
        if (Processes.Find(job.ProcessId) is not { Outcome: Outcome.Chop } def) return;
        if (EquippedTool(ctx, def.NeedsTool) is null) CancelJob(ctx, Me(ctx));
    }
}
