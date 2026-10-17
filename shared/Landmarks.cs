// Things to steer by (env-5): deer trails worn between glades and water, huge old
// oaks standing alone in glades, rock outcrops on the hilltop heath, and stepping
// stones where a stream is easy to ford. All from the shared seed, so every player
// sees the same landmarks and can give directions by them. Trails also thin the
// trees (Forest.Density), so the server's choppable trees leave the paths open.
using System;

namespace LifeGame.Shared
{
    public static class Landmarks
    {
        // How worn the ground is by a deer trail, 0 untouched to 1 the middle of the
        // path. Trails follow the contours of a slow noise, so they wander, fork and
        // loop like real game paths, one to two and a half meters wide.
        public static float Trail(int x, int z)
        {
            float meters = TrailDistance(x, z);
            // Narrower in some stretches, broader where the deer spread out.
            float width = 0.6f + 0.9f * World.Noise(x - 4121, z + 2203, 45f, 92);
            return 1f - SmoothStep(0.5f * width, 1.6f * width, meters);
        }

        static float TrailField(int x, int z) =>
            World.Noise(x + 1777, z + 919, 80f, 90) * 0.7f + World.Noise(x - 2311, z + 4441, 27f, 91) * 0.3f;

        // The field changes at most this much per meter (each octave's steepest smoothed
        // slope, 1.5 / scale, on both axes), so a point that far off the 0.5 contour
        // is clear of the path and needn't work out the slope.
        const float MaxTrailSlope = 1.5f * 1.42f * (0.7f / 80f + 0.3f / 27f);

        static float TrailDistance(int x, int z)
        {
            float off = MathF.Abs(TrailField(x, z) - 0.5f);
            if (off >= 2.4f * MaxTrailSlope) return off / MaxTrailSlope;
            float gx = (TrailField(x + 1, z) - TrailField(x - 1, z)) * 0.5f;
            float gz = (TrailField(x, z + 1) - TrailField(x, z - 1)) * 0.5f;
            return off / MathF.Max(MathF.Sqrt(gx * gx + gz * gz), 0.002f);
        }

        // Placement rules for the world features in shared/Features.cs, which says how
        // many of each there are (one per square, or every cell that fits).

        // A huge old oak, one at most in each 64 m square: the first of a few spots in
        // the square that falls in an open glade on dry ground, away from the spawn
        // clearing. Scenery: no choppable tree grows on its cell.
        public static bool OakFits(int x, int z) =>
            x * x + z * z >= 20 * 20 && !Trees.GrowsAt(x, z)
            && Forest.Density(x, z) < 0.25f && Forest.Heath(x, z) < 0.5f && Forest.Wetland(x, z) < 0.3f
            && World.BaseHeight(x, z) > World.WaterLevel + 0.4f && Trail(x, z) < 0.3f;

        // The middle of a rock outcrop, one at most in each 48 m square, on the heath.
        public const int OutcropSquare = 48;

        public static bool OutcropFits(int x, int z) => Forest.Heath(x, z) > 0.55f && x * x + z * z > 20 * 20;

        // A huge old trunk lying across a glade (env-33), one at most in each 56 m square:
        // in an open glade with the wood not far off, on dry ground, away from the spawn
        // clearing and trails.
        public const int FallenGiantSquare = 56;

        public static bool FallenGiantFits(int x, int z)
        {
            if (x * x + z * z < 24 * 24 || Trees.GrowsAt(x, z)) return false;
            float density = Forest.Density(x, z);
            if (density > 0.2f || Forest.Heath(x, z) > 0.5f || Forest.Wetland(x, z) > 0.3f
                || World.BaseHeight(x, z) < World.WaterLevel + 0.5f || Trail(x, z) > 0.3f) return false;
            // Room for a trunk up to 13 m long whichever way it fell: open ground all round.
            for (int k = 0; k < 8; k++)
            {
                float a = k * MathF.PI / 4f;
                if (Forest.Density(x + (int)(MathF.Cos(a) * 6f), z + (int)(MathF.Sin(a) * 6f)) > 0.3f) return false;
            }
            return true;
        }

        public static int SquareOf(int cell, int size) => FloorDiv(cell, size);

        // A stepping stone across a stream, in a row every 40 m along it.
        public static bool FordFits(int x, int z) =>
            Mod(z + 13, 40) == 0 && Land.StreamDistance(x, z) < 3.2f;

        // Whether this cell is the chosen spot in its square of the given size: the first
        // of a few seeded candidates there that fits.
        const int Candidates = 6;

        public static bool IsChosen(int x, int z, int size, uint salt, Func<int, int, bool> fits)
        {
            int sx = FloorDiv(x, size), sz = FloorDiv(z, size);
            int mine = -1;
            for (int k = 0; k < Candidates && mine < 0; k++)
                if (Candidate(sx, sz, size, salt, k) == (x, z)) mine = k;
            if (mine < 0) return false;
            // An earlier candidate that fits takes the square.
            for (int k = 0; k < mine; k++)
            {
                var (px, pz) = Candidate(sx, sz, size, salt, k);
                if (fits(px, pz)) return false;
            }
            return fits(x, z);
        }

        // The chosen spot in square (sx, sz), if one fits (for collision, which needs the
        // pieces round a point without testing every cell).
        public static bool ChosenIn(int sx, int sz, int size, uint salt, Func<int, int, bool> fits, out int x, out int z)
        {
            for (int k = 0; k < Candidates; k++)
            {
                (x, z) = Candidate(sx, sz, size, salt, k);
                if (fits(x, z)) return true;
            }
            x = z = 0;
            return false;
        }

        static (int, int) Candidate(int sx, int sz, int size, uint salt, int k)
        {
            uint s = salt * 16u + (uint)k;
            return (sx * size + 6 + (int)(Trees.Hash01(sx, sz, 900u + s) * (size - 12)),
                    sz * size + 6 + (int)(Trees.Hash01(sx, sz, 1900u + s) * (size - 12)));
        }

        static int FloorDiv(int a, int b) => a >= 0 ? a / b : (a - b + 1) / b;
        static int Mod(int a, int b) => ((a % b) + b) % b;

        static float SmoothStep(float from, float to, float v)
        {
            float t = Math.Clamp((v - from) / (to - from), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
