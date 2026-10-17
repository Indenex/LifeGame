using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Chunk building on worker threads (env-12). The noise, placement and instance math
// for a chunk runs on a background task against a snapshot of the little server
// state it needs (dug cells, cleared cells and their easing), taken on the main
// thread; the main thread then only turns the finished lists into nodes. Far views
// (env-9) fill in without the main thread paying about 9 ms a chunk.
public partial class Terrain
{
    // Background builds at once: enough to keep up with walking, leaving cores for the game.
    static readonly int Workers = Math.Clamp(System.Environment.ProcessorCount - 1, 1, 3);

    // Landmark pieces reach about 8 m from their cell (LandmarkProps), so dug cells are
    // snapshotted this far round the chunk.
    const int SnapshotMargin = 10;

    // What a worker needs from the main thread to build one chunk.
    sealed class Snapshot
    {
        public int Cx, Cz, MinX, MinZ, Size, CellMinX, CellMinZ, CellSize;
        public short[] Depth = Array.Empty<short>();
        public bool[] Edited = Array.Empty<bool>();
        public bool[] Cleared = Array.Empty<bool>();
        public (float At, bool Cleared)?[] Change = Array.Empty<(float, bool)?>();
        public bool Tufts;
        // Set for a one-cell build (TreeFall): only this cell's plants, drawn standing.
        public (int X, int Z)? Only;
        public readonly Dictionary<long, float> Heights = new();

        public void Keep(int x, int z)
        {
            Only = (x, z);
            Tufts = false;
            if (CellIndex(x, z) is var k and >= 0) { Cleared[k] = false; Change[k] = null; }
        }

        public short DepthAt(int x, int z)
        {
            int i = x - MinX, j = z - MinZ;
            return i >= 0 && j >= 0 && i < Size && j < Size ? Depth[i * Size + j] : (short)0;
        }

        // Whether the server has a dig record for the cell (even one filled back to 0).
        public bool EditedAt(int x, int z)
        {
            int i = x - MinX, j = z - MinZ;
            return i >= 0 && j >= 0 && i < Size && j < Size && Edited[i * Size + j];
        }

        public float TileHeight(int x, int z)
        {
            long key = World.CellKey(x, z);
            if (!Heights.TryGetValue(key, out float h)) Heights[key] = h = World.BaseHeight(x, z);
            return h - DepthAt(x, z) * World.DigStep;
        }

        int CellIndex(int x, int z)
        {
            int i = x - CellMinX, j = z - CellMinZ;
            return i >= 0 && j >= 0 && i < CellSize && j < CellSize ? i * CellSize + j : -1;
        }

        public bool IsCleared(int x, int z) => CellIndex(x, z) is var k and >= 0 && Cleared[k];
        public (float At, bool Cleared)? ClearChange(int x, int z) => CellIndex(x, z) is var k and >= 0 ? Change[k] : null;
    }

    // The snapshot the current thread is building from, if it is building a chunk.
    [ThreadStatic] static Snapshot? building;

    // What a build hands back: the ground's vertex arrays and the instance batches.
    sealed class ChunkData
    {
        public Vector3[] Vertices = Array.Empty<Vector3>(), Normals = Array.Empty<Vector3>();
        public Color[] Colors = Array.Empty<Color>();
        public float[] Custom = Array.Empty<float>();
        public bool Tufts;
        public readonly List<(Mesh Mesh, Material Material, List<(Transform3D, Color, Color)> Items, bool Shadow, (float, float) Range)> Batches = new();

        // range: the distances from the camera this batch shows between (0 for no limit).
        public void Add(Mesh mesh, List<(Transform3D, Color, Color)> items, Material material, bool castShadow = true, (float, float) range = default)
        {
            if (items.Count > 0) Batches.Add((mesh, material, items, castShadow, range));
        }
    }

    readonly Dictionary<(int, int), Task<ChunkData>> pending = new();

    // Everything a build touches that is made on first use (meshes, materials) is made
    // here on the main thread first, so workers only ever read it.
    static bool prewarmed;

