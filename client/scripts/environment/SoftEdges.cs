using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Soft, ragged edges for every patch the grass gives way to (env-26, Justin: "can this be
// fixed/smoothed so it's not so square ever?"). Clay deposits, cleared ground (buildings,
// claims, fires, felled trees) and dug cells are cell masks, so a hard cut-off draws the
// cell grid. Here grass thins and shortens over about two metres toward them, and the
// worn earth of a clearing fades out over the same distance, both along a line broken
// by noise. Clay itself is unchanged for the server (World.IsClay); only how it looks
// at its edge.
public partial class Terrain
{
    // How far, in cells, an edge feathers; the snapshot carries cleared cells this far
    // round each chunk so the feather runs across chunk borders unbroken.
    const int FeatherCells = 2;

    // How fully grass covers a cell: 1 in the open meadow, falling to 0 on clay, cleared
    // and dug ground.
    float GrassCover(int x, int z)
    {
        float clay = World.ClayScore(x, z) + (Hash01(x, z, 43) - 0.5f) * 0.12f + (World.Noise(x, z, 4f, 102) - 0.5f) * 0.1f;
        float open = 1f - Mathf.SmoothStep(0.6f, 0.76f, clay);
        if (open <= 0f) return 0f;
        return open * Feather(x, z, includeDug: true);
    }

    // How worn to earth a cell is by a clearing next to it: 1 on cleared ground, fading
    // out round it.
    float Worn(int x, int z)
    {
        if (IsCleared(x, z)) return 1f;
        return (1f - Feather(x, z, includeDug: false)) * 0.6f;
    }

    // 1 well away from bare cells, 0 on one, rising over FeatherCells metres on a ragged line.
    float Feather(int x, int z, bool includeDug)
    {
        int nearest = int.MaxValue;
        for (int dx = -FeatherCells; dx <= FeatherCells; dx++)
            for (int dz = -FeatherCells; dz <= FeatherCells; dz++)
            {
                int d2 = dx * dx + dz * dz;
                if (d2 >= nearest || d2 > FeatherCells * FeatherCells + 1) continue;
                if (IsCleared(x + dx, z + dz) || (includeDug && Edited(x + dx, z + dz))) nearest = d2;
            }
        if (nearest == int.MaxValue) return 1f;
        float jitter = (Hash01(x, z, 44) - 0.5f) * 0.9f + (World.Noise(x, z, 3f, 101) - 0.5f) * 0.8f;
        return Mathf.SmoothStep(0.6f, 2.4f, Mathf.Sqrt(nearest) + jitter);
    }
}
