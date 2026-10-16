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
        // Mutton (Justin, D-039): roast fills 40% of hunger. Burnt is a default of
        // ours; raw is not eaten for now.
        public const float RoastMuttonFood = 0.4f;
        public const float BurntMuttonFood = 0.1f;
        // A pot of water (Justin, D-030): a pinch pot quenches half your thirst, a coil pot all of it.
        public const float PinchPotWater = 0.5f;
        public const float CoilPotWater = 1f;

        // Weather (Justin, 2026-10-15): the cold drain doubles from a mild 15 °C
        // down to 0 °C, and rain adds half again unless you are under cover.
        public const float MildCelsius = 15f;
        public const float RainChill = 0.5f;
        // Cold and wet together bite at most this much harder than a mild dry day
        // (Justin, D-099): about 8 minutes from warm to frozen at worst, away from fire.
        public const float MaxChill = 2f;

        // Getting wet (survival-5). Wetness runs from 0 (dry) to 1 (soaked). Full
        // rain soaks you in two minutes and wading in about ten seconds; you dry
        // in five minutes, twice as fast by a fire. Cover stops rain wetting you.
        // Soaked, the cold bites RainChill (50%) harder: this replaces the old
        // straight rain bonus rather than stacking on it.
        public const float RainWetting = 1f / 120f;
        public const float WadeWetting = 1f / 10f;
        public const float Drying = 1f / 300f;
        public const float FireDrying = 2f;

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
        public static (float food, float water) Nourishment(Item item) => item switch
        {
            Item.Berries => (BerryFood, BerryWater),
            Item.CookedMutton => (RoastMuttonFood, 0f),
            Item.BurntMutton => (BurntMuttonFood, 0f),
            _ => (0f, 0f),
        };

        public static bool Edible(Item item) => Nourishment(item).food > 0f;

        // A cooked meal, which lets you run longer for a while (D-111). Berries and
        // burnt food don't count.
        public static bool GoodMeal(Item item) => item is Item.CookedMutton;

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
