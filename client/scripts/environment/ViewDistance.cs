using System;
using System.Linq;
using Godot;

namespace LifeGame;

// How far the world is drawn in full (env-9, Justin: more real chunks, no pop-in, the
// edge hidden in fog). Justin's four steps, a slider under Options -> Graphics
// (Settings graphics/view_distance): Short 3 chunks out from the player's own chunk
// (48 m, as before), Near 5 (80 m), Far 8 (128 m, the default), Very far 10 (160 m).
// --view-radius=N on the command line overrides it for testing. Atmosphere's painted shaders fade land,
// trees and water into the fog over the last quarter of far_land_radius, which is
// set to the edge of the drawn chunks, so nothing ever appears suddenly at the edge.
public static class ViewDistance
{
    public enum Level { Short, Near, Far, VeryFar }
    public static readonly string[] Names = { "Short", "Near", "Far", "Very far" };
    static readonly int[] Radii = { 3, 5, 8, 10 };

    // Grass tufts are only built this many chunks out, and fade out before that.
    public const int GrassChunks = 3;

    public static Level Setting
    {
        get => (Level)Math.Clamp(Settings.Get("graphics", "view_distance", (int)Level.Far), 0, Radii.Length - 1);
        set => Settings.Set("graphics", "view_distance", (int)value);
    }

    static readonly int? Override = ReadOverride();

    public static int Radius => Override ?? Radii[(int)Setting];

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
