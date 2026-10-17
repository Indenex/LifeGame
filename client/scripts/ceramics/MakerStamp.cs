using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The twelve maker's-mark glyphs (ceramics-15), drawn once into a small atlas that the clay
// shader samples. Each glyph is a simple shape described by a distance function on a square
// from -1 to 1 (y down); the atlas holds them in a 4 by 3 grid. Rough prototype art until
// Justin picks the look (D-154): carved-stamp impressions, dark in the dent.
public static class MakerStamp
{
    public const int Cols = 4, Rows = 3, Cell = 64;

    static ImageTexture? atlas;
    public static ImageTexture Atlas => atlas ??= Build();

    static ImageTexture Build()
    {
        var image = Image.CreateEmpty(Cols * Cell, Rows * Cell, false, Image.Format.Rgba8);
        for (int g = 0; g < Marks.Glyphs; g++)
        {
            int cx = g % Cols, cz = g / Cols;
            for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                {
                    var p = new Vector2((x + 0.5f) / Cell * 2f - 1f, (y + 0.5f) / Cell * 2f - 1f);
                    float d = Distance(g, p);
                    // Soft edge about one pixel wide; the cell edge fades so cells never bleed.
                    float edge = Mathf.Clamp(0.5f - d * Cell * 0.5f, 0f, 1f);
                    float fade = Mathf.Clamp((1f - Mathf.Max(Mathf.Abs(p.X), Mathf.Abs(p.Y))) * 8f, 0f, 1f);
                    image.SetPixel(cx * Cell + x, cz * Cell + y, new Color(1, 1, 1, edge * fade));
                }
        }
        return ImageTexture.CreateFromImage(image);
    }

    // Signed distance to glyph g at p (negative inside the mark).
    public static float Distance(int g, Vector2 p) => g switch
    {
        0 => Sun(p),
        1 => Moon(p),
        2 => Wave(p),
        3 => Leaf(p),
        4 => Tree(p),
        5 => Antler(p),
        6 => Fish(p),
        7 => Bird(p),
        8 => Spiral(p),
        9 => Hand(p),
        10 => Flame(p),
        _ => Seed(p),
    };

    // ---- distance helpers
    static float Circle(Vector2 p, Vector2 c, float r) => (p - c).Length() - r;

    static float Segment(Vector2 p, Vector2 a, Vector2 b, float half)
    {
        var pa = p - a;
        var ba = b - a;
        float h = Mathf.Clamp(pa.Dot(ba) / ba.Dot(ba), 0f, 1f);
        return (pa - ba * h).Length() - half;
    }

    // ---- the glyphs
    static float Sun(Vector2 p)
    {
        float d = Circle(p, Vector2.Zero, 0.34f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.Tau / 8f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            d = Mathf.Min(d, Segment(p, dir * 0.52f, dir * 0.86f, 0.07f));
        }
        return d;
    }

    static float Moon(Vector2 p) =>
        Mathf.Max(Circle(p, Vector2.Zero, 0.72f), -Circle(p, new Vector2(0.32f, -0.1f), 0.6f));

    static float Wave(Vector2 p)
    {
        float d = 10f;
        for (int k = -1; k <= 1; k++)
        {
            float y = 0.22f * Mathf.Sin(p.X * 4.2f) + k * 0.46f;
            float band = Mathf.Abs(p.Y - y) * 0.9f - 0.075f;
            band = Mathf.Max(band, Mathf.Abs(p.X) - 0.82f);
            d = Mathf.Min(d, band);
        }
        return d;
    }

    static float Leaf(Vector2 p)
    {
        float lens = Mathf.Max(Circle(p, new Vector2(-0.55f, 0), 0.98f), Circle(p, new Vector2(0.55f, 0), 0.98f));
        float rib = Segment(p, new Vector2(0, 0.7f), new Vector2(0, -0.1f), 0.035f);
        float stem = Segment(p, new Vector2(0, 0.7f), new Vector2(0, 0.9f), 0.045f);
        return Mathf.Min(Mathf.Max(lens, -rib), stem);
    }

    static float Tree(Vector2 p)
    {
        float d = Segment(p, new Vector2(0, 0.85f), new Vector2(0, 0.2f), 0.08f);
        d = Mathf.Min(d, Segment(p, new Vector2(0, -0.85f), new Vector2(0, 0.2f), 0.06f));
        float[] ys = { -0.45f, -0.05f, 0.35f };
        float[] widths = { 0.38f, 0.55f, 0.7f };
        for (int i = 0; i < 3; i++)
        {
            d = Mathf.Min(d, Segment(p, new Vector2(0, ys[i]), new Vector2(-widths[i], ys[i] + 0.4f), 0.07f));
            d = Mathf.Min(d, Segment(p, new Vector2(0, ys[i]), new Vector2(widths[i], ys[i] + 0.4f), 0.07f));
        }
        return d;
    }

