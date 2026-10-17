// Which tree trunks and boulders stand in the way (bugfix-34). The forest is deterministic per
// cell, so the solid circle on a cell is worked out from the same rules the client draws
// (client/scripts/Terrain.cs AddVegetation) rather than from a list. Shared by the client, the
// server (which refuses steps into them) and the check bots (which slide round them).
// Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    // What the caller knows about the ground: dug cells, cleared cells (a building stands there
    // or the tree was felled) and the height of a cell.
    public interface ICellGround
    {
        float TileHeight(int x, int z);
        bool Dug(int x, int z);
        bool Cleared(int x, int z);
        // How far a felled tree on the cell has grown back, 0 just felled to 1 regrown
        // (Trees.RegrowSeconds), or -1 where no tree is felled (env-29).
        float FelledProgress(int x, int z);
    }

    public static class Obstacles
    {
        // The client's walker body; the server keeps a smaller one so an honest client is never refused.
        public const float WalkerRadius = 0.3f;

        // One helper per kind so they follow the meshes in Terrain.EnsureMeshes.
        public static float TrunkRadius(float scale) => 0.2f * scale;
        public static float RockRadius(float scale) => 0.4f * scale;

        static float Smooth(float a, float b, float t)
        {
            t = Math.Clamp((t - a) / (b - a), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        // How likely a bank cell holds a stone: pebbles gather in beds along the waterline with
        // bare stretches between (env-21), about as many in all as a flat 8% (env-28: the client
        // drew these while the server still used the flat chance).
        public static float ShoreStones(int x, int z) => 0.02f + 0.24f * Smooth(0.55f, 0.75f, World.Noise(x - 3121, z + 1877, 5f, 100));

        // Whether any ground within two cells is under water (a shore: rocks are smaller there).
        static bool NearWater(ICellGround g, int x, int z, float top)
        {
            if (top > World.WaterLevel + 0.5f) return false;
            for (int dx = -2; dx <= 2; dx++)
                for (int dz = -2; dz <= 2; dz++)
                    if (g.TileHeight(x + dx, z + dz) < World.WaterLevel) return true;
            return false;
        }

        // The solid circle standing on cell (x, z): an ordinary tree trunk or a boulder. Crowns,
        // small stones, ferns and fallen trunks never block; neither do landmarks (Features),
        // which the client handles itself. height is how high a jump must be to clear it
        // (trunks are never hopped).
        public static bool At(ICellGround g, int x, int z, out float ox, out float oz, out float radius, out float height)
        {
            (ox, oz) = Trees.Trunk(x, z);
            radius = 0f;
            height = float.MaxValue;

            float roll = Trees.Hash01(x, z, 11);
            float density = Forest.Density(x, z);
            float heath = Forest.Heath(x, z), wet = Forest.Wetland(x, z);
            float tree = Forest.TreeChance(density, heath);
            float sapling = tree + 0.03f * Forest.Edge(density) * (1f - heath);
            // Most cells hold nothing: leave before asking the ground anything (the server looks
            // that up in its tables). 0.334 is the most a boulder's chance can add to a sapling's.
            if (roll >= sapling + 0.334f) return false;
            if (g.Dug(x, z)) return false;
            // A felled tree's stump blocks until it rots, then its regrowth once it is big
            // enough (env-29, Justin: "stumps don't have collision, they should").
            float felled = g.FelledProgress(x, z);
            if (felled >= 0f) return Regrowing(x, z, felled, out radius, out height);
            if (g.Cleared(x, z)) return false;
            float top = g.TileHeight(x, z);
            if (top < World.WaterLevel + 0.1f) return false;
            if (Features.At(x, z, FeatureGround.Dry) != null) return false;
            float scale = 1.3f + Trees.Hash01(x, z, 15) * 0.6f;
            // Trees don't grow in the spawn clearing, but boulders can.
            if (!Trees.NearSpawn(x, z) && roll < sapling)
            {
                if (roll >= tree) scale *= 0.45f + Trees.Hash01(x, z, 21) * 0.3f;
                // Birch by the water is slim; the odd heath tree is a broad oak (env-4).
                float girth = 1f;
                if (wet > 0.4f && Trees.Hash01(x, z, 23) < 0.8f) girth = 0.6f;
                else if (heath > 0.5f && roll < tree) girth = 1.35f;
                radius = TrunkRadius(scale * girth);
                return true;
            }
            bool shore = NearWater(g, x, z, top);
            float rocky = Smooth(0.7f, 0.85f, World.Noise(x, z, 16f, 65));
            float rock = sapling + 0.004f + 0.07f * rocky + (shore ? ShoreStones(x, z) : 0f);
            if (roll >= rock) return false;
            float size = shore && rocky < 0.5f ? 0.2f + Trees.Hash01(x, z, 22) * 0.2f : 0.3f + Trees.Hash01(x, z, 22) * 0.55f;
            radius = RockRadius(scale * size);
            height = Jumping.RockHeight(radius);
            // Pebbles are stepped over.
            return radius >= 0.2f;
        }

        // The full-grown trunk's radius at the foot on a cell where Trees.GrowsAt (birches by
        // the water slim, the odd heath tree broad), as At works it out for a standing tree.
        public static float TrunkFoot(int x, int z)
        {
            float scale = 1.3f + Trees.Hash01(x, z, 15) * 0.6f, girth = 1f;
            float heath = Forest.Heath(x, z);
            if (Forest.Wetland(x, z) > 0.4f && Trees.Hash01(x, z, 23) < 0.8f) girth = 0.6f;
            else if (heath > 0.5f) girth = 1.35f;
            return TrunkRadius(scale * girth);
        }

        // What blocks on a felled tree's cell at regrowth stage p: the stump (low enough to
        // hop) while more than half of it is left, else the young tree once it is a third
        // grown, its trunk scaled with it.
        public static bool Regrowing(int x, int z, float p, out float radius, out float height)
        {
            radius = 0f;
            height = float.MaxValue;
            float foot = TrunkFoot(x, z);
            float size = Trees.RegrowSize(p);
            if (size >= 0.33f)
            {
                radius = foot * size;
                return true;
            }
            if (Trees.StumpLeft(p) > 0.5f)
            {
                radius = foot * 1.05f;
                height = Trees.StumpHeight;
                return true;
            }
            return false;
        }

        // Whether a step from (fx, fz) to (tx, tz) walks into an obstacle: it ends inside the body
        // of one and closer to it than it started, so someone already inside (a tree that grew back
        // under them) can always walk out. airHeight is how high a jump has lifted the walker.
        public static bool StepBlocked(ICellGround g, float body, float fx, float fz, float tx, float tz, float airHeight)
        {
            int cx = (int)MathF.Floor(tx + 0.5f), cz = (int)MathF.Floor(tz + 0.5f);
            for (int x = cx - 1; x <= cx + 1; x++)
                for (int z = cz - 1; z <= cz + 1; z++)
                {
                    if (!At(g, x, z, out float ox, out float oz, out float radius, out float height) || Jumping.Clears(height, airHeight)) continue;
                    float keep = radius + body;
                    float to = (tx - ox) * (tx - ox) + (tz - oz) * (tz - oz);
                    float from = (fx - ox) * (fx - ox) + (fz - oz) * (fz - oz);
                    if (to < keep * keep && to < from) return true;
                }
            return false;
        }

        // Pushes a point out of any obstacle, sliding along its edge instead of stopping dead.
        public static (float X, float Z) PushOut(ICellGround g, float body, float tx, float tz, float airHeight)
        {
            int cx = (int)MathF.Floor(tx + 0.5f), cz = (int)MathF.Floor(tz + 0.5f);
            for (int x = cx - 1; x <= cx + 1; x++)
                for (int z = cz - 1; z <= cz + 1; z++)
                {
                    if (!At(g, x, z, out float ox, out float oz, out float radius, out float height) || Jumping.Clears(height, airHeight)) continue;
                    float dx = tx - ox, dz = tz - oz, keep = radius + body, d2 = dx * dx + dz * dz;
                    if (d2 >= keep * keep) continue;
                    if (d2 < 0.0001f) { dx = 1f; dz = 0f; d2 = 1f; }
                    float d = MathF.Sqrt(d2);
                    tx = ox + dx / d * keep;
                    tz = oz + dz / d * keep;
                }
            return (tx, tz);
        }
    }
}
