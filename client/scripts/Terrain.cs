using System;
using System.Collections.Generic;
using System.Linq;
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
    // Each chunk's node of see-through tree copies, looked up once when it is built.
    readonly Dictionary<(int, int), Node3D> ghostHolders = new();
    readonly Dictionary<long, short> depths = new();
    readonly HashSet<(int, int)> dirty = new();
    (int, int) center = (int.MinValue, int.MinValue);

    static Mesh? trunkMesh, leafyMesh, pineMesh, tuftMesh;

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
        if (building is { } snapshot) return snapshot.TileHeight(x, z);
        long key = World.CellKey(x, z);
        short depth = depths.TryGetValue(key, out var d) ? d : (short)0;
        return BaseHeight(key, x, z) - depth * World.DigStep;
    }

    // The land's shape costs a dozen noise lookups a cell (shared/Land.cs) and a chunk
    // asks for each cell many times, so heights are kept once worked out. The cache
    // is dropped when it outgrows about two views' worth of ground at the current
    // view distance (Very far draws about 113,000 cells).
    readonly Dictionary<long, float> baseHeights = new();

    float BaseHeight(long key, int x, int z)
    {
        if (baseHeights.TryGetValue(key, out var h)) return h;
        int side = (2 * Math.Max(focusRadius, 3) + 1) * World.ChunkSize + 2;
        if (baseHeights.Count > Math.Max(60_000, 2 * side * side)) baseHeights.Clear();
        return baseHeights[key] = World.BaseHeight(x, z);
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

    // The land past the loaded chunks, drawn coarse so hills and woods read from afar.
    int focusRadius = -1;
    // Chunks built with grass tufts (those within ViewDistance.GrassChunks when built).
    readonly HashSet<(int, int)> tufted = new();

    // How many chunks out from the player's are drawn in full (ViewDistance).
    public static int ViewRadius => ViewDistance.Radius;

    // Loads chunks within view of (chunkX, chunkZ) and frees the ones that fell out.
    public void Focus(int chunkX, int chunkZ)
    {
        if (BenchWanted) { RunViewBench(chunkX, chunkZ); return; }

        int r = ViewRadius;
        if (r != focusRadius)
        {
            ViewDistance.TellFog(r);
            // Dug cells, felled trees and buildings arrive out to the drawn edge (#339).
            Subscriptions.FarRadius = r;
        }
        if (center != (chunkX, chunkZ) || r != focusRadius)
        {
            center = (chunkX, chunkZ);
            focusRadius = r;
            var keep = new HashSet<(int, int)>();
            for (int cx = chunkX - r; cx <= chunkX + r; cx++)
                for (int cz = chunkZ - r; cz <= chunkZ + r; cz++)
                    keep.Add((cx, cz));

            foreach (var key in new List<(int, int)>(chunks.Keys))
                if (!keep.Contains(key))
                {
                    chunks[key].QueueFree();
                    chunks.Remove(key);
                    tufted.Remove(key);
                    ghostHolders.Remove(key);
                }
            foreach (var key in keep)
                if (!chunks.ContainsKey(key) || tufted.Contains(key) != WantsTufts(key))
                    dirty.Add(key);
        }

        // Chunks build on worker threads, nearest first (env-12); the main thread only
        // snapshots what they need and puts finished ones in place.
        if (dirty.Count == 0 && pending.Count == 0) return;
        loading ??= System.Diagnostics.Stopwatch.StartNew();
        PumpBuilds(chunkX, chunkZ, r);
        if (dirty.Count == 0 && pending.Count == 0)
        {
            GD.Print($"[view] radius {r}: {chunks.Count} chunks in place {loading.Elapsed.TotalMilliseconds:F0} ms after the first was asked for");
            loading = null;
            if (LoadedShot is { } shot) TakeLoadedShot(shot);
        }
    }

    System.Diagnostics.Stopwatch? loading;

    // --screenshot-when-loaded=path.png saves one frame once every chunk in view is in
    // place, then quits: for renders of the far view, where a fixed delay may fire
    // before the outer chunks land (slow software renderers build only a few a frame).
    static readonly string? LoadedShot = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs())
        .FirstOrDefault(a => a.StartsWith("--screenshot-when-loaded="))?["--screenshot-when-loaded=".Length..];

    async void TakeLoadedShot(string path)
    {
        // A few frames for the last chunks to draw and the fade to settle.
        for (int i = 0; i < 4; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"[view] saved {path}");
        GetTree().Quit();
    }

    // Grass tufts only near the player: they fade out before ViewDistance.GrassChunks,
    // so a chunk gaining or losing them on a rebuild never shows.
    bool WantsTufts((int, int) key) =>
        Math.Max(Math.Abs(key.Item1 - center.Item1), Math.Abs(key.Item2 - center.Item2)) <= ViewDistance.GrassChunks;

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
        building is { } snapshot ? snapshot.ClearChange(x, z) :
        clearChanges.TryGetValue(World.CellKey(x, z), out var c) && Look.Clock - c.At < ClearFadeSeconds + 1f ? c : null;

    // Whether a cell is under a building (from the snapshot on a worker).
    bool IsCleared(int x, int z) => building is { } snapshot ? snapshot.IsCleared(x, z) : Cleared?.Invoke(x, z) == true;

    bool Edited(int x, int z) => building is { } snapshot ? snapshot.EditedAt(x, z) : depths.ContainsKey(World.CellKey(x, z));

    short Depth(int x, int z) =>
        building is { } snapshot ? snapshot.DepthAt(x, z) : depths.TryGetValue(World.CellKey(x, z), out var d) ? d : (short)0;

    void BuildGround(ChunkData data, int cx, int cz)
    {
        int n = World.ChunkSize;

        // Each corner is shared by up to six triangles, so work out its height,
        // normal and colors once, then lay the triangles out from the grid.
        var heights = new float[n + 1, n + 1];
        var normals = new Vector3[n + 1, n + 1];
        var colors = new Color[n + 1, n + 1];
        var changes = new Color[n + 1, n + 1];
        for (int i = 0; i <= n; i++)
            for (int j = 0; j <= n; j++)
            {
                int x = cx * n + i, z = cz * n + j;
                float h = TileHeight(x, z);
                heights[i, j] = h;
                normals[i, j] = NormalAt(x, z);
                colors[i, j] = UnderGrass(x, z, h, TileColor(x, z, h));
                // A cell whose clearing just changed carries its old color and when it
                // changed, and the ground shader blends from one to the other.
                changes[i, j] = ClearChange(x, z) is { } c
                    ? UnderGrass(x, z, h, TileColor(x, z, h, cleared: !c.Cleared)) with { A = c.At }
                    : new Color(0, 0, 0, 0);
            }

        var vertices = new Vector3[n * n * 6];
        var vertexNormals = new Vector3[vertices.Length];
        var vertexColors = new Color[vertices.Length];
        var custom = new float[vertices.Length * 4];
        int k = 0;
        void Corner(int i, int j)
        {
            vertices[k] = new Vector3(cx * n + i, heights[i, j], cz * n + j);
            vertexNormals[k] = normals[i, j];
            vertexColors[k] = colors[i, j];
            var c = changes[i, j];
            custom[k * 4] = c.R; custom[k * 4 + 1] = c.G; custom[k * 4 + 2] = c.B; custom[k * 4 + 3] = c.A;
            k++;
        }

        // One vertex per cell; neighboring chunks share their edge row.
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                Corner(i, j); Corner(i + 1, j); Corner(i + 1, j + 1);
                Corner(i, j); Corner(i + 1, j + 1); Corner(i, j + 1);
            }

        data.Vertices = vertices;
        data.Normals = vertexNormals;
        data.Colors = vertexColors;
        data.Custom = custom;
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

        // Under the trees the grass gives way to leaf litter; on the hilltops to
        // heath, by the water to moss.
        color = color.Lerp(LeafLitter, Forest.Density(x, z) * 0.4f);
        color = color.Lerp(Look.HeathGround, Forest.Heath(x, z) * 0.45f);
        color = color.Lerp(Look.Moss, Forest.Wetland(x, z) * 0.4f);
        // Deer trails wear the ground bare, some stretches more than others.
        color = color.Lerp(Look.Earth, Landmarks.Trail(x, z) * (0.25f + 0.35f * World.Noise(x + 733, z - 1291, 20f, 93)));

        float clay = Mathf.SmoothStep(0.64f, 0.85f, World.ClayScore(x, z));
        color = color.Lerp(Look.Clay, clay * 0.35f);

        if (top < Look.WaterLevel + 0.2f) color = Look.Shore.Lerp(color, 0.2f);

        if (Depth(x, z) > 0)
            color = World.IsClay(x, z) ? Look.Clay.Darkened(0.15f) : Look.DugEarth;

        if (cleared ?? IsCleared(x, z)) color = color.Lerp(Look.Earth, 0.6f);

        return color.Lightened((Hash01(x, z, 7) - 0.5f) * 0.08f);
    }

    // How strongly the ground under the grass leans toward the tufts' own olive
    // (env-18), so the odd cell without a tuft doesn't show as a lighter square.
    const float UnderGrassMatch = 0.7f;

    // The ground's color where grass grows, toward the tufts' muted olive in proportion
    // to how much grass the cell is likely to carry: full in the open meadow, fading
    // under the trees, on trails and wet ground. Mirrors Look's grass shader (stylised).
    Color UnderGrass(int x, int z, float top, Color color)
    {
        if (top < Look.WaterLevel + 0.2f || World.ClayScore(x, z) >= 0.7f || Depth(x, z) > 0) return color;
        float cover = Mathf.Clamp(0.93f - 0.5f * Forest.Density(x, z) - 0.5f * Forest.Wetland(x, z) - 0.6f * Landmarks.Trail(x, z), 0f, 1f);
        var lin = color.SrgbToLinear();
        float lum = lin.R * 0.3f + lin.G * 0.59f + lin.B * 0.11f;
        var olive = new Color(
            Mathf.Lerp(lin.R, lum * 1.1f, 0.3f),
            Mathf.Lerp(lin.G, lum * 1.05f, 0.3f),
            Mathf.Lerp(lin.B, lum * 0.55f, 0.3f)) * 0.9f;
        return lin.Lerp(olive with { A = lin.A }, cover * UnderGrassMatch).LinearToSrgb();
    }

    static readonly Color LeafLitter = Look.Earth.Lerp(Look.LeafWarm, 0.25f);

    // A dry cell within two meters of open water, where reeds and stones gather.
    bool NearWater(int x, int z, float top)
    {
        if (top > Look.WaterLevel + 0.5f) return false;
        for (int dx = -2; dx <= 2; dx++)
            for (int dz = -2; dz <= 2; dz++)
                if (TileHeight(x + dx, z + dz) < Look.WaterLevel) return true;
        return false;
    }

    void AddVegetation(ChunkData data, int cx, int cz)
    {
        EnsureMeshes();
        // Each instance: transform, color, and custom data saying when it started to
        // go away (or come back) on a cell that was just cleared.
        var trunks = new List<(Transform3D, Color, Color)>();
        var leafy = new List<(Transform3D, Color, Color)>();
        var pines = new List<(Transform3D, Color, Color)>();
        // Grass tufts only in the chunks near the player (WantsTufts).
        var tufts = data.Tufts ? new List<(Transform3D, Color, Color)>() : null;
        var ferns = new List<(Transform3D, Color, Color)>();
        var reeds = new List<(Transform3D, Color, Color)>();
        var flowers = new List<(Transform3D, Color, Color)>();
        var bushes = new List<(Transform3D, Color, Color)>();
        var landmarks = new LandmarkProps.Lists();
        var painted = new Dictionary<(bool Pine, int Variant), List<(Transform3D, Color, Color)>>();
        for (int v = 0; v < PaintedTrees.Variants; v++)
        {
            painted[(true, v)] = new();
            painted[(false, v)] = new();
        }

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
                if (Edited(x, z) || (IsCleared(x, z) && !flattening)) continue;
                float top = TileHeight(x, z);
                var (fx, fz) = Trees.Trunk(x, z);
                float yaw = Hash01(x, z, 14) * Mathf.Tau;
                // Reeds stand in the shallows and along the bank, leaving the middle
                // of a stream open.
                if (top < Look.WaterLevel + 0.1f)
                {
                    // Features in the water (shared/Features.cs): stepping stones or a
                    // fallen log mark an easy place to ford a stream.
                    if (Features.At(x, z, FeatureGround.Stream) is { } wading)
                        LandmarkProps.For(wading).Build(x, z, new Vector3(fx, top, fz), HeightAt, landmarks, custom);
                    else if (top > Look.WaterLevel - 0.15f && Hash01(x, z, 60) < 0.4f)
                    {
                        var reed = (Plant(fx, top, fz, yaw, 0.8f + Hash01(x, z, 61) * 0.5f), Look.LeafLight.Lerp(Look.LeafWarm, Hash01(x, z, 62) * 0.3f), custom);
                        // Some clumps are cattails, brown heads above the blades.
                        if (Hash01(x, z, 63) < 0.3f) landmarks.Put(PropMeshes.Cattails, Look.Grass, reed);
                        else reeds.Add(reed);
                    }
                    continue;
                }
                bool shore = NearWater(x, z, top);

                bool nearSpawn = Trees.NearSpawn(x, z);
                float roll = Hash01(x, z, 11);
                float scale = 1.3f + Hash01(x, z, 15) * 0.6f;
                var basis = new Basis(Vector3.Up, yaw).Scaled(Vector3.One * scale);
                var at = new Transform3D(basis, new Vector3(fx, top, fz));

                // Groves with glades between them, thinning into the clearing where new
                // players wake up (shared/Forest.cs). Full-grown trees follow
                // shared/Trees.cs, so the server knows which tree is chopped; saplings
                // are only scenery.
                float density = Forest.Density(x, z);
                float edge = Forest.Edge(density);
                // Heath on the hilltops, wetland by the water (env-4).
                float heath = Forest.Heath(x, z), wet = Forest.Wetland(x, z);
                float tree = Forest.TreeChance(density, heath); // the same as Trees.Forest(x, z)
                float trail = Landmarks.Trail(x, z);

                // Landmarks to steer by (env-5), each shaped by its own seed; whatever
                // the feature registry puts on a cell is all that stands there.
                if (Features.At(x, z, FeatureGround.Dry) is { } feature)
                {
                    LandmarkProps.For(feature).Build(x, z, new Vector3(fx, top, fz), HeightAt, landmarks, custom);
                    continue;
                }
                float sapling = tree + 0.03f * edge * (1f - heath);
                float rocky = Mathf.SmoothStep(0.7f, 0.85f, World.Noise(x, z, 16f, 65));
                float rock = sapling + 0.004f + 0.07f * rocky + (shore ? 0.08f : 0f);
                if (!nearSpawn && roll < sapling)
                {
                    // Saplings crowd the grove edges, a half to three quarters full size.
                    if (roll >= tree) at = at.ScaledLocal(Vector3.One * (0.45f + Hash01(x, z, 21) * 0.3f));
                    if (wet > 0.4f && Hash01(x, z, 23) < 0.8f)
                    {
                        // Birch and alder by the water: slim, pale and light-leaved. Modeling's
                        // birch trunk is already slim, so only the canopy is narrowed.
                        var slim = at.ScaledLocal(new Vector3(0.6f, 1.1f, 0.6f));
                        landmarks.Put(PropMeshes.Birch(Variant(x, z, 25, PropMeshes.BirchVariants)), Look.TreeBark,
                            (at.ScaledLocal(new Vector3(1f, 1.1f, 1f)), Colors.White.Darkened(Hash01(x, z, 24) * 0.12f), custom));
                        leafy.Add((slim, Look.LeafLight.Lerp(Look.Leaf, Hash01(x, z, 18) * 0.3f), custom with { A = SeasonBroadleaf }));
                    }
                    else if (heath > 0.5f && roll < tree)
                    {
                        // The odd tree on the heath is a broad old oak.
                        var broad = at.ScaledLocal(new Vector3(1.35f, 1.15f, 1.35f));
                        painted[(false, TreeVariant(x, z, 30))].Add((broad, Colors.White.Darkened(0.05f + Hash01(x, z, 19) * 0.08f), custom with { A = SeasonBroadleaf }));
                    }
                    else
                    {
                        // Modeling's painted trees (model-10): a pine or a broadleaf, trunk and
                        // all, one of a few seeded shapes, each a shade lighter or darker.
                        var shade = Colors.White.Darkened(Hash01(x, z, 18) * 0.1f);
                        if (Hash01(x, z, 16) < 0.4f)
                            painted[(true, TreeVariant(x, z, 31))].Add((at, shade, custom));
                        else
                            painted[(false, TreeVariant(x, z, 30))].Add((at, shade, custom with { A = SeasonBroadleaf }));
                    }
                }
                else if (roll < rock)
                {
                    // Boulders gather in rocky patches; small stones line the water. Nearby
                    // boulders share a shape, as stone of one kind would.
                    bool pebble = shore && rocky < 0.5f;
                    float size = pebble ? 0.2f + Hash01(x, z, 22) * 0.2f : 0.3f + Hash01(x, z, 22) * 0.55f;
                    var stone = (at.ScaledLocal(Vector3.One * size), Look.WorldRock.Darkened(Hash01(x, z, 20) * 0.15f), custom with { B = 1f });
                    if (pebble)
                        landmarks.Put(PropMeshes.RiverStone(Regional(x, z, 95, PropMeshes.RiverStoneVariants)), Look.RockSurface,
                            (at.ScaledLocal(Vector3.One * size * 3.3f), stone.Item2, stone.Item3));
                    else
                        landmarks.Put(PropMeshes.Rock(Regional(x, z, 94, PropMeshes.RockVariants)), Look.RockSurface, stone);
                    continue;
                }
                else if (density > 0.45f && Hash01(x, z, 50) < 0.006f)
                {
                    // A fallen trunk lying in the forest floor (Modeling's logs lie along x).
                    var lying = new Basis(Vector3.Up, yaw) * Basis.FromScale(Vector3.One * (0.75f + Hash01(x, z, 51) * 0.35f));
                    landmarks.Put(PropMeshes.FallenLog(Variant(x, z, 27, PropMeshes.FallenLogVariants)), Look.TreeBark,
                        (new Transform3D(lying, new Vector3(fx, top, fz)), Look.Bark.Darkened(0.1f), custom));
                }

                if (!shore && trail < 0.5f)
                {
                    // Ferns under the trees and bushes where a grove opens into a glade;
                    // heather and gorse on the heath; sedge on wet ground.
                    float plant = Hash01(x, z, 52);
                    // Where the meadow meets the wood a loose, patchy band of ferns and low
                    // shrubs softens the line between grass and leaf litter (env-19).
                    float rim = Mathf.SmoothStep(0.08f, 0.2f, density) * (1f - Mathf.SmoothStep(0.3f, 0.45f, density))
                        * Mathf.SmoothStep(0.42f, 0.62f, World.Noise(x - 1907, z + 2633, 7f, 98)) * (1f - heath);
                    float fernChance = (0.10f * Mathf.SmoothStep(0.2f, 0.6f, density) + 0.12f * edge) * (1f - heath) + 0.22f * rim;
                    // Heather grows in a few irregular patches with plenty of open grass
                    // between them, never sprinkled evenly (Justin's playtest); gorse is
                    // rarer still, in its own scattered clumps.
                    float heatherPatch = Mathf.SmoothStep(0.62f, 0.74f, World.Noise(x + 2917, z - 1433, 11f, 95) * 0.65f + World.Noise(x - 811, z + 3701, 4.3f, 96) * 0.35f);
                    float gorsePatch = Mathf.SmoothStep(0.66f, 0.78f, World.Noise(x - 5023, z + 617, 9f, 97));
                    float heather = 0.26f * heath * heatherPatch, gorse = 0.03f * heath * gorsePatch, sedge = 0.35f * wet;
                    // Heather and gorse carry their own colours (Modeling); the tint only
                    // varies them a little.
                    if (plant < heather)
                        landmarks.Put(PropMeshes.Heather(Variant(x, z, 28, PropMeshes.HeatherVariants)), Look.Foliage,
                            (Plant(fx, top, fz, yaw, 0.8f + Hash01(x, z, 55) * 0.45f), Colors.White.Darkened(Hash01(x, z, 56) * 0.15f), custom with { A = SeasonHeather }));
                    else if (plant < heather + gorse)
                        landmarks.Put(PropMeshes.Gorse(Variant(x, z, 29, PropMeshes.GorseVariants)), Look.Foliage,
                            (Plant(fx, top, fz, yaw, 0.8f + Hash01(x, z, 55) * 0.5f), Colors.White.Darkened(Hash01(x, z, 56) * 0.12f), custom));
                    else if (plant < heather + gorse + sedge)
                        reeds.Add((Plant(fx, top, fz, yaw, 0.45f + Hash01(x, z, 53) * 0.2f), Look.Sedge.Lerp(Look.LeafWarm, Hash01(x, z, 54) * 0.2f), custom));
                    else if (plant < heather + gorse + sedge + fernChance)
                        ferns.Add((Plant(fx, top, fz, yaw, 0.8f + Hash01(x, z, 53) * 0.6f), Look.Leaf.Lerp(Look.LeafLight, Hash01(x, z, 54) * 0.4f), custom));
                    else if (plant < heather + gorse + sedge + fernChance + 0.035f * edge + 0.01f * density + 0.08f * rim)
                        bushes.Add((Plant(fx, top, fz, yaw, 0.7f + Hash01(x, z, 55) * 0.6f), Look.Leaf.Lerp(Look.Pine, Hash01(x, z, 56) * 0.4f), custom));
                }
                else if (shore && Hash01(x, z, 60) < 0.25f)
                    reeds.Add((Plant(fx, top, fz, yaw, 0.7f + Hash01(x, z, 61) * 0.4f), Look.LeafLight.Lerp(Look.LeafWarm, Hash01(x, z, 62) * 0.3f), custom));

                // Wildflowers in patches across the open ground.
                if (density < 0.2f && heath < 0.5f && !shore && World.Noise(x, z, 6f, 63) > 0.62f && Hash01(x, z, 57) < 0.45f)
                {
                    int kind = (int)(World.Noise(x, z, 18f, 64) * Undergrowth.FlowerColors.Length * 0.999f);
                    flowers.Add((Plant(x + 0.5f - Hash01(x, z, 58), top, z + 0.5f - Hash01(x, z, 59), yaw, 1f),
                        Undergrowth.FlowerColors[kind].Lightened((Hash01(x, z, 66) - 0.5f) * 0.1f), custom with { A = SeasonFlower }));
                }
                // And the odd lone white flower between the patches (env-17).
                else if (density < 0.2f && heath < 0.5f && !shore && Hash01(x, z, 67) < 0.04f)
                    flowers.Add((Plant(x + 0.5f - Hash01(x, z, 68), top, z + 0.5f - Hash01(x, z, 69), yaw, 0.8f + Hash01(x, z, 70) * 0.3f),
                        Look.FlowerWhite.Darkened(Hash01(x, z, 71) * 0.06f), custom with { A = SeasonFlower }));

                // A carpet of grass over each grassy tile, rooted in the tile's own color
                // so blades and ground read as one surface.
                bool grassy = top >= Look.WaterLevel + 0.2f && World.ClayScore(x, z) < 0.7f;
                // Under the trees the grass thins and stays short; it is short on the
                // heath and gives way to moss on wet ground.
                if (grassy && tufts != null && Hash01(x, z, 40) < 0.93f - 0.5f * density - 0.5f * wet - 0.6f * trail)
                {
                    // Shear the patch onto the slope so every blade stays rooted.
                    float sx = (TileHeight(x + 1, z) - TileHeight(x - 1, z)) * 0.5f;
                    float sz = (TileHeight(x, z + 1) - TileHeight(x, z - 1)) * 0.5f;
                    var slope = new Basis(new Vector3(1, sx, 0), new Vector3(0, (0.75f + Hash01(x, z, 42) * 0.5f) * (1f - 0.35f * density) * (1f - 0.45f * heath) * (1f - 0.5f * trail), 0), new Vector3(0, sz, 1));
                    var g = new Transform3D(slope, new Vector3(x, top, z));
                    tufts!.Add((g, TileColor(x, z, top, cleared: false), custom));
                }
            }

        trunks.AddRange(landmarks.Trunks);
        leafy.AddRange(landmarks.Leafy);
        pines.AddRange(landmarks.Pines);
        data.Add(trunkMesh!, trunks, Look.TreeBark);
        data.Add(leafyMesh!, leafy, Look.TreeFoliage);
        data.Add(pineMesh!, pines, Look.TreeFoliage);
        // Painted trees in full near the camera and as a lighter far version beyond.
        foreach (var ((pine, v), items) in painted)
        {
            var material = pine ? PaintedTrees.PineMaterial : PaintedTrees.BroadleafMaterial;
            data.Add(pine ? PaintedTrees.Pine(v, false) : PaintedTrees.Broadleaf(v, false), items, material, range: (0f, PaintedTrees.NearRange));
            data.Add(pine ? PaintedTrees.Pine(v, true) : PaintedTrees.Broadleaf(v, true), items, material, range: (PaintedTrees.NearRange, 0f));
        }
        foreach (var ((mesh, material), items) in landmarks.Batches)
            data.Add(mesh, items, material, castShadow: material != Look.Grass);
        if (tufts != null) data.Add(tuftMesh!, tufts, Look.GrassTufts, castShadow: false);
        data.Add(Undergrowth.Fern, ferns, Look.Grass, castShadow: false);
        data.Add(Undergrowth.Reed, reeds, Look.Grass, castShadow: false);
        data.Add(Undergrowth.Flowers, flowers, Look.Grass, castShadow: false);
        // Bushes lean toward the grass tufts' muted olive (env-20).
        for (int i = 0; i < bushes.Count; i++) bushes[i] = bushes[i] with { Item2 = Undergrowth.Olive(bushes[i].Item2) };
        data.Add(Undergrowth.Bush, bushes, Look.Foliage);
    }

    // Which of a prop's shape variants stands on a cell: at random, or (Regional) the
    // same over a stretch of ground, so neighbouring boulders look like one stone.
    static int Variant(int x, int z, uint salt, int count) => Math.Min((int)(Hash01(x, z, 1100u + salt) * count), count - 1);
    static int Regional(int x, int z, uint salt, int count) => Math.Min((int)(World.Noise(x + 211, z - 977, 30f, salt) * count), count - 1);

    // Which painted tree shape stands on a cell: one of two neighbouring shapes chosen by
    // the region, so a chunk holds about two of each kind (fewer draw calls) while the
    // forest still changes shape as you walk through it (D-166).
    internal static int TreeVariant(int x, int z, uint salt) =>
        (Regional(x, z, salt, PaintedTrees.Variants) + (Hash01(x, z, 1200u + salt) < 0.5f ? 0 : 1)) % PaintedTrees.Variants;

    static Transform3D Plant(float x, float y, float z, float yaw, float scale) =>
        new(new Basis(Vector3.Up, yaw).Scaled(Vector3.One * scale), new Vector3(x, y, z));

    // Season codes in each instance's custom data (w), for the seasons in Look's
    // shaders (D-099): broadleaf canopies thin in winter and turn in autumn, flowers
    // only show in spring and summer, heather blooms purple in late summer. 0 (pines,
    // other bushes, rocks) never changes.
    internal const float SeasonBroadleaf = 1f, SeasonFlower = 2f, SeasonHeather = 3f;

    static void AddMulti(Node3D root, Mesh mesh, List<(Transform3D, Color, Color)> items, Material material, bool castShadow = true, (float Begin, float End) range = default)
    {
        if (items.Count == 0) return;
        bool custom = items.Exists(item => item.Item3.R > 0f || item.Item3.A > 0f);
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
            // A near or far version of a batch shows only within its range of the camera.
            VisibilityRangeBegin = range.Begin, VisibilityRangeEnd = range.End,
            VisibilityRangeBeginMargin = range.Begin > 0f ? 4f : 0f, VisibilityRangeEndMargin = range.End > 0f ? 4f : 0f,
        });
        // Trees in the way of the camera fade; their ghost draws them see-through.
        // Ghosts sit under one node per chunk so ShowGhosts can switch them off
        // wherever nothing can be in the way.
        // A far batch never stands between the camera and the player, so it needs no ghost.
        if (range.Begin == 0f && Look.GhostOf(material) is { } ghost)
        {
            if (root.GetNodeOrNull<Node3D>(GhostsName) is not { } ghosts)
            {
                ghosts = new Node3D { Name = GhostsName, Visible = false };
                root.AddChild(ghosts);
            }
            ghosts.AddChild(new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = ghost, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, VisibilityRangeBegin = range.Begin, VisibilityRangeEnd = range.End });
        }
    }

    const string GhostsName = "Ghosts";

    // Only chunks the line from the camera to the player passes near can have a tree
    // in the way, so only their see-through copies are drawn. Saves three draw calls
    // for nearly every chunk.
    public void ShowGhosts(Vector3 camera, Vector3 player)
    {
        const float Margin = 12f; // the fade's reach round the widest canopy: a giant tree's lobes (env-5) reach about 11 m
        int n = World.ChunkSize;
        var a = new Vector2(camera.X, camera.Z);
        var b = new Vector2(player.X, player.Z);
        foreach (var ((cx, cz), ghosts) in ghostHolders)
        {
            var min = new Vector2(cx * n - 0.5f - Margin, cz * n - 0.5f - Margin);
            var max = new Vector2((cx + 1) * n - 0.5f + Margin, (cz + 1) * n - 0.5f + Margin);
            bool show = SegmentHitsBox(a, b, min, max);
            if (ghosts.Visible != show) ghosts.Visible = show;
        }
    }

    // Liang-Barsky: does the segment from a to b touch the box?
    static bool SegmentHitsBox(Vector2 a, Vector2 b, Vector2 min, Vector2 max)
    {
        float t0 = 0f, t1 = 1f;
        var d = b - a;
        for (int axis = 0; axis < 2; axis++)
        {
            float p = axis == 0 ? d.X : d.Y, start = axis == 0 ? a.X : a.Y;
            float lo = axis == 0 ? min.X : min.Y, hi = axis == 0 ? max.X : max.Y;
            if (Mathf.Abs(p) < 1e-6f)
            {
                if (start < lo || start > hi) return false;
                continue;
            }
            float ta = (lo - start) / p, tb = (hi - start) / p;
            if (ta > tb) (ta, tb) = (tb, ta);
            t0 = Mathf.Max(t0, ta);
            t1 = Mathf.Min(t1, tb);
            if (t0 > t1) return false;
        }
        return true;
    }

    static void EnsureMeshes()
    {
        if (trunkMesh != null) return;

        // Modeling's trunk: flared roots and branch stubs, top at (0, 2.8, 0) where the
        // canopies sit.
        trunkMesh = PropMeshes.Trunk;

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

        tuftMesh = BuildTuft();
    }

    internal static SphereMesh Blob(float radius) => new() { Radius = radius, Height = radius * 1.7f, RadialSegments = 10, Rings = 6 };

    internal static Transform3D Xf(float x, float y, float z) => Transform3D.Identity.Translated(new Vector3(x, y, z));

    // Appends a primitive with its smooth normals, so the banded light gives soft, painted forms.
    internal static void Append(SurfaceTool target, PrimitiveMesh mesh, Transform3D offset, Color shade, float lumps = 0f)
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

    public static float Hash01(int x, int z, uint salt) => Trees.Hash01(x, z, salt);
}
