using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// A maker's mark (ui-23): a glyph from Ceramics' shared Marks (line art in the unit
// square) in a colour. Ceramics owns the data and the server side (player_mark, SetMark,
// shared/Marks.cs); this file is the picker's and the tooltip's view of it.

// Who made a thing, as the inspect tooltip shows it.
public record MakerInfo(string Name, ushort Glyph, Color Color);

public interface IMarkLink
{
    IReadOnlyList<MarkGroup> Groups { get; }
    Glyph? Find(ushort id);
    // My mark now, or null if I have none.
    (ushort glyph, Color color)? Mine { get; }
    void Set(ushort glyph, Color color);
    // The maker of a made thing (Marks.MakerOf's id), for tooltips; null when unknown.
    MakerInfo? Maker(uint makerId);
    // Keeps a colour in the world's range (no neon, no near black or white); the server checks the same.
    Color Clamp(Color c);
}

public static class MarkColour
{
    public static Color ToColor(uint rgb) => new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    public static uint ToRgb(Color c) => ((uint)c.R8 << 16) | ((uint)c.G8 << 8) | (uint)c.B8;
    public static Color Clamp(Color c) => ToColor(Marks.ClampColor(ToRgb(c)));
}

// For previews without a server: the real glyphs, a mark kept in memory, one sample maker.
public sealed class MockMarkLink : IMarkLink
{
    (ushort, Color)? mine;
    public IReadOnlyList<MarkGroup> Groups => Marks.Groups;
    public Glyph? Find(ushort id) => Marks.Find(id);
    public (ushort glyph, Color color)? Mine => mine;
    public void Set(ushort glyph, Color color) => mine = (glyph, Clamp(color));
    public MakerInfo? Maker(uint makerId) => makerId == 0 ? null : new MakerInfo("Mara", Marks.All[0].Id, MarkColour.ToColor(Marks.DefaultColor));
    public Color Clamp(Color c) => MarkColour.Clamp(c);
}

// The live game: marks from the player_mark table, set with SetMark.
public sealed class ServerMarkLink : IMarkLink
{
    readonly DbConnection conn;
    readonly Func<uint> myId;
    public ServerMarkLink(DbConnection conn, Func<uint> myId) { this.conn = conn; this.myId = myId; }

    public IReadOnlyList<MarkGroup> Groups => Marks.Groups;
    public Glyph? Find(ushort id) => Marks.Find(id);
    public (ushort glyph, Color color)? Mine =>
        conn.Db.PlayerMark.PlayerId.Find(myId()) is { } m ? (m.Glyph, MarkColour.ToColor(m.Color)) : null;
    public void Set(ushort glyph, Color color) => conn.Reducers.SetMark(glyph, Marks.ClampColor(MarkColour.ToRgb(color)));
    public Color Clamp(Color c) => MarkColour.Clamp(c);

    // Makers are stored by the low 16 bits of their player id (Marks.Pack).
    public MakerInfo? Maker(uint makerId)
    {
        if (makerId == 0) return null;
        var mark = conn.Db.PlayerMark.PlayerId.Find(makerId) ?? conn.Db.PlayerMark.Iter().FirstOrDefault(r => (r.PlayerId & 0xFFFFu) == makerId);
        if (mark == null) return null;
        string name = conn.Db.Player.PlayerId.Find(mark.PlayerId)?.Name ?? "someone";
        return new MakerInfo(name, mark.Glyph, MarkColour.ToColor(mark.Color));
    }
}

// Draws one glyph's strokes, fitted to the control, in a colour.
public partial class MarkView : Control
{
    public Glyph? Glyph;
    public Color Ink = Look.Cream;
    public float Width = 2.4f;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public void Show(Glyph? glyph, Color ink)
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
                DrawPolyline(stroke.Select(p => origin + new Vector2(p.x, p.y) * side).ToArray(), Ink, Width, true);
    }
}
