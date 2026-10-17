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

    // The year (D-099): one row. Clients work out the season from YearStart, the
    // day length and DaysPerSeason with Seasons.YearFraction, like the time of day.
    [SpacetimeDB.Table(Accessor = "SeasonClock", Public = true)]
    public partial struct SeasonClock
    {
        [PrimaryKey]
        public uint Id;
        public Timestamp YearStart;
        public float DaysPerSeason;
    }

    // What the season does to the weather right now, updated every weather tick.
    // Precipitation eases like Weather.Rain used to; it falls as Weather.Rain when
    // warm and as Snow when cold, so Weather.Rain still means liquid rain for every
    // system that reads it. SnowCover is how much snow lies on the ground (0 to 1).
    [SpacetimeDB.Table(Accessor = "SeasonState", Public = true)]
    public partial struct SeasonState
    {
        [PrimaryKey]
        public uint Id;
        public float Precipitation;
        public float Snow;
        public float SnowCover;
        public float Temperature;
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
        if (ctx.Db.SeasonClock.Id.Find(0) is null)
        {
            var dayLength = ctx.Db.WorldClock.Id.Find(0)?.DayLengthSeconds ?? (uint)WorldTime.DayLengthSeconds;
            double yearMicros = dayLength * 1_000_000.0 * 4.0 * Seasons.DaysPerSeason;
            ctx.Db.SeasonClock.Insert(new SeasonClock
            {
                Id = 0,
                YearStart = new Timestamp(ctx.Timestamp.MicrosecondsSinceUnixEpoch - (long)(yearMicros * Seasons.StartYear)),
                DaysPerSeason = Seasons.DaysPerSeason,
            });
        }
        if (ctx.Db.SeasonState.Id.Find(0) is null)
            ctx.Db.SeasonState.Insert(new SeasonState { Id = 0, Precipitation = ctx.Db.Weather.Id.Find(0)?.Rain ?? 0f, Temperature = 10f });
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
        RequireServer(ctx);
        EnsureWorldClock(ctx);
        var clock = ctx.Db.WorldClock.Id.Find(0)!.Value;
        ctx.Db.WorldClock.Id.Update(clock with { ServerNow = ctx.Timestamp });

        var w = ctx.Db.Weather.Id.Find(0)!.Value;
        if (ctx.Timestamp.MicrosecondsSinceUnixEpoch >= w.NextChangeAt.MicrosecondsSinceUnixEpoch)
            w = NextWeather(ctx, w);

        // Clouds gather over a few minutes and rain follows them; a storm takes six
        // or seven minutes to build and as long to pass. Below freezing it comes
        // down as snow instead.
        var season = ctx.Db.SeasonState.Id.Find(0)!.Value;
        w.Cloud = Toward(w.Cloud, w.TargetCloud, 0.06f);
        season.Precipitation = Toward(season.Precipitation, Math.Min(w.TargetRain, w.Cloud * 1.1f), 0.05f);
        w.Wind = Toward(w.Wind, w.TargetWind, 0.06f);
        season.Temperature = Temperature(ctx, w, season.Precipitation);
        float snowy = Seasons.SnowFraction(season.Temperature);
        season.Snow = season.Precipitation * snowy;
        w.Rain = season.Precipitation - season.Snow;

        // Heavy snow covers the ground in about seven minutes. Above freezing it
        // melts slowly, faster in sun and rain: snow from a winter night lasts into
        // the afternoon and thins out by dusk.
        if (season.Snow > 0.05f)
            season.SnowCover = Math.Min(1f, season.SnowCover + season.Snow * 0.05f);
        else if (season.Temperature > 0.5f)
            season.SnowCover = Math.Max(0f, season.SnowCover - 0.0006f * Math.Min(season.Temperature, 10f) * (1f + WorldTime.Daylight(TimeOfDay(ctx)) + 2f * w.Rain));
        ctx.Db.SeasonState.Id.Update(season);

        // Heavy rain soaks the ground in about five minutes. It dries over half an
        // hour or so in sun, and much more slowly at night or under cloud.
        if (w.Rain > 0.05f)
            w.Wetness = Math.Min(1f, w.Wetness + w.Rain * 0.07f);
        else
            w.Wetness = Math.Max(0f, w.Wetness - 0.012f * GroundDrying(ctx, w));

        ctx.Db.Weather.Id.Update(w);
    }

    // A front moves in, picked from the kinds in shared/Weathers.cs by this
    // season's odds.
    static Weather NextWeather(ReducerContext ctx, Weather w)
    {
        var kind = Weathers.Pick(SeasonNow(ctx).Season, ctx.Rng.NextDouble());
        w.TargetCloud = Range(ctx, kind.CloudMin, kind.CloudMax);
        w.TargetRain = Range(ctx, kind.RainMin, kind.RainMax);
        w.TargetWind = Range(ctx, kind.WindMin, kind.WindMax);
        int minutes = ctx.Rng.Next(kind.MinutesMin, kind.MinutesMax);
        w.NextChangeAt = After(ctx, minutes * 60);
        return w;
    }

    // --- Setting the time (admins only) ---------------------------------------

    // Who may change the time from the game. The publisher (the claims admin) can
    // always; they add a player with `spacetime call <db> make_admin '"Name"'`,
    // since a game client signs in with its own identity, not the publisher's.
    [SpacetimeDB.Table(Accessor = "WorldAdmin")] // private (admin-1): clients read only their own row through my_admin
    public partial struct WorldAdmin
    {
        [PrimaryKey]
        public Identity Identity;
    }

    static bool IsWorldAdmin(ReducerContext ctx) =>
        ctx.Db.ClaimConfig.Id.Find(0)?.Admin == ctx.Sender || ctx.Db.WorldAdmin.Identity.Find(ctx.Sender) is not null;

    [SpacetimeDB.Reducer]
    public static void MakeAdmin(ReducerContext ctx, string playerName)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can make admins");
        Player? found = null;
        foreach (var p in ctx.Db.Player.Iter())
            if (string.Equals(p.Name, playerName.Trim(), StringComparison.OrdinalIgnoreCase)) { found = p; break; }
        if (found is not { } player) throw new Exception($"No player named {playerName}");
        if (ctx.Db.WorldAdmin.Identity.Find(player.Identity) is null)
            ctx.Db.WorldAdmin.Insert(new WorldAdmin { Identity = player.Identity });
    }

    // Moves the clock so it is now this far through the day (0 midnight, 0.25
    // sunrise, 0.5 noon, 0.75 sunset). Clients glide to the new time.
    [SpacetimeDB.Reducer]
    public static void SetTimeOfDay(ReducerContext ctx, float fraction)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can change the time");
        if (float.IsNaN(fraction) || fraction < 0f || fraction > 1f) throw new Exception("Time of day runs from 0 to 1");
        EnsureWorldClock(ctx);
        if (ctx.Db.WorldClock.Id.Find(0) is not { } clock) return;
        long length = clock.DayLengthSeconds * 1_000_000L;
        long start = ctx.Timestamp.MicrosecondsSinceUnixEpoch - (long)(fraction * length);
        ctx.Db.WorldClock.Id.Update(clock with { DayStart = new Timestamp(start), ServerNow = ctx.Timestamp });
    }

    // Moves the year so it is now this far through (0 spring, 0.25 summer, 0.5
    // autumn, 0.75 winter). Snow on the ground stays and melts or builds from there.
    [SpacetimeDB.Reducer]
    public static void SetSeason(ReducerContext ctx, float yearFraction)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can change the season");
        if (float.IsNaN(yearFraction) || yearFraction < 0f || yearFraction > 1f) throw new Exception("The year runs from 0 to 1");
        EnsureWorldClock(ctx);
        var clock = ctx.Db.SeasonClock.Id.Find(0)!.Value;
        long start = ctx.Timestamp.MicrosecondsSinceUnixEpoch - (long)(yearFraction * YearMicros(ctx, clock));
        ctx.Db.SeasonClock.Id.Update(clock with { YearStart = new Timestamp(start) });
    }

    // How many in-game days each season lasts, keeping the current point in the year.
    [SpacetimeDB.Reducer]
    public static void SetDaysPerSeason(ReducerContext ctx, float days)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can change the length of the seasons");
        if (float.IsNaN(days) || days < 0.05f || days > 365f) throw new Exception("Days per season run from 0.05 to 365");
        EnsureWorldClock(ctx);
        float year = SeasonNow(ctx).Year;
        var clock = ctx.Db.SeasonClock.Id.Find(0)!.Value with { DaysPerSeason = days };
        long start = ctx.Timestamp.MicrosecondsSinceUnixEpoch - (long)(year * YearMicros(ctx, clock));
        ctx.Db.SeasonClock.Id.Update(clock with { YearStart = new Timestamp(start) });
    }

    static double YearMicros(ReducerContext ctx, SeasonClock clock) =>
        (ctx.Db.WorldClock.Id.Find(0)?.DayLengthSeconds ?? (uint)WorldTime.DayLengthSeconds) * 1_000_000.0 * 4.0 * clock.DaysPerSeason;

    // --- For other systems -------------------------------------------------

    // Where the year is: Year 0 to 1 (0 spring, 0.25 summer, 0.5 autumn, 0.75
    // winter), the Season, and Warmth from -1 deep winter to 1 high summer.
    public static (float Year, Season Season, float Warmth) SeasonNow(ReducerContext ctx)
    {
        if (ctx.Db.SeasonClock.Id.Find(0) is not { } clock || ctx.Db.WorldClock.Id.Find(0) is not { } day)
            return (Seasons.StartYear, Seasons.SeasonOf(Seasons.StartYear), Seasons.Warmth(Seasons.StartYear));
        float year = Seasons.YearFraction(ctx.Timestamp.MicrosecondsSinceUnixEpoch, clock.YearStart.MicrosecondsSinceUnixEpoch, (int)day.DayLengthSeconds, clock.DaysPerSeason);
        return (year, Seasons.SeasonOf(year), Seasons.Warmth(year));
    }

    // How much snow lies on the ground, 0 to 1.
    public static float SnowCover(ReducerContext ctx) => ctx.Db.SeasonState.Id.Find(0)?.SnowCover ?? 0f;

    // How hard it is snowing right now, 0 to 1 (Weather.Rain is the rain).
    public static float Snowfall(ReducerContext ctx) => ctx.Db.SeasonState.Id.Find(0)?.Snow ?? 0f;

    // Survival's hook: falling snow wets you at half rain's rate (D-134).
    static partial void SnowfallAt(ReducerContext ctx, ref float snow) => snow = Snowfall(ctx);

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

    // Outdoor air temperature in degrees Celsius: a mild forest, cooler at night,
    // in rain or snow and in wind; 27 on a summer afternoon; a cool 10 or so on a
    // winter day and about -8 on a winter night (D-136).
    public static float AirTemperature(ReducerContext ctx) =>
        Temperature(ctx, CurrentWeather(ctx), ctx.Db.SeasonState.Id.Find(0)?.Precipitation ?? CurrentWeather(ctx).Rain);

    static float Temperature(ReducerContext ctx, Weather w, float precipitation)
    {
        float day = WorldTime.Daylight(TimeOfDay(ctx));
        return 9f + 10f * day * (1f - 0.4f * w.Cloud) - 3f * precipitation - 2f * w.Wind
            + Seasons.TemperatureOffset(SeasonNow(ctx).Warmth, day);
    }

    static float Toward(float value, float target, float step) =>
        value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);

    static float Range(ReducerContext ctx, float min, float max) => min + (float)ctx.Rng.NextDouble() * (max - min);

    static Timestamp After(ReducerContext ctx, int seconds) =>
        new(ctx.Timestamp.MicrosecondsSinceUnixEpoch + seconds * 1_000_000L);
}
