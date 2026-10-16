using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The land beyond the loaded chunks (env-9): a coarse ground mesh and simple tree
// silhouettes out to the chosen reach, so hills, heath tops, valleys and the woods read
// from a distance and the fog swallows them gently instead of the world ending at
// the last chunk. Everything comes from the shared height and forest rules, worked
// out on a background thread whenever the player moves into another chunk.
public partial class FarLand : Node3D
{
    // How far it reaches, from the Options page (Settings graphics/distant_land):
    // Far is the full view, Near a shorter, coarser one for slow PCs, Off none.
    // Far is limited by the water plane (260 m across, centred on the player), so a
    // far pond always has water in it.
    public enum Reach { Off, Near, Far }
    public static readonly string[] ReachNames = { "Off", "Near", "Far" };

    public static Reach Level
    {
        get => (Reach)Math.Clamp(Settings.Get("graphics", "distant_land", (int)Reach.Far), 0, 2);
        set => Settings.Set("graphics", "distant_land", (int)value);
    }

    static (int Radius, int Step) Shape(Reach level) => level == Reach.Near ? (96, 8) : (128, 4);

    MeshInstance3D? ground;
    MultiMeshInstance3D? broadleaf, conifer;
    (int, int, Reach) builtFor = (int.MinValue, int.MinValue, Reach.Off);
    Task<Built>? pending;

    static Mesh? blob, cone;

    sealed class Built
    {
        public Vector3[] Vertices = Array.Empty<Vector3>();
        public Vector3[] Normals = Array.Empty<Vector3>();
        public Color[] Colors = Array.Empty<Color>();
        public readonly List<(Transform3D, Color)> Broadleaf = new(), Conifer = new();
    }

    // Called every frame with the player's chunk; rebuilds when it changes, and puts a
    // finished build in place.
    public void Update(int chunkX, int chunkZ)
    {
        if (pending is { IsCompleted: true })
        {
            if (pending.Status == TaskStatus.RanToCompletion) Apply(pending.Result);
            pending = null;
        }
        var level = Level;
        Visible = level != Reach.Off;
        if (pending == null && level != Reach.Off && builtFor != (chunkX, chunkZ, level))
        {
            builtFor = (chunkX, chunkZ, level);
            var (radius, step) = Shape(level);
            int r = World.ViewRadiusChunks, n = World.ChunkSize;
            // The loaded chunks cover this square; the far ground leaves a hole there,
            // overlapping its edge by one step so no gap shows.
            var near = ((chunkX - r) * n + step, (chunkZ - r) * n + step, (chunkX + r + 1) * n - step, (chunkZ + r + 1) * n - step);
            int cx = chunkX * n + n / 2, cz = chunkZ * n + n / 2;
            pending = Task.Run(() => Build(cx, cz, near, radius, step));
        }
    }

    static Built Build(int cx, int cz, (int MinX, int MinZ, int MaxX, int MaxZ) near, int radius, int step)
    {
        var built = new Built();
        int ox = (cx - radius) / step * step, oz = (cz - radius) / step * step;
        int count = radius * 2 / step + 1;
        var heights = new float[count, count];
        var colors = new Color[count, count];
        for (int i = 0; i < count; i++)
            for (int j = 0; j < count; j++)
            {
                int x = ox + i * step, z = oz + j * step;
                heights[i, j] = World.BaseHeight(x, z);
                colors[i, j] = GroundColor(x, z, heights[i, j]);
            }

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var cols = new List<Color>();
        bool Inside(int x, int z) => x >= near.MinX && x <= near.MaxX && z >= near.MinZ && z <= near.MaxZ;
        Vector3 NormalAt(int i, int j)
        {
            float l = heights[Math.Max(i - 1, 0), j], r = heights[Math.Min(i + 1, count - 1), j];
            float d = heights[i, Math.Max(j - 1, 0)], u = heights[i, Math.Min(j + 1, count - 1)];
            return new Vector3(l - r, 2f * step, d - u).Normalized();
        }
        void Corner(int i, int j)
        {
            // A touch below the true ground, so where it slips under the loaded
            // chunks at the hole's edge it never shows through them.
            vertices.Add(new Vector3(ox + i * step, heights[i, j] - 0.3f, oz + j * step));
            normals.Add(NormalAt(i, j));
            cols.Add(colors[i, j]);
        }
        for (int i = 0; i < count - 1; i++)
            for (int j = 0; j < count - 1; j++)
            {
                int x = ox + i * step, z = oz + j * step;
                if (Inside(x, z) && Inside(x + step, z + step)) continue;
                float dx = x + step / 2 - cx, dz = z + step / 2 - cz;
                if (dx * dx + dz * dz > radius * radius) continue;
                Corner(i, j); Corner(i + 1, j); Corner(i + 1, j + 1);
                Corner(i, j); Corner(i + 1, j + 1); Corner(i, j + 1);

                // Tree silhouettes: one crown standing for the trees of this patch,
                // more likely and larger the denser the woods.
                float density = Forest.Density(x, z);
                float chance = Math.Min(1f, Forest.TreeChanceAt(x, z) * step * step * 0.55f);
                if (Inside(x, z) || Terrain.Hash01(x, z, 801) >= chance) continue;
                float px = x + Terrain.Hash01(x, z, 802) * step, pz = z + Terrain.Hash01(x, z, 803) * step;
                int pi = Math.Clamp((int)MathF.Round((px - ox) / step), 0, count - 1), pj = Math.Clamp((int)MathF.Round((pz - oz) / step), 0, count - 1);
                if (heights[pi, pj] < World.WaterLevel + 0.1f) continue;
                // On the coarser grid each crown stands for more trees, so it is a little larger.
                float scale = (1.3f + density * 0.9f + Terrain.Hash01(x, z, 804) * 0.5f) * (step > 4 ? 1.25f : 1f);
                var at = new Transform3D(new Basis(Vector3.Up, Terrain.Hash01(x, z, 805) * Mathf.Tau).Scaled(Vector3.One * scale), new Vector3(px, heights[pi, pj] - 0.2f, pz));
                bool pine = Terrain.Hash01(x, z, 806) < 0.4f && Forest.Wetland(x, z) < 0.4f && Forest.Heath(x, z) < 0.5f;
                if (pine) built.Conifer.Add((at, Look.Pine.Lightened(Terrain.Hash01(x, z, 807) * 0.08f)));
                else built.Broadleaf.Add((at, Look.Leaf.Lerp(Look.LeafLight, Terrain.Hash01(x, z, 808) * 0.3f)));
            }
        built.Vertices = vertices.ToArray();
        built.Normals = normals.ToArray();
        built.Colors = cols.ToArray();
        return built;
    }

