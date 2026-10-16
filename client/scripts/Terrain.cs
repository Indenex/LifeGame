using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Builds the ground, trees, rocks and grass for the chunks around the player.
// The ground is a smooth heightfield with one vertex per one-meter cell, so
// digging a cell leaves a soft hollow. Everything comes from the shared seed;
// only dug cells come from the server.
public partial class Terrain : Node3D
{
    readonly Dictionary<(int, int), Node3D> chunks = new();
    readonly Dictionary<long, short> depths = new();
    readonly HashSet<(int, int)> dirty = new();
    (int, int) center = (int.MinValue, int.MinValue);

    static Mesh? trunkMesh, leafyMesh, pineMesh, rockMesh, tuftMesh;

    // Cells under buildings: no trees or grass, and the ground is trodden bare.
    public Func<int, int, bool>? Cleared;

    // When a cell is cleared or grows back, its grass flattens or regrows and the
    // ground color shifts over a second or two instead of popping (D-036).
    const float ClearFadeSeconds = 1.6f;
    readonly HashSet<long> wasCleared = new();
    readonly Dictionary<long, (float At, bool Cleared)> clearChanges = new();

    public void Refresh(int x, int z) => dirty.Add((World.ChunkOfCell(x), World.ChunkOfCell(z)));

    // Ground height at a world position, blended between the four nearest cells.
    public float HeightAt(float x, float z)
    {
        int x0 = (int)MathF.Floor(x), z0 = (int)MathF.Floor(z);
        float fx = x - x0, fz = z - z0;
        float a = Mathf.Lerp(TileHeight(x0, z0), TileHeight(x0 + 1, z0), fx);
        float b = Mathf.Lerp(TileHeight(x0, z0 + 1), TileHeight(x0 + 1, z0 + 1), fx);
        return Mathf.Lerp(a, b, fz);
    }

    public float TileHeight(int x, int z)
    {
        short depth = depths.TryGetValue(World.CellKey(x, z), out var d) ? d : (short)0;
        return World.Height(x, z, depth);
    }

    Vector3 NormalAt(int x, int z) =>
        new Vector3(TileHeight(x - 1, z) - TileHeight(x + 1, z), 2f, TileHeight(x, z - 1) - TileHeight(x, z + 1)).Normalized();

    public void SetDepth(int x, int z, short depth)
    {
        depths[World.CellKey(x, z)] = depth;
        int cx = World.ChunkOfCell(x), cz = World.ChunkOfCell(z);
        dirty.Add((cx, cz));
        // Neighboring chunks draw the side walls facing this tile.
        dirty.Add((World.ChunkOfCell(x - 1), cz));
        dirty.Add((World.ChunkOfCell(x + 1), cz));
        dirty.Add((cx, World.ChunkOfCell(z - 1)));
        dirty.Add((cx, World.ChunkOfCell(z + 1)));
    }

    // Loads chunks within view of (chunkX, chunkZ) and frees the ones that fell out.
    public void Focus(int chunkX, int chunkZ)
    {
        int r = World.ViewRadiusChunks;
        if (center != (chunkX, chunkZ))
        {
            center = (chunkX, chunkZ);
            var keep = new HashSet<(int, int)>();
            for (int cx = chunkX - r; cx <= chunkX + r; cx++)
                for (int cz = chunkZ - r; cz <= chunkZ + r; cz++)
                    keep.Add((cx, cz));

            foreach (var key in new List<(int, int)>(chunks.Keys))
                if (!keep.Contains(key))
                {
                    chunks[key].QueueFree();
                    chunks.Remove(key);
                }
            foreach (var key in keep)
                if (!chunks.ContainsKey(key))
                    dirty.Add(key);
        }

        // Build a few chunks per frame so walking never hitches. Nearest first.
        var order = new List<(int, int)>(dirty);
        order.Sort((a, b) => Dist(a).CompareTo(Dist(b)));
        int budget = 3;
        foreach (var key in order)
        {
            dirty.Remove(key);
            if (Math.Abs(key.Item1 - chunkX) > r || Math.Abs(key.Item2 - chunkZ) > r) continue;
            bool rebuilt = chunks.Remove(key, out var old);
            if (rebuilt) old!.QueueFree();
            NoteClearing(key.Item1, key.Item2, rebuilt);
            var node = BuildChunk(key.Item1, key.Item2);
            AddChild(node);
            chunks[key] = node;
            if (--budget <= 0) break;
        }

        int Dist((int, int) k) => Math.Abs(k.Item1 - chunkX) + Math.Abs(k.Item2 - chunkZ);
    }

