using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

public partial class Terrain
{
    // One standing tree, as the chunk would draw it on cell (x, z), with only its trunk
    // and crown (TreeFall). Instances are in world space. Null where no tree grows.
    public Node3D? StandingTree(int x, int z)
    {
        Prewarm();
        var snapshot = TakeSnapshot(World.ChunkOfCell(x), World.ChunkOfCell(z));
        snapshot.Keep(x, z);
        var data = Compute(snapshot);
        var root = new Node3D { Name = $"Tree_{x}_{z}" };
        foreach (var (mesh, material, items, shadow) in data.Batches)
            if (material == Look.TreeBark || material == Look.TreeFoliage)
                AddMulti(root, mesh, items.Select(i => (i.Item1, i.Item2, new Color(0, 0, 0, i.Item3.A))).ToList(), material, shadow);
        if (root.GetChildCount() == 0) { root.Free(); return null; }
        // A tree near the player fades out of the camera's way and its see-through copy
        // draws it; with one tree, that copy can simply stay on.
        if (root.GetNodeOrNull<Node3D>(GhostsName) is { } ghosts) ghosts.Visible = true;
        return root;
    }

    // The trunk's radius at the foot, as TerrainCollision has it (birches by the water
    // are slim, the odd heath tree broad).
    public static float TrunkFoot(int x, int z)
    {
        float scale = 1.3f + Hash01(x, z, 15) * 0.6f, girth = 1f;
        if (Forest.Wetland(x, z) > 0.4f && Hash01(x, z, 23) < 0.8f) girth = 0.6f;
        else if (Forest.Heath(x, z) > 0.5f) girth = 1.35f;
        return TrunkRadius(scale * girth);
    }
}

// Felled trees (env-22). When a ChoppedTree row turns Felled the tree tips over away
// from whoever chopped it, falls with weight, bounces once and lies as a log for a
// few seconds before sinking away; a stump stays. Over the regrow time (Trees.RegrowSeconds)
// the stump rots away and a sapling comes up in its place and grows into a young tree, so when the server
// clears the row the full tree only has its last step to take. All client-side: the
// chunk draws the felled cell as cleared at once (Terrain.NoEase) and this draws the rest.
public partial class TreeFall : Node3D
{
    // The fall: tips for FallSeconds (the crown hits the ground then: Landed fires, and
    // Audio and Atmosphere time the crash and dust to it), bounces, lies, sinks.
    public const float FallSeconds = 1.6f;
    const float BounceSeconds = 0.35f, LieSeconds = 4f, SinkSeconds = 1.5f;
    const float FallAngle = 1.48f; // about 85 degrees: the crown props the trunk a little off the ground
    const float StumpHeight = 0.32f;

    // When a tree starts to fall (where its foot is, which way) and when it hits the ground.
    public static event Action<Vector3, Vector3>? Falling;
    public static event Action<Vector3, Vector3>? Landed;

    readonly Terrain terrain;
    readonly Dictionary<long, Felled> felled = new();
    // Cells regrown a moment ago: the chunk draws them back at once too.
    readonly Dictionary<long, float> regrown = new();
    DbConnection? conn;

    sealed class Felled
    {
        public int X, Z;
        public double ChoppedAt; // unix seconds
        public Node3D? Fall;     // pivot at the foot, while falling and lying
        public float FallStart;
        public Vector3 Axis;
        public Vector3 Direction;
        public Vector3 Foot;
        public float Radius;
        public bool HasLanded;
        public Node3D? Stump;
        public Node3D? Growth;   // the young tree, scaled about its foot
        public float Removing = -1f;
    }

    public TreeFall(Terrain terrain)
    {
        this.terrain = terrain;
        Name = "TreeFall";
        var before = terrain.NoEase;
        terrain.NoEase = (x, z) => before?.Invoke(x, z) == true || felled.ContainsKey(World.CellKey(x, z)) || regrown.ContainsKey(World.CellKey(x, z));
    }

    public void Attach(DbConnection conn)
    {
        this.conn = conn;
        conn.Db.ChoppedTree.OnInsert += (_, t) => Changed(t);
        conn.Db.ChoppedTree.OnUpdate += (_, _, t) => Changed(t);
        conn.Db.ChoppedTree.OnDelete += (_, t) => Regrown(t.CellKey);
    }