    static void Prewarm()
    {
        if (prewarmed) return;
        prewarmed = true;
        EnsureMeshes();
        PaintedTrees.Prewarm();
        _ = Painted.GhostOf(Painted.Rock);
        _ = Painted.Bush;
        _ = Painted.Foliage;
        _ = PaintedBushes.Bush;
        _ = Painted.GhostOf(Painted.Bark);
        _ = Painted.GhostOf(Painted.Leaves);
        _ = PropMeshes.Trunk;
        for (int v = 0; v < PropMeshes.CattailVariants; v++) PropMeshes.CattailsOf(v);
        for (int v = 0; v < PropMeshes.RockVariants; v++) PropMeshes.Rock(v);
        for (int v = 0; v < PropMeshes.FallenLogVariants; v++) PropMeshes.FallenLog(v);
        for (int v = 0; v < PropMeshes.FallenGiantLogVariants; v++) PropMeshes.FallenGiantLog(v);
        for (int v = 0; v < PropMeshes.BirchVariants; v++) PropMeshes.Birch(v);
        for (int v = 0; v < PropMeshes.HeatherVariants; v++) PropMeshes.Heather(v);
        for (int v = 0; v < PropMeshes.GorseVariants; v++) PropMeshes.Gorse(v);
        for (int v = 0; v < PropMeshes.OutcropVariants; v++) PropMeshes.Outcrop(v);
        for (int v = 0; v < PropMeshes.RiverStoneVariants; v++) PropMeshes.RiverStone(v);
        for (int v = 0; v < Undergrowth.FernVariants; v++) Undergrowth.FernOf(v);
        _ = Undergrowth.Reed;
        _ = Undergrowth.Flowers;
        _ = Undergrowth.Bush;
        PaintedTerrain.Prewarm();
        foreach (var material in new Material[] { Look.Ground, Look.RockSurface, Look.Foliage, Look.TreeBark, Look.TreeFoliage, Look.Grass, Look.GrassTufts })
            _ = Look.GhostOf(material);
    }

    // On the main thread: what chunk (cx, cz) needs from the server and the clock.
    Snapshot TakeSnapshot(int cx, int cz)
    {
        int n = World.ChunkSize;
        var s = new Snapshot
        {
            Cx = cx, Cz = cz,
            MinX = cx * n - SnapshotMargin, MinZ = cz * n - SnapshotMargin, Size = n + 2 * SnapshotMargin + 1,
            // Cleared cells and their easing are read for the chunk's cells, the
            // ground's far corner row, and the few cells round it that soft edges
            // feather across (SoftEdges.FeatherCells).
            CellMinX = cx * n - FeatherCells, CellMinZ = cz * n - FeatherCells, CellSize = n + 1 + 2 * FeatherCells,
            Tufts = WantsTufts((cx, cz)),
        };
        s.Depth = new short[s.Size * s.Size];
        s.Edited = new bool[s.Size * s.Size];
        if (depths.Count > 0)
            for (int i = 0; i < s.Size; i++)
                for (int j = 0; j < s.Size; j++)
                    if (depths.TryGetValue(World.CellKey(s.MinX + i, s.MinZ + j), out var d))
                    {
                        s.Depth[i * s.Size + j] = d;
                        s.Edited[i * s.Size + j] = true;
                    }
        s.Cleared = new bool[s.CellSize * s.CellSize];
        s.Change = new (float, bool)?[s.CellSize * s.CellSize];
        for (int i = 0; i < s.CellSize; i++)
            for (int j = 0; j < s.CellSize; j++)
            {
                int x = s.CellMinX + i, z = s.CellMinZ + j;
                s.Cleared[i * s.CellSize + j] = Cleared?.Invoke(x, z) == true;
                s.Change[i * s.CellSize + j] = ClearChange(x, z);
            }
        return s;
    }

    // On any thread: the chunk's ground and instances, from the snapshot alone.
    ChunkData Compute(Snapshot snapshot)
    {
        var previous = building;
        building = snapshot;
        try
        {
            var data = new ChunkData { Tufts = snapshot.Tufts };
            if (snapshot.Only == null) BuildGround(data, snapshot.Cx, snapshot.Cz);
            AddVegetation(data, snapshot.Cx, snapshot.Cz);
            return data;
        }
        finally
        {
            building = previous;
        }
    }

