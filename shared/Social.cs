// Rules for chat, land claims and PvP, shared by the server module and the
// client so both agree on ranges and costs. Keep this file free of engine
// and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public static class Social
    {
        // Chat is heard by anyone within this many meters of the speaker.
        public const float ChatRange = 24f;
        public const int ChatMaxLength = 200;
        public const long ChatCooldownMicros = 400_000;
        // Messages are kept briefly so people who walk up can catch the last few lines.
        public const long ChatKeepMicros = 120_000_000;

        // A claim is a circle of land around a marker post.
        // 20 m so a group's huts and fire fit inside one claim (Justin, D-023).
        public const float ClaimRadius = 20f;
        // The soft border line on the ground fades in within this distance of a claim's edge (D-044).
        public const float BorderFadeDistance = 6f;
        public const int MaxClaimsPerPlayer = 1;
        // No claim may reach into the clearing where new players wake up.
        public const float SpawnClearRadius = 20f;
        // You tend a marker by standing next to it.
        public const float MarkerReach = 3f;

        // What a marker post costs to raise.
        public const uint MarkerSticks = 4;
        public const uint MarkerFiber = 2;

        // A marker stands for this long after it was last tended, then falls and the
        // land is free again. It starts to look weathered in the last quarter.
        public const long DefaultClaimLifetimeSeconds = 3 * 24 * 60 * 60;
        public const float WeatheredFrom = 0.75f;

        // --- Fighting (claims-3; starting values, PM card to Justin pending) ---
        public const float MaxHealth = 100f;
        // A strike lands on someone in front of you within this reach. The server
        // allows a little slack for movement still in flight.
        public const float StrikeReach = 1.6f;
        public const float StrikeReachSlack = 0.6f;
        public const long StrikeCooldownMicros = 800_000;
        // Health comes back this fast once nobody has hit you for a while, if you are
        // fed and watered (above WellFed).
        public const float HealthRegenPerSecond = 0.5f;
        public const long RegenDelayMicros = 10_000_000;
        public const float WellFed = 0.2f;
        // New players can't strike or be struck for this long after first waking.
        public const long NewcomerGraceMicros = 10L * 60 * 1_000_000;

        // Damage by the best weapon carried: about 12 bare-handed hits to kill, about 6
        // with a hand axe.
        public static float StrikeDamage(bool spear, bool handAxe, bool flake) =>
            spear ? 20f : handAxe ? 17f : flake ? 12f : 8.5f;

        public static bool InSpawnClearing(float x, float z) =>
            x * x + z * z < SpawnClearRadius * SpawnClearRadius;

        // Health now, given the last recorded value and when it was last hit.
        public static float HealthAt(float health, long asOfMicros, long lastHitMicros, long nowMicros, bool fed)
        {
            if (!fed || health >= MaxHealth) return Math.Min(health, MaxHealth);
            long from = Math.Max(asOfMicros, lastHitMicros + RegenDelayMicros);
            if (nowMicros <= from) return health;
            return Math.Min(MaxHealth, health + (nowMicros - from) / 1_000_000f * HealthRegenPerSecond);
        }

        public static bool InClaim(float claimX, float claimZ, float x, float z)
        {
            float dx = x - claimX, dz = z - claimZ;
            return dx * dx + dz * dz <= ClaimRadius * ClaimRadius;
        }

        // How worn a marker looks, from 0 (fresh) to 1 (about to fall).
        public static float Wear(long tendedAtMicros, long lifetimeSeconds, long nowMicros)
        {
            if (lifetimeSeconds <= 0) return 1f;
            float age = (nowMicros - tendedAtMicros) / 1_000_000f / lifetimeSeconds;
            return Math.Clamp(age, 0f, 1f);
        }
    }
}
