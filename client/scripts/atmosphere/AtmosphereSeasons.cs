using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The seasons, as Atmosphere shows them (D-099). The server owns the year and the
// snow; this works out where the year is from SeasonClock, eases the snow, sets the
// season globals every shader reads (season_warmth, season_autumn, snow_cover,
// snowing) and drives the snowfall.
//
// Command line: --season=0.8 holds the year there (0 spring, 0.25 summer, 0.5
// autumn, 0.75 winter); --snow=1 and --snow-cover=1 hold the snow, for screenshots.
public partial class Atmosphere
{
    static readonly StringName NSeasonWarmth = "season_warmth";
    static readonly StringName NSeasonAutumn = "season_autumn";
    static readonly StringName NSnowCover = "snow_cover";
    static readonly StringName NSnowing = "snowing";

    long yearStartMicros;
    float daysPerSeason = Seasons.DaysPerSeason;
    bool seasonSynced;
    float targetSnow, targetSnowCover, snow, snowCover;
    float? targetAir;
    float air = 10f;
    float? argSeason, argSnow, argSnowCover;
    readonly Rain snowfall = new(snow: true);

    // Where the year is, 0 to 1 (0 spring, 0.25 summer, 0.5 autumn, 0.75 winter).
    public float Year { get; private set; } = Seasons.StartYear;
    public Season Season => Seasons.SeasonOf(Year);
    // How warm the season is, -1 deep winter to 1 high summer.
    public float SeasonWarmth => Seasons.Warmth(Year);
    // Snow lying on the ground and snow falling now, both 0 to 1.
    public float SnowCover => snowCover;
    public float Snowfall => snow;
    // Outdoor air temperature, degrees C: the server's AirTemperature, refreshed
    // every weather tick (20 s) and eased between ticks (e.g. visible breath, D-144).
    public float AirCelsius => air;

    void ReadySeasons()
    {
        AddChild(snowfall);
        argSeason = ArgFloat("season");
        argSnow = ArgFloat("snow");
        argSnowCover = ArgFloat("snow-cover");
    }

    void ConnectSeasons(DbConnection conn)
    {
        conn.Db.SeasonClock.OnInsert += (_, c) => OnSeasonClock(c);
        conn.Db.SeasonClock.OnUpdate += (_, _, c) => OnSeasonClock(c);
        conn.Db.SeasonState.OnInsert += (_, s) => OnSeasonState(s);
        conn.Db.SeasonState.OnUpdate += (_, _, s) => OnSeasonState(s);
    }

    void OnSeasonClock(SeasonClock c)
    {
        yearStartMicros = c.YearStart.MicrosecondsSinceUnixEpoch;
        daysPerSeason = c.DaysPerSeason;
        seasonSynced = true;
    }

    void OnSeasonState(SeasonState s)
    {
        targetSnow = s.Snow;
        targetSnowCover = s.SnowCover;
        targetAir = s.Temperature;
    }

    void UpdateSeasons(float dt, Vector3 focus, float windNow, Color air)
    {
        float year = argSeason ?? (seasonSynced
            ? Seasons.YearFraction(LocalMicros() + clockOffsetMicros, yearStartMicros, dayLength, daysPerSeason)
            : Year);
        // Glide rather than jump when an admin moves the year.
        float diff = year - Year;
        diff -= Mathf.Round(diff);
        Year = Mathf.Abs(diff) < 0.0005f ? year : Mathf.PosMod(Year + diff * (1f - Mathf.Exp(-dt / 2f)), 1f);

        snow = Mathf.Lerp(snow, argSnow ?? targetSnow, 1f - Mathf.Exp(-dt / 6f));
        snowCover = Mathf.Lerp(snowCover, argSnowCover ?? targetSnowCover, 1f - Mathf.Exp(-dt / 10f));
        if (targetAir is { } t) air = Mathf.Lerp(air, t, 1f - Mathf.Exp(-dt / 8f));

        RenderingServer.GlobalShaderParameterSet(NSeasonWarmth, Seasons.Warmth(Year));
        RenderingServer.GlobalShaderParameterSet(NSeasonAutumn, Seasons.Autumn(Year));
        RenderingServer.GlobalShaderParameterSet(NSnowCover, snowCover);
        RenderingServer.GlobalShaderParameterSet(NSnowing, snow);
        // Snow drifts down pale and slow, lit like the air around it.
        snowfall.Follow(focus, snow, windNow, air.Lerp(new Color(0.95f, 0.96f, 1f), 0.6f));
    }

    // Winter light: a paler, cooler sun and a slightly greyer world; fresh snow
    // throws light back up into the shadows.
    float WinterLight => Mathf.Max(0f, -Seasons.Warmth(Year));
}