    // On the main thread: the chunk's nodes from a finished build.
    Node3D Assemble(int cx, int cz, ChunkData data, bool plants = true)
    {
        var root = new Node3D { Name = $"Chunk_{cx}_{cz}" };
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.Vertices;
        arrays[(int)Mesh.ArrayType.Normal] = data.Normals;
        arrays[(int)Mesh.ArrayType.Color] = data.Colors;
        arrays[(int)Mesh.ArrayType.Custom0] = data.Custom;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays,
            flags: (Mesh.ArrayFormat)((long)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift));
        root.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = PaintedTerrain.Ground });
        if (plants) AddPlants(root, data);
        return root;
    }

    // The chunk's trees, rocks and plants under one child node, so they can move into a
    // far region's shared batches and back without rebuilding the chunk (env-24).
    static Node3D AddPlants(Node3D root, ChunkData data)
    {
        var holder = new Node3D { Name = PlantsName };
        root.AddChild(holder);
        foreach (var (batchMesh, material, items, shadow, range) in data.Batches)
            AddMulti(holder, batchMesh, items, material, shadow, range);
        return holder;
    }

    const string PlantsName = "Plants";

    // Builds a chunk right here, on the main thread (the bench).
    Node3D BuildChunk(int cx, int cz) => Assemble(cx, cz, Compute(TakeSnapshot(cx, cz)));

    // Puts finished builds in place, nearest first, within a few milliseconds, then
    // starts new ones for the nearest dirty chunks while workers are free.
    void PumpBuilds(int chunkX, int chunkZ, int r)
    {
        int Dist((int, int) k) => Math.Abs(k.Item1 - chunkX) + Math.Abs(k.Item2 - chunkZ);
        bool InRange((int, int) k) => Math.Abs(k.Item1 - chunkX) <= r && Math.Abs(k.Item2 - chunkZ) <= r;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        if (pending.Count > 0)
        {
            var done = new List<(int, int)>();
            foreach (var (key, task) in pending)
                if (task.IsCompleted) done.Add(key);
            done.Sort((a, b) => Dist(a).CompareTo(Dist(b)));
            foreach (var key in done)
            {
                if (clock.Elapsed.TotalMilliseconds > 3) break;
                var task = pending[key];
                pending.Remove(key);
                if (task.IsFaulted)
                {
                    GD.PushError($"chunk {key} failed to build: {task.Exception?.GetBaseException()}");
                    continue;
                }
                if (!InRange(key)) continue;
                if (chunks.Remove(key, out var old)) old.QueueFree();
                // A far chunk draws only its ground; its plants join its region's shared
                // batches (env-24).
                bool far = IsFar(key);
                var node = Assemble(key.Item1, key.Item2, task.Result, plants: !far);
                if (task.Result.Tufts) tufted.Add(key); else tufted.Remove(key);
                AddChild(node);
                chunks[key] = node;
                Placed(key, task.Result, far);
                if (node.GetNodeOrNull<Node3D>($"{PlantsName}/{GhostsName}") is { } ghosts) ghostHolders[key] = ghosts;
                else ghostHolders.Remove(key);
            }
        }

        if (dirty.Count == 0 || pending.Count >= Workers) return;
        Prewarm();
        var order = new List<(int, int)>(dirty);
        order.Sort((a, b) => Dist(a).CompareTo(Dist(b)));
        foreach (var key in order)
        {
            if (pending.Count >= Workers) break;
            // A chunk already building is started again once that build lands.
            if (pending.ContainsKey(key)) continue;
            dirty.Remove(key);
            if (!InRange(key)) continue;
            NoteClearing(key.Item1, key.Item2, chunks.ContainsKey(key));
            var snapshot = TakeSnapshot(key.Item1, key.Item2);
            pending[key] = Task.Run(() => Compute(snapshot));
        }
    }
}
