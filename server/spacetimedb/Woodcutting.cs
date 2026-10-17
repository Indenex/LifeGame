using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// Chopping logs from standing trees with an axe (process chop.log). Trees are
// generated from the shared seed (shared/Trees.cs); only the ones somebody has
// chopped are stored. With Trees.Felling on, a tree falls after Trees.LogsPerTree
// logs and grows back later; with it off, it gives one log of deadwood at a time.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ChoppedTree", Public = true)]
    [SpacetimeDB.Index.BTree(Accessor = "ByChunk", Columns = new[] { nameof(ChunkX), nameof(ChunkZ) })]
    public partial struct ChoppedTree
    {
        [PrimaryKey]
        public long CellKey;
        public int CellX;
        public int CellZ;
        public int ChunkX;
        public int ChunkZ;
        public byte Logs;
        public Timestamp LastChop;
        // Felled trees are gone from the world until the row is cleared.
        public bool Felled;
    }

    [SpacetimeDB.Table(Accessor = "TreeTimer", Scheduled = nameof(RegrowTrees), ScheduledAt = nameof(ScheduledAt))]
    public partial struct TreeTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // The standing tree nearest to (x, z) whose trunk is within reach, or null.
    static (int X, int Z)? TreeWithinReach(ReducerContext ctx, float x, float z)
    {
        int cx = (int)MathF.Round(x), cz = (int)MathF.Round(z);
        (int, int)? best = null;
        float bestD = World.Reach + 0.5f;
        for (int i = cx - 3; i <= cx + 3; i++)
            for (int j = cz - 3; j <= cz + 3; j++)
            {
                if (!Trees.GrowsAt(i, j) || !TreeStands(ctx, i, j)) continue;
                var (tx, tz) = Trees.Trunk(i, j);
                float d = MathF.Sqrt((tx - x) * (tx - x) + (tz - z) * (tz - z));
                if (d < bestD) { bestD = d; best = (i, j); }
            }
        return best;
    }

    // A tree is gone where someone dug the ground out from under it or felled it.
    static bool TreeStands(ReducerContext ctx, int x, int z)
    {
        long key = World.CellKey(x, z);
        if ((ctx.Db.TerrainEdit.CellKey.Find(key)?.Depth ?? 0) != 0) return false;
        return ctx.Db.ChoppedTree.CellKey.Find(key) is not { Felled: true };
    }

    // Called when a chop starts: which tree, or why not.
    static (float X, float Z) TreeToChop(ReducerContext ctx, float x, float z)
    {
        if (TreeWithinReach(ctx, x, z) is not { } tree)
            throw new Exception("Stand next to a tree to chop it");
        if (!Trees.Felling && ctx.Db.ChoppedTree.CellKey.Find(World.CellKey(tree.X, tree.Z)) is { } row
            && (ctx.Timestamp.MicrosecondsSinceUnixEpoch - row.LastChop.MicrosecondsSinceUnixEpoch) < (long)(Trees.DeadwoodSeconds * 1_000_000))
            throw new Exception("That tree has no deadwood left for now. Try another");
        return Trees.Trunk(tree.X, tree.Z);
    }

    // A chop finished: the log goes in the bag and the tree remembers it.
    static void ChopTree(ReducerContext ctx, CraftJob job, ProcessDef def)
    {
        int x = (int)MathF.Round(job.X), z = (int)MathF.Round(job.Z);
        long key = World.CellKey(x, z);
        var row = ctx.Db.ChoppedTree.CellKey.Find(key);
        // Someone else felled it while this chop was under way: the tree gives nothing more (D-182).
        if (row is { Felled: true })
        {
            News(ctx, job.PlayerId, "The tree came down before your last swing landed.");
            return;
        }
        AddToInventory(ctx, job.Owner, def.Makes, def.MakesCount, Items.RawQuality);
        byte logs = (byte)((row?.Logs ?? 0) + 1);
        bool felled = Trees.Felling && logs >= Trees.LogsPerTree;
        var next = new ChoppedTree
        {
            CellKey = key,
            CellX = x,
            CellZ = z,
            ChunkX = World.ChunkOfCell(x),
            ChunkZ = World.ChunkOfCell(z),
            Logs = logs,
            LastChop = ctx.Timestamp,
            Felled = felled,
        };
        if (row is null) ctx.Db.ChoppedTree.Insert(next);
        else ctx.Db.ChoppedTree.CellKey.Update(next);
        if (felled) News(ctx, job.PlayerId, "The tree comes down.");
        EnsureTreeTimer(ctx);
    }

    // Felled trees grow back, half-chopped ones heal, and spent deadwood returns.
    [SpacetimeDB.Reducer]
    public static void RegrowTrees(ReducerContext ctx, TreeTimer timer)
    {
        RequireServer(ctx);
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        long after = (long)((Trees.Felling ? Trees.RegrowSeconds : Trees.DeadwoodSeconds) * 1_000_000);
        foreach (var row in ctx.Db.ChoppedTree.Iter().ToList())
            if (now - row.LastChop.MicrosecondsSinceUnixEpoch >= after)
                ctx.Db.ChoppedTree.CellKey.Delete(row.CellKey);
    }

    // Scheduled on the first chop, so worlds published before this file get it too.
    static void EnsureTreeTimer(ReducerContext ctx)
    {
        if (ctx.Db.TreeTimer.Count > 0) return;
        ctx.Db.TreeTimer.Insert(new TreeTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMinutes(1)) });
    }
}
