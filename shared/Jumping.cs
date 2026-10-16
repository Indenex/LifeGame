// The jump (bugfix-25): its arc and what it clears. Shared by the client and the server
// module, so the server can check a jump later (D-182). Keep this file free of engine
// and SpacetimeDB types. (Named Jumping because the server has a `Jump` reducer.)
using System;

namespace LifeGame.Shared
{
    public static class Jumping
    {
        // Justin's picks: 1.1 m high, 8% of the stamina bar, no steering in the air, not while sneaking.
        public const float Apex = 1.1f;
        // From leaving the ground to touching down.
        public const float AirSeconds = 0.65f;
        // Between one takeoff and the next.
        public const float CooldownSeconds = 0.4f;
        // A hop's landing strength for the sound and the landing pose (0 hop, 1 long fall).
        public const float LandingStrength = 0.3f;

        // Constant gravity that gives exactly Apex at the middle of AirSeconds.
        public const float Gravity = 8f * Apex / (AirSeconds * AirSeconds);
        public const float LaunchSpeed = Gravity * AirSeconds / 2f;

        // How high the feet are `seconds` after takeoff; 0 on the ground.
        public static float Height(float seconds) =>
            seconds <= 0f || seconds >= AirSeconds ? 0f : LaunchSpeed * seconds - 0.5f * Gravity * seconds * seconds;

        public static bool Rising(float seconds) => seconds < AirSeconds / 2f;

        // How high what you jump over stands. A walker is above it when Height() is at least this.
        public const float LogHeight = 0.5f, FenceHeight = 0.8f, GateHeight = 0.9f;

        // A boulder stands about one and a half times its radius high; pebbles never block.
        public static float RockHeight(float radius) => 1.5f * radius;

        public static float FenceTop(bool gate) => gate ? GateHeight : FenceHeight;

        // Whether a walker airborne at this height clears something this tall.
        public static bool Clears(float obstacleHeight, float airHeight) => airHeight >= obstacleHeight;

        // A fence or gate standing in a claim (Social.InClaim) stops everyone who isn't a member,
        // however high they jump; members hop their own (Justin's rule).
        public static bool FenceStops(float airHeight, bool gate, bool inClaim, bool member) =>
            !Clears(FenceTop(gate), airHeight) || (inClaim && !member);

        // Takeoff is allowed on the ground, after the cooldown, and not while sneaking.
        public static bool CanStart(bool grounded, float sinceLastTakeoff, bool sneaking) =>
            grounded && !sneaking && sinceLastTakeoff >= CooldownSeconds;
    }
}
