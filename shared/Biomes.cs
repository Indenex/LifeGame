// Which biome a cell is in (world-1, docs in /mnt/project-files/world/biomes-design.md).
// A pure function of the position and World's noise, so the server and every client agree
// and nothing is stored. Biomes are blobs a few hundred meters across with soft borders;
// the first 150 m around spawn stay forest. Only forest, marsh and rocky highland exist so
// far; meadow and birch hills are reserved ids for later (Justin, 2026-10-17).
// tools/env-preview/biomes.py draws the same map. Keep the two in step.
using System;

namespace LifeGame.Shared
{
    // Stable ids: append only, never renumber.
    public enum Biome : byte { Forest = 0, Meadow = 1, Marsh = 2, Highland = 3, BirchHills = 4 }

    public static class Biomes
    {
        // Noise lattice spacing: blobs come out roughly 250 to 700 m across.
        const float Scale = 400f;
        // A biome starts where its score passes Threshold and is full strength Blend later,
        // which is about 40 m of ground, so borders are soft but not smeared.
        const float Threshold = 0.62f;
        const float Blend = 0.12f;
        // Spawn and its surroundings are forest: the score is pulled down to nothing
        // inside KeepForest and fades back in by KeepForest + KeepFade meters.
        const float KeepForest = 150f;
        const float KeepFade = 100f;

        public static Biome At(int x, int z)
        {
            float marsh = Strength(Biome.Marsh, x, z), highland = Strength(Biome.Highland, x, z);
            if (highland > 0.5f && highland >= marsh) return Biome.Highland;
            if (marsh > 0.5f) return Biome.Marsh;
            return Biome.Forest;
        }

        // How strongly cell (x, z) belongs to a biome, 0 outside to 1 inside, for blending
        // looks across a border. Forest is whatever is left over.
        public static float Strength(Biome biome, int x, int z)
        {
            if (biome == Biome.Forest) return 1f - MathF.Max(Strength(Biome.Marsh, x, z), Strength(Biome.Highland, x, z));
            if (biome != Biome.Marsh && biome != Biome.Highland) return 0f;
            float raw = biome == Biome.Marsh
                ? World.Noise(x + 1301, z - 877, Scale, 200) + Ragged(x, z, 202)
                : World.Noise(x - 2203, z + 1559, Scale, 201) + Ragged(x, z, 203);
            float away = SmoothStep(KeepForest, KeepForest + KeepFade, MathF.Sqrt(x * x + z * z));
            return SmoothStep(Threshold, Threshold + Blend, raw - (1f - away));
        }

        // Small wobble so a border never follows a smooth contour of the big noise.
        static float Ragged(int x, int z, uint salt) => (World.Noise(x, z, 45f, salt) - 0.5f) * 0.12f;

        static float SmoothStep(float from, float to, float v)
        {
            float t = Math.Clamp((v - from) / (to - from), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
