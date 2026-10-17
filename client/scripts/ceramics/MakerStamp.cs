using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The maker's-mark glyphs drawn into one atlas for the clay shader (ceramics-15). The art is the
// stroke data in shared/MarkGlyphs.cs, the same lines the picker panel draws; here each glyph is
// rasterised as thick lines into a cell of the atlas. Slot = the glyph's place in Marks.All.
public static class MakerStamp
{
    public const int Cols = 8, Cell = 64;
    public static int Rows => (Marks.All.Count + Cols - 1) / Cols;

    // Half the line width, in the glyph's -1 to 1 square.
    const float Half = 0.085f;

    static ImageTexture? atlas;
    public static ImageTexture Atlas => atlas ??= Build();

    // The slot of a glyph id in the atlas, or -1 if there is no such glyph.
    public static int SlotOf(ushort glyphId)
    {
        for (int i = 0; i < Marks.All.Count; i++)
            if (Marks.All[i].Id == glyphId) return i;
        return -1;
    }

    static ImageTexture Build()
    {
        var image = Image.CreateEmpty(Cols * Cell, Rows * Cell, false, Image.Format.Rgba8);
        for (int slot = 0; slot < Marks.All.Count; slot++)
        {
            var strokes = Marks.All[slot].Strokes;
            int cx = slot % Cols, cz = slot / Cols;
            for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                {
                    var p = new Vector2((x + 0.5f) / Cell * 2f - 1f, (y + 0.5f) / Cell * 2f - 1f);
                    float d = Distance(strokes, p);
                    // A soft edge about one pixel wide; the cell border fades so cells never bleed.
                    float edge = Mathf.Clamp(0.5f - d * Cell * 0.5f, 0f, 1f);
                    float fade = Mathf.Clamp((1f - Mathf.Max(Mathf.Abs(p.X), Mathf.Abs(p.Y))) * 10f, 0f, 1f);
                    image.SetPixel(cx * Cell + x, cz * Cell + y, new Color(1, 1, 1, edge * fade));
                }
        }
        return ImageTexture.CreateFromImage(image);
    }

    // Distance from p to the nearest line of a glyph, less half the line width (negative inside).
    static float Distance((float x, float y)[][] strokes, Vector2 p)
    {
        float best = 10f;
        foreach (var stroke in strokes)
            for (int i = 0; i + 1 < stroke.Length; i++)
            {
                var a = new Vector2(stroke[i].x * 2f - 1f, stroke[i].y * 2f - 1f);
                var b = new Vector2(stroke[i + 1].x * 2f - 1f, stroke[i + 1].y * 2f - 1f);
                best = Mathf.Min(best, Segment(p, a, b));
            }
        return best - Half;
    }

    static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        var pa = p - a;
        var ba = b - a;
        float len2 = ba.Dot(ba);
        float h = len2 <= 1e-8f ? 0f : Mathf.Clamp(pa.Dot(ba) / len2, 0f, 1f);
        return (pa - ba * h).Length();
    }
}
