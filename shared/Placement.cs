// Where a made thing may be set down (crafting-21), shared by the server and the
// client's ghost so both agree. The server has the last word: it also checks reach,
// claims, trees and boulders. Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public static class Placement
    {
        // How far around the spot the ground is looked at, and the most it may rise or fall there.
        public const float Probe = 0.4f, MaxSlope = 0.3f;

        // Why a placeable item can't go here (water or steep ground), or null if it can.
        public static string? Check(Item item, float x, float z, Func<float, float, float> groundAt)
        {
            if (!Items.IsKnown((ushort)item) || !Items.Def(item).Placeable) return "That can't be set down";
            float h = groundAt(x, z), lo = h, hi = h;
            foreach (var (dx, dz) in new[] { (Probe, 0f), (-Probe, 0f), (0f, Probe), (0f, -Probe) })
            {
                float g = groundAt(x + dx, z + dz);
                lo = MathF.Min(lo, g);
                hi = MathF.Max(hi, g);
            }
            if (lo < Structures.WaterLevel + 0.1f) return "Not in the water";
            if (hi - lo > MaxSlope) return "That ground is too steep";
            return null;
        }
    }
}