    // Notes which cells of a chunk just became cleared or uncleared, so the rebuild
    // can ease them over. A chunk loading for the first time just records them.
    void NoteClearing(int cx, int cz, bool rebuilt)
    {
        float now = Look.Clock;
        int n = World.ChunkSize;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                int x = cx * n + i, z = cz * n + j;
                long key = World.CellKey(x, z);
                bool cleared = Cleared?.Invoke(x, z) == true;
                bool was = cleared ? !wasCleared.Add(key) : wasCleared.Remove(key);
                if (rebuilt && was != cleared) clearChanges[key] = (now, cleared);
                else if (clearChanges.TryGetValue(key, out var c) && now - c.At > ClearFadeSeconds + 1f) clearChanges.Remove(key);
            }
    }

    // When this cell's clearing changed, if it is still easing over.
    (float At, bool Cleared)? ClearChange(int x, int z) =>
        clearChanges.TryGetValue(World.CellKey(x, z), out var c) && Look.Clock - c.At < ClearFadeSeconds + 1f ? c : null;

    Node3D BuildChunk(int cx, int cz)
    {
        var root = new Node3D { Name = $"Chunk_{cx}_{cz}" };
        root.AddChild(new MeshInstance3D { Mesh = BuildGround(cx, cz), MaterialOverride = Look.Ground });
        AddVegetation(root, cx, cz);
        return root;
    }

    Mesh BuildGround(int cx, int cz)
    {
        int n = World.ChunkSize;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbaFloat);

        void Corner(int x, int z)
        {
            float h = TileHeight(x, z);
            st.SetNormal(NormalAt(x, z));
            st.SetColor(TileColor(x, z, h));
            // A cell whose clearing just changed carries its old color and when it
            // changed, and the ground shader blends from one to the other.
            st.SetCustom(0, ClearChange(x, z) is { } c
                ? TileColor(x, z, h, cleared: !c.Cleared) with { A = c.At }
                : new Color(0, 0, 0, 0));
            st.AddVertex(new Vector3(x, h, z));
        }

        // One vertex per cell; neighboring chunks share their edge row.
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                int x = cx * n + i, z = cz * n + j;
                Corner(x, z); Corner(x + 1, z); Corner(x + 1, z + 1);
                Corner(x, z); Corner(x + 1, z + 1); Corner(x, z + 1);
            }

        return st.Commit();
    }

    static void Quad(SurfaceTool st, Vector3 normal, Color color, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        foreach (var v in new[] { a, b, c, a, c, d })
        {
            st.SetNormal(normal);
            st.SetColor(color);
            st.AddVertex(v);
        }
    }

    Color TileColor(int x, int z, float top, bool? cleared = null)
    {
        float lush = World.Noise(x, z, 14f, 31);
        var color = Look.GrassLow.Lerp(Look.GrassHigh, Mathf.Clamp(lush * 1.1f - 0.1f, 0f, 1f));
        color = color.Lerp(Look.LeafWarm, Mathf.Clamp(World.Noise(x, z, 40f, 32) - 0.62f, 0f, 1f) * 0.5f);

        float clay = Mathf.SmoothStep(0.64f, 0.85f, World.ClayScore(x, z));
        color = color.Lerp(Look.Clay, clay * 0.35f);

        if (top < Look.WaterLevel + 0.2f) color = Look.Shore.Lerp(color, 0.2f);

        if (depths.TryGetValue(World.CellKey(x, z), out var d) && d > 0)
            color = World.IsClay(x, z) ? Look.Clay.Darkened(0.15f) : Look.DugEarth;

        if (cleared ?? Cleared?.Invoke(x, z) == true) color = color.Lerp(Look.Earth, 0.6f);

        return color.Lightened((Hash01(x, z, 7) - 0.5f) * 0.08f);
    }

    void AddVegetation(Node3D root, int cx, int cz)
    {
        EnsureMeshes();
        // Each instance: transform, color, and custom data saying when it started to
        // go away (or come back) on a cell that was just cleared.
        var trunks = new List<(Transform3D, Color, Color)>();
        var leafy = new List<(Transform3D, Color, Color)>();
        var pines = new List<(Transform3D, Color, Color)>();
        var rocks = new List<(Transform3D, Color, Color)>();
        var tufts = new List<(Transform3D, Color, Color)>();

        int n = World.ChunkSize;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                int x = cx * n + i, z = cz * n + j;
                // A cell cleared a moment ago keeps its grass, trees and rocks just long
                // enough for them to go: grass flattens, trees topple, rocks sink.
                var change = ClearChange(x, z);
                bool flattening = change is { Cleared: true };
                // Custom data: when the change started, and +1 going away or -1 coming back.
                var custom = change is { } c ? new Color(c.At, c.Cleared ? 1f : -1f, 0, 0) : new Color(0, 0, 0, 0);
                if (depths.ContainsKey(World.CellKey(x, z)) || (Cleared?.Invoke(x, z) == true && !flattening)) continue;
                float top = TileHeight(x, z);
                if (top < Look.WaterLevel + 0.1f) continue;

                bool nearSpawn = x * x + z * z < 12 * 12;
                float roll = Hash01(x, z, 11);
                float fx = x + (Hash01(x, z, 12) - 0.5f) * 0.5f, fz = z + (Hash01(x, z, 13) - 0.5f) * 0.5f;
                float yaw = Hash01(x, z, 14) * Mathf.Tau;
                float scale = 1.3f + Hash01(x, z, 15) * 0.6f;
                var basis = new Basis(Vector3.Up, yaw).Scaled(Vector3.One * scale);
                var at = new Transform3D(basis, new Vector3(fx, top, fz));

                // A forest that thins out into a clearing where new players wake up.
                float forest = World.IsClay(x, z) ? 0.02f : 0.03f + 0.10f * World.Noise(x, z, 24f, 33);
                if (!nearSpawn && roll < forest)
                {
                    trunks.Add((at, Look.Bark, custom));
                    if (Hash01(x, z, 16) < 0.4f)
                        pines.Add((at, Look.Pine.Lightened(Hash01(x, z, 17) * 0.1f), custom));
                    else
                        leafy.Add((at, Look.Leaf.Lerp(Look.LeafLight, Hash01(x, z, 18) * 0.35f)
                            .Lerp(Look.LeafWarm, Mathf.Max(0f, Hash01(x, z, 19) - 0.85f) * 2f), custom));
                }
                else if (roll < forest + 0.006f)
                {
                    rocks.Add((at.ScaledLocal(Vector3.One * 0.55f), Look.Rock.Darkened(Hash01(x, z, 20) * 0.15f), custom with { B = 1f }));
                    continue;
                }

                // A carpet of grass over each grassy tile, rooted in the tile's own color
                // so blades and ground read as one surface.
                bool grassy = top >= Look.WaterLevel + 0.2f && World.ClayScore(x, z) < 0.7f;
                if (grassy && Hash01(x, z, 40) < 0.93f)
                {
                    // Shear the patch onto the slope so every blade stays rooted.
                    float sx = (TileHeight(x + 1, z) - TileHeight(x - 1, z)) * 0.5f;
                    float sz = (TileHeight(x, z + 1) - TileHeight(x, z - 1)) * 0.5f;
                    var slope = new Basis(new Vector3(1, sx, 0), new Vector3(0, 0.75f + Hash01(x, z, 42) * 0.5f, 0), new Vector3(0, sz, 1));
                    var g = new Transform3D(slope, new Vector3(x, top, z));
                    tufts.Add((g, TileColor(x, z, top, cleared: false), custom));
                }
            }

        AddMulti(root, trunkMesh!, trunks, Look.TreeBark);
        AddMulti(root, leafyMesh!, leafy, Look.TreeFoliage);
        AddMulti(root, pineMesh!, pines, Look.TreeFoliage);
        AddMulti(root, rockMesh!, rocks, Look.RockSurface);
        AddMulti(root, tuftMesh!, tufts, Look.Grass, castShadow: false);
    }

    static void AddMulti(Node3D root, Mesh mesh, List<(Transform3D, Color, Color)> items, Material material, bool castShadow = true)
    {
        if (items.Count == 0) return;
        bool custom = items.Exists(item => item.Item3.R > 0f);
        var mm = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            UseCustomData = custom,
            Mesh = mesh,
            InstanceCount = items.Count,
        };
        for (int i = 0; i < items.Count; i++)
        {
            mm.SetInstanceTransform(i, items[i].Item1);
            mm.SetInstanceColor(i, items[i].Item2);
            if (custom) mm.SetInstanceCustomData(i, items[i].Item3);
        }
        root.AddChild(new MultiMeshInstance3D
        {
            Multimesh = mm,
            MaterialOverride = material,
            CastShadow = castShadow ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
        });
        // Trees in the way of the camera fade; their ghost draws them see-through.
        if (Look.GhostOf(material) is { } ghost)
            root.AddChild(new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = ghost, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }

    static void EnsureMeshes()
    {
        if (trunkMesh != null) return;

        var trunk = new SurfaceTool();
        trunk.Begin(Mesh.PrimitiveType.Triangles);
        Append(trunk, new CylinderMesh { TopRadius = 0.1f, BottomRadius = 0.2f, Height = 2.8f, RadialSegments = 7, Rings = 2 },
            Xf(0, 1.4f, 0), Colors.White);
        trunkMesh = trunk.Commit();

        // A broadleaf canopy built from clumps of leaves, lighter on top.
        var leafy = new SurfaceTool();
        leafy.Begin(Mesh.PrimitiveType.Triangles);
        Append(leafy, Blob(1.05f), Xf(0, 3.6f, 0), new Color(1.12f, 1.1f, 1f), 0.14f);
        for (int k = 0; k < 6; k++)
        {
            float a = k * Mathf.Tau / 6f + 0.4f;
            float r = 0.85f + 0.1f * (k % 2);
            Append(leafy, Blob(0.8f + 0.1f * (k % 3)), Xf(Mathf.Cos(a) * r, 3.0f + 0.15f * (k % 2), Mathf.Sin(a) * r),
                new Color(0.95f, 0.97f, 0.95f), 0.14f);
        }
        Append(leafy, Blob(0.75f), Xf(0.2f, 2.4f, -0.1f), new Color(0.8f, 0.82f, 0.85f), 0.12f);
        leafyMesh = leafy.Commit();

        // A pine of stacked tiers.
        var pine = new SurfaceTool();
        pine.Begin(Mesh.PrimitiveType.Triangles);
        float[] radii = { 1.25f, 1.0f, 0.75f, 0.45f };
        for (int k = 0; k < radii.Length; k++)
            Append(pine, new CylinderMesh { TopRadius = 0.02f, BottomRadius = radii[k], Height = 1.2f, RadialSegments = 10, Rings = 1 },
                Xf(0, 1.6f + k * 0.7f, 0), new Color(0.9f + k * 0.06f, 0.9f + k * 0.06f, 0.9f + k * 0.06f));
        pineMesh = pine.Commit();

        // A weathered boulder: lumpy, with flat facets that catch the light.
        var rock = new SurfaceTool();
        rock.Begin(Mesh.PrimitiveType.Triangles);
        Append(rock, new SphereMesh { Radius = 0.45f, Height = 0.6f, RadialSegments = 11, Rings = 6 }, Xf(0, 0.12f, 0), Colors.White, 0.09f);
        Append(rock, new SphereMesh { Radius = 0.28f, Height = 0.4f, RadialSegments = 9, Rings = 5 }, Xf(0.42f, 0.06f, 0.15f), Colors.White, 0.06f);
        rock.Deindex();
        rock.GenerateNormals();
        rockMesh = rock.Commit();

        tuftMesh = BuildTuft();
    }

    static SphereMesh Blob(float radius) => new() { Radius = radius, Height = radius * 1.7f, RadialSegments = 10, Rings = 6 };

    static Transform3D Xf(float x, float y, float z) => Transform3D.Identity.Translated(new Vector3(x, y, z));

    // Appends a primitive with its smooth normals, so the banded light gives soft, painted forms.
    static void Append(SurfaceTool target, PrimitiveMesh mesh, Transform3D offset, Color shade, float lumps = 0f)
    {
        var arrays = mesh.GetMeshArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
        var indices = (int[])arrays[(int)Mesh.ArrayType.Index];
        foreach (int idx in indices)
        {
            var v = offset * verts[idx];
            target.SetNormal(normals[idx]);
            target.SetColor(shade);
            target.AddVertex(v + normals[idx] * Lump(v) * lumps);
        }
    }

    // Smooth pseudo-random relief in 3D, the same at every shared vertex.
    static float Lump(Vector3 p) =>
        Mathf.Sin(p.X * 3.1f + p.Y * 1.7f) * Mathf.Sin(p.Z * 2.7f + p.Y * 2.3f) * 0.6f
        + Mathf.Sin(p.X * 7.3f - p.Z * 5.9f + p.Y * 4.1f) * 0.4f;

    // A one-meter patch of thin blades that fills its tile edge to edge. UV.y runs
    // from root to tip so the grass shader can color and bend each blade.
    static Mesh BuildTuft()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        const int grid = 10;
        for (int gx = 0; gx < grid; gx++)
            for (int gz = 0; gz < grid; gz++)
            {
                float px = (gx + 0.5f) / grid - 0.5f + (Hash01(gx, gz, 300) - 0.5f) / grid;
                float pz = (gz + 0.5f) / grid - 0.5f + (Hash01(gx, gz, 301) - 0.5f) / grid;
                float angle = Hash01(gx, gz, 302) * Mathf.Pi;
                float height = 0.2f + Hash01(gx, gz, 303) * 0.22f;
                float width = 0.05f + Hash01(gx, gz, 304) * 0.03f;
                var side = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * width;
                var lean = new Vector3(Hash01(gx, gz, 305) - 0.5f, 0, Hash01(gx, gz, 306) - 0.5f) * 0.12f;
                var root = new Vector3(px, -0.02f, pz);
                Vertex(st, root - side, 0f);
                Vertex(st, root + side, 0f);
                Vertex(st, root + lean + new Vector3(0, height, 0), 1f);
            }
        return st.Commit();

        static void Vertex(SurfaceTool st, Vector3 v, float h)
        {
            st.SetNormal(Vector3.Up);
            st.SetUV(new Vector2(0, h));
            st.SetColor(Colors.White);
            st.AddVertex(v);
        }
    }

    public static float Hash01(int x, int z, uint salt)
    {
        unchecked
        {
            uint h = (uint)x * 0x27D4EB2Du ^ (uint)z * 0x165667B1u ^ salt * 0x9E3779B9u;
            h ^= h >> 15;
            h *= 0x85EBCA6Bu;
            h ^= h >> 13;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }
}