    void Changed(ChoppedTree t)
    {
        if (!t.Felled || felled.ContainsKey(t.CellKey)) return;
        double chopped = t.LastChop.MicrosecondsSinceUnixEpoch / 1e6;
        // Rows that were felled before we looked (joining, walking up to them) start
        // as a stump or a growing tree; only a fresh felling falls.
        bool fresh = Now - chopped < 10;
        Fell(t.CellX, t.CellZ, chopped, fresh ? ChopperNear(t.CellX, t.CellZ) : null, fresh);
    }

    static double Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    // Where the chopper stands: the nearest player to the trunk.
    Vector2? ChopperNear(int x, int z)
    {
        if (conn == null) return null;
        var (tx, tz) = Trees.Trunk(x, z);
        Vector2? best = null;
        float bestD = 6f;
        foreach (var p in conn.Db.PlayerPos.Iter())
        {
            float d = new Vector2(p.X - tx, p.Z - tz).Length();
            if (d < bestD) { bestD = d; best = new Vector2(p.X, p.Z); }
        }
        return best;
    }

    void Fell(int x, int z, double choppedAt, Vector2? chopper, bool fall)
    {
        long key = World.CellKey(x, z);
        var f = new Felled { X = x, Z = z, ChoppedAt = choppedAt };
        felled[key] = f;
        regrown.Remove(key);
        var (tx, tz) = Trees.Trunk(x, z);
        var foot = new Vector3(tx, terrain.TileHeight(x, z), tz);
        f.Stump = Stump(x, z, foot);
        AddChild(f.Stump);
        if (fall && terrain.StandingTree(x, z) is { } tree)
        {
            // Away from the chopper; with nobody near, the way the shader used to pick.
            Vector2 away = chopper is { } c && (new Vector2(tx, tz) - c).LengthSquared() > 0.01f
                ? (new Vector2(tx, tz) - c).Normalized()
                : Vector2.FromAngle(Terrain.Hash01(x, z, 73) * Mathf.Tau);
            f.Direction = new Vector3(away.X, 0, away.Y);
            f.Axis = Vector3.Up.Cross(f.Direction).Normalized();
            // It pivots at the top of the stump; what is below goes into the ground.
            f.Foot = foot;
            f.Radius = Terrain.TrunkFoot(x, z);
            var hinge = foot + Vector3.Up * StumpHeight;
            f.Fall = new Node3D { Position = hinge };
            tree.Position = -hinge;
            f.Fall.AddChild(tree);
            AddChild(f.Fall);
            f.FallStart = Look.Clock;
            Falling?.Invoke(foot, f.Direction);
        }
    }

    void Regrown(long key)
    {
        if (!felled.Remove(key, out var f)) return;
        regrown[key] = Look.Clock;
        // The chunk puts the full tree back on its next build; the young one waits a
        // moment so there is never a frame with neither.
        felled[key] = f;
        f.Removing = Look.Clock;
    }

    public override void _Process(double delta)
    {
        float now = Look.Clock;
        double wall = Now;
        List<long>? gone = null;
        foreach (var (key, f) in felled)
        {
            if (f.Removing >= 0f)
            {
                if (now - f.Removing > 0.6f) (gone ??= new()).Add(key);
                continue;
            }
            if (f.Fall != null) Animate(f, DebugPose is { } pose ? Mathf.Min(now - f.FallStart, pose) : now - f.FallStart);
            Grow(f, (float)((wall - f.ChoppedAt) / Trees.RegrowSeconds));
        }
        if (gone != null)
            foreach (var key in gone)
            {
                var f = felled[key];
                f.Fall?.QueueFree();
                f.Stump?.QueueFree();
                f.Growth?.QueueFree();
                felled.Remove(key);
            }
        foreach (var key in regrown.Where(r => now - r.Value > 10f).Select(r => r.Key).ToList()) regrown.Remove(key);
        if (DebugFell is { } debug && now > debug.At) { DebugFell = null; FellNearest(debug.X, debug.Z); }
        if (DebugStages is { } st && now > 2f) { DebugStages = null; ShowStages(st.X, st.Z); }
    }

