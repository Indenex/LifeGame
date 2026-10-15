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
        // A pot of water (Justin, D-030): a pinch pot quenches half your thirst, a coil pot all of it.
        public const float PinchPotWater = 0.5f;
        public const float CoilPotWater = 1f;

        // Weather (Justin, 2026-10-15): the cold drain doubles from a mild 15 °C
        // down to 0 °C, and rain adds half again unless you are under cover.
        public const float MildCelsius = 15f;
        public const float RainChill = 0.5f;

        // Ripe berries grow back this long after a bush is picked.
        public const long BushRegrowMicros = 8L * 60 * 1_000_000;
        public const int BushesPerChunk = 2;

        // The surface of rivers and ponds, in meters. Matches Look.WaterLevel on the client.
        public const float WaterLevel = -1.0f;

        // Below this a need is worth warning about.
        public const float Low = 0.25f;

        // A need's value some seconds after it was recorded at `value` changing by `rate`.
        public static float At(float value, float rate, float seconds) =>
            Math.Clamp(value + rate * Math.Max(0f, seconds), 0f, 1f);

        // How many seconds until a falling need reaches zero; infinity if it is not falling.
        public static float SecondsToEmpty(float value, float rate) =>
            rate < 0f ? value / -rate : float.PositiveInfinity;
    }
}
