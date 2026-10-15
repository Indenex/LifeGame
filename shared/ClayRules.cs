// Rules for clay and ceramics, shared so the server decides outcomes and the
// client shows the same drying and firing progress without asking. Keep this
// file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    // What a piece of worked clay was shaped into.
    public enum ClayForm : byte
    {
        PinchPot,
        CoilPot,
        Brick,
    }

    // Where a piece of worked clay is in its life.
    public enum ClayStage : byte
    {
        Drying,
        Firing,
        Fired,
        Cracked,
    }

    public enum FireKind : byte
    {
        Pit,
        Kiln,
    }

    public static class ClayRules
    {
        // Moisture runs from 1 (just shaped) to 0 (bone dry).
        public const float LeatherHard = 0.45f;
        public const float SafeToFire = 0.08f;

        // Seconds to dry from wet to bone dry in the open on a dry day.
        public const float BaseDrySeconds = 480f;
        // Rain halves drying at full strength; a fire close by doubles it.
        public const float RainSlowdown = 0.5f;
        public const float WarmthSpeedup = 1f;

        public const float PitFireSeconds = 240f;
        public const float KilnFireSeconds = 180f;

        // How close a pot must be to a fire to fire in it, and to dry faster beside it.
        public const float FireReach = 1.6f;
        public const float WarmthReach = 4f;

        // Firing is a little risky even when dry. Temper (sand, grog) takes the
        // shock out of it, and a kiln heats evenly.
        public const float PitCrackChance = 0.22f;
        public const float PitCrackChanceTempered = 0.05f;
        public const float KilnCrackChance = 0.02f;

        // Sifting a lump of dug clay gives this many lumps of clean clay.
        public const int CleanClayPerLump = 1;
        public static int ClayFor(ClayForm form) => form switch
        {
            ClayForm.PinchPot => 1,
            ClayForm.CoilPot => 3,
            _ => 1,
        };

        // rain and warmth are both 0..1.
        public static float DryRate(float rain, float warmth) =>
            (1f - RainSlowdown * Clamp01(rain)) * (1f + WarmthSpeedup * Clamp01(warmth)) / BaseDrySeconds;

        // Drying is stored as the moisture at a moment plus a rate, so it only
        // needs rewriting when the weather or a nearby fire changes.
        public static float MoistureAt(float moisture, float ratePerSecond, float secondsSince) =>
            Math.Max(0f, moisture - ratePerSecond * Math.Max(0f, secondsSince));

        public static float FireSeconds(FireKind kind) => kind == FireKind.Kiln ? KilnFireSeconds : PitFireSeconds;

        // A pot fired before it is bone dry cracks: the water left inside turns to steam.
        public static bool TooWetToFire(float moisture) => moisture > SafeToFire;

        public static float CrackChance(FireKind kind, bool tempered) =>
            kind == FireKind.Kiln ? KilnCrackChance : tempered ? PitCrackChanceTempered : PitCrackChance;

        // Quality 1..100 from how well it was shaped (0..1), the temper and the fire.
        public static byte Quality(float shaping, bool tempered, FireKind kind)
        {
            float q = 20f + 50f * Clamp01(shaping) + (tempered ? 10f : 0f) + (kind == FireKind.Kiln ? 20f : 0f);
            return (byte)Math.Clamp((int)MathF.Round(q), 1, 100);
        }

        public static string Describe(float moisture) =>
            moisture > LeatherHard ? "wet" : moisture > SafeToFire ? "leather-hard" : "bone dry";

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
