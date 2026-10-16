// The shape of the land before anyone digs (env-2, D-084, D-129): rolling hills,
// streams that wander down broad valleys, and round ponds, with the clearing where
// new players wake up kept dry and gentle. World.BaseHeight returns this, so the
// server and every client agree. Plain C# using only World's noise and correctly
// rounded float math, so every machine computes the same heights.
// tools/env-preview/land.py draws the same shape as an ascii map.
using System;

namespace LifeGame.Shared
{
    public static class Land
    {
        // Streams run roughly north to south, one every RiverSpacing meters, wandering
        // side to side by a warp gentle enough (slope under 1) that a stream never
        // doubles back into a loop. The first passes a short walk west of spawn.
        const float RiverSpacing = 300f;
        const float RiverOffset = 66f;

        public static float Height(int x, int z)
        {
            float hills = World.Noise(x, z, 48f, 1) * 2.0f + World.Noise(x, z, 8f, 2) * 0.4f - 0.6f;

            // The water is a flat plane, so a stream runs in a valley: within about
            // 26 m of its line the hills sink toward a floor just above the water,
            // then a channel about 6 m wide slopes gently down to a bed below it.
            // The bed stays shallow enough to wade (the client turns players back
            // below WaterLevel - 0.3), so a stream never walls anyone in; ponds
            // are deeper and are walked round.
            float meters = RiverDistance(x, z);
            float valley = 1f - SmoothStep(4f, 26f, meters);
            float bed = 1f - SmoothStep(1.0f, 6.0f, meters);
            float h = hills * (1f - 0.85f * valley) - 0.55f * 0.85f * valley;
            h = h * (1f - bed) + (World.WaterLevel - 0.22f) * bed;

            // Ponds: round hollows filled to a fixed depth, in a wider dip so the banks
            // slope gently. The offsets keep them off the noise lattice, where they
            // would line up in rows.
            float pondField = World.Noise(x + 5113, z - 2971, 26f, 70) * 0.6f + World.Noise(x - 3307, z + 7741, 16.4f, 75) * 0.4f;
            float dip = SmoothStep(0.36f, 0.74f, pondField);
            h += (MathF.Min(h, -0.6f) - h) * dip;
            float pond = SmoothStep(0.68f, 0.82f, pondField);
            h += (MathF.Min(h, World.WaterLevel - 0.4f) - h) * pond;

            // The spawn clearing is a dry, gentle rise.
            float keep = SmoothStep(18f, 32f, MathF.Sqrt(x * x + z * z));
            return h * keep + (0.1f + hills * 0.3f) * (1f - keep);
        }

        // Meters from the nearest stream's center line, for telling stream water from
        // pond water (the sound of running water, D-079). About ten noise lookups.
        public static float StreamDistance(int x, int z) => RiverDistance(x, z);

        static float River(int x, int z) =>
            x + RiverOffset + (World.Noise(x, z, 120f, 71) - 0.5f) * 70f + (World.Noise(x, z, 40f, 74) - 0.5f) * 18f;

        // Meters from the nearest stream line: how far River is from a multiple of
        // RiverSpacing, divided by how fast it changes there.
        static float RiverDistance(int x, int z)
        {
            float u = River(x, z);
            float off = MathF.Abs(u - RiverSpacing * MathF.Round(u / RiverSpacing));
            float gx = (River(x + 1, z) - River(x - 1, z)) * 0.5f;
            float gz = (River(x, z + 1) - River(x, z - 1)) * 0.5f;
            return off / MathF.Max(MathF.Sqrt(gx * gx + gz * gz), 0.2f);
        }

        static float SmoothStep(float from, float to, float v)
        {
            float t = Math.Clamp((v - from) / (to - from), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
