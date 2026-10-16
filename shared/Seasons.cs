// The year and its seasons, shared by the server module and the client so both
// agree on the season, the cold and the snow. Keep this file free of engine and
// SpacetimeDB types. Atmosphere leads seasons (D-099); the year runs through
// spring, summer, autumn and winter, one in-game day each for now (D-114).
using System;

namespace LifeGame.Shared
{
    public enum Season { Spring, Summer, Autumn, Winter }

    public static class Seasons
    {
        // In-game days per season for new worlds. Admins change it with /season-speed.
        public const float DaysPerSeason = 1f;

        // Where a new world's year starts: early spring, so the first change comes soon.
        public const float StartYear = 0.1f;

        // Precipitation falls as snow below this air temperature (degrees C), as
        // rain above SleetTop, and as a mix in between.
        public const float SnowBelow = -0.5f;
        public const float SleetTop = 1.5f;

        // How far through the year it is, 0 to 1: 0 the first day of spring, 0.25
        // summer, 0.5 autumn, 0.75 winter.
        public static float YearFraction(long nowMicros, long yearStartMicros, int dayLengthSeconds, float daysPerSeason)
        {
            double length = Math.Max(1, dayLengthSeconds) * 1_000_000.0 * 4.0 * Math.Max(0.01f, daysPerSeason);
            double into = (nowMicros - yearStartMicros) % length;
            if (into < 0) into += length;
            return (float)(into / length);
        }

        public static Season SeasonOf(float year) => (Season)(Math.Clamp((int)(Wrap(year) * 4f), 0, 3));

        // How warm the season is, -1 deep winter (mid-winter) to 1 high summer
        // (mid-summer), easing smoothly through spring and autumn.
        public static float Warmth(float year) => MathF.Cos((Wrap(year) - 0.375f) * MathF.PI * 2f);

        // How far broadleaf trees have turned, 0 green to 1 fully turned: green
        // through spring and summer, turning through autumn, bare or brown through
        // winter, greening again in the first days of spring.
        public static float Autumn(float year)
        {
            year = Wrap(year);
            if (year < 0.1f) return 1f - Smooth(0f, 0.1f, year);
            return Smooth(0.5f, 0.68f, year);
        }

        // What the season adds to the air temperature, degrees C, given daylight (0
        // night to 1 full day): about +8 in high summer; in deep winter -5 by day and
        // -17 at night, so winter days only cool you and the hard cold comes at night
        // (D-136): about 14 on a clear winter day, 10 overcast, -8 at night.
        public static float TemperatureOffset(float warmth, float daylight) =>
            warmth >= 0f ? 8f * warmth : warmth * (5f + 12f * (1f - Math.Clamp(daylight, 0f, 1f)));

        // How much of the precipitation falls as snow at this air temperature, 0 to 1.
        public static float SnowFraction(float temperature) => 1f - Smooth(SnowBelow, SleetTop, temperature);

        public static string Name(Season season) => season switch
        {
            Season.Spring => "spring",
            Season.Summer => "summer",
            Season.Autumn => "autumn",
            _ => "winter",
        };

        // The middle of each season as a year fraction, for /season jumps.
        public static float Middle(Season season) => (int)season * 0.25f + 0.125f;

        static float Wrap(float year) => year - MathF.Floor(year);

        static float Smooth(float a, float b, float x)
        {
            float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
