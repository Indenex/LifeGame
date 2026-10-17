using LifeGame.Shared;
using SpacetimeDB;

// Maker's marks (ceramics-15): each player has one of twelve painted glyphs, pressed into
// every pot they shape. The server picks the first one from their id and records who made
// each pot (D-182); at the Potter's bench they can change to the next glyph. The mark
// itself rides on the pot's per-stack Wear number (shared/Marks.cs).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "MakerMark", Public = true)]
    public partial struct MakerMark
    {
        [PrimaryKey]
        public uint PlayerId;
        public byte Glyph;
    }

    // A player's current mark, made from their id the first time they shape a pot.
    static uint MarkFor(ReducerContext ctx, uint playerId)
    {
        var row = ctx.Db.MakerMark.PlayerId.Find(playerId)
            ?? ctx.Db.MakerMark.Insert(new MakerMark { PlayerId = playerId, Glyph = Marks.DefaultGlyph(playerId) });
        return Marks.Pack(playerId, row.Glyph);
    }

    // At the Potter's bench: change to the next glyph. Pots already made keep the old one.
    [SpacetimeDB.Reducer]
    public static void NextMakerMark(ReducerContext ctx)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        if (StationNear(ctx, StationKind.PottersBench, pos.X, pos.Z) is null)
            throw new Exception("Change your mark at a potter's bench");
        var state = RequireCooldown(ctx, player);
        byte current = ctx.Db.MakerMark.PlayerId.Find(player.PlayerId)?.Glyph ?? Marks.DefaultGlyph(player.PlayerId);
        byte next = (byte)((current + 1) % Marks.Glyphs);
        var row = new MakerMark { PlayerId = player.PlayerId, Glyph = next };
        if (ctx.Db.MakerMark.PlayerId.Find(player.PlayerId) is null) ctx.Db.MakerMark.Insert(row);
        else ctx.Db.MakerMark.PlayerId.Update(row);
        News(ctx, player.PlayerId, $"Your mark is now a {Marks.GlyphNames[next]}.");
        ctx.Db.PlayerState.PlayerId.Update(state with { LastActionAt = ctx.Timestamp });
    }
}
