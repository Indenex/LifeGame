// A maker's mark (ceramics-15): the small personal stamp a player presses into every pot
// they shape, so anyone can see who made it. Shared by the server module and the client;
// keep this file free of engine and SpacetimeDB types.
//
// A mark rides on the same per-stack number that tool wear uses (the Wear column on
// inventory, ground, storage and stash rows). Pots never wear, so the number is free for
// them, every stack that moves keeps it, and pots from different makers never merge.
//   bits 0-15  the maker's player id (low 16 bits)
//   bits 16-23 the glyph, 1 to 12 (0 means no mark)
using System;

namespace LifeGame.Shared
{
    public static class Marks
    {
        // The twelve painted glyphs, in the order they cycle at the bench.
        public static readonly string[] GlyphNames =
        {
            "sun", "crescent moon", "wave", "leaf", "tree", "antler",
            "fish", "bird", "spiral", "hand", "flame", "seed",
        };

        public static int Glyphs => GlyphNames.Length;

        public static uint Pack(uint makerId, byte glyph) =>
            (makerId & 0xFFFFu) | ((uint)(glyph % Glyphs + 1) << 16);

        public static bool Has(uint mark) => ((mark >> 16) & 0xFF) != 0;
        public static byte Glyph(uint mark) => (byte)(((mark >> 16) & 0xFF) - 1);
        public static uint Maker(uint mark) => mark & 0xFFFFu;

        // Everyone starts with a mark the first time they shape a pot: one of the twelve,
        // worked out from their id, so the very first pot already carries it.
        public static byte DefaultGlyph(uint playerId) => (byte)((playerId * 7u + 3u) % (uint)Glyphs);

        public static string GlyphName(uint mark) => Has(mark) ? GlyphNames[Glyph(mark)] : "";

        // The clay items that carry a mark: pots, jars and lamps, unfired or fired, full or empty.
        public static bool Marked(Item item) => item switch
        {
            Item.PinchPot or Item.CoilPot or Item.PinchPotOfWater or Item.CoilPotOfWater
                or Item.UnfiredPinchPot or Item.UnfiredCoilPot or Item.StorageJar or Item.UnfiredStorageJar
                or Item.ClayLamp or Item.UnfiredLamp => true,
            _ => false,
        };
    }
}
