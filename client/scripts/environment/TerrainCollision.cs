using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Which trunks and boulders block walking (bugfix-23). The forest is deterministic per
// cell, so this re-derives the tree or rock on a cell with the same rules as
// AddVegetation instead of keeping a second list. Keep the two in step.
public partial class Terrain
{
    const float WalkerRadius = 0.3f;

    // Placement radii, one helper per kind so they follow the meshes in Terrain.EnsureMeshes.
    // The trunk mesh's bottom radius is 0.2 and the boulder mesh's is about 0.45, times the instance scale.
    static float TrunkRadius(float scale) => Obstacles.TrunkRadius(scale);

    // Pushes a step out of any trunk or boulder, sliding along its edge instead of stopping dead.
    public Vector3 CollideVegetation(Vector3 to, float airHeight = 0f)
    {
        int cx = Mathf.FloorToInt(to.X + 0.5f), cz = Mathf.FloorToInt(to.Z + 0.5f);
        for (int x = cx - 1; x <= cx + 1; x++)
            for (int z = cz - 1; z <= cz + 1; z++)
            {
                if (!ObstacleAt(x, z, out float ox, out float oz, out float radius, out float height) || Jumping.Clears(height, airHeight)) continue;
                float dx = to.X - ox, dz = to.Z - oz, keep = radius + WalkerRadius, d2 = dx * dx + dz * dz;
                if (d2 >= keep * keep) continue;
                if (d2 < 0.0001f) { dx = 1f; dz = 0f; d2 = 1f; }
                float d = Mathf.Sqrt(d2);
                to = new Vector3(ox + dx / d * keep, to.Y, oz + dz / d * keep);
            }
        return CollideFeatures(to, airHeight);
    }

    // Features that block with stones (outcrops, env-5) spread several meters from their
    // middle cell, so their stones are found by square rather than by the cells next
    // to the walker.
    readonly Dictionary<(FeatureId, int, int), List<(float X, float Z, float R)>> featureStones = new();

    Vector3 CollideFeatures(Vector3 to, float airHeight)
    {
        foreach (var feature in Features.All)
        {
            if (feature.Block != FeatureBlock.Stones) continue;
            int size = feature.Square;
            float reach = feature.Spread;
            int sx0 = Landmarks.SquareOf(Mathf.FloorToInt(to.X - reach), size), sx1 = Landmarks.SquareOf(Mathf.FloorToInt(to.X + reach), size);
            int sz0 = Landmarks.SquareOf(Mathf.FloorToInt(to.Z - reach), size), sz1 = Landmarks.SquareOf(Mathf.FloorToInt(to.Z + reach), size);
            for (int sx = sx0; sx <= sx1; sx++)
                for (int sz = sz0; sz <= sz1; sz++)
                    foreach (var (ox, oz, radius) in FeatureStones(feature, sx, sz))
                    {
                        if (Jumping.Clears(Jumping.RockHeight(radius), airHeight)) continue;
                        float dx = to.X - ox, dz = to.Z - oz, keep = radius + WalkerRadius, d2 = dx * dx + dz * dz;
                        if (d2 >= keep * keep) continue;
                        if (d2 < 0.0001f) { dx = 1f; dz = 0f; d2 = 1f; }
                        float d = Mathf.Sqrt(d2);
                        to = new Vector3(ox + dx / d * keep, to.Y, oz + dz / d * keep);
                    }
        }
        return to;
    }

    // The blocking stones of a feature in a square, worked out once with the same
    // seed the terrain draws them from.
    List<(float X, float Z, float R)> FeatureStones(FeatureDef feature, int sx, int sz)
    {
        if (featureStones.TryGetValue((feature.Id, sx, sz), out var stones)) return stones;
        stones = new();
        if (feature.In(sx, sz, out int x, out int z))
        {
            var (fx, fz) = Trees.Trunk(x, z);
            var built = new LandmarkProps.Lists();
            LandmarkProps.For(feature).Build(x, z, new Vector3(fx, 0f, fz), (_, _) => 0f, built, new Color(0, 0, 0, 0));
            stones.AddRange(built.Blocks);
        }
        if (featureStones.Count > 256) featureStones.Clear();
        featureStones[(feature.Id, sx, sz)] = stones;
        return stones;
    }

    // How likely a bank cell holds a stone (env-21); the rule lives in shared/Obstacles.cs so the
    // server and the check bots agree, and AddVegetation uses the same.
    static float ShoreStones(int x, int z) => Obstacles.ShoreStones(x, z);

    // The ground as the shared obstacle rule sees it (bugfix-34).
    sealed class Ground : ICellGround
    {
        readonly Terrain t;
        public Ground(Terrain terrain) => t = terrain;
        public float TileHeight(int x, int z) => t.TileHeight(x, z);
        public bool Dug(int x, int z) => t.depths.ContainsKey(World.CellKey(x, z));
        public bool Cleared(int x, int z) => t.Cleared?.Invoke(x, z) == true;
    }

    Ground? ground;

    // The solid circle standing on cell (x, z): a tree trunk or a boulder. Crowns, small
    // stones, ferns and fallen trunks never block. Landmarks are handled here; ordinary
    // trunks and boulders follow the rule in shared/Obstacles.cs, which the server uses too.
    bool ObstacleAt(int x, int z, out float ox, out float oz, out float radius, out float height)
    {
        (ox, oz) = Trees.Trunk(x, z);
        radius = 0f;
        height = float.MaxValue; // trunks are never hopped
        if (Features.At(x, z, FeatureGround.Dry) is { } feature)
        {
            // Features (env-5, env-10): a giant tree's broad trunk; an outcrop's middle cell
            // holds only its stones, which CollideFeatures handles.
            if (depths.ContainsKey(World.CellKey(x, z)) || Cleared?.Invoke(x, z) == true) return false;
            if (TileHeight(x, z) < Look.WaterLevel + 0.1f || feature.Block != FeatureBlock.Trunk) return false;
            radius = TrunkRadius(LandmarkProps.For(feature).Girth?.Invoke(x, z) ?? 1f);
            return true;
        }
        return Obstacles.At(ground ??= new Ground(this), x, z, out ox, out oz, out radius, out height);
    }
}
