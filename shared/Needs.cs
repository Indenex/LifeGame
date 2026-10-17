// Survival needs shared by the server module and the Godot client: how fast
// hunger, thirst and cold set in, and how a needs snapshot is read at any
// moment. Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public static class Needs
    {
        // Each need runs from 1 (full, warm) down to 0, which is death.
        // Rates are per second of time spent online. Justin chose the gentler
        // rates (2026-10-15): 90 minutes for food, 60 for water, and 16 minutes
        // of cold away from a fire.
        public const float FoodDrain = 1f / (90 * 60);
        public const float WaterDrain = 1f / (60 * 60);
        public const float ColdDrain = 1f / (16 * 60);
        // Standing in water chills you three times as fast.
        public const float WetChill = 3f;

        public const float StartFood = 0.8f;
        public const float StartWater = 0.8f;
        public const float StartWarmth = 1f;
        // After death you wake hungry and thirsty, but warm enough to find a fire.
        public const float RespawnFood = 0.6f;
        public const float RespawnWater = 0.6f;
        public const float RespawnWarmth = 1f;

        public const float BerryFood = 0.12f;
        public const float BerryWater = 0.02f;
        public const float SipWater = 0.2f;
        // Hazelnuts (survival-30, a PM default): a nibble, a little under a berry; only the hazel bushes
        // (about one in HazelEvery) have them, and only in autumn.
        public const float HazelnutFood = 0.08f;
        public const int HazelnutsPerPick = 3, HazelEvery = 3;
        public static bool IsHazelBush(ulong bushId) => bushId % HazelEvery == 0;
        public static bool HazelnutsRipe(Season season) => season == Season.Autumn;
        // Mutton (Justin, D-039): roast fills 40% of hunger. Burnt is a default of
        // ours; raw is not eaten for now.
        public const float RoastMuttonFood = 0.4f;
        public const float BurntMuttonFood = 0.1f;
        // Hens (animals-19): a raw egg is a snack, a roast chicken a little under mutton.
        public const float EggFood = 0.08f, RoastChickenFood = 0.3f, BurntChickenFood = 0.08f;
        // A boiled egg is a better snack than a raw one, still light (crafting-24).
        public const float BoiledEggFood = 0.2f, BurntEggFood = 0.05f;
        // Cooked in a pot (Justin, D-118; ceramics-7, agreed by Survival and Ceramics): better than
        // the same food eaten raw or roasted, and it quenches some thirst too.
        public const float BrothFood = 0.45f, BrothWater = 0.3f;
        public const float StewFood = 0.65f, StewWater = 0.3f;
        // A pot of water (Justin, D-030): a pinch pot quenches half your thirst, a coil pot all of it.
        // A hide waterskin (Justin picked it, survival-20): carries less than a pot, 40% of a drink.
        public const float WaterskinWater = 0.4f;
        public const float PinchPotWater = 0.5f;
        public const float CoilPotWater = 1f;

        // Weather (Justin, 2026-10-15): the cold drain doubles from a mild 15 °C
        // down to 0 °C, and rain adds half again unless you are under cover.
        public const float MildCelsius = 15f;
        public const float RainChill = 0.5f;
        // Cold and wet together bite at most this much harder than a mild dry day
        // (Justin, D-099): about 8 minutes from warm to frozen at worst, away from fire.
        public const float MaxChill = 2f;
        // How cold the air is: 0 at MildCelsius or warmer, 1 at 0 C or colder. The cold
        // drain is 1 + this; the client shows breath from it (D-144).
        public static float AirCold(float celsius) => Math.Clamp((MildCelsius - celsius) / MildCelsius, 0f, 1f);
        // How clearly breath shows (D-144), 0 to 1: from about 6 C (AirCold 0.6), full
        // at 0 C. Animation reads it as Needs.Breath(Needs.AirCold(Atmosphere.AirCelsius)).
        public static float Breath(float airCold) => Math.Clamp((airCold - 0.6f) / 0.4f, 0f, 1f);

        // Getting wet (survival-5). Wetness runs from 0 (dry) to 1 (soaked). Full
        // rain soaks you in two minutes and wading in about ten seconds; you dry
        // in five minutes, twice as fast by a fire. Cover stops rain wetting you.
        // Soaked, the cold bites RainChill (50%) harder: this replaces the old
        // straight rain bonus rather than stacking on it.
        public const float RainWetting = 1f / 120f;
        public const float WadeWetting = 1f / 10f;
        public const float Drying = 1f / 300f;
        public const float FireDrying = 2f;
        // Falling snow wets you at half rain's rate (Justin, D-134).
        public const float SnowWetting = 0.5f;

        // A new arrival (D-115): a new character's first half hour online, the cold
        // drains at half speed, so a winter start isn't a race. Not after a death.
        public const float ArrivalSeconds = 30 * 60;
        public const float ArrivalChill = 0.5f;
        // First-evening ease (survival-10, Justin picked C): a new character's first
        // hour online, thirst falls 20% slower, since water is a longer walk than
        // bushes or a fire. Not after a death.
        public const float ThirstEaseSeconds = 60 * 60;
        public const float ThirstEaseFactor = 0.8f;

        // How fast wetness changes: rain under the sky (0 to 1, less any cover),
        // standing in water, and whether a fire is near.
        public static float WetnessRate(float rain, float rainCover, bool wading, bool nearFire)
        {
            float wetting = (wading ? WadeWetting : 0f) + RainWetting * Math.Clamp(rain, 0f, 1f) * (1f - Math.Clamp(rainCover, 0f, 1f));
            return wetting > 0f ? wetting : -Drying * (nearFire ? FireDrying : 1f);
        }

        // Ripe berries grow back this long after a bush is picked.
        public const long BushRegrowMicros = 8L * 60 * 1_000_000;
        public const int BushesPerChunk = 2;
        // How close you must stand to pick a bush. The client offers it a little
        // inside this so a picking never gets refused for being a step too far.
        public const float BushReach = 2.5f;
        // The process id picking berries reports, so it trains Foraging. Not a crafting
        // recipe, so it lives here rather than in Processes.List (like ClayRules.FillProcess).
        public const string ForageProcess = "gather.berries";

        // The surface of rivers and ponds, in meters. Matches Look.WaterLevel on the client.
        public const float WaterLevel = -1.0f;

        // Below this a need is worth warning about.
        public const float Low = 0.25f;

        // What eating one of an item gives: (food, water). Zero for anything inedible.
        public static (float food, float water) Nourishment(Item item) =>
            Foods.Find(item) is { } f ? (f.Food, f.Water) : (0f, 0f);

        public static bool Edible(Item item) => Nourishment(item).food > 0f;

        // Which of the foods you carry to eat at this hunger: the most filling one
        // that doesn't waste much, or else the smallest, so a roast isn't spent on a
        // nibble. Null if nothing carried is edible.
        public static Item? BestFood(float food, System.Collections.Generic.IEnumerable<Item> carried)
        {
            float room = 1f - food + 0.05f;
            Item? best = null, smallest = null;
            float bestFill = 0f, smallestFill = float.MaxValue;
            foreach (var item in carried)
            {
                float fill = Nourishment(item).food;
                if (fill <= 0f) continue;
                if (fill <= room && fill > bestFill) { best = item; bestFill = fill; }
                if (fill < smallestFill) { smallest = item; smallestFill = fill; }
            }
            return best ?? smallest;
        }

        // A need's value some seconds after it was recorded at `value` changing by `rate`.
        public static float At(float value, float rate, float seconds) =>
            Math.Clamp(value + rate * Math.Max(0f, seconds), 0f, 1f);

        // How many seconds until a falling need reaches zero; infinity if it is not falling.
        public static float SecondsToEmpty(float value, float rate) =>
            rate < 0f ? value / -rate : float.PositiveInfinity;
    }
}