    // Tips over slowly, then faster, as a tree does; bounces once off its crown, lies
    // still, then sinks away (its logs are already in the chopper's bag).
    void Animate(Felled f, float t)
    {
        float angle;
        if (t < FallSeconds)
        {
            float k = t / FallSeconds;
            angle = FallAngle * k * k * (0.6f + 0.4f * k);
        }
        else
        {
            if (!f.HasLanded)
            {
                f.HasLanded = true;
                Landed?.Invoke(f.Foot + f.Direction * 4f, f.Direction);
                if (DebugLog) GD.Print($"[treefall] landed at clock {Look.Clock:F2}");
            }
            float b = (t - FallSeconds) / BounceSeconds;
            angle = FallAngle - (b < 1f ? 0.09f * Mathf.Sin(Mathf.Pi * b) : 0f);
        }
        f.Fall!.Basis = new Basis(f.Axis, angle);
        // As it goes over, the butt slides off the stump and drops to the ground in
        // front of it, so the log lies beside the stump, not on it.
        float over = Mathf.Clamp(angle / FallAngle, 0f, 1f);
        over *= over;
        f.Fall.Position = f.Foot + Vector3.Up * Mathf.Lerp(StumpHeight, f.Radius * 0.8f, over)
            + f.Direction * (StumpHeight + f.Radius * 1.6f) * over;
        float sinkAt = FallSeconds + BounceSeconds + LieSeconds;
        if (t > sinkAt)
        {
            float s = Mathf.Clamp((t - sinkAt) / SinkSeconds, 0f, 1f);
            f.Fall.Basis = f.Fall.Basis.Scaled(Vector3.One * (1f - s * s));
            f.Fall.Position = f.Fall.Position with { Y = f.Fall.Position.Y - s * 0.6f };
            if (s >= 1f) { f.Fall.QueueFree(); f.Fall = null; }
        }
    }

    // Regrowth in stages over the regrow time: a stump alone, which rots away, then a
    // sapling in its place, a young tree, and a nearly full one.
    void Grow(Felled f, float p)
    {
        p = Mathf.Clamp(p, 0f, 1f);
        float size = p < 0.28f ? 0f
            : p < 0.5f ? Mathf.Lerp(0.12f, 0.4f, (p - 0.28f) / 0.22f)
            : p < 0.85f ? Mathf.Lerp(0.4f, 0.78f, (p - 0.5f) / 0.35f)
            : Mathf.Lerp(0.78f, 0.97f, (p - 0.85f) / 0.15f);
        if (size > 0f && f.Fall == null)
        {
            if (f.Growth == null && terrain.StandingTree(f.X, f.Z) is { } tree)
            {
                var (tx, tz) = Trees.Trunk(f.X, f.Z);
                var foot = new Vector3(tx, terrain.TileHeight(f.X, f.Z), tz);
                f.Growth = new Node3D { Position = foot };
                tree.Position = -foot;
                f.Growth.AddChild(tree);
                AddChild(f.Growth);
            }
            if (f.Growth != null) f.Growth.Scale = Vector3.One * size;
        }
        if (f.Stump != null)
        {
            // The stump rots down into the ground before the sapling comes up in its place.
            float weather = Mathf.Clamp((p - 0.14f) / 0.14f, 0f, 1f);
            f.Stump.Visible = weather < 1f;
            f.Stump.Scale = new Vector3(1f, 1f - weather, 1f);
        }
    }

    static ShaderMaterial? bark, cut;

