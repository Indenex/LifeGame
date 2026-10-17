// Things that drift down through the year (docs/CONTENT.md, Weather / season event
// row): autumn leaves, spring blossom. A new one is a new entry here; the client's
// FallingLeaves draws every entry the same way. Keep this file free of engine and
// SpacetimeDB types. Ids are append-only.
using System;

namespace LifeGame.Shared
{
    // When in the year it falls: nothing before From, rising to full by FullFrom,
    // full until FullTo, gone by To (year fractions, 0 spring, 0.5 autumn). Rate is
    // the share of the particle budget at full; WindBoost how much a strong wind
    // adds; UnderTrees scales it by how wooded it is around the player. Colors are
    // picked at random per piece (linear RGB, 0 to 1); Size is its length in metres.
    public sealed record SeasonEventDef(
        int Id, string Name, float From, float FullFrom, float FullTo, float To,
        float Rate, float WindBoost, bool UnderTrees, float[][] Colors, float Size);

    public static class SeasonEvents
    {
        public static readonly SeasonEventDef[] All =
        {
            // Leaves come down while the broadleaf trees turn and go bare (Seasons.Autumn
            // rises from 0.5; the shader's leaf_drop finishes by about 0.72).
            new(0, "falling leaves", 0.52f, 0.58f, 0.68f, 0.74f, 1f, 1.5f, true,
                new[]
                {
                    new[] { 0.62f, 0.42f, 0.08f }, new[] { 0.70f, 0.30f, 0.06f },
                    new[] { 0.58f, 0.16f, 0.06f }, new[] { 0.36f, 0.22f, 0.10f },
                }, 0.11f),
            // Pale blossom petals in mid-spring, fewer and smaller.
            new(1, "blossom", 0.05f, 0.09f, 0.16f, 0.21f, 0.55f, 1.2f, true,
                new[] { new[] { 0.97f, 0.94f, 0.92f }, new[] { 0.95f, 0.80f, 0.84f }, new[] { 0.98f, 0.88f, 0.90f } },
                0.06f),
        };

        public static SeasonEventDef? Find(int id) => Array.Find(All, e => e.Id == id);

        // How much of it is falling now, 0 to 1: by the year, the wind (0 to 1) and
        // how wooded it is around the player (0 to 1).
        public static float Strength(SeasonEventDef e, float year, float wind, float woods)
        {
            year -= MathF.Floor(year);
            float on = Smooth(e.From, e.FullFrom, year) * (1f - Smooth(e.FullTo, e.To, year));
            float blow = 1f + e.WindBoost * Math.Clamp(wind, 0f, 1f);
            float where = e.UnderTrees ? Math.Clamp(woods, 0f, 1f) : 1f;
            return Math.Clamp(e.Rate * on * where * blow * 0.5f, 0f, 1f);
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
