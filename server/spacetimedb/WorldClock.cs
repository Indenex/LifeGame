using LifeGame.Shared;
using SpacetimeDB;

// The world clock and the weather. Clients read both to draw the sky and play
// the ambience; other systems read them through the helpers at the bottom
// (drying, air temperature).
public static partial class Module
{
    const int WeatherTickSeconds = 20;

    // One row. Clients compute the time of day from DayStart and their offset to
    // ServerNow, which is refreshed on every weather tick.
    [SpacetimeDB.Table(Accessor = "WorldClock", Public = true)]
    public partial struct WorldClock
    {
        [PrimaryKey]
        public uint Id;
        public Timestamp DayStart;
        public uint DayLengthSeconds;
        public Timestamp ServerNow;
    }

    // One row for the region. All values run from 0 to 1. Rain and Cloud ease
    // toward their targets; Wetness is how soaked the ground is and lags behind.
    [SpacetimeDB.Table(Accessor = "Weather", Public = true)]
    public partial struct Weather
    {
        [PrimaryKey]
        public uint Id;
        public float Cloud;
        public float Rain;
        public float Wind;
        public float Wetness;
        public float TargetCloud;
        public float TargetRain;
        public float TargetWind;
        public Timestamp NextChangeAt;
    }

    [SpacetimeDB.Table(Accessor = "WeatherTickTimer", Scheduled = nameof(WeatherTick), ScheduledAt = nameof(ScheduledAt))]
    public partial struct WeatherTickTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    // Safe to call any time: creates the clock, weather and timer if they are missing,
    // so databases published before this existed pick it up on the next connect.
    static void EnsureWorldClock(ReducerContext ctx)
    {
        if (ctx.Db.WorldClock.Id.Find(0) is null)
        {
            // Start the first day mid-morning so new worlds open in daylight.
            long start = ctx.Timestamp.MicrosecondsSinceUnixEpoch - (long)(WorldTime.DayLengthSeconds * 0.35 * 1_000_000);
            ctx.Db.WorldClock.Insert(new WorldClock
            {
                Id = 0,
                DayStart = new Timestamp(start),
                DayLengthSeconds = WorldTime.DayLengthSeconds,
                ServerNow = ctx.Timestamp,
            });
        }
        if (ctx.Db.Weather.Id.Find(0) is null)
        {
            ctx.Db.Weather.Insert(new Weather
            {
                Id = 0,
                Cloud = 0.2f,
                TargetCloud = 0.2f,
                Wind = 0.3f,
                TargetWind = 0.3f,
                NextChangeAt = After(ctx, 10 * 60),
            });
        }
        if (!ctx.Db.WeatherTickTimer.Iter().Any())
        {
            ctx.Db.WeatherTickTimer.Insert(new WeatherTickTimer
            {
                ScheduledId = 0,
                ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(WeatherTickSeconds)),
            });
        }
    }

    [SpacetimeDB.Reducer]
    public static void WeatherTick(ReducerContext ctx, WeatherTickTimer timer)
    {
        if (!ctx.SenderAuth.IsInternal)
            throw new Exception("Only the server changes the weather");
        EnsureWorldClock(ctx);
        var clock = ctx.Db.WorldClock.Id.Find(0)!.Value;
        ctx.Db.WorldClock.Id.Update(clock with { ServerNow = ctx.Timestamp });

        var w = ctx.Db.Weather.Id.Find(0)!.Value;
        if (ctx.Timestamp.MicrosecondsSinceUnixEpoch >= w.NextChangeAt.MicrosecondsSinceUnixEpoch)
            w = NextWeather(ctx, w);

        // Clouds gather over a few minutes and rain follows them; a storm takes six
        // or seven minutes to build and as long to pass.
        w.Cloud = Toward(w.Cloud, w.TargetCloud, 0.06f);
        w.Rain = Toward(w.Rain, Math.Min(w.TargetRain, w.Cloud * 1.1f), 0.05f);
        w.Wind = Toward(w.Wind, w.TargetWind, 0.06f);

        // Heavy rain soaks the ground in about five minutes. It dries over half an
        // hour or so in sun, and much more slowly at night or under cloud.
        if (w.Rain > 0.05f)
            w.Wetness = Math.Min(1f, w.Wetness + w.Rain * 0.07f);
        else
            w.Wetness = Math.Max(0f, w.Wetness - 0.012f * GroundDrying(ctx, w));

        ctx.Db.Weather.Id.Update(w);
    }

    // A front moves in: mostly fair weather, sometimes overcast, sometimes rain.
    // Most rain is a drizzle or a light shower you can see through; steady rain is
    // less common, and a heavy storm that closes the world in is rare.
    static Weather NextWeather(ReducerContext ctx, Weather w)
    {
        double roll = ctx.Rng.NextDouble();
        int minutes;
        if (roll < 0.55)
        {
            w.TargetCloud = Range(ctx, 0.05f, 0.35f);
            w.TargetRain = 0f;
            w.TargetWind = Range(ctx, 0.15f, 0.45f);
            minutes = ctx.Rng.Next(12, 30);
        }
        else if (roll < 0.78)
        {
            w.TargetCloud = Range(ctx, 0.55f, 0.85f);
            w.TargetRain = 0f;
            w.TargetWind = Range(ctx, 0.35f, 0.65f);
            minutes = ctx.Rng.Next(8, 20);
        }
        else
        {
            double kind = ctx.Rng.NextDouble();
            (w.TargetRain, w.TargetCloud, w.TargetWind, minutes) = kind switch
            {
                < 0.38 => (Range(ctx, 0.12f, 0.25f), Range(ctx, 0.7f, 0.85f), Range(ctx, 0.2f, 0.45f), ctx.Rng.Next(8, 20)),   // drizzle
                < 0.72 => (Range(ctx, 0.3f, 0.45f), Range(ctx, 0.8f, 0.95f), Range(ctx, 0.35f, 0.6f), ctx.Rng.Next(6, 16)),    // light rain
                < 0.93 => (Range(ctx, 0.55f, 0.72f), Range(ctx, 0.9f, 1f), Range(ctx, 0.5f, 0.75f), ctx.Rng.Next(6, 14)),      // steady rain
                _ => (Range(ctx, 0.88f, 1f), 1f, Range(ctx, 0.75f, 1f), ctx.Rng.Next(5, 10)),                                  // storm
            };
        }
        w.NextChangeAt = After(ctx, minutes * 60);
        return w;
    }

    // --- For other systems -------------------------------------------------

    // How far through the day it is: 0 midnight, 0.25 sunrise, 0.5 noon, 0.75 sunset.
    public static float TimeOfDay(ReducerContext ctx)
    {
        if (ctx.Db.WorldClock.Id.Find(0) is not { } clock) return 0.4f;
        return WorldTime.TimeOfDay(ctx.Timestamp.MicrosecondsSinceUnixEpoch, clock.DayStart.MicrosecondsSinceUnixEpoch, (int)clock.DayLengthSeconds);
    }

    public static Weather CurrentWeather(ReducerContext ctx) =>
        ctx.Db.Weather.Id.Find(0) ?? new Weather { Cloud = 0.2f, Wind = 0.3f };

    // How fast things dry outdoors right now, where 1 is a dry day. Rain halves it
    // (a pot takes about twice as long to dry in rain); night and still air slow
    // it a little, sun and wind speed it up a little.
    public static float DryingRate(ReducerContext ctx)
    {
        var w = CurrentWeather(ctx);
        float sun = 0.85f + 0.2f * WorldTime.Daylight(TimeOfDay(ctx)) * (1f - w.Cloud);
        float rain = 1f - 0.5f * Math.Clamp(w.Rain * 2f, 0f, 1f);
        float wind = 0.95f + 0.1f * w.Wind;
        return sun * rain * wind;
    }

    // How fast soaked ground dries: hardly at all at night or under heavy cloud.
    static float GroundDrying(ReducerContext ctx, Weather w) =>
        0.15f + 0.85f * WorldTime.Daylight(TimeOfDay(ctx)) * (1f - 0.6f * w.Cloud);

    // Outdoor air temperature in degrees Celsius: a mild forest, cooler at night
    // and in rain, colder in wind.
    public static float AirTemperature(ReducerContext ctx)
    {
        var w = CurrentWeather(ctx);
        float day = WorldTime.Daylight(TimeOfDay(ctx));
        return 9f + 10f * day * (1f - 0.4f * w.Cloud) - 3f * w.Rain - 2f * w.Wind;
    }

    static float Toward(float value, float target, float step) =>
        value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);

    static float Range(ReducerContext ctx, float min, float max) => min + (float)ctx.Rng.NextDouble() * (max - min);

    static Timestamp After(ReducerContext ctx, int seconds) =>
        new(ctx.Timestamp.MicrosecondsSinceUnixEpoch + seconds * 1_000_000L);
}
