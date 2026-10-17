// World features as data (env-10, docs/CONTENT.md): every landmark the world places
// by rule is one FeatureDef in Features.All, the only list of them. Terrain draws,
// and collision blocks, whatever the registry says stands on a cell, so a new kind of
// feature is a Def entry (its placement rule in Landmarks plus a builder keyed by
// Model in client/scripts/environment/LandmarkProps.cs), with no new branch anywhere.
// Each instance shapes itself from its own seed, so no two look alike (D-166).
using System;

namespace LifeGame.Shared
{
    // Stable ids: append only, never renumber.
    public enum FeatureId : byte { GiantOak = 1, Outcrop = 2, Ford = 3 }

    // Where a feature stands: on dry land, or in a stream's shallows.
    public enum FeatureGround : byte { Dry, Stream }

    // What stops a walker: nothing, one broad trunk on the feature's cell, or the
    // stones its builder lays out (found by square, as they spread past the cell).
    public enum FeatureBlock : byte { None, Trunk, Stones }

    // Square > 0: at most one per Square-meter square, the first of a few seeded spots
    // there that Fits (Salt keeps each kind's spots apart). Square 0: on every cell that
    // Fits. Spread is how far from its cell the feature's pieces reach, in meters.
    public sealed record FeatureDef(FeatureId Id, string Name, FeatureGround Ground, FeatureBlock Block,
        int Square, uint Salt, float Spread, Func<int, int, bool> Fits, string Model)
    {
        public bool At(int x, int z) => Square > 0 ? Landmarks.IsChosen(x, z, Square, Salt, Fits) : Fits(x, z);

        // The cell the feature takes in square (sx, sz), if it has one there.
        public bool In(int sx, int sz, out int x, out int z) => Landmarks.ChosenIn(sx, sz, Square, Salt, Fits, out x, out z);
    }

    public static class Features
    {
        // In the order they are tried on a cell: the first that stands there wins.
        public static readonly FeatureDef[] All =
        {
            new(FeatureId.GiantOak, "giant oak", FeatureGround.Dry, FeatureBlock.Trunk, 64, 0u, 7f, Landmarks.OakFits, "landmark.giant_tree"),
            new(FeatureId.Outcrop, "rock outcrop", FeatureGround.Dry, FeatureBlock.Stones, Landmarks.OutcropSquare, 1u, 8f, Landmarks.OutcropFits, "landmark.outcrop"),
            new(FeatureId.Ford, "ford", FeatureGround.Stream, FeatureBlock.None, 0, 0u, 0f, Landmarks.FordFits, "landmark.ford"),
        };

        public static FeatureDef Find(FeatureId id) => Array.Find(All, f => f.Id == id)
            ?? throw new ArgumentException($"no feature {id}");

        // The feature standing on cell (x, z) of the given ground, or null.
        public static FeatureDef? At(int x, int z, FeatureGround ground)
        {
            foreach (var f in All)
                if (f.Ground == ground && f.At(x, z)) return f;
            return null;
        }
    }
}
