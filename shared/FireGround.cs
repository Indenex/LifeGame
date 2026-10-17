// Whether a spot is fit for a fire ring: level, above the water, clear of boulders and
// trunks. Shared so the server decides it and the tools and client ghost can ask the same.
// Fire and hearth spacing, and ground a building has cleared, stay with the caller.
using System;

namespace LifeGame.Shared
{
    public static class FireGround
    {
        public const float MaxSlope = 0.4f;

        // depthAt gives dug depth per cell; cleared (optional) says a building has cleared a cell.
        public static bool Clear(float x, float z, Func<int, int, short> depthAt, Func<int, int, bool> cleared = null)
        {
            int cx = World.CellOf(x), cz = World.CellOf(z);
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = cx - 1; i <= cx + 1; i++)
                for (int j = cz - 1; j <= cz + 1; j++)
                {
                    float h = World.Height(i, j, depthAt(i, j));
                    lo = Math.Min(lo, h);
                    hi = Math.Max(hi, h);
                }
            if (lo < World.WaterLevel + 0.02f || hi - lo > MaxSlope) return false;

            for (int i = cx - 2; i <= cx + 2; i++)
                for (int j = cz - 2; j <= cz + 2; j++)
                {
                    if (depthAt(i, j) != 0) continue;
                    if (Rocks.LiesAt(i, j))
                    {
                        if (cleared != null && cleared(i, j)) continue;
                        var (rx, rz) = Rocks.Center(i, j);
                        if (Dist(rx, rz, x, z) < Rocks.Radius(i, j) + Fires.RingRadius) return false;
                    }
                    else if (Trees.GrowsAt(i, j))
                    {
                        if (cleared != null && cleared(i, j)) continue;
                        var (tx, tz) = Trees.Trunk(i, j);
                        if (Dist(tx, tz, x, z) < 0.4f + Fires.RingRadius) return false;
                    }
                }
            return true;
        }

        static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx, dz = az - bz;
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }
}
