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

    // Where a piece of worked clay set down in the world is in its life. Once a
    // firing ends the piece leaves the world as a pot or potsherds on the ground.
    public enum ClayStage : byte
    {
        Drying,
        Firing,
        // Only used for drawing: a fired pot, or one that broke in the fire.
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
        // Firing is not a crafting-panel process, but it is a skill like one.
        public const string PitFireProcess = "clay.pit_fire";
        public const string FillProcess = "clay.fill";

        // Moisture runs from 1 (just shaped) to 0 (bone dry).
        public const float LeatherHard = 0.45f;
        public const float SafeToFire = 0.08f;

        // Seconds to dry from wet to bone dry in the open on a dry day (Justin, D-013).
        public const float BaseDrySeconds = 480f;
        // Rain halves drying at full strength; a fire close by doubles it.
        public const float RainSlowdown = 0.5f;
        public const float WarmthSpeedup = 1f;

        public const float PitFireSeconds = 240f;
        public const float KilnFireSeconds = 180f;

        // A dry pot this close to a burning fire can be fired where it stands.
        public const float FireReach = 1.8f;
        // Clay shaped right where a fire burns is set down at this distance from it.
        public const float FireEdge = 1.0f;
        // Pots set into a fire sit on a ring this far from its center.
        public const float FireRing = 0.45f;
        public const int MaxPotsPerFire = 4;
        // Each player can have this many pieces drying or firing at once.
        public const int MaxPiecesPerPlayer = 24;

        // Even a dry pot can crack in an open fire. Better made pots (tempered,
        // well shaped) crack less; a kiln heats evenly and rarely cracks one.
        public static float CrackChance(FireKind kind, byte quality) =>
            kind == FireKind.Kiln ? 0.02f : 0.04f + 0.2f * (1f - quality / 100f) * (1f - quality / 100f);

        public const int SherdsPerCrack = 2;

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

        public static string Describe(float moisture) =>
            moisture > LeatherHard ? "wet" : moisture > SafeToFire ? "leather-hard" : "bone dry";

        public static string FormName(ClayForm form) => form switch
        {
            ClayForm.PinchPot => "pinch pot",
            ClayForm.CoilPot => "coil pot",
            _ => "brick",
        };

        // What a shaping process sets down.
        public static ClayForm? FormFor(string processId) => processId switch
        {
            "clay.pinch_pot" => ClayForm.PinchPot,
            "clay.coil_pot" => ClayForm.CoilPot,
            "clay.brick" => ClayForm.Brick,
            _ => null,
        };

        // The item a dry, unfired piece becomes when picked up, and back.
        public static Item UnfiredItem(ClayForm form) => form == ClayForm.CoilPot ? Item.UnfiredCoilPot : Item.UnfiredPinchPot;

        public static ClayForm? FormOfUnfired(Item item) => item switch
        {
            Item.UnfiredPinchPot => ClayForm.PinchPot,
            Item.UnfiredCoilPot => ClayForm.CoilPot,
            _ => null,
        };

        public static Item FiredItem(ClayForm form) => form == ClayForm.CoilPot ? Item.CoilPot : Item.PinchPot;

        public static Item? FilledItem(Item empty) => empty switch
        {
            Item.PinchPot => Item.PinchPotOfWater,
            Item.CoilPot => Item.CoilPotOfWater,
            _ => null,
        };

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
