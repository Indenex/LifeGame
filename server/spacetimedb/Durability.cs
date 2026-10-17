using LifeGame.Shared;
using SpacetimeDB;

// Tool wear and repair (crafting-19, D-155). Hand tools wear a little with each use;
// a spent tool stops working until it is mended at a repair table. The numbers and
// the words live in shared/Durability.cs. Wear rides on the item's Wear column in
// every table that holds items, so it survives drops, baskets, stashes and trades.
public static partial class Module
{
    // Playtest tuning: how much wear one use counts for. One row, normally absent (1).
    [SpacetimeDB.Table(Accessor = "WearConfig")]
    public partial struct WearConfig
    {
        [PrimaryKey]
        public byte Id;
        public ushort Step;
    }

    // Database owner only, for playtests and the crafting check: make each use wear a
    // tool by this many uses (1 is normal).
    [SpacetimeDB.Reducer]
    public static void SetToolWearStep(ReducerContext ctx, ushort step)
    {
        var config = ctx.Db.ClaimConfig.Id.Find(0) ?? throw new Exception("Not set up");
        if (config.Admin != Me(ctx)) throw new Exception("Only the server admin can change this");
        var row = new WearConfig { Id = 0, Step = Math.Max((ushort)1, step) };
        if (ctx.Db.WearConfig.Id.Find(0) is null) ctx.Db.WearConfig.Insert(row);
        else ctx.Db.WearConfig.Id.Update(row);
    }

    // One use of a tool: it wears a little. Tools that never wear return at once.
    static void WearTool(ReducerContext ctx, InventoryItem tool)
    {
        if (!Items.IsKnown(tool.Item) || !Durability.Wears((Item)tool.Item)) return;
        int step = ctx.Db.WearConfig.Id.Find(0)?.Step ?? 1;
        uint wear = Durability.Pack(Durability.Used(tool.Wear) + step, Durability.Origin(tool.Wear));
        // Keep the row's other columns (a tool kept on the belt keeps its place): update a lone
        // tool in place, or split one off a stack of several.
        if (tool.Count == 1)
            ctx.Db.InventoryItem.Id.Update(tool with { Wear = wear });
        else
        {
            RemoveFromStack(ctx, tool, 1);
            ctx.Db.InventoryItem.Insert(tool with { Id = 0, Count = 1, Wear = wear, OnBelt = false, Slot = FreeBagPlace(ctx, tool.Owner) });
        }
    }

    // What to tell a player who has no working tool: if they carry one that is spent,
    // say so, so they know to mend it; otherwise the usual message.
    static string NoToolMessage(ReducerContext ctx, ItemTags tags, string otherwise)
    {
        foreach (var row in ctx.Db.InventoryItem.Owner.Filter(Me(ctx)))
            if (Items.IsKnown(row.Item) && Durability.Wears((Item)row.Item)
                && (Items.Def((Item)row.Item).Tags & tags) != ItemTags.None
                && Durability.IsSpent(row.Quality, row.Wear))
                return $"Your {Items.Def((Item)row.Item).Name.ToLowerInvariant()} is spent. Mend it at a repair table.";
        return otherwise;
    }

    // Mends one worn tool at a repair table in a claim. Repair takes less than a new
    // tool (fresh lashing), restores a full life, and costs the tool a little quality,
    // down to half of what it was made at. The client names the exact stack by its wear.
    [SpacetimeDB.Reducer]
    public static void RepairTool(ReducerContext ctx, ushort item, byte quality, uint wear)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var station = StationNear(ctx, StationKind.Repair, pos.X, pos.Z)
            ?? throw new Exception($"You need to be at a {StationInfo.Name(StationKind.Repair)} in your claim");
        if (!Items.IsKnown(item) || !Durability.Wears((Item)item))
            throw new Exception("That doesn't wear out");
        var stack = FindStack(ctx, Me(ctx), (Item)item, quality, wear) ?? throw new Exception("You are not carrying that");
        if (Durability.Used(wear) == 0)
            throw new Exception("That's as good as it can be");

        var needsTool = Durability.RepairNeedsTool((Item)item);
        InventoryItem? helper = null;
        if (needsTool != ItemTags.None)
            helper = BestTool(ctx, needsTool) ?? throw new Exception(NoToolMessage(ctx, needsTool, "You need something sharp to mend that with"));
        var cost = Durability.RepairCost((Item)item);
        foreach (var input in cost)
            if (CountFor(ctx, station, input.Item) < input.Count)
                throw new Exception($"You need {input.Count} {Items.Def(input.Item).Count(input.Count).ToLowerInvariant()}");
        foreach (var input in cost)
            TakeFor(ctx, station, input.Item, input.Count);
        if (helper is { } tool) WearTool(ctx, tool);

        // Taking the cordage may have changed the stack's row; look it up again.
        var again = FindStack(ctx, Me(ctx), (Item)item, quality, wear) ?? stack;
        byte mendedQuality = Durability.RepairedQuality(quality, wear);
        uint mendedWear = Durability.Repaired(quality, wear);
        // A tool on the belt is mended where it hangs, not dropped back into the bag.
        if (again.OnBelt && again.Count == 1)
            ctx.Db.InventoryItem.Id.Update(again with { Quality = mendedQuality, Wear = mendedWear });
        else
        {
            RemoveFromStack(ctx, again, 1);
            AddToInventory(ctx, Me(ctx), (Item)item, 1, mendedQuality, mendedWear, again.Maker);
        }
        PerformedProcess(ctx, player.PlayerId, Skills.Repair, pos.X, pos.Z);
    }
}
