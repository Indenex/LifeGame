// Where trees grow, shared so the server can tell which tree a player is chopping.
// The client draws the forest from the same rules (client/scripts/Terrain.cs).
using System;

namespace LifeGame.Shared
{
    public static class Trees
    {
        // Chopping fells a tree after LogsPerTree logs; it grows back RegrowSeconds
        // later. With Felling off, trees stay and give one log of deadwood per
        // DeadwoodSeconds instead. Justin chose felling (D-060).
        public const bool Felling = true;
        public const int LogsPerTree = 3;
        public const float RegrowSeconds = 2 * 60 * 60;

        // How a felled tree comes back over RegrowSeconds, as a fraction p of that time
        // (env-22, env-29), shared by the client's drawing (TreeFall) and collision
        // (Obstacles): a stump that rots into the ground, then a sapling in its place
        // growing into a young tree and a nearly full one.
        public const float StumpHeight = 0.32f;
        // How much of the stump is left: all of it until 14%, gone by 28%.
        public static float StumpLeft(float p) => 1f - Math.Clamp((p - 0.14f) / 0.14f, 0f, 1f);
        // The regrowing tree's size against the full tree: none until 28%.
        public static float RegrowSize(float p)
        {
            p = Math.Clamp(p, 0f, 1f);
            return p < 0.28f ? 0f
                : p < 0.5f ? 0.12f + 0.28f * (p - 0.28f) / 0.22f
                : p < 0.85f ? 0.4f + 0.38f * (p - 0.5f) / 0.35f
                : 0.78f + 0.19f * (p - 0.85f) / 0.15f;
        }
        public const float DeadwoodSeconds = 5 * 60;

        // Water sits at this height; nothing grows within 0.1 m above it. Matches Look.WaterLevel.
        public const float WaterLevel = -1.0f;

        // The clearing where new players wake up has no trees.
        public static bool NearSpawn(int x, int z) => x * x + z * z < 12 * 12;

        // Chance a cell of untouched ground grows a tree: groves with glades between
        // them, thinning over clay, into the spawn clearing and on the hilltop heath
        // (shared/Forest.cs, env-1 and env-4).
        public static float Forest(int x, int z) => Shared.Forest.TreeChanceAt(x, z);

        // Whether a tree grows on this cell before anyone digs, builds or chops there.
        public static bool GrowsAt(int x, int z) =>
            !NearSpawn(x, z)
            && World.Height(x, z, 0) >= WaterLevel + 0.1f
            && Hash01(x, z, 11) < Forest(x, z);

        // Where the trunk of the tree on cell (x, z) stands: jittered within its cell.
        public static (float X, float Z) Trunk(int x, int z) =>
            (x + (Hash01(x, z, 12) - 0.5f) * 0.5f, z + (Hash01(x, z, 13) - 0.5f) * 0.5f);

        // A stable pseudo-random number in [0, 1) per cell and salt.
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
}
