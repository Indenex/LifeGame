// The maker's-mark glyphs (ceramics-15): about fifty line drawings in five groups. Each is a few
// polylines in the unit square (0,0 top left). They are written here on a -1 to 1 square with y
// down and converted once, which is easier to draw by hand. Ids are append-only; never renumber.
// Keep this file free of engine and SpacetimeDB types.
using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeGame.Shared
{
    static class MarkGlyphs
    {
        public static readonly IReadOnlyList<MarkGroup> Groups = Build();
        public static readonly IReadOnlyList<Glyph> All = Groups.SelectMany(g => g.Glyphs).ToArray();
        public static readonly IReadOnlyDictionary<ushort, Glyph> ById = All.ToDictionary(g => g.Id);

        // ---- drawing helpers (coordinates -1 to 1, y down)
        const int Steps = 24;

        static (float x, float y)[] Line(params float[] xy)
        {
            var pts = new (float, float)[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = (xy[i * 2], xy[i * 2 + 1]);
            return pts;
        }

        static (float x, float y)[] Closed(params float[] xy)
        {
            var open = Line(xy);
            return open.Append(open[0]).ToArray();
        }

        // An arc of a circle from a0 to a1 degrees (0 is +x, 90 is down), the short way round if a0 < a1.
        static (float x, float y)[] Arc(float cx, float cy, float r, float a0, float a1, int n = Steps)
        {
            var pts = new (float, float)[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float a = (a0 + (a1 - a0) * i / n) * MathF.PI / 180f;
                pts[i] = (cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
            }
            return pts;
        }

        static (float x, float y)[] Circle(float cx, float cy, float r) => Arc(cx, cy, r, 0f, 360f);

        static (float x, float y)[] Ellipse(float cx, float cy, float rx, float ry, float turn)
        {
            float t = turn * MathF.PI / 180f, ct = MathF.Cos(t), st = MathF.Sin(t);
            var pts = new (float, float)[Steps + 1];
            for (int i = 0; i <= Steps; i++)
            {
                float a = i * MathF.Tau / Steps;
                float px = MathF.Cos(a) * rx, py = MathF.Sin(a) * ry;
                pts[i] = (cx + px * ct - py * st, cy + px * st + py * ct);
            }
            return pts;
        }

        static (float x, float y)[] Poly(float cx, float cy, float r, int sides, float first)
        {
            var pts = new (float, float)[sides + 1];
            for (int i = 0; i <= sides; i++)
            {
                float a = (first + 360f * (i % sides) / sides) * MathF.PI / 180f;
                pts[i] = (cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
            }
            return pts;
        }

        static (float x, float y)[] Star(float cx, float cy, float outer, float inner, int points, float first)
        {
            var pts = new (float, float)[points * 2 + 1];
            for (int i = 0; i <= points * 2; i++)
            {
                float a = (first + 180f * (i % (points * 2)) / points) * MathF.PI / 180f;
                float r = i % 2 == 0 ? outer : inner;
                pts[i] = (cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
            }
            return pts;
        }

        // Converts -1..1 to the unit square.
        static (float x, float y)[] U((float x, float y)[] stroke) =>
            stroke.Select(p => ((p.x + 1f) / 2f, (p.y + 1f) / 2f)).ToArray();

        static Glyph G(ushort id, string name, params (float x, float y)[][] strokes) =>
            new(id, name, strokes.Select(U).ToArray());

        static MarkGroup[] Build() => new[]
        {
            new MarkGroup("Nature", new[]
            {
                G(0, "Sun", Circle(0, 0, 0.34f), Line(0.55f, 0, 0.92f, 0), Line(-0.55f, 0, -0.92f, 0), Line(0, 0.55f, 0, 0.92f), Line(0, -0.55f, 0, -0.92f),
                    Line(0.39f, 0.39f, 0.65f, 0.65f), Line(-0.39f, 0.39f, -0.65f, 0.65f), Line(0.39f, -0.39f, 0.65f, -0.65f), Line(-0.39f, -0.39f, -0.65f, -0.65f)),
                G(1, "Crescent moon", Arc(0, 0, 0.8f, 40f, 320f), Arc(0.35f, 0, 0.577f, 297.1f, 62.9f)),
                G(2, "Waves", WaveLine(-0.5f), WaveLine(0f), WaveLine(0.5f)),
                G(3, "Leaf", LeafOutline(), Line(0, 0.95f, 0, -0.5f), Line(0, 0.3f, -0.3f, 0.05f), Line(0, 0.3f, 0.3f, 0.05f), Line(0, -0.1f, -0.25f, -0.3f), Line(0, -0.1f, 0.25f, -0.3f)),
                G(4, "Fir tree", Closed(0, -0.92f, 0.38f, -0.3f, 0.2f, -0.3f, 0.52f, 0.12f, 0.3f, 0.12f, 0.66f, 0.5f, 0.08f, 0.5f, 0.08f, 0.9f, -0.08f, 0.9f, -0.08f, 0.5f, -0.66f, 0.5f, -0.3f, 0.12f, -0.52f, 0.12f, -0.2f, -0.3f, -0.38f, -0.3f)),
                G(5, "Mountain", Line(-0.92f, 0.62f, -0.35f, -0.5f, 0f, 0.05f, 0.3f, -0.25f, 0.92f, 0.62f, -0.92f, 0.62f), Line(-0.52f, -0.16f, -0.35f, 0.05f, -0.2f, -0.12f)),
                G(6, "Flower", Circle(0, 0, 0.14f), Circle(0, -0.5f, 0.28f), Circle(0.475f, -0.155f, 0.28f), Circle(0.29f, 0.405f, 0.28f), Circle(-0.29f, 0.405f, 0.28f), Circle(-0.475f, -0.155f, 0.28f)),
                G(7, "Raindrop", Concat(new[] { (0f, -0.88f) }, Arc(0, 0.3f, 0.5f, -25.8f, 205.8f, 28), new[] { (0f, -0.88f) })),
                G(8, "Mushroom", Arc(0, -0.05f, 0.8f, 180f, 360f), Line(-0.8f, -0.05f, 0.8f, -0.05f), Line(-0.22f, -0.05f, -0.22f, 0.85f, 0.22f, 0.85f, 0.22f, -0.05f), Circle(-0.35f, -0.4f, 0.1f), Circle(0.3f, -0.5f, 0.08f)),
                G(9, "Spiral", SpiralLine()),
            }),
            new MarkGroup("Creatures", new[]
            {
                G(10, "Fish", FishBody(), Closed(0.4f, 0, 0.9f, -0.42f, 0.9f, 0.42f), Circle(-0.5f, -0.06f, 0.06f)),
                G(11, "Bird", Line(-0.92f, -0.1f, -0.7f, -0.38f, -0.4f, -0.45f, -0.15f, -0.2f, 0f, 0.15f, 0.15f, -0.2f, 0.4f, -0.45f, 0.7f, -0.38f, 0.92f, -0.1f), Line(0f, 0.15f, 0f, 0.85f)),
                G(12, "Antler", Line(0, 0.9f, 0, 0.1f), Line(0, 0.1f, -0.62f, -0.8f), Line(0, 0.1f, 0.62f, -0.8f), Line(-0.3f, -0.3f, -0.75f, -0.25f), Line(0.3f, -0.3f, 0.75f, -0.25f), Line(-0.45f, -0.55f, -0.12f, -0.8f), Line(0.45f, -0.55f, 0.12f, -0.8f)),
                G(13, "Paw", Ellipse(0, 0.35f, 0.5f, 0.42f, 0f), Circle(-0.55f, -0.25f, 0.17f), Circle(-0.2f, -0.6f, 0.17f), Circle(0.2f, -0.6f, 0.17f), Circle(0.55f, -0.25f, 0.17f)),
                G(14, "Snake", SnakeLine(), Circle(0.0f, -0.85f, 0.1f)),
                G(15, "Owl", Circle(0, 0.1f, 0.72f), Circle(-0.3f, -0.05f, 0.22f), Circle(0.3f, -0.05f, 0.22f), Circle(-0.3f, -0.05f, 0.07f), Circle(0.3f, -0.05f, 0.07f), Closed(0, 0.08f, -0.1f, 0.28f, 0.1f, 0.28f), Line(-0.55f, -0.55f, -0.6f, -0.9f, -0.3f, -0.65f), Line(0.55f, -0.55f, 0.6f, -0.9f, 0.3f, -0.65f)),
                G(16, "Butterfly", Ellipse(-0.42f, -0.25f, 0.4f, 0.3f, -30f), Ellipse(0.42f, -0.25f, 0.4f, 0.3f, 30f), Ellipse(-0.3f, 0.4f, 0.28f, 0.22f, 30f), Ellipse(0.3f, 0.4f, 0.28f, 0.22f, -30f), Line(0, -0.5f, 0, 0.7f), Line(0, -0.5f, -0.2f, -0.85f), Line(0, -0.5f, 0.2f, -0.85f)),
                G(17, "Hand", Circle(0, 0.28f, 0.36f), Line(-0.5f, 0.3f, -0.85f, 0f), Line(-0.24f, 0.05f, -0.3f, -0.7f), Line(-0.07f, 0f, -0.06f, -0.85f), Line(0.12f, 0.02f, 0.14f, -0.78f), Line(0.3f, 0.08f, 0.4f, -0.6f)),
                G(18, "Feather", Line(-0.65f, 0.9f, 0.65f, -0.9f), Ellipse(0.05f, -0.05f, 0.82f, 0.28f, -55f), Line(-0.1f, 0.3f, 0.3f, 0.55f), Line(0.15f, -0.05f, 0.5f, 0.2f), Line(0.4f, -0.4f, 0.65f, -0.15f)),
                G(19, "Fox", Closed(-0.82f, -0.82f, -0.3f, -0.32f, 0.3f, -0.32f, 0.82f, -0.82f, 0.7f, 0.1f, 0, 0.88f, -0.7f, 0.1f), Line(-0.4f, -0.05f, -0.2f, 0.05f), Line(0.4f, -0.05f, 0.2f, 0.05f), Circle(0, 0.62f, 0.07f)),
            }),
            new MarkGroup("Runes", new[]
            {
                G(20, "Rune of voice", Line(-0.3f, -0.9f, -0.3f, 0.9f), Line(-0.3f, -0.9f, 0.4f, -0.45f), Line(-0.3f, -0.35f, 0.4f, 0.1f)),
                G(21, "Rune of shelter", Line(0, 0.9f, 0, -0.9f), Line(0, -0.1f, -0.6f, -0.75f), Line(0, -0.1f, 0.6f, -0.75f)),
                G(22, "Rune of the spear", Line(0, 0.9f, 0, -0.9f), Line(-0.55f, -0.4f, 0, -0.9f, 0.55f, -0.4f)),
                G(23, "Rune of birth", Line(-0.4f, -0.9f, -0.4f, 0.9f), Line(-0.4f, -0.9f, 0.4f, -0.45f, -0.4f, 0f, 0.45f, 0.5f, -0.4f, 0.9f)),
                G(24, "Rune of home", Closed(0, -0.8f, 0.5f, -0.1f, 0, 0.4f, -0.5f, -0.1f), Line(-0.5f, 0.9f, 0, 0.4f, 0.5f, 0.9f)),
                G(25, "Rune of dawn", Line(-0.6f, -0.8f, -0.6f, 0.8f), Line(0.6f, -0.8f, 0.6f, 0.8f), Line(-0.6f, -0.8f, 0.6f, 0.8f), Line(-0.6f, 0.8f, 0.6f, -0.8f)),
                G(26, "Rune of gifts", Line(-0.7f, -0.8f, 0.7f, 0.8f), Line(0.7f, -0.8f, -0.7f, 0.8f)),
                G(27, "Rune of hail", Line(-0.5f, -0.9f, -0.5f, 0.9f), Line(0.5f, -0.9f, 0.5f, 0.9f), Line(-0.5f, -0.3f, 0.5f, 0.3f)),
                G(28, "Rune of the sun", Line(0.4f, -0.9f, -0.4f, -0.05f, 0.4f, 0.05f, -0.4f, 0.9f)),
                G(29, "Rune of seed", Closed(0, -0.8f, 0.6f, 0, 0, 0.8f, -0.6f, 0), Line(-0.2f, 0, 0.2f, 0)),
            }),
            new MarkGroup("Alchemy and science", new[]
            {
                G(30, "Fire", Closed(0, -0.8f, 0.75f, 0.6f, -0.75f, 0.6f)),
                G(31, "Water", Closed(0, 0.8f, 0.75f, -0.6f, -0.75f, -0.6f)),
                G(32, "Earth", Closed(0, 0.8f, 0.75f, -0.6f, -0.75f, -0.6f), Line(-0.42f, -0.05f, 0.42f, -0.05f)),
                G(33, "Air", Closed(0, -0.8f, 0.75f, 0.6f, -0.75f, 0.6f), Line(-0.42f, 0.1f, 0.42f, 0.1f)),
                G(34, "Mercury", Circle(0, -0.1f, 0.36f), Arc(0, -0.52f, 0.3f, 180f, 360f, 14), Line(0, 0.26f, 0, 0.9f), Line(-0.3f, 0.6f, 0.3f, 0.6f)),
                G(35, "Sulphur", Closed(0, -0.9f, 0.55f, 0f, -0.55f, 0f), Line(0, 0, 0, 0.9f), Line(-0.3f, 0.55f, 0.3f, 0.55f)),
                G(36, "Gold", Circle(0, 0, 0.72f), Circle(0, 0, 0.1f)),
                G(37, "Salt", Circle(0, 0, 0.72f), Line(-0.72f, 0, 0.72f, 0)),
                G(38, "Atom", Ellipse(0, 0, 0.88f, 0.3f, 0f), Ellipse(0, 0, 0.88f, 0.3f, 60f), Ellipse(0, 0, 0.88f, 0.3f, 120f), Circle(0, 0, 0.1f)),
                G(39, "Flask", Line(-0.18f, -0.9f, -0.18f, -0.3f, -0.7f, 0.7f, 0.7f, 0.7f, 0.18f, -0.3f, 0.18f, -0.9f), Line(-0.3f, -0.9f, 0.3f, -0.9f), Line(-0.45f, 0.25f, 0.45f, 0.25f)),
            }),
            new MarkGroup("Geometric", new[]
            {
                G(40, "Triangle", Poly(0, 0.1f, 0.85f, 3, -90f)),
                G(41, "Square", Closed(-0.7f, -0.7f, 0.7f, -0.7f, 0.7f, 0.7f, -0.7f, 0.7f)),
                G(42, "Diamond", Closed(0, -0.9f, 0.6f, 0, 0, 0.9f, -0.6f, 0), Line(0, -0.9f, 0, 0.9f)),
                G(43, "Hexagon", Poly(0, 0, 0.85f, 6, 0f), Poly(0, 0, 0.4f, 6, 0f)),
                G(44, "Star", Star(0, 0.05f, 0.9f, 0.38f, 5, -90f)),
                G(45, "Eight-point star", Star(0, 0, 0.9f, 0.35f, 8, -90f)),
                G(46, "Cross", Closed(-0.25f, -0.85f, 0.25f, -0.85f, 0.25f, -0.25f, 0.85f, -0.25f, 0.85f, 0.25f, 0.25f, 0.25f, 0.25f, 0.85f, -0.25f, 0.85f, -0.25f, 0.25f, -0.85f, 0.25f, -0.85f, -0.25f, -0.25f, -0.25f)),
                G(47, "Rings", Circle(0, 0, 0.85f), Circle(0, 0, 0.55f), Circle(0, 0, 0.25f)),
                G(48, "Zigzag", Line(-0.9f, 0.4f, -0.6f, -0.4f, -0.3f, 0.4f, 0f, -0.4f, 0.3f, 0.4f, 0.6f, -0.4f, 0.9f, 0.4f)),
                G(49, "Knot", Circle(0, -0.3f, 0.45f), Circle(-0.38f, 0.3f, 0.45f), Circle(0.38f, 0.3f, 0.45f)),
            }),
        };

        // ---- shaped strokes
        static (float x, float y)[] Concat((float, float)[] a, (float, float)[] b) => a.Concat(b).ToArray();
        static (float x, float y)[] Concat((float, float)[] a, (float, float)[] b, (float, float)[] c) => a.Concat(b).Concat(c).ToArray();

        static (float x, float y)[] WaveLine(float y)
        {
            var pts = new (float, float)[Steps + 1];
            for (int i = 0; i <= Steps; i++)
            {
                float x = -0.9f + 1.8f * i / Steps;
                pts[i] = (x, y + 0.2f * MathF.Sin(x * 4.2f));
            }
            return pts;
        }

        static (float x, float y)[] LeafOutline()
        {
            var pts = new (float, float)[Steps * 2 + 1];
            for (int i = 0; i <= Steps; i++)
            {
                float t = i / (float)Steps;
                pts[i] = (-0.52f * MathF.Sin(MathF.PI * t), -0.85f + 1.7f * t);
            }
            for (int i = 1; i <= Steps; i++)
            {
                float t = 1f - i / (float)Steps;
                pts[Steps + i] = (0.52f * MathF.Sin(MathF.PI * t), -0.85f + 1.7f * t);
            }
            return pts;
        }

        static (float x, float y)[] SpiralLine()
        {
            const int n = 60;
            var pts = new (float, float)[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n, a = t * MathF.Tau * 2.7f, r = 0.05f + t * 0.82f;
                pts[i] = (MathF.Cos(a) * r, MathF.Sin(a) * r);
            }
            return pts;
        }

        static (float x, float y)[] SnakeLine()
        {
            var pts = new (float, float)[Steps + 1];
            for (int i = 0; i <= Steps; i++)
            {
                float y = -0.72f + 1.6f * i / Steps;
                pts[i] = (0.42f * MathF.Sin(y * 4.0f), y);
            }
            return pts;
        }

        static (float x, float y)[] FishBody()
        {
            var pts = new (float, float)[Steps * 2 + 1];
            for (int i = 0; i <= Steps; i++)
            {
                float t = i / (float)Steps;
                pts[i] = (-0.9f + 1.3f * t, -0.4f * MathF.Sin(MathF.PI * t));
            }
            for (int i = 1; i <= Steps; i++)
            {
                float t = 1f - i / (float)Steps;
                pts[Steps + i] = (-0.9f + 1.3f * t, 0.4f * MathF.Sin(MathF.PI * t));
            }
            return pts;
        }
    }
}
