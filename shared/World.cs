// Shared by the server module and the Godot client, so both agree on the
// terrain, chunk layout and gameplay limits. Keep this file free of engine
// and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public static class World
    {
        // Terrain is a grid of height points one meter apart, grouped in chunks.
        public const int ChunkSize = 16;
        public const float CellSize = 1f;

        // Items are seeded in a square of chunks around the origin.
        public const int SpawnRadiusChunks = 4;
        public const int ItemsPerChunkTarget = 12;

        // Player limits the server enforces.
        public const float Reach = 2.5f;
        public const float MaxSpeed = 6f;
        // Holding Shift walks this fast; animals don't take fright at it.
        public const float SneakSpeed = 1.6f;
        public const float MoveSlack = 1.5f;
        // A mover may bank this many seconds of walking (a lagging client catches up, bugfix-24).
        public const float MoveBankSeconds = 1f;

        // How far a player may step now: what they had banked plus what the time since the
        // last report earned at `speed`, never more than a second of walking plus the slack.
        // Each report spends from it, so many small reports can't add up to more than `speed`.
        public static float MoveAllowance(float banked, float speed, float seconds) =>
            Math.Min(Math.Max(banked, 0f) + speed * Math.Max(seconds, 0f), speed * MoveBankSeconds + MoveSlack);
        public const long DigCooldownMicros = 600_000;

        // Each dig lowers a height point by DigStep meters, down to MaxDigDepth steps.
        public const short MaxDigDepth = 12;
        public const float DigStep = 0.1f;

        // Movement reports are applied and sent to other clients in one batch per tick.
        public const int MoveTickMillis = 100;

        // Ground below this height is under water.
        public const float WaterLevel = -1.0f;

        // How many chunks around the player a client loads and subscribes to.
        public const int ViewRadiusChunks = 3;

        public static int ChunkOf(float worldPos) => (int)MathF.Floor(worldPos / (ChunkSize * CellSize));

        public static int CellOf(float worldPos) => (int)MathF.Round(worldPos / CellSize);

        public static int ChunkOfCell(int cell) => cell >= 0 ? cell / ChunkSize : (cell - ChunkSize + 1) / ChunkSize;

        public static long CellKey(int x, int z) => ((long)x << 32) | (uint)z;

        // Base height in meters at a grid point, before any digging: hills, streams
        // and ponds (shared/Land.cs, env-2).
        public static float BaseHeight(int x, int z) => Land.Height(x, z);

        // Clay shows up in patches, more often in low ground near water.
        public static bool IsClay(int x, int z) => ClayScore(x, z) > 0.62f;

        // Above 0.62 the ground is clay; clients use the raw score to blend colors softly.
        public static float ClayScore(int x, int z)
        {
            float patch = ValueNoise(x, z, 10f, 3u);
            float low = BaseHeight(x, z) < -0.4f ? 0.15f : 0f;
            return patch + low;
        }

        // Smooth noise in [0, 1) for anything else that should vary gently across the map.
        public static float Noise(int x, int z, float scale, uint salt) => ValueNoise(x, z, scale, salt);

        public static float Height(int x, int z, short depth) => BaseHeight(x, z) - depth * DigStep;

        static uint Hash(int x, int z, uint salt)
        {
            unchecked
            {
                uint h = (uint)x * 0x8DA6B343u ^ (uint)z * 0xD8163841u ^ salt * 0xCB1AB31Fu;
                h ^= h >> 13;
                h *= 0x5BD1E995u;
                h ^= h >> 15;
                return h;
            }
        }

        static float Rand01(int x, int z, uint salt) => (Hash(x, z, salt) & 0xFFFFFF) / 16777216f;

        static float Smooth(float t) => t * t * (3f - 2f * t);

        // Bilinear value noise in [0, 1). Uses only + - * / and floor, so the
        // server and every client compute the same values.
        static float ValueNoise(int x, int z, float scale, uint salt)
        {
            float fx = x / scale, fz = z / scale;
            int x0 = (int)MathF.Floor(fx), z0 = (int)MathF.Floor(fz);
            float tx = Smooth(fx - x0), tz = Smooth(fz - z0);
            float a = Rand01(x0, z0, salt), b = Rand01(x0 + 1, z0, salt);
            float c = Rand01(x0, z0 + 1, salt), d = Rand01(x0 + 1, z0 + 1, salt);
            float top = a + (b - a) * tx, bottom = c + (d - c) * tx;
            return top + (bottom - top) * tz;
        }
    }
}
