// Where trees grow, shared so the server can tell which tree a player is chopping.
// The client draws the forest from the same rules (client/scripts/Terrain.cs).
using System;

namespace LifeGame.Shared
{
    public static class Trees
    {
        // Chopping fells a tree after LogsPerTree logs; it grows back RegrowSeconds
        // later. With Felling off, trees stay and give one log of deadwood per
        // DeadwoodSeconds instead.
        public const bool Felling = true;
        public const int LogsPerTree = 3;
        public const float RegrowSeconds = 2 * 60 * 60;
        public const float DeadwoodSeconds = 5 * 60;

        // Water sits at this height; nothing grows within 0.1 m above it. Matches Look.WaterLevel.
        public const float WaterLevel = -1.0f;

        // The clearing where new players wake up has no trees.
        public static bool NearSpawn(int x, int z) => x * x + z * z < 12 * 12;

        // Chance a cell of untouched ground grows a tree: a forest that thins out
        // over clay and into the spawn clearing.
        public static float Forest(int x, int z) => World.IsClay(x, z) ? 0.02f : 0.03f + 0.10f * World.Noise(x, z, 24f, 33);

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
