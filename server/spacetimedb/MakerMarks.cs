using LifeGame.Shared;
using SpacetimeDB;

// Maker's marks (ceramics-15): each player picks one glyph and a colour, and the pots they
// shape carry who made them. The server records the mark and checks it (D-182); the glyph art
// is shared/MarkGlyphs.cs. A pot carries only its maker's id, in the per-stack Wear number
// (shared/Marks.cs), so the stamp always shows the maker's current glyph and colour.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "PlayerMark", Public = true)]
    public partial struct PlayerMark
    {
        [PrimaryKey]
        public uint PlayerId;
        public ushort Glyph;
        // Packed 0xRRGGBB, already clamped (Marks.ClampColor).
        public uint Color;
    }

    // What a pot shaped now by this player carries: their id, or 0 while they have no mark yet.
    static uint MarkFor(ReducerContext ctx, uint playerId) =>
        ctx.Db.PlayerMark.PlayerId.Find(playerId) is null ? 0u : Marks.Pack(playerId);

    // Sets (or changes) your mark. Pots already made keep their maker, so they follow the change.
    [SpacetimeDB.Reducer]
    public static void SetMark(ReducerContext ctx, ushort glyph, uint color)
    {
        var player = RequirePlayer(ctx);
        if (Marks.Find(glyph) is null) throw new Exception("There is no such mark");
        var row = new PlayerMark { PlayerId = player.PlayerId, Glyph = glyph, Color = Marks.ClampColor(color & 0xFFFFFFu) };
        if (ctx.Db.PlayerMark.PlayerId.Find(player.PlayerId) is null) ctx.Db.PlayerMark.Insert(row);
        else ctx.Db.PlayerMark.PlayerId.Update(row);
    }
}
