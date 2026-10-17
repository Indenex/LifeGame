using System;
using SpacetimeDB;

public static partial class Module
{
    // Test-only time speed-up for the bot checks: ClayCheck and other bots that
    // wait on drying, firing or crafting set it on their own test database, so a run
    // takes minutes instead of a quarter hour. Game times stay as they are: the default
    // is 1, nothing in the game sets it, and only the server admin (Claims' ClaimConfig)
    // can change it. Private, so clients never see it; they read the scaled rates and
    // finish times off the rows as usual.
    [SpacetimeDB.Table(Accessor = "TestSpeedConfig")]
    public partial struct TestSpeedConfig
    {
        [PrimaryKey]
        public uint Id;
        public float Factor;
    }

    const float MaxTestSpeed = 60f;

    // How many times faster test-scaled waits run: 1 in every player's world.
    static float TestSpeed(ReducerContext ctx) => ctx.Db.TestSpeedConfig.Id.Find(0)?.Factor ?? 1f;

    [SpacetimeDB.Reducer]
    public static void SetTestSpeed(ReducerContext ctx, float factor)
    {
        MeterCall(ctx);
        var config = ctx.Db.ClaimConfig.Id.Find(0) ?? throw new Exception("Not set up");
        if (config.Admin != Me(ctx)) throw new Exception("Only the server admin can change this");
        if (!(factor >= 1f && factor <= MaxTestSpeed)) throw new Exception($"Between 1 and {MaxTestSpeed:0}");
        var row = new TestSpeedConfig { Id = 0, Factor = factor };
        if (ctx.Db.TestSpeedConfig.Id.Find(0) is null) ctx.Db.TestSpeedConfig.Insert(row);
        else ctx.Db.TestSpeedConfig.Id.Update(row);
    }
}
