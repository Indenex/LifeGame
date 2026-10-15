using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Builds the ground, trees, rocks and grass for the chunks around the player.
// Everything is generated from the shared seed, so every client sees the same
// world; only dug height points come from the server.
public partial class Terrain : Node3D
{
    readonly Dictionary<(int, int), Node3D> chunks = new();
    readonly Dictionary<long, short> depths = new();
    readonly HashSet<(int, int)> dirty = new();
    (int, int) center = (int.MinValue, int.MinValue);

    static Mesh? trunkMesh, pineMesh, roundMesh, rockMesh, tuftMesh;

    public float HeightAt(float x, float z)
    {
        int x0 = (int)MathF.Floor(x), z0 = (int)MathF.Floor(z);
        float tx = x - x0, tz = z - z0;
        float a = PointHeight(x0, z0), b = PointHeight(x0 + 1, z0);
        float c = PointHeight(x0, z0 + 1), d = PointHeight(x0 + 1, z0 + 1);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
    }

    public float PointHeight(int x, int z) =>
        World.Height(x, z, depths.TryGetValue(World.CellKey(x, z), out var d) ? d : (short)0);

    public void SetDepth(int x, int z, short depth)
    {
        depths[World.CellKey(x, z)] = depth;
        int cx = World.ChunkOfCell(x), cz = World.ChunkOfCell(z);
        dirty.Add((cx, cz));
        // A height point on a chunk edge is shared with the neighbor.
        if (x == cx * World.ChunkSize) dirty.Add((cx - 1, cz));
        if (z == cz * World.ChunkSize) dirty.Add((cx, cz - 1));
        if (x == cx * World.ChunkSize && z == cz * World.ChunkSize) dirty.Add((cx - 1, cz - 1));
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

        // Build a few chunks per frame so walking never hitches.
        int budget = 3;
        foreach (var key in new List<(int, int)>(dirty))
        {
            if (Math.Abs(key.Item1 - chunkX) > r || Math.Abs(key.Item2 - chunkZ) > r)
            {
                dirty.Remove(key);
                continue;
            }
            if (budget-- <= 0) break;
            dirty.Remove(key);
            if (chunks.Remove(key, out var old)) old.QueueFree();
            var node = BuildChunk(key.Item1, key.Item2);
            AddChild(node);
            chunks[key] = node;
        }
    }

    Node3D BuildChunk(int cx, int cz)
    {
        var root = new Node3D { Name = $"Chunk_{cx}_{cz}" };
        root.AddChild(new MeshInstance3D { Mesh = BuildGround(cx, cz), MaterialOverride = Look.VertexColored });
        AddVegetation(root, cx, cz);
        return root;
    }

    Mesh BuildGround(int cx, int cz)
    {
        int n = World.ChunkSize;
        int ox = cx * n, oz = cz * n;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                int x = ox + i, z = oz + j;
                var p00 = Vertex(x, z);
                var p10 = Vertex(x + 1, z);
                var p01 = Vertex(x, z + 1);
                var p11 = Vertex(x + 1, z + 1);
                // Alternate the diagonal for a less gridded, hand-made look.
                bool flip = ((x ^ z) & 1) == 0;
                if (flip)
                {
                    Triangle(st, p00, p10, p11, x, z);
                    Triangle(st, p00, p11, p01, x, z);
                }
                else
                {
                    Triangle(st, p00, p10, p01, x, z);
                    Triangle(st, p10, p11, p01, x, z);
                }
            }

