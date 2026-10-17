// Slip decoration (ceramics-14): a band or two of coloured slip round a pot's rim, put on at the
// Potter's bench while the clay is still damp. Look only: it never changes quality, and it needs no
// upkeep (D-148). Justin picked the banded rim and a free colour like the maker's mark.
//
// It rides on the item rows in the same per-stack Wear number as the maker's mark, in the bits above
// the maker's (Marks uses bits 0 to 16; pots never wear): bit 17 set when slipped, bit 18 set for two
// bands (else one), then the colour as hue (5 bits), saturation (3) and value (3) on a grid inside the
// mark colour range (Marks.ClampColor), bits 19 to 29. Stacks with different slip never merge. Shared
// by the server module and the client; keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public static class Slip
    {
        const uint MakerBits = 0x1FFFFu;   // bits 0 to 16: the maker (Marks)
        const int HueSteps = 32, SatSteps = 8, ValSteps = 8;

        public const int MaxBands = 2;

        // The forms that take slip: pots and jars. Lamps, tiles, bricks and troughs do not.
        public static bool CanDecorate(ClayForm form) => form is ClayForm.PinchPot or ClayForm.CoilPot or ClayForm.StorageJar;

        public static bool CanDecorate(Item item) => item switch
        {
            Item.PinchPot or Item.CoilPot or Item.PinchPotOfWater or Item.CoilPotOfWater
                or Item.UnfiredPinchPot or Item.UnfiredCoilPot or Item.StorageJar or Item.UnfiredStorageJar => true,
            _ => false,
        };

        // Damp clay takes slip: wet, up to leather-hard (ClayRules.LeatherHard), and no drier.

        // Clean clay it takes (a little, thinned to slip).
        public const uint ClayCost = 1;

        public static bool Has(uint wear) => (wear & (1u << 17)) != 0;

        public static int Bands(uint wear) => (wear & (1u << 18)) != 0 ? 2 : 1;

        // The wear number with the slip taken off.
        public static uint Without(uint wear) => wear & MakerBits;

        // The wear number with this slip on it: the colour is clamped to the mark range and snapped
        // to the grid; bands is 1 or 2. The maker bits are kept.
        public static uint With(uint wear, uint rgb, int bands)
        {
            var (h, s, v) = Marks.ToHsv(Marks.ClampColor(rgb));
            uint hi = (uint)Math.Clamp((int)MathF.Round(h / 6f * HueSteps) % HueSteps, 0, HueSteps - 1);
            uint si = (uint)Math.Clamp((int)MathF.Round((s - Marks.MinSaturation) / (Marks.MaxSaturation - Marks.MinSaturation) * (SatSteps - 1)), 0, SatSteps - 1);
            uint vi = (uint)Math.Clamp((int)MathF.Round((v - Marks.MinValue) / (Marks.MaxValue - Marks.MinValue) * (ValSteps - 1)), 0, ValSteps - 1);
            uint two = bands >= 2 ? 1u : 0u;
            return (wear & MakerBits) | (1u << 17) | (two << 18) | (hi << 19) | (si << 24) | (vi << 27);
        }

        // The slip colour (0xRRGGBB) a wear number carries; only meaningful when Has.
        public static uint Color(uint wear)
        {
            float h = ((wear >> 19) & 0x1Fu) / (float)HueSteps * 6f;
            float s = Marks.MinSaturation + ((wear >> 24) & 0x7u) / (float)(SatSteps - 1) * (Marks.MaxSaturation - Marks.MinSaturation);
            float v = Marks.MinValue + ((wear >> 27) & 0x7u) / (float)(ValSteps - 1) * (Marks.MaxValue - Marks.MinValue);
            return Marks.FromHsv(h, s, v);
        }

        // The slip a wear number carries on this item, or none: only pots and jars show it.
        public static bool On(Item item, uint wear) => CanDecorate(item) && Has(wear);
    }
}