    // A short stump at the foot with a pale cut face.
    Node3D Stump(int x, int z, Vector3 foot)
    {
        float r = Terrain.TrunkFoot(x, z) * 1.05f;
        bark ??= Look.Solid(Look.Bark);
        cut ??= Look.Solid(new Color(0.78f, 0.64f, 0.45f));
        var root = new Node3D { Position = foot, Name = $"Stump_{x}_{z}" };
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = r * 0.92f, BottomRadius = r * 1.15f, Height = StumpHeight + 0.15f, RadialSegments = 9, Rings = 1 },
            MaterialOverride = bark,
            Position = new Vector3(0, (StumpHeight - 0.15f) * 0.5f, 0),
        });
        // The cut leans a little, the side the tree fell from left higher.
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = r * 0.86f, BottomRadius = r * 0.86f, Height = 0.02f, RadialSegments = 9, Rings = 1 },
            MaterialOverride = cut,
            Position = new Vector3(0, StumpHeight + 0.005f, 0),
            Rotation = new Vector3(0.12f, Terrain.Hash01(x, z, 74) * Mathf.Tau, 0),
        });
        return root;
    }

    // --debug-fell=x,z[,seconds]: a player standing at (x, z) fells the nearest tree
    // after that many seconds (default 6), without a server: for renders of the fall.
    (float X, float Z, float At)? DebugFell;
    bool DebugLog;
    float? DebugPose;

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).ToList();
        if (args.FirstOrDefault(a => a.StartsWith("--debug-stages=")) is { } stages)
        {
            var at = stages["--debug-stages=".Length..].Split(',');
            if (at.Length >= 2 && float.TryParse(at[0], out float sx) && float.TryParse(at[1], out float sz)) DebugStages = (sx, sz);
        }
        // --debug-fall-pose=t holds every fall at t seconds, for stills.
        if (args.FirstOrDefault(a => a.StartsWith("--debug-fall-pose=")) is { } pose && float.TryParse(pose["--debug-fall-pose=".Length..], out float t)) DebugPose = t;
        var arg = args.FirstOrDefault(a => a.StartsWith("--debug-fell="));
        if (arg == null) return;
        var parts = arg["--debug-fell=".Length..].Split(',');
        if (parts.Length < 2 || !float.TryParse(parts[0], out float x) || !float.TryParse(parts[1], out float z)) return;
        float after = parts.Length > 2 && float.TryParse(parts[2], out float s) ? s : 6f;
        DebugFell = (x, z, Look.Clock + after);
    }

    // --debug-stages=x,z: the four trees nearest (x, z) as if felled a while ago, one at
    // each regrowth stage (stump, sapling, young, nearly full), for renders.
    (float X, float Z)? DebugStages;

    void ShowStages(float x, float z)
    {
        var cells = new List<(float D, int X, int Z)>();
        for (int i = (int)x - 14; i <= (int)x + 14; i++)
            for (int j = (int)z - 14; j <= (int)z + 14; j++)
                if (Trees.GrowsAt(i, j))
                {
                    var (tx, tz) = Trees.Trunk(i, j);
                    cells.Add((new Vector2(tx - x, tz - z).Length(), i, j));
                }
        var before = terrain.Cleared;
        var keys = new HashSet<long>();
        float[] stages = { 0.08f, 0.35f, 0.65f, 0.92f };
        foreach (var (stage, cell) in stages.Zip(cells.OrderBy(c => c.D).Take(4)))
        {
            Fell(cell.X, cell.Z, Now - stage * Trees.RegrowSeconds, null, false);
            keys.Add(World.CellKey(cell.X, cell.Z));
            terrain.Refresh(cell.X, cell.Z);
            GD.Print($"[treefall] stage {stage} at ({cell.X}, {cell.Z})");
        }
        terrain.Cleared = (i, j) => before?.Invoke(i, j) == true || keys.Contains(World.CellKey(i, j));
    }

    void FellNearest(float x, float z)
    {
        (int, int)? best = null;
        float bestD = 14f;
        for (int i = (int)x - 14; i <= (int)x + 14; i++)
            for (int j = (int)z - 14; j <= (int)z + 14; j++)
            {
                if (!Trees.GrowsAt(i, j) || felled.ContainsKey(World.CellKey(i, j))) continue;
                var (tx, tz) = Trees.Trunk(i, j);
                float d = new Vector2(tx - x, tz - z).Length();
                if (d < bestD) { bestD = d; best = (i, j); }
            }
        if (best is not { } cell) { GD.Print("[treefall] no tree near the debug chopper"); return; }
        GD.Print($"[treefall] felling {cell}, {bestD:F1} m from the chopper, at clock {Look.Clock:F2}");
        DebugLog = true;
        Fell(cell.Item1, cell.Item2, Now, new Vector2(x, z), true);
        var before = terrain.Cleared;
        long key = World.CellKey(cell.Item1, cell.Item2);
        terrain.Cleared = (i, j) => before?.Invoke(i, j) == true || World.CellKey(i, j) == key;
        terrain.Refresh(cell.Item1, cell.Item2);
    }
}
