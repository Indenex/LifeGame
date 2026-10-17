// A maker's mark (ceramics-15): the small personal stamp a player presses into the pots they
// make, so anyone inspecting one can see who made it. Shared by the server module and the
// client; keep this file free of engine and SpacetimeDB types.
//
// Each player picks a glyph from about fifty, in groups, and a colour. Glyphs are line art: a few
// polylines in the unit square (0,0 top left), drawn with whatever stroke width the drawer likes,
// so the stamp on a pot and the picker panel use the same data. Ids never change.
//
// On the item rows, a pot carries only WHO made it, in the per-stack Wear number that tool wear
// also uses (pots never wear): bits 0-15 the maker's player id, bit 16 set when it is made by
// someone. Stacks of pots from different makers never merge. The glyph and colour come from the
// maker's current mark (the player_mark table), so changing it restyles their older pots.
using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeGame.Shared
{
    public sealed record Glyph(ushort Id, string Name, (float x, float y)[][] Strokes);

    public sealed record MarkGroup(string Name, Glyph[] Glyphs);

    public static class Marks
    {
        // ---- who made it, on an item row
        public static uint Pack(uint makerId) => (makerId & 0xFFFFu) | (1u << 16);
        public static bool Has(uint wear) => (wear & (1u << 16)) != 0;
        public static uint Maker(uint wear) => wear & 0xFFFFu;

        // The clay items that carry their maker: pots, jars and lamps, unfired or fired, full or empty.
        public static bool Marked(Item item) => item switch
        {
            Item.PinchPot or Item.CoilPot or Item.PinchPotOfWater or Item.CoilPotOfWater
                or Item.UnfiredPinchPot or Item.UnfiredCoilPot or Item.StorageJar or Item.UnfiredStorageJar
                or Item.ClayLamp or Item.UnfiredLamp => true,
            _ => false,
        };

        // The maker's player id (low 16 bits) of an item row, or 0 for none.
        public static uint MakerOf(Item item, uint wear) => Marked(item) && Has(wear) ? Maker(wear) : 0u;

        // ---- colour: a packed 0xRRGGBB, kept earthy
        // A free colour wheel is fine, but never neon, nor near black or white: saturation and
        // brightness are clamped, so every mark reads as paint on clay.
        public const float MinSaturation = 0.18f, MaxSaturation = 0.90f, MinValue = 0.30f, MaxValue = 0.92f;

        public static uint ClampColor(uint rgb)
        {
            var (h, s, v) = ToHsv(rgb);
            return FromHsv(h, Math.Clamp(s, MinSaturation, MaxSaturation), Math.Clamp(v, MinValue, MaxValue));
        }

        // Hue 0 to 6 (sixths of the wheel), saturation and value 0 to 1.
        public static (float h, float s, float v) ToHsv(uint rgb)
        {
            float r = ((rgb >> 16) & 0xFF) / 255f, g = ((rgb >> 8) & 0xFF) / 255f, b = (rgb & 0xFF) / 255f;
            float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            float d = max - min, h = 0f;
            if (d > 0f)
            {
                if (max == r) h = ((g - b) / d + 6f) % 6f;
                else if (max == g) h = (b - r) / d + 2f;
                else h = (r - g) / d + 4f;
            }
            return (h, max <= 0f ? 0f : d / max, max);
        }

        public static uint FromHsv(float h, float s, float v)
        {
            h = ((h % 6f) + 6f) % 6f;
            float c = v * s, x = c * (1f - Math.Abs(h % 2f - 1f)), m = v - c;
            (float rr, float gg, float bb) = ((int)h) switch
            {
                0 => (c, x, 0f), 1 => (x, c, 0f), 2 => (0f, c, x), 3 => (0f, x, c), 4 => (x, 0f, c), _ => (c, 0f, x),
            };
            static uint B(float f) => (uint)Math.Clamp((int)MathF.Round(f * 255f), 0, 255);
            return (B(rr + m) << 16) | (B(gg + m) << 8) | B(bb + m);
        }

        // A few earthy colours to start from, already inside the clamp.
        public static readonly uint[] Presets =
        {
            0xB7631F, // ochre
            0xD9CDB0, // kaolin
            0x4E453D, // charcoal
            0x9A3B26, // rust
            0x5E7A3A, // moss
            0x3A4F7A, // indigo
            0x2F7A78, // teal
            0xD9A33B, // saffron
            0x6B3A5B, // plum
        };

        public const uint DefaultColor = 0xB7631F;

        // ---- the glyphs
        public static IReadOnlyList<MarkGroup> Groups => MarkGlyphs.Groups;
        public static IReadOnlyList<Glyph> All => MarkGlyphs.All;
        public static Glyph? Find(ushort id) => MarkGlyphs.ById.TryGetValue(id, out var g) ? g : null;
    }
}
