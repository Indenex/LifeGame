using SpacetimeDB;

public static partial class Module
{
    // Rain is region-wide for now (Atmosphere's Weather row, 0 to 1). Clay out in
    // the rain dries about half as fast; the drying timer picks up changes.
    static partial void RainAt(ReducerContext ctx, float x, float z, ref float rain) =>
        rain = CurrentWeather(ctx).Rain;
}
