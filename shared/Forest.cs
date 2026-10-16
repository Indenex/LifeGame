using System;

namespace LifeGame.Shared
{
    // Where the woods are thick and where they open up (env-1, D-078). Trees grow in
    // groves tens of meters across with glades between them, inside broader stretches
    // of deep woods and open country. Everything comes from the shared seed, so every
    // player sees the same forest. Shared by the server and the client, so it stays
    // free of engine and SpacetimeDB types: Trees.cs grows full trees by it (the server
    // needs them for chopping), the terrain plants undergrowth by it, and the atmosphere
    // reads it for the sound of leaves and light through the canopy.
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
            // Hilltops open into heath, with only the odd lone tree: the trees thin out
            // early, so even the heath's edge reads as open ground.
            d *= 1f - 0.97f * SmoothStep(0.15f, 0.6f, Heath(x, z));
            // Deer trails keep a path open through the trees (env-5).
            d *= 1f - 0.9f * Landmarks.Trail(x, z);
            return d;
        }

        // Open heath on the hilltops, 0 none to 1 full heath: heather and gorse,
        // short grass and the odd lone oak (env-4). The noise ragged the edge so it
        // doesn't follow one contour line.
        public static float Heath(int x, int z) =>
            SmoothStep(0.65f, 0.95f, World.BaseHeight(x, z) + (World.Noise(x + 911, z - 433, 30f, 80) - 0.5f) * 0.6f);

        // Wet ground by streams and ponds, 0 dry to 1 boggy: birch and alder, sedge
        // and moss instead of grass (env-4).
        public static float Wetland(int x, int z) =>
            SmoothStep(-0.15f, -0.75f, World.BaseHeight(x, z) + (World.Noise(x - 617, z + 287, 14f, 81) - 0.5f) * 0.3f);

        // Where a grove gives way to a glade: the band undergrowth and saplings crowd.
        public static float Edge(float density) => Math.Clamp(1f - MathF.Abs(density - 0.4f) / 0.35f, 0f, 1f);

        // Chance a cell holds a full-grown tree.
        public static float TreeChance(float density) => 0.004f + 0.16f * density;

        // The same, with the heath's lone trees kept rare: only the odd oak stands on
        // an open hilltop. Trees.Forest grows the server's trees by this.
        public static float TreeChance(float density, float heath) =>
            TreeChance(density) * (1f - 0.8f * SmoothStep(0.3f, 0.8f, heath));

        public static float TreeChanceAt(int x, int z) => TreeChance(Density(x, z), Heath(x, z));

        static float SmoothStep(float from, float to, float x)
        {
            float t = Math.Clamp((x - from) / (to - from), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
