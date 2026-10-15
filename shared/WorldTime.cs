// Time of day and the sun's path, shared by the server module and the client so
// both agree on when it is day. Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public static class WorldTime
    {
        // One in-game day lasts this many real seconds.
        public const int DayLengthSeconds = 90 * 60;

        // Time of day runs from 0 to 1: 0 is midnight, 0.25 sunrise, 0.5 noon, 0.75 sunset.
        public const float Sunrise = 0.25f;
        public const float Sunset = 0.75f;

        // How far through the day it is, given the server clock and when day zero began.
        public static float TimeOfDay(long nowMicros, long dayStartMicros, int dayLengthSeconds)
        {
            long length = Math.Max(1, dayLengthSeconds) * 1_000_000L;
            long into = (nowMicros - dayStartMicros) % length;
            if (into < 0) into += length;
            return (float)((double)into / length);
        }

        // Height of the sun above the horizon from -1 (midnight) to 1 (noon).
        public static float SunHeight(float timeOfDay) => -MathF.Cos(timeOfDay * MathF.PI * 2f);

        // 0 at night, 1 in full day, easing through dawn and dusk.
        public static float Daylight(float timeOfDay)
        {
            float t = Math.Clamp((SunHeight(timeOfDay) + 0.12f) / 0.42f, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
