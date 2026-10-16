// The kinds of weather a front can bring, shared by the server module and the
// client (docs/CONTENT.md, Weather row). A new kind of weather is a new entry
// here: the server picks fronts from this list by the season's odds. Keep this
// file free of engine and SpacetimeDB types. Ids are append-only.
using System;

namespace LifeGame.Shared
{
    // Cloud, rain and wind are the targets a front sets (0 to 1), each drawn
    // between Min and Max; Minutes is how long it holds, Min inclusive and Max
    // exclusive. Odds are the chance per front in each season, in percent,
    // indexed by Season (spring, summer, autumn, winter).
    public sealed record WeatherDef(
        int Id, string Name,
        float CloudMin, float CloudMax, float RainMin, float RainMax, float WindMin, float WindMax,
        int MinutesMin, int MinutesMax, float[] Odds);

    public static class Weathers
    {
        // Mostly fair weather, sometimes overcast, sometimes rain. Most rain is a
        // drizzle or a light shower you can see through; steady rain is less common,
        // and a thunderstorm is rare: most likely in summer, almost never in winter
        // (atmosphere-21). Seasons tilt the odds: a dry summer, wet spring and
        // autumn, grey winter.
        public static readonly WeatherDef[] All =
        {
            //  id  name           cloud          rain          wind          minutes   odds: spring summer autumn winter
            new(0, "fair",         0.05f, 0.35f,  0f,    0f,    0.15f, 0.45f, 12, 30,   new[] { 48f,    66f,   48f,   45f }),
            new(1, "overcast",     0.55f, 0.85f,  0f,    0f,    0.35f, 0.65f, 8,  20,   new[] { 23f,    20f,   23f,   33f }),
            new(2, "drizzle",      0.7f,  0.85f,  0.12f, 0.25f, 0.2f,  0.45f, 8,  20,   new[] { 11.02f, 4.81f, 11.02f, 8.72f }),
            new(3, "light rain",   0.8f,  0.95f,  0.3f,  0.45f, 0.35f, 0.6f,  6,  16,   new[] { 9.86f,  4.30f, 9.86f, 7.80f }),
            new(4, "steady rain",  0.9f,  1f,     0.55f, 0.72f, 0.5f,  0.75f, 6,  14,   new[] { 6.09f,  2.65f, 6.09f, 4.82f }),
            new(5, "thunderstorm", 1f,    1f,     0.88f, 1f,    0.75f, 1f,    5,  10,   new[] { 2.03f,  2.24f, 2.03f, 0.66f }),
        };

        public static WeatherDef? Find(int id) => Array.Find(All, w => w.Id == id);

        // The kind a roll between 0 and 1 lands on in this season, by the odds.
        public static WeatherDef Pick(Season season, double roll)
        {
            int s = (int)season;
            float total = 0f;
            foreach (var w in All) total += w.Odds[s];
            double at = roll * total;
            foreach (var w in All)
            {
                at -= w.Odds[s];
                if (at < 0) return w;
            }
            return All[0];
        }
    }
}
