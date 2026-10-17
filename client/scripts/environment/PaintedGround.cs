using System;
using System.Linq;
using Godot;

namespace LifeGame;

// A prototype of the ground in Justin's art references (docs/art-reference 02 and
// 03): thick, blade-shaped yellow-green grass in clumps, each blade painted in a lit,
// mid or shade tone with a lighter and a darker half; darker olive earth under it so
// the gaps read as shade; bare dirt on the deer trails; small mushrooms, twigs and
// old stumps scattered through. Only with --ground=painted on the command line, until
// Justin picks (D-154). Blades use the grass shader (UV.y root to tip), so they sway
// and take the season like today's grass.
public static class PaintedGround
{
    public static readonly bool On = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ground=painted");

    // Three painted tones for grass (sRGB, before the grass shader's warm tips).
    static readonly Color Lit = new(0.66f, 0.70f, 0.26f), Mid = new(0.47f, 0.55f, 0.19f), Shade = new(0.30f, 0.38f, 0.15f);
    // The earth under the grass and on paths.
    static readonly Color UnderGrass = new(0.30f, 0.30f, 0.15f), PathDirt = new(0.47f, 0.35f, 0.22f);

    static Mesh? blades, mushroom, twig;

    public static Mesh Blades => blades ??= BuildBlades();
    public static Mesh Mushroom => mushroom ??= BuildMushroom();
    public static Mesh Twig => twig ??= BuildTwig();

    // The ground colour: today's tile colour pulled darker and toward olive earth, so
    // it reads as shade between the blades; worn paths go to bare dirt.
    public static Color Ground(Color tile, float trail) =>
        tile.Darkened(0.2f).Lerp(UnderGrass, 0.35f).Lerp(PathDirt, Mathf.SmoothStep(0.25f, 0.7f, trail));

    // A grass clump's colour on a tile, at its lit tone (the blades bake their mid and
    // shade tones below it): brighter where the meadow is lush, and still darker
    // under trees, on the heath and in the wet (as the tile colour says).
    public static Color Grass(Color tile, float lush) =>
        Lit.Darkened(0.12f * (1f - Mathf.Clamp(lush * 1.2f - 0.15f, 0f, 1f))).Lerp(tile * (Lit.V / Mathf.Max(tile.V, 0.05f)), 0.3f);

    // One square metre of thick grass: three clumps of broad, tapering, arching
    // blades. Each blade is a lit, mid or shade tone (baked in the vertex colour,
    // multiplied by the instance colour), with its left half a touch lighter than
    // its right, like a painted leaf.
    static Mesh BuildBlades()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        int blade = 0;
        for (int c = 0; c < 3; c++)
        {
            float cx = (H(c, 0, 600) - 0.5f) * 0.6f, cz = (H(c, 1, 600) - 0.5f) * 0.6f;
            int count = 4 + (int)(H(c, 2, 600) * 3f);
            for (int b = 0; b < count; b++, blade++)
            {
                float angle = b * Mathf.Tau / count + H(blade, 3, 601) * 0.9f;
                var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var side = new Vector3(-dir.Z, 0, dir.X);
                var root = new Vector3(cx, -0.03f, cz) + dir * (0.02f + H(blade, 4, 601) * 0.08f);
                float length = 0.26f + H(blade, 5, 601) * 0.28f;
                float width = 0.045f + H(blade, 6, 601) * 0.03f;
                float lean = 0.25f + H(blade, 7, 601) * 0.35f;
                float roll = H(blade, 8, 601);
                var tone = roll < 0.3f ? Shade : roll < 0.7f ? Mid : Lit;
                // Vertex colours are relative to the instance colour (the lit tone), so
                // they stay at or below 1 (vertex colours are stored in 8 bits).
                var baked = new Color(tone.R / Lit.R, tone.G / Lit.G, tone.B / Lit.B).Darkened(0.06f);
                Vector3 At(float t) => root + dir * (lean * length * t * t) + Vector3.Up * (length * (t - 0.18f * t * t));
                float Width(float t) => width * Mathf.Pow(1f - t, 0.8f);
                const int segments = 3;
                for (int s = 0; s < segments; s++)
                {
                    float t0 = s / (float)segments, t1 = (s + 1) / (float)segments;
                    Vector3 a = At(t0), b2 = At(t1);
                    Vector3 w0 = side * Width(t0), w1 = side * Width(t1);
                    Tri(st, a - w0, t0, baked.Lightened(0.06f), a + w0, t0, baked.Darkened(0.06f), b2 + w1, t1, baked.Darkened(0.06f));
                    Tri(st, a - w0, t0, baked.Lightened(0.06f), b2 + w1, t1, baked.Darkened(0.06f), b2 - w1, t1, baked.Lightened(0.06f));
                }
            }
        }
        return st.Commit();
    }

    // A small mushroom about 12 cm tall: a pale stem under a rounded russet cap. The
    // instance colour only varies them a little.
    static Mesh BuildMushroom()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        Terrain.Append(st, new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.018f, Height = 0.09f, RadialSegments = 6, Rings = 1 }, Terrain.Xf(0, 0.045f, 0), new Color(0.9f, 0.86f, 0.76f));
        Terrain.Append(st, new SphereMesh { Radius = 0.045f, Height = 0.045f, RadialSegments = 8, Rings = 3, IsHemisphere = true }, Terrain.Xf(0, 0.085f, 0), new Color(0.66f, 0.38f, 0.2f));
        return st.Commit();
    }

    // A dead twig lying in the grass: a bent stick with a fork, about 40 cm long.
    static Mesh BuildTwig()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var stick = new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.014f, Height = 0.4f, RadialSegments = 5, Rings = 1 };
        Terrain.Append(st, stick, new Transform3D(new Basis(Vector3.Forward, Mathf.Pi / 2 - 0.08f), new Vector3(0, 0.02f, 0)), Colors.White);
        var fork = new CylinderMesh { TopRadius = 0.005f, BottomRadius = 0.009f, Height = 0.16f, RadialSegments = 5, Rings = 1 };
        Terrain.Append(st, fork, new Transform3D(new Basis(Vector3.Up, 0.6f) * new Basis(Vector3.Forward, Mathf.Pi / 2 - 0.25f), new Vector3(0.1f, 0.035f, 0.04f)), new Color(0.9f, 0.9f, 0.9f));
        return st.Commit();
    }

    static float H(int a, int b, uint salt) => Terrain.Hash01(a, b, salt);

    static void Tri(SurfaceTool st, Vector3 a, float ta, Color ca, Vector3 b, float tb, Color cb, Vector3 c, float tc, Color cc)
    {
        Vertex(st, a, ta, ca);
        Vertex(st, b, tb, cb);
        Vertex(st, c, tc, cc);
    }

    static void Vertex(SurfaceTool st, Vector3 v, float tip, Color color)
    {
        st.SetNormal(Vector3.Up);
        st.SetUV(new Vector2(0, tip));
        st.SetColor(color);
        st.AddVertex(v);
    }
}
