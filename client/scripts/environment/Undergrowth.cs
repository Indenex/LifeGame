using Godot;

namespace LifeGame;

// Meshes for the small plants that dress the forest floor (env-1): ferns under the
// trees, bushes and saplings where groves open into glades, wildflowers in the
// clearings and reeds at the water's edge. Ferns, reeds and flowers are drawn with
// the grass shader (UV.y runs root to tip), so they sway in the same gusts and take
// their color from the instance; bushes use the foliage surface.
public static class Undergrowth
{
    static Mesh? fern, reed, flowers, bush;

    public static Mesh Fern => fern ??= BuildFern();
    public static Mesh Reed => reed ??= BuildReed();
    public static Mesh Flowers => flowers ??= BuildFlowers();
    public static Mesh Bush => bush ??= BuildBush();

    // Wildflower colors, from the Look palette.
    // A colour leaned toward the muted olive of the stylised grass tufts (env-20), so
    // low plants sit with the meadow instead of standing out saturated green.
    public static Color Olive(Color color)
    {
        float lum = color.R * 0.3f + color.G * 0.59f + color.B * 0.11f;
        return color.Lerp(new Color(lum * 1.1f, lum * 1.05f, lum * 0.6f, color.A), 0.4f);
    }

    public static readonly Color[] FlowerColors = { Look.FlowerWhite, Look.FlowerButtercup, Look.FlowerViolet, Look.FlowerPink };

    // A fern: fronds arching up and out from the middle, about half a meter across.
    static Mesh BuildFern()
    {
        var st = Begin();
        const int fronds = 7;
        for (int f = 0; f < fronds; f++)
        {
            float angle = f * Mathf.Tau / fronds + Terrain.Hash01(f, 0, 500) * 0.5f;
            float length = 0.42f + Terrain.Hash01(f, 0, 501) * 0.18f;
            var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var side = new Vector3(-dir.Z, 0, dir.X);
            // Four points along the frond: up from the root, out, then drooping.
            Vector3 At(float t) => dir * (length * t) + Vector3.Up * (0.32f * Mathf.Sin(t * 2.6f) * length / 0.5f);
            float Width(float t) => 0.09f * Mathf.Sin(Mathf.Pi * Mathf.Min(1f, t * 1.15f + 0.08f));
            const int segments = 4;
            for (int s = 0; s < segments; s++)
            {
                float t0 = s / (float)segments, t1 = (s + 1) / (float)segments;
                Vector3 a = At(t0), b = At(t1);
                Vector3 w0 = side * Width(t0), w1 = side * Width(t1);
                Tri(st, a - w0, t0, a + w0, t0, b + w1, t1);
                Tri(st, a - w0, t0, b + w1, t1, b - w1, t1);
            }
        }
        return st.Commit();
    }

    // A clump of reeds: tall thin blades, mostly upright.
    static Mesh BuildReed()
    {
        var st = Begin();
        const int blades = 9;
        for (int b = 0; b < blades; b++)
        {
            float px = (Terrain.Hash01(b, 1, 510) - 0.5f) * 0.45f, pz = (Terrain.Hash01(b, 2, 510) - 0.5f) * 0.45f;
            float height = 0.75f + Terrain.Hash01(b, 3, 510) * 0.55f;
            float angle = Terrain.Hash01(b, 4, 510) * Mathf.Pi;
            var side = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.025f;
            var lean = new Vector3(Terrain.Hash01(b, 5, 510) - 0.5f, 0, Terrain.Hash01(b, 6, 510) - 0.5f) * 0.25f;
            var root = new Vector3(px, -0.05f, pz);
            var mid = root + lean * 0.35f + Vector3.Up * height * 0.55f;
            var tip = root + lean + Vector3.Up * height;
            Tri(st, root - side, 0f, root + side, 0f, mid + side * 0.7f, 0.55f);
            Tri(st, root - side, 0f, mid + side * 0.7f, 0.55f, mid - side * 0.7f, 0.55f);
            Tri(st, mid - side * 0.7f, 0.55f, mid + side * 0.7f, 0.55f, tip, 1f);
        }
        return st.Commit();
    }

    // A few flower heads just above the grass; the grass hides their stems.
    static Mesh BuildFlowers()
    {
        var st = Begin();
        const int heads = 6;
        for (int h = 0; h < heads; h++)
        {
            float px = (Terrain.Hash01(h, 1, 520) - 0.5f) * 0.8f, pz = (Terrain.Hash01(h, 2, 520) - 0.5f) * 0.8f;
            float y = 0.24f + Terrain.Hash01(h, 3, 520) * 0.16f;
            float r = 0.035f + Terrain.Hash01(h, 4, 520) * 0.02f;
            var c = new Vector3(px, y, pz);
            // Five petals around a center, tipped slightly up to the sky.
            for (int p = 0; p < 5; p++)
            {
                float a0 = p * Mathf.Tau / 5f, a1 = a0 + Mathf.Tau / 5f;
                var e0 = new Vector3(Mathf.Cos(a0) * r, r * 0.3f, Mathf.Sin(a0) * r);
                var e1 = new Vector3(Mathf.Cos(a1) * r, r * 0.3f, Mathf.Sin(a1) * r);
                Tri(st, c, 1f, c + e0, 1f, c + e1, 1f);
            }
        }
        return st.Commit();
    }

    // A low, rounded bush of leafy clumps, about a meter across.
    static Mesh BuildBush()
    {
        var st = Begin();
        Terrain.Append(st, Terrain.Blob(0.42f), Terrain.Xf(0, 0.36f, 0), new Color(1.08f, 1.06f, 1f), 0.1f);
        for (int k = 0; k < 4; k++)
        {
            float a = k * Mathf.Tau / 4f + 0.7f;
            Terrain.Append(st, Terrain.Blob(0.3f + 0.05f * (k % 2)), Terrain.Xf(Mathf.Cos(a) * 0.32f, 0.22f, Mathf.Sin(a) * 0.32f),
                new Color(0.9f, 0.92f, 0.9f), 0.08f);
        }
        return st.Commit();
    }

    static SurfaceTool Begin()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        return st;
    }

    static void Tri(SurfaceTool st, Vector3 a, float ha, Vector3 b, float hb, Vector3 c, float hc)
    {
        Vertex(st, a, ha);
        Vertex(st, b, hb);
        Vertex(st, c, hc);
    }

    static void Vertex(SurfaceTool st, Vector3 v, float tip)
    {
        st.SetNormal(Vector3.Up);
        st.SetUV(new Vector2(0, tip));
        st.SetColor(Colors.White);
        st.AddVertex(v);
    }
}
