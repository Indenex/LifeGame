using System;

namespace LifeGame;

// How thick the grass tufts are drawn (Options -> Graphics -> Grass, Settings
// graphics/grass_detail): Normal draws every blade, as Justin approved (D-256); Low
// draws about half of them, for slower PCs. Changing it redraws every tuft at once.
public static class GrassDetail
{
    public static readonly string[] Names = { "Low", "Normal" };
    static readonly float[] Shares = { 0.5f, 1f };

    public static int Setting
    {
        get => Math.Clamp(Settings.Get("graphics", "grass_detail", 1), 0, Names.Length - 1);
        set
        {
            Settings.Set("graphics", "grass_detail", value);
            Terrain.RebuildTufts(Keep);
        }
    }

    // The share of each tuft's blades drawn.
    public static float Keep => Shares[Setting];
}
