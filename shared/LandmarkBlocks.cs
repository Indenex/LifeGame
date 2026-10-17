// The stones that stop a walker on a landmark (bugfix-49, a pure move out of the client's
// LandmarkProps): where each stone of an outcrop lies, how big it is, and so the blocking
// circle it makes. The client draws the stones from these same pieces and the server, the check
// bots and the client's collision all read the circles, so the meshes and the circles cannot
// drift apart. Keep this file free of engine and SpacetimeDB types.
using System;
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static class LandmarkBlocks
    {
        // One stone or formation: a boulder (Rock) or one of Modeling's layered outcrop shapes
        // (Formation). SeedX/SeedZ seed its own proportions, Size is the boulder's size or the
        // formation's scale, Tipped leans a formation up on one edge like a slab.
        public enum PieceKind : byte { Rock, Formation }

        public readonly record struct Piece(PieceKind Kind, int SeedX, int SeedZ, float X, float Z, float Size, bool Tipped);

        // The same hash and range the client's landmark builders use.
        public static float H(int x, int z, uint salt) => Trees.Hash01(x, z, 1000u + salt);
        public static float Range(int x, int z, uint salt, float from, float to) => from + H(x, z, salt) * (to - from);

        // A boulder's proportions, from its own seed.
        public static (float X, float Y, float Z) RockStretch(int sx, int sz) =>
            (Range(sx, sz, 300, 0.7f, 1.35f), Range(sx, sz, 301, 0.55f, 1.2f), Range(sx, sz, 302, 0.7f, 1.35f));

        // A formation's proportions, from its own seed.
        public static (float X, float Y, float Z) FormationStretch(int sx, int sz) =>
            (Range(sx, sz, 310, 0.85f, 1.2f), Range(sx, sz, 311, 0.8f, 1.25f), Range(sx, sz, 312, 0.85f, 1.2f));

        // The radius a boulder blocks with; pebbles under 0.2 are stepped over and do not block.
        public static float RockRadius(int sx, int sz, float size)
        {
            var s = RockStretch(sx, sz);
            return 0.4f * size * MathF.Max(s.X, s.Z);
        }

        public static float FormationRadius(int sx, int sz, float scale)
        {
            var s = FormationStretch(sx, sz);
            return 1.25f * scale * MathF.Max(s.X, s.Z);
        }

        // A rock outcrop on the heath, in one of three kinds: a rounded cluster, a ridge of stones
        // running one way, or one great tilted slab with rubble. (atX, atZ) is its trunk point.
        public static List<Piece> Outcrop(int x, int z, float atX, float atZ)
        {
            var pieces = new List<Piece>();
            int kind = (int)(H(x, z, 200) * 3f);
            float dir = H(x, z, 201) * MathF.Tau;
            int count = kind switch { 0 => 4 + (int)(H(x, z, 202) * 4f), 1 => 3 + (int)(H(x, z, 202) * 4f), _ => 1 };
            for (int k = 0; k < count; k++)
            {
                float ox, oz, size;
                if (kind == 1)
                {
                    // A ridge: stones strung along a line, biggest in the middle.
                    float t = count == 1 ? 0f : k / (float)(count - 1) - 0.5f;
                    float along = t * Range(x, z, 203, 5f, 9f);
                    float side = Range(x, z, 210 + (uint)k, -0.8f, 0.8f);
                    ox = MathF.Cos(dir) * along + -MathF.Sin(dir) * side;
                    oz = MathF.Sin(dir) * along + MathF.Cos(dir) * side;
                    size = Range(x, z, 220 + (uint)k, 1.2f, 2.2f) * (1.3f - MathF.Abs(t));
                }
                else
                {
                    float a = k * MathF.Tau / Math.Max(count, 1) + H(x, z, 230 + (uint)k) * 0.9f;
                    float r = k == 0 ? 0f : Range(x, z, 240 + (uint)k, 1.0f, 2.8f);
                    ox = MathF.Cos(a) * r;
                    oz = MathF.Sin(a) * r;
                    size = kind == 2 ? Range(x, z, 250, 3.0f, 4.0f) : k == 0 ? Range(x, z, 251, 2.2f, 3.2f) : Range(x, z, 260 + (uint)k, 1.1f, 2.3f);
                }
                float px = atX + ox, pz = atZ + oz;
                // The heart of a cluster and every slab is one of Modeling's layered outcrop shapes;
                // the rest are boulders.
                if (kind == 2 || (kind == 0 && k == 0))
                    pieces.Add(new Piece(PieceKind.Formation, x * 17 + k, z * 13, px, pz, size / 2.6f, kind == 2));
                else
                    pieces.Add(new Piece(PieceKind.Rock, x * 17 + k, z * 13, px, pz, size, false));
            }
            if (kind == 2)
            {
                // Rubble broken off the slab.
                int rubble = 2 + (int)(H(x, z, 270) * 4f);
                for (int k = 0; k < rubble; k++)
                {
                    float a = H(x, z, 271 + (uint)k) * MathF.Tau, r = Range(x, z, 280 + (uint)k, 2.2f, 4f);
                    pieces.Add(new Piece(PieceKind.Rock, x * 7 + k, z * 5 + k, atX + MathF.Cos(a) * r, atZ + MathF.Sin(a) * r, Range(x, z, 290 + (uint)k, 0.3f, 0.9f), false));
                }
            }
            return pieces;
        }

        // The blocking circles of the feature standing on cell (x, z): an outcrop's stones. A giant
        // oak's trunk is handled by Obstacles.At, and the stones round its foot never blocked.
        public static List<(float X, float Z, float R)> Circles(FeatureDef feature, int x, int z)
        {
            var circles = new List<(float X, float Z, float R)>();
            if (feature.Block != FeatureBlock.Stones) return circles;
            var (fx, fz) = Trees.Trunk(x, z);
            foreach (var p in feature.Model switch { "landmark.outcrop" => Outcrop(x, z, fx, fz), _ => new List<Piece>() })
            {
                float r = p.Kind == PieceKind.Formation ? FormationRadius(p.SeedX, p.SeedZ, p.Size) : RockRadius(p.SeedX, p.SeedZ, p.Size);
                if (p.Kind == PieceKind.Formation || r >= 0.2f) circles.Add((p.X, p.Z, r));
            }
            return circles;
        }

        // The blocking stones of a feature in a square, worked out once.
        static readonly Dictionary<(FeatureId, int, int), List<(float X, float Z, float R)>> cache = new();

        static List<(float X, float Z, float R)> InSquare(FeatureDef feature, int sx, int sz)
        {
            if (cache.TryGetValue((feature.Id, sx, sz), out var stones)) return stones;
            stones = feature.In(sx, sz, out int x, out int z) ? Circles(feature, x, z) : new List<(float X, float Z, float R)>();
            if (cache.Count > 256) cache.Clear();
            cache[(feature.Id, sx, sz)] = stones;
            return stones;
        }

        // Every blocking stone of a Stones landmark that could reach a point: they spread several
        // metres from their middle cell, so they are found by square rather than by the cells next to
        // the walker.
        public static IEnumerable<(float X, float Z, float R)> Near(float px, float pz)
        {
            foreach (var feature in Features.All)
            {
                if (feature.Block != FeatureBlock.Stones) continue;
                int size = feature.Square;
                float reach = feature.Spread;
                int sx0 = Landmarks.SquareOf((int)MathF.Floor(px - reach), size), sx1 = Landmarks.SquareOf((int)MathF.Floor(px + reach), size);
                int sz0 = Landmarks.SquareOf((int)MathF.Floor(pz - reach), size), sz1 = Landmarks.SquareOf((int)MathF.Floor(pz + reach), size);
                for (int sx = sx0; sx <= sx1; sx++)
                    for (int sz = sz0; sz <= sz1; sz++)
                        foreach (var stone in InSquare(feature, sx, sz))
                            yield return stone;
            }
        }
    }
}