        return st.Commit();
    }

    Vector3 Vertex(int x, int z) => new(x * World.CellSize, PointHeight(x, z), z * World.CellSize);

    void Triangle(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, int x, int z)
    {
        // Flat normals give the faceted look; colors are per vertex so they blend softly.
        var normal = (b - a).Cross(c - a).Normalized();
        if (normal.Y < 0) normal = -normal;
        float shade = (1f - normal.Y) * 0.6f;
        foreach (var v in new[] { a, b, c })
        {
            st.SetNormal(normal);
            st.SetColor(GroundColor((int)MathF.Round(v.X), (int)MathF.Round(v.Z), v.Y).Darkened(shade));
            st.AddVertex(v);
        }
    }

    Color GroundColor(int x, int z, float h)
    {
        float lush = World.Noise(x, z, 18f, 31);
        var color = Look.GrassLow.Lerp(Look.GrassHigh, Mathf.Clamp(lush * 0.8f + (h + 0.5f) * 0.15f, 0f, 1f));
        color = color.Lerp(Look.LeafWarm, Mathf.Clamp(World.Noise(x, z, 40f, 32) - 0.6f, 0f, 1f) * 0.25f);

        float clay = Mathf.SmoothStep(0.64f, 0.85f, World.ClayScore(x, z));
        color = color.Lerp(Look.Clay, clay * 0.5f);

        float shore = Mathf.SmoothStep(Look.WaterLevel + 0.35f, Look.WaterLevel + 0.05f, h);
        color = color.Lerp(Look.Shore, shore);

        if (depths.TryGetValue(World.CellKey(x, z), out var d) && d > 0)
            color = (World.IsClay(x, z) ? Look.Clay.Darkened(0.2f) : Look.DugEarth).Lerp(color, 0.15f);

        return color.Lightened((Hash01(x, z, 7) - 0.5f) * 0.04f);
    }

    void AddVegetation(Node3D root, int cx, int cz)
    {
        EnsureMeshes();
        var trunks = new List<(Transform3D, Color)>();
        var pines = new List<(Transform3D, Color)>();
        var rounds = new List<(Transform3D, Color)>();
        var rocks = new List<(Transform3D, Color)>();
        var tufts = new List<(Transform3D, Color)>();

        int n = World.ChunkSize;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                int x = cx * n + i, z = cz * n + j;
                float h = World.BaseHeight(x, z);
                if (h < Look.WaterLevel + 0.3f) continue;
                bool nearSpawn = x * x + z * z < 11 * 11;
                float roll = Hash01(x, z, 11);
                float fx = x + Hash01(x, z, 12) * 0.8f, fz = z + Hash01(x, z, 13) * 0.8f;
                float y = HeightAt(fx, fz);
                float yaw = Hash01(x, z, 14) * Mathf.Tau;
                float scale = 0.6f + Hash01(x, z, 15) * 0.45f;
                var basis = new Basis(Vector3.Up, yaw).Scaled(Vector3.One * scale);
                var at = new Transform3D(basis, new Vector3(fx, y - 0.05f, fz));

                // A forest that thins out into a clearing where new players wake up.
                float forest = World.IsClay(x, z) ? 0.03f : 0.05f + 0.12f * World.Noise(x, z, 24f, 33);
                if (!nearSpawn && roll < forest)
                {
                    trunks.Add((at, Look.Bark));
                    bool pine = Hash01(x, z, 16) < 0.55f;
                    var canopy = at.TranslatedLocal(new Vector3(0, pine ? 1.0f : 1.9f, 0));
                    if (pine) pines.Add((canopy, Look.Pine.Lightened(Hash01(x, z, 17) * 0.12f)));
                    else rounds.Add((canopy, Look.Leaf.Lerp(Look.LeafWarm, Hash01(x, z, 18) * 0.5f)));
                }
                else if (roll < forest + 0.004f)
                {
                    var flat = new Transform3D(basis.Scaled(new Vector3(0.8f, 0.45f, 0.8f)), at.Origin);
                    rocks.Add((flat, Look.Rock.Lerp(Look.Pine, 0.15f).Darkened(Hash01(x, z, 19) * 0.2f)));
                }
                else if (roll < 0.45f)
                {
                    tufts.Add((at, Look.GrassHigh.Lightened(0.12f).Lerp(Look.LeafWarm, Hash01(x, z, 20) * 0.25f)));
                }
            }

        AddMulti(root, trunkMesh!, trunks, Look.VertexColored);
        AddMulti(root, pineMesh!, pines, Look.Swaying);
        AddMulti(root, roundMesh!, rounds, Look.Swaying);
        AddMulti(root, rockMesh!, rocks, Look.VertexColored);
        AddMulti(root, tuftMesh!, tufts, Look.Grass);
    }

    static void AddMulti(Node3D root, Mesh mesh, List<(Transform3D, Color)> items, Material material)
    {
        if (items.Count == 0) return;
        var mm = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = items.Count,
        };
        for (int i = 0; i < items.Count; i++)
        {
            mm.SetInstanceTransform(i, items[i].Item1);
            mm.SetInstanceColor(i, items[i].Item2);
        }
        root.AddChild(new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = material });
    }

    static void EnsureMeshes()
    {
        if (trunkMesh != null) return;
        trunkMesh = Facet(new CylinderMesh { TopRadius = 0.09f, BottomRadius = 0.16f, Height = 2.2f, RadialSegments = 6, Rings = 1 },
            Transform3D.Identity.Translated(new Vector3(0, 1.1f, 0)));
        var pine = new SurfaceTool();
        pine.Begin(Mesh.PrimitiveType.Triangles);
        AppendFaceted(pine, new CylinderMesh { TopRadius = 0f, BottomRadius = 1.1f, Height = 1.8f, RadialSegments = 7, Rings = 1 },
            Transform3D.Identity.Translated(new Vector3(0, 0.9f, 0)));
        AppendFaceted(pine, new CylinderMesh { TopRadius = 0f, BottomRadius = 0.8f, Height = 1.5f, RadialSegments = 7, Rings = 1 },
            Transform3D.Identity.Translated(new Vector3(0, 2.0f, 0)));
        pineMesh = pine.Commit();
        roundMesh = Facet(new SphereMesh { Radius = 1.25f, Height = 2.1f, RadialSegments = 8, Rings = 5 },
            Transform3D.Identity.Translated(new Vector3(0, 0.6f, 0)));
        rockMesh = Facet(new SphereMesh { Radius = 0.45f, Height = 0.7f, RadialSegments = 6, Rings = 3 }, Transform3D.Identity);
        tuftMesh = BuildTuft();
    }

    static Mesh Facet(PrimitiveMesh mesh, Transform3D offset)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        AppendFaceted(st, mesh, offset);
        return st.Commit();
    }

    static void AppendFaceted(SurfaceTool target, PrimitiveMesh mesh, Transform3D offset)
    {
        var arrays = mesh.GetMeshArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        var indices = (int[])arrays[(int)Mesh.ArrayType.Index];
        for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            var a = offset * verts[indices[i]];
            var b = offset * verts[indices[i + 1]];
            var c = offset * verts[indices[i + 2]];
            var normal = (b - a).Cross(c - a).Normalized();
            if (normal == Vector3.Zero) continue;
            target.SetNormal(-normal);
            target.SetColor(Colors.White);
            target.AddVertex(a);
            target.AddVertex(b);
            target.AddVertex(c);
        }
    }

    // A tuft of five blades, darker at the root.
    static Mesh BuildTuft()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int k = 0; k < 5; k++)
        {
            float angle = k * Mathf.Tau / 5f;
            var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var side = new Vector3(-dir.Z, 0, dir.X) * 0.035f;
            var root = dir * 0.05f;
            var tip = dir * 0.16f + new Vector3(0, 0.38f + 0.06f * (k % 2), 0);
            st.SetNormal(Vector3.Up);
            st.SetColor(new Color(0.55f, 0.55f, 0.55f));
            st.AddVertex(root - side);
            st.SetColor(new Color(0.55f, 0.55f, 0.55f));
            st.AddVertex(root + side);
            st.SetColor(Colors.White);
            st.AddVertex(tip);
        }
        return st.Commit();
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
