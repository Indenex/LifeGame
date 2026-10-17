using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The sundial (building-18; model by Modeling, model-12): a squat dressed-stone plinth with a
// round stone face on top, hour lines cut into it and a slanted gnomon whose tip stands
// over the face's centre. The gnomon's real shadow from the sun falls across the face, and
// the hour lines are laid where the tip's shadow points at each hour of the game's sun path,
// so it tells the time. Stage 0 sets the plinth; stage 1 the face, the lines and the stick.
public static partial class StructureModel
{
    internal const float SundialFaceY = 0.56f, SundialFaceRadius = 0.27f, SundialTipHeight = 0.17f;

    internal static List<Piece> Sundial()
    {
        var list = new List<Piece>();
        var corners = new Vector3[8];
        for (int i = 0; i < corners.Length; i++)
            corners[i] = Around(0.46f, i * Mathf.Tau / corners.Length, 0);
        Site(list, corners);

        // Stage 0: a footing slab, then the plinth, a little narrower at the top.
        Add(list, 0, Stone, Prism(0.3f, 0.28f, 0f, 0.07f, 9, 0.02f, 201));
        Add(list, 0, Stone, Prism(0.17f, 0.14f, 0.07f, 0.47f, 7, 0.012f, 202));
        Add(list, 0, Stone, Prism(0.2f, 0.21f, 0.47f, 0.5f, 9, 0.01f, 203));

        // Stage 1: the face, its hour lines, and the stick.
        Add(list, 1, Stone, Prism(SundialFaceRadius, SundialFaceRadius + 0.01f, 0.5f, SundialFaceY, 16, 0.008f, 204));
        Add(list, 1, Carved, HourLines());
        Add(list, 1, Wood, Gnomon());
        return list;
    }

    // Where the sun stands at a time of day, as a unit vector toward it. Mirrors
    // Atmosphere.ApplyLight (elevation SunHeight × 55°, azimuth -40° + (t - 0.68) × 360°);
    // keep the two in step.
    internal static Vector3 SunToward(float timeOfDay)
    {
        float elevation = WorldTime.SunHeight(timeOfDay) * 55f, azimuth = -40f + (timeOfDay - 0.68f) * 360f;
        return Basis.FromEuler(new Vector3(Mathf.DegToRad(-elevation), Mathf.DegToRad(azimuth), 0)).Z;
    }

    // The angle (in Around's terms) the tip's shadow points from the face's centre at an hour.
    static float ShadowAngle(float hour)
    {
        var toward = SunToward(hour / 24f);
        return Mathf.Atan2(-toward.X, -toward.Z);
    }

    // One cut line per daylight hour, 7 to 17; noon's runs right to the centre and the
    // quarter hours (9 and 15) are a touch longer than the rest. A dot at the outer end of each.
    static Mesh HourLines()
    {
        var st = Begin();
        const float y = SundialFaceY + 0.002f;
        var dark = new Color(0.8f, 0.8f, 0.8f);
        for (int hour = 7; hour <= 17; hour++)
        {
            float a = ShadowAngle(hour);
            float inner = hour == 12 ? 0.03f : hour is 9 or 15 ? 0.09f : 0.12f, outer = SundialFaceRadius - 0.035f;
            float half = hour == 12 ? 0.007f : 0.005f;
            var along = Around(1, a, 0);
            var side = new Vector3(along.Z, 0, -along.X) * half;
            var p0 = along * inner + new Vector3(0, y, 0);
            var p1 = along * outer + new Vector3(0, y, 0);
            Quad(st, p0 + side, p1 + side, p1 - side, p0 - side, Vector3.Up, dark);
            // The hour's dot just beyond the line.
            var dot = along * (outer + 0.016f) + new Vector3(0, y, 0);
            float r = hour == 12 ? 0.011f : 0.007f;
            for (int s = 0; s < 6; s++)
            {
                var d0 = dot + Around(r, s * Mathf.Tau / 6, 0);
                var d1 = dot + Around(r, (s + 1) * Mathf.Tau / 6, 0);
                Vert(st, dot, Vector3.Up, dark); Vert(st, d1, Vector3.Up, dark); Vert(st, d0, Vector3.Up, dark);
            }
        }
        // A ring cut round the edge.
        const int segs = 32;
        for (int s = 0; s < segs; s++)
        {
            float a0 = s * Mathf.Tau / segs, a1 = (s + 1) * Mathf.Tau / segs;
            float r0 = SundialFaceRadius - 0.012f, r1 = SundialFaceRadius - 0.006f;
            Quad(st, Around(r0, a1, y), Around(r1, a1, y), Around(r1, a0, y), Around(r0, a0, y), Vector3.Up, dark);
        }
        return st.Commit();
    }

    // The gnomon is the stick the recipe asks for: pushed into the face near its sunward rim at
    // noon and leaning over, its tip standing above the centre, so its shadow sweeps the lines.
    static Mesh Gnomon()
    {
        float noon = ShadowAngle(12) + Mathf.Pi;
        var foot = Around(SundialFaceRadius - 0.06f, noon, SundialFaceY - 0.01f);
        var tip = new Vector3(0, SundialFaceY + SundialTipHeight, 0);
        return Stick(foot, tip + (tip - foot).Normalized() * 0.012f, 0.016f);
    }

    // A dressed-stone prism, its sides very slightly uneven, capped top and bottom.
    static Mesh Prism(float rBottom, float rTop, float yBottom, float yTop, int segs, float rough, int seed)
    {
        var st = Begin();
        float R(int s, float r) => r + (Hash(s % segs, seed) - 0.5f) * 2f * rough;
        for (int s = 0; s < segs; s++)
        {
            float a0 = s * Mathf.Tau / segs, a1 = (s + 1) * Mathf.Tau / segs;
            var b0 = Around(R(s, rBottom), a0, yBottom); var b1 = Around(R(s + 1, rBottom), a1, yBottom);
            var t0 = Around(R(s, rTop), a0, yTop); var t1 = Around(R(s + 1, rTop), a1, yTop);
            var shade = Shade(seed * 7 + s);
            Quad(st, b0, b1, t1, t0, Around(1, (a0 + a1) / 2, 0), shade);
            Vert(st, new Vector3(0, yTop, 0), Vector3.Up, shade); Vert(st, t1, Vector3.Up, shade); Vert(st, t0, Vector3.Up, shade);
            Vert(st, new Vector3(0, yBottom, 0), Vector3.Down, shade); Vert(st, b0, Vector3.Down, shade); Vert(st, b1, Vector3.Down, shade);
        }
        return st.Commit();
    }

    // The cut lines: the same stone, much darker.
    static ShaderMaterial Carved => carved ??= Tinted(Look.RockSurface, Look.Rock.Darkened(0.55f));
    static ShaderMaterial? carved;
}
