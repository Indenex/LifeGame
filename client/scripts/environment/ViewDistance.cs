using System;
using System.Linq;
using Godot;

namespace LifeGame;

// How far the world is drawn in full (env-9, Justin: more real chunks, no pop-in, the
// edge hidden in fog). Far and Near are radii in chunks from the player's own chunk,
// from the Options page (Settings graphics/view_distance); --view-radius=N on the
// command line overrides both for testing. Atmosphere's painted shaders fade land,
// trees and water into the fog over the last quarter of far_land_radius, which is
// set to the edge of the drawn chunks, so nothing ever appears suddenly at the edge.
public static class ViewDistance
{
    public enum Level { Near, Far }
    public static readonly string[] Names = { "Near", "Far" };

    const int NearRadius = 5, FarRadius = 8;

    // Grass tufts are only built this many chunks out, and fade out before that.
    public const int GrassChunks = 3;

    public static Level Setting
    {
        get => (Level)Math.Clamp(Settings.Get("graphics", "view_distance", (int)Level.Far), 0, 1);
        set => Settings.Set("graphics", "view_distance", (int)value);
    }

    static readonly int? Override = ReadOverride();

    public static int Radius => Override ?? (Setting == Level.Near ? NearRadius : FarRadius);

    static int? ReadOverride()
    {
        foreach (var arg in OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()))
            if (arg.StartsWith("--view-radius=") && int.TryParse(arg["--view-radius=".Length..], out int r))
                return Math.Clamp(r, 1, 16);
        return null;
    }

    // Tells the fog where the drawn world ends: the nearest a chunk edge can be.
    // (Before Atmosphere's #289 registers far_land_radius, this logs one error.)
    public static void TellFog(int radius) =>
        RenderingServer.GlobalShaderParameterSet("far_land_radius", (float)(radius * Shared.World.ChunkSize));
}
