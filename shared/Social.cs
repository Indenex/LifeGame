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
