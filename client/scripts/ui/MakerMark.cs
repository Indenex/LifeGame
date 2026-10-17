using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// A maker's mark (ui-23): a glyph drawn as strokes in a unit square, in a colour. Ceramics
// owns the glyph data and the server side (shared Marks, player_mark, SetMark); until
// that lands the picker runs on MockMarkLink's small set.
public record MarkGlyph(ushort Id, string Name, Vector2[][] Strokes);
public record MarkGroup(string Name, MarkGlyph[] Glyphs);

// Who made a thing, as the inspect tooltip shows it.
public record MakerInfo(string Name, ushort Glyph, Color Color);

public interface IMarkLink
{
    IReadOnlyList<MarkGroup> Groups { get; }
    MarkGlyph? Find(ushort id);
    // My mark now, or null if I have none.
    (ushort glyph, Color color)? Mine { get; }
    void Set(ushort glyph, Color color);
    // The maker of a made thing (a player id), for tooltips; null when unknown.
    MakerInfo? Maker(uint playerId);
    // Keeps a colour in the world's range (no neon, no pure black); the server checks the same.
    Color Clamp(Color c);
}

public sealed class MockMarkLink : IMarkLink
{
    static Vector2 P(float x, float y) => new(x, y);

    // A few marks per group, enough to lay out and judge the panel.
    static readonly MarkGroup[] groups =
    {
        new("Nature", new[]
        {
            new MarkGlyph(1, "Leaf", new[] { new[] { P(.5f, .1f), P(.8f, .45f), P(.5f, .9f), P(.2f, .45f), P(.5f, .1f) }, new[] { P(.5f, .1f), P(.5f, .9f) } }),
            new MarkGlyph(2, "Sun", Enumerable.Range(0, 8).Select(i => { float a = i * Mathf.Tau / 8; return new[] { P(.5f + .22f * Mathf.Cos(a), .5f + .22f * Mathf.Sin(a)), P(.5f + .42f * Mathf.Cos(a), .5f + .42f * Mathf.Sin(a)) }; }).Append(Circle(.5f, .5f, .16f)).ToArray()),
            new MarkGlyph(3, "Wave", new[] { Enumerable.Range(0, 13).Select(i => P(.08f + i * .07f, .5f + .15f * Mathf.Sin(i * .9f))).ToArray() }),
            new MarkGlyph(4, "Mountain", new[] { new[] { P(.08f, .82f), P(.38f, .25f), P(.55f, .55f), P(.7f, .38f), P(.92f, .82f) } }),
        }),
        new("Fantasy", new[]
        {
            new MarkGlyph(20, "Star", new[] { Enumerable.Range(0, 6).Select(i => { float a = -Mathf.Pi / 2 + i * Mathf.Tau * 2 / 5; return P(.5f + .4f * Mathf.Cos(a), .52f + .4f * Mathf.Sin(a)); }).ToArray() }),
            new MarkGlyph(21, "Eye", new[] { new[] { P(.08f, .5f), P(.5f, .22f), P(.92f, .5f), P(.5f, .78f), P(.08f, .5f) }, Circle(.5f, .5f, .12f) }),
            new MarkGlyph(22, "Crown", new[] { new[] { P(.12f, .78f), P(.12f, .3f), P(.32f, .52f), P(.5f, .22f), P(.68f, .52f), P(.88f, .3f), P(.88f, .78f), P(.12f, .78f) } }),
            new MarkGlyph(23, "Moon", new[] { Arc(.5f, .5f, .38f, .7f, 5.6f), Arc(.62f, .42f, .3f, 1.3f, 5.0f) }),
        }),
        new("Alchemical", new[]
        {
            new MarkGlyph(40, "Fire", new[] { new[] { P(.5f, .12f), P(.88f, .82f), P(.12f, .82f), P(.5f, .12f) } }),
            new MarkGlyph(41, "Water", new[] { new[] { P(.12f, .18f), P(.88f, .18f), P(.5f, .88f), P(.12f, .18f) } }),
            new MarkGlyph(42, "Earth", new[] { new[] { P(.12f, .18f), P(.88f, .18f), P(.5f, .88f), P(.12f, .18f) }, new[] { P(.25f, .45f), P(.75f, .45f) } }),
            new MarkGlyph(43, "Salt", new[] { Circle(.5f, .5f, .38f), new[] { P(.12f, .5f), P(.88f, .5f) } }),
        }),
        new("Geometric", new[]
        {
            new MarkGlyph(60, "Ring", new[] { Circle(.5f, .5f, .38f), Circle(.5f, .5f, .2f) }),
            new MarkGlyph(61, "Diamond", new[] { new[] { P(.5f, .08f), P(.9f, .5f), P(.5f, .92f), P(.1f, .5f), P(.5f, .08f) } }),
            new MarkGlyph(62, "Cross", new[] { new[] { P(.5f, .1f), P(.5f, .9f) }, new[] { P(.1f, .5f), P(.9f, .5f) } }),
            new MarkGlyph(63, "Spiral", new[] { Enumerable.Range(0, 40).Select(i => { float t = i / 39f, a = t * Mathf.Tau * 2.2f, r = .05f + .37f * t; return P(.5f + r * Mathf.Cos(a), .5f + r * Mathf.Sin(a)); }).ToArray() }),
        }),
    };

    static Vector2[] Circle(float cx, float cy, float r) => Arc(cx, cy, r, 0, Mathf.Tau);

    static Vector2[] Arc(float cx, float cy, float r, float from, float to) =>
        Enumerable.Range(0, 25).Select(i => { float a = Mathf.Lerp(from, to, i / 24f); return P(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a)); }).ToArray();

    (ushort, Color)? mine;
    public IReadOnlyList<MarkGroup> Groups => groups;
    public MarkGlyph? Find(ushort id) => groups.SelectMany(g => g.Glyphs).FirstOrDefault(g => g.Id == id);
    public (ushort glyph, Color color)? Mine => mine;
    public void Set(ushort glyph, Color color) => mine = (glyph, Clamp(color));
    public MakerInfo? Maker(uint playerId) => playerId == 0 ? null : new MakerInfo("Mara", 20, new Color(0.62f, 0.32f, 0.22f));

    public Color Clamp(Color c)
    {
        c.ToHsv(out float h, out float s, out float v);
        return Color.FromHsv(h, Mathf.Min(s, 0.8f), Mathf.Clamp(v, 0.22f, 0.9f));
    }
}

// Draws one glyph's strokes, fitted to the control, in a colour.
public partial class MarkView : Control
{
    public MarkGlyph? Glyph;
    public Color Ink = Look.Cream;
    public float Width = 2.4f;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public void Show(MarkGlyph? glyph, Color ink)
    {
        Glyph = glyph;
        Ink = ink;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Glyph == null) return;
        float side = Mathf.Min(Size.X, Size.Y);
        var origin = (Size - new Vector2(side, side)) / 2;
        foreach (var stroke in Glyph.Strokes)
            if (stroke.Length > 1)
                DrawPolyline(stroke.Select(p => origin + p * side).ToArray(), Ink, Width, true);
    }
}
