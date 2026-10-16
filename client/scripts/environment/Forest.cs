using System;
using LifeGame.Shared;

namespace LifeGame;

// Where the woods are thick and where they open up (env-1, D-078). Trees grow in
// groves tens of meters across with glades between them, inside broader stretches
// of deep woods and open country. Everything comes from the shared seed, so every
// player sees the same forest. Plain C# with no engine types: the terrain plants
// by it and the atmosphere reads it to fade in the sound of leaves.
public static class Forest
{
    // How thickly wooded a cell is, 0 open ground to 1 the heart of a grove.
    public static float Density(int x, int z)
    {
        // Deep woods and open country, a few hundred meters across.
        float region = World.Noise(x, z, 70f, 60);
        // Groves with glades between them, with ragged edges.
        float grove = World.Noise(x, z, 20f, 61) * 0.7f + World.Noise(x, z, 7f, 62) * 0.3f;
        float d = SmoothStep(0.38f, 0.60f, grove + (region - 0.5f) * 0.6f);

        // New players wake in a sunny clearing that fades into the trees.
        d *= SmoothStep(12f, 24f, MathF.Sqrt(x * x + z * z));
        // Clay ground is boggy and thinly wooded; so is low ground by the water.
        d *= 1f - 0.75f * SmoothStep(0.55f, 0.7f, World.ClayScore(x, z));
        d *= SmoothStep(-0.9f, -0.4f, World.BaseHeight(x, z));
        return d;
    }

    // Where a grove gives way to a glade: the band undergrowth and saplings crowd.
    public static float Edge(float density) => Math.Clamp(1f - MathF.Abs(density - 0.4f) / 0.35f, 0f, 1f);

    // Chance a cell holds a full-grown tree.
    public static float TreeChance(float density) => 0.004f + 0.16f * density;

    static float SmoothStep(float from, float to, float x)
    {
        float t = Math.Clamp((x - from) / (to - from), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