    static float Antler(Vector2 p)
    {
        float d = Segment(p, new Vector2(0, 0.85f), new Vector2(0, 0.2f), 0.08f);
        foreach (float s in new[] { -1f, 1f })
        {
            var fork = new Vector2(0, 0.2f);
            var tip = new Vector2(0.62f * s, -0.7f);
            d = Mathf.Min(d, Segment(p, fork, tip, 0.07f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.28f * s, -0.2f), new Vector2(0.72f * s, -0.15f), 0.06f));
            d = Mathf.Min(d, Segment(p, new Vector2(0.44f * s, -0.45f), new Vector2(0.15f * s, -0.72f), 0.055f));
        }
        return d;
    }

    static float Fish(Vector2 p)
    {
        var q = new Vector2((p.X + 0.12f) / 0.62f, p.Y / 0.34f);
        float body = (q.Length() - 1f) * 0.34f;
        float tail = Mathf.Min(Segment(p, new Vector2(0.42f, 0), new Vector2(0.86f, -0.36f), 0.07f),
                               Segment(p, new Vector2(0.42f, 0), new Vector2(0.86f, 0.36f), 0.07f));
        tail = Mathf.Min(tail, Segment(p, new Vector2(0.86f, -0.36f), new Vector2(0.86f, 0.36f), 0.06f));
        float d = Mathf.Min(body, tail);
        return Mathf.Max(d, -Circle(p, new Vector2(-0.42f, -0.06f), 0.06f));
    }

    static float Bird(Vector2 p)
    {
        float d = Segment(p, new Vector2(-0.85f, -0.25f), new Vector2(-0.3f, 0.12f), 0.075f);
        d = Mathf.Min(d, Segment(p, new Vector2(-0.3f, 0.12f), new Vector2(0, -0.05f), 0.075f));
        d = Mathf.Min(d, Segment(p, new Vector2(0, -0.05f), new Vector2(0.3f, 0.12f), 0.075f));
        d = Mathf.Min(d, Segment(p, new Vector2(0.3f, 0.12f), new Vector2(0.85f, -0.25f), 0.075f));
        d = Mathf.Min(d, Segment(p, new Vector2(0, -0.05f), new Vector2(0, 0.7f), 0.06f));
        return d;
    }

    static float Spiral(Vector2 p)
    {
        float d = 10f;
        var prev = new Vector2(0.05f, 0);
        for (int i = 1; i <= 48; i++)
        {
            float t = i / 48f;
            float a = t * Mathf.Tau * 2.6f;
            float r = 0.05f + t * 0.8f;
            var next = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            d = Mathf.Min(d, Segment(p, prev, next, 0.07f));
            prev = next;
        }
        return d;
    }

    static float Hand(Vector2 p)
    {
        float d = Circle(p, new Vector2(0, 0.28f), 0.36f);
        d = Mathf.Min(d, Segment(p, new Vector2(-0.5f, 0.3f), new Vector2(-0.85f, 0.0f), 0.075f));
        d = Mathf.Min(d, Segment(p, new Vector2(-0.24f, 0.05f), new Vector2(-0.3f, -0.7f), 0.075f));
        d = Mathf.Min(d, Segment(p, new Vector2(-0.07f, 0.0f), new Vector2(-0.06f, -0.85f), 0.075f));
        d = Mathf.Min(d, Segment(p, new Vector2(0.12f, 0.02f), new Vector2(0.14f, -0.78f), 0.075f));
        d = Mathf.Min(d, Segment(p, new Vector2(0.3f, 0.08f), new Vector2(0.4f, -0.6f), 0.07f));
        return d;
    }

    static float Flame(Vector2 p)
    {
        float body = Mathf.Min(Circle(p, new Vector2(0, 0.28f), 0.46f),
                               Segment(p, new Vector2(0, 0.0f), new Vector2(0.05f, -0.82f), 0.05f));
        // A tapering tip: wide near the bowl, a point at the top.
        float tip = Mathf.Max(Segment(p, new Vector2(0, 0.25f), new Vector2(0.04f, -0.82f), 0.0f) - Mathf.Lerp(0.42f, 0.02f, Mathf.Clamp((0.25f - p.Y) / 1.07f, 0f, 1f)), -1f);
        float d = Mathf.Min(body, tip);
        return Mathf.Max(d, -Circle(p, new Vector2(0, 0.4f), 0.14f));
    }

    static float Seed(Vector2 p)
    {
        var s = new Vector2(Mathf.Cos(0.35f), Mathf.Sin(0.35f));
        var q = new Vector2(p.X * s.X + p.Y * s.Y, -p.X * s.Y + p.Y * s.X);
        float body = (new Vector2(q.X / 0.4f, q.Y / 0.66f).Length() - 1f) * 0.4f;
        float groove = Segment(q, new Vector2(0, -0.45f), new Vector2(0, 0.5f), 0.035f);
        return Mathf.Max(body, -groove);
    }
}