    // The ground's colour from afar: the same grass, litter, heath, moss, clay and
    // shore tints the near terrain uses, without the per-cell detail.
    static Color GroundColor(int x, int z, float top)
    {
        float lush = World.Noise(x, z, 14f, 31);
        var color = Look.GrassLow.Lerp(Look.GrassHigh, Mathf.Clamp(lush * 1.1f - 0.1f, 0f, 1f));
        color = color.Lerp(Look.Earth.Lerp(Look.LeafWarm, 0.25f), Forest.Density(x, z) * 0.4f);
        color = color.Lerp(Look.HeathGround, Forest.Heath(x, z) * 0.45f);
        color = color.Lerp(Look.Moss, Forest.Wetland(x, z) * 0.4f);
        color = color.Lerp(Look.Clay, Mathf.SmoothStep(0.64f, 0.85f, World.ClayScore(x, z)) * 0.35f);
        if (top < Look.WaterLevel + 0.2f) color = Look.Shore.Lerp(color, 0.2f);
        return color;
    }

    void Apply(Built built)
    {
        EnsureMeshes();
        ground ??= Add(new MeshInstance3D { MaterialOverride = Look.Ground, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        if (built.Vertices.Length == 0)
            ground.Mesh = null;
        else
        {
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = built.Vertices;
            arrays[(int)Mesh.ArrayType.Normal] = built.Normals;
            arrays[(int)Mesh.ArrayType.Color] = built.Colors;
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            ground.Mesh = mesh;
        }
        broadleaf = Trees(broadleaf, blob!, built.Broadleaf);
        conifer = Trees(conifer, cone!, built.Conifer);
    }

    T Add<T>(T node) where T : Node
    {
        AddChild(node);
        return node;
    }

    MultiMeshInstance3D Trees(MultiMeshInstance3D? holder, Mesh mesh, List<(Transform3D, Color)> items)
    {
        holder ??= Add(new MultiMeshInstance3D { MaterialOverride = Look.Foliage, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh, InstanceCount = items.Count };
        for (int i = 0; i < items.Count; i++)
        {
            mm.SetInstanceTransform(i, items[i].Item1);
            mm.SetInstanceColor(i, items[i].Item2);
        }
        holder.Multimesh = mm;
        return holder;
    }

    // Low-detail crowns: a lumpy round canopy on a short stem, and a pine cone,
    // shaped and sized like the near trees so they read the same from afar.
    static void EnsureMeshes()
    {
        if (blob != null) return;
        var b = new SurfaceTool();
        b.Begin(Mesh.PrimitiveType.Triangles);
        Terrain.Append(b, new SphereMesh { Radius = 1.6f, Height = 2.6f, RadialSegments = 8, Rings = 4 }, Terrain.Xf(0, 3.3f, 0), new Color(1.05f, 1.05f, 1f), 0.2f);
        Terrain.Append(b, new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.18f, Height = 2.2f, RadialSegments = 5, Rings = 1 }, Terrain.Xf(0, 1.1f, 0), new Color(0.45f, 0.35f, 0.3f));
        blob = b.Commit();
        var c = new SurfaceTool();
        c.Begin(Mesh.PrimitiveType.Triangles);
        Terrain.Append(c, new CylinderMesh { TopRadius = 0.02f, BottomRadius = 1.2f, Height = 3.4f, RadialSegments = 7, Rings = 1 }, Terrain.Xf(0, 2.9f, 0), Colors.White);
        Terrain.Append(c, new CylinderMesh { TopRadius = 0.1f, BottomRadius = 0.16f, Height = 1.4f, RadialSegments = 5, Rings = 1 }, Terrain.Xf(0, 0.7f, 0), new Color(0.45f, 0.35f, 0.3f));
        cone = c.Commit();
    }
}
