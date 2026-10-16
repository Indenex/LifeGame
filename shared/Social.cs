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

        // claims-6: "/me waves" is an action, shown as "Mira waves".
        public static bool IsEmote(string text) =>
            text.StartsWith("/me ", StringComparison.OrdinalIgnoreCase) || text.Equals("/me", StringComparison.OrdinalIgnoreCase);
        public static string EmoteBody(string text) => text.Length > 3 ? text[3..].Trim() : "";

        // How bright a chat line is in the log: full within a third of chat range,
        // easing to 55% at the edge.
        public static float ChatDim(float distance)
        {
            float t = Math.Clamp((distance - ChatRange / 3f) / (ChatRange * 2f / 3f), 0f, 1f);
            return 1f - 0.45f * t * t * (3f - 2f * t);
        }
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
        // claims-5: working inside your claim tends it, at most this often.
        public const long WorkTendEverySeconds = 60;
        // The "tended … · stands …" line shows above a marker within this distance.
        public const float MarkerLineDistance = 5f;

        // --- Fighting (claims-3, D-053) ---
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

        // A player who strikes someone down shows a red name for this long (D-053).
        public const long KillerMarkMicros = 30L * 60 * 1_000_000;

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

        // claims-7: trading (D-069). Start within TradeReach (the server allows
        // TradeReachSlack more for lag); farther apart than TradeBreakDistance ends it.
        public const float TradeReach = 3f;
        public const float TradeReachSlack = 1f;
        public const float TradeBreakDistance = 4f;
        public const long TradeAskMicros = 30_000_000;
        public const int TradeMaxStacks = 8;
        public const byte TradeAsked = 0, TradeOpen = 1;
        public const byte SideA = 0, SideB = 1;

        // The process a finished claim marker reports (Journal's "Things to try").
        public const string MarkProcess = "claim.mark";

        // claims-9: a claim's name, as the owner gave it: trimmed, single-spaced, at
        // most ClaimNameMax characters. Empty means unnamed.
        public const int ClaimNameMax = 28;

        public static string CleanClaimName(string name)
        {
            var words = (name ?? "").Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string clean = string.Join(" ", words);
            return clean.Length > ClaimNameMax ? clean[..ClaimNameMax].TrimEnd() : clean;
        }

        // "a stone flake", "an axe", "some clay", "3 sticks": how a gift reads in a line.
        public static string GiftPhrase(ItemDef def, uint count)
        {
            if (count > 1) return $"{count} {def.Plural.ToLowerInvariant()}";
            string name = def.Name.ToLowerInvariant();
            if (def.Plural == def.Name) return $"some {name}";
            return ("aeiou".IndexOf(name[0]) >= 0 ? "an " : "a ") + name;
        }

        // "Tended 2 days ago · stands 1 more day": what a marker and the claim panel say.
        public static string TendedLine(long tendedAtMicros, long expiresAtMicros, long nowMicros)
        {
            long ago = Math.Max(0, (nowMicros - tendedAtMicros) / 1_000_000);
            long left = (expiresAtMicros - nowMicros) / 1_000_000;
            string tended = ago < 60 ? "Tended just now" : $"Tended {Span(ago)} ago";
            string stands = left <= 0 ? "falling now" : left < 60 ? "stands a few more moments" : $"stands {Span(left, more: true)}";
            return $"{tended} · {stands}";
        }

        // "3 days", "5 h", "12 min"; with more: "3 more days", "5 more hours", "12 more minutes".
        static string Span(long seconds, bool more = false)
        {
            if (seconds >= 86400) { long d = seconds / 86400; return more ? $"{d} more {(d == 1 ? "day" : "days")}" : $"{d} {(d == 1 ? "day" : "days")}"; }
            if (seconds >= 3600) { long h = seconds / 3600; return more ? $"{h} more {(h == 1 ? "hour" : "hours")}" : $"{h} h"; }
            long m = Math.Max(1, seconds / 60);
            return more ? $"{m} more {(m == 1 ? "minute" : "minutes")}" : $"{m} min";
        }
    }
}
