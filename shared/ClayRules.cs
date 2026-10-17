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
        // A big coil-built jar; kiln-fired only, then set down as a store (ceramics-6).
        StorageJar,
        // A small oil lamp: a shallow bowl with a pinched spout (ceramics-9).
        Lamp,
        // A curved roof tile; kiln-fired only, like the jar (ceramics-10).
        Tile,
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

        // The updraft kiln (ceramics-2). Built from bricks and daub, loaded with dry
        // pots, then fired with fuel from your bag. It heats evenly, so pots rarely
        // crack and come out a little better than from a pit.
        // Building gives this process to everyone who helps finish a kiln stage.
        public const string KilnProcess = "clay.kiln";
        public const string KilnFireProcess = "clay.kiln_fire";
        public const int KilnCapacity = 6;
        public const int KilnQualityBonus = 10;

        // The storage jar (ceramics-6): tempered clay it takes. Shared code finds
        // the jar and the kiln by their StructureShape flags (Kiln, Jar).
        public const int JarClay = 4;
        // Only a kiln heats a piece this big evenly enough.
        public static bool KilnOnly(ClayForm form) => form == ClayForm.StorageJar || form == ClayForm.Tile;

        // Clay grades by depth (D-041). Hand digging stops at World.MaxDigDepth; a
        // digging stick reaches one level further, into the deep clay: purer, with
        // fewer stones and roots, so it starts at "fine" quality. That quality
        // carries through cleaning and tempering into the pots, which then crack
        // less. depth is the cell's depth before this dig (0 = untouched ground).
        public const byte DeepClayQuality = 80;
        public static byte DugClayQuality(int depth) =>
            depth >= World.MaxDigDepth ? DeepClayQuality : Items.RawQuality;

        public static byte KilnFired(byte quality) => (byte)Math.Min(100, quality + KilnQualityBonus);

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

        // Seconds until a piece is dry enough to fire at its current drying rate;
        // infinite when it isn't drying (soaked through in heavy rain).
        public static float SecondsUntilDry(float moisture, float ratePerSecond) =>
            moisture <= SafeToFire ? 0f : ratePerSecond <= 0f ? float.PositiveInfinity : (moisture - SafeToFire) / ratePerSecond;

        // "about 3 min", "under a minute": how a player hears the wait.
        public static string DryWait(float seconds) =>
            float.IsInfinity(seconds) ? "not drying in this rain"
            : seconds < 60f ? "under a minute left"
            : $"about {MathF.Round(seconds / 60f):0} min left";

        // Whether a fire can take one more pot: fuel for a whole firing and a free place
        // in its ring. When it can't, G by a dry pot picks it up instead (bugfix-18).
        public static bool FireCanTakePot(float fuelSecondsLeft, int potsInFire) =>
            fuelSecondsLeft >= PitFireSeconds && potsInFire < MaxPotsPerFire;

        // Fuel for a pit firing, in sticks, when a fire has this many seconds left.
        public static int SticksForFiring(float secondsLeft, float stickSeconds) =>
            (int)MathF.Ceiling(Math.Max(0f, PitFireSeconds - secondsLeft) / stickSeconds);

        public static string Describe(float moisture) =>
            moisture > LeatherHard ? "wet" : moisture > SafeToFire ? "leather-hard" : "bone dry";

        public static string FormName(ClayForm form) => form switch
        {
            ClayForm.PinchPot => "pinch pot",
            ClayForm.CoilPot => "clay pot",
            ClayForm.StorageJar => "storage jar",
            ClayForm.Lamp => "lamp",
            ClayForm.Tile => "roof tile",
            _ => "brick",
        };

        // What a shaping process sets down.
        public static ClayForm? FormFor(string processId) => processId switch
        {
            "clay.pinch_pot" => ClayForm.PinchPot,
            "clay.coil_pot" => ClayForm.CoilPot,
            "clay.brick" => ClayForm.Brick,
            "clay.storage_jar" => ClayForm.StorageJar,
            "clay.lamp" => ClayForm.Lamp,
            "clay.tile" => ClayForm.Tile,
            _ => null,
        };

        // The item a dry, unfired piece becomes when picked up, and back.
        public static Item UnfiredItem(ClayForm form) => form switch
        {
            ClayForm.CoilPot => Item.UnfiredCoilPot,
            ClayForm.StorageJar => Item.UnfiredStorageJar,
            ClayForm.Lamp => Item.UnfiredLamp,
            ClayForm.Tile => Item.UnfiredRoofTile,
            _ => Item.UnfiredPinchPot,
        };

        public static ClayForm? FormOfUnfired(Item item) => item switch
        {
            Item.UnfiredPinchPot => ClayForm.PinchPot,
            Item.UnfiredCoilPot => ClayForm.CoilPot,
            Item.UnfiredStorageJar => ClayForm.StorageJar,
            Item.UnfiredLamp => ClayForm.Lamp,
            Item.UnfiredRoofTile => ClayForm.Tile,
            _ => null,
        };

        public static Item FiredItem(ClayForm form) => form switch
        {
            ClayForm.CoilPot => Item.CoilPot,
            ClayForm.StorageJar => Item.StorageJar,
            ClayForm.Lamp => Item.ClayLamp,
            ClayForm.Tile => Item.RoofTile,
            _ => Item.PinchPot,
        };

        // What an empty water container becomes when filled (pots and the waterskin): Foods.Pots.
        public static Item? FilledItem(Item empty) => Foods.FilledFrom(empty);

        // Cooking in a pot (ceramics-7). A coil pot of water and food set by a burning
        // fire simmers for this long, then waits, never burning (D-081). It keeps
        // simmering if the fire dies: the pot holds the heat.
        public const float BrothSeconds = 60f, StewSeconds = 90f;
        // Pots simmering by one fire, and per player anywhere.
        public const int PotsPerFire = 2, SimmersPerPlayer = 2;
        // Simmering pots sit on a ring this far from the fire's center.
        public const float SimmerRing = 0.55f;

        // The dish a cooking process makes, or null for other processes.
        public static Item? DishFor(string processId) => processId switch
        {
            "cook.broth" => Item.PotOfBroth,
            "cook.stew" => Item.PotOfStew,
            "cook.tallow" => Item.Tallow,
            _ => null,
        };

        // A pot that holds a dish while it cooks and comes back after eating, at its own quality.
        public static bool IsVessel(Item item) => item == Item.CoilPotOfWater;

        public static float SimmerSeconds(Item dish) => dish == Item.PotOfStew ? StewSeconds : BrothSeconds;

        public static string DishName(Item dish) => dish switch
        {
            Item.PotOfStew => "mutton stew",
            Item.Tallow => "tallow",
            _ => "berry broth",
        };

        // Tallow is skimmed off and the pot comes back empty; broth and stew stay in their pot.
        public static bool KeepsPot(Item dish) => dish == Item.Tallow;

        // The clay oil lamp (ceramics-9, Justin D-147): each tallow burns half an hour,
        // a lamp holds up to MaxLampTallow, and it lights this far round. A lamp refills
        // itself from tallow in storage nearby, so it never needs tending.
        public const float LampSecondsPerTallow = 1800f;
        public const float LampRefillCheckSeconds = 60f;
        public const int MaxLampTallow = 3;
        public const float LampLightRadius = 4f;
        // A lamp is lit from a burning fire or a lit lamp this close.
        public const float LightFromReach = 8f;
        public const int LampsPerPlayer = 6;

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
