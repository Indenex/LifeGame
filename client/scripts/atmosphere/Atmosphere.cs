using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Day and night, weather and the sounds of the forest. The server owns the clock
// and the weather; this node turns them into sun, sky, fog, rain and ambience.
//
// Preview keys in debug builds (only change what you see and hear): hold F5 to
// fast-forward time, F6 to cycle rain / clear / the server's weather, M to mute or unmute music.
// Command line: --time-of-day=0.8 --rain=1 --wetness=1 --cloud=0.5 --wind=0.5 --smoke-signal=1
// --day-minutes=4 (a shorter day, for watching the whole cycle), and
// --debug-position=x,z to hold the player somewhere for screenshots.
public partial class Atmosphere : Node3D
{
    // Cached names: a string here would build a new StringName every frame.
    static readonly StringName NClouds = "clouds";
    static readonly StringName NWetness = "wetness";
    static readonly StringName NPuddles = "puddles";
    static readonly StringName NWindStrength = "wind_strength";
    static readonly StringName NWindGust = "wind_gust";
    static readonly StringName NRainAmount = "rain_amount";
    static readonly StringName NCloudCover = "cloud_cover";
    static readonly StringName NCloudOffset = "cloud_offset";
    static readonly StringName NPlayerPosition = "player_position";
    static readonly StringName NViewCameraPosition = "view_camera_position";
    static readonly StringName NTopColor = "top_color";
    static readonly StringName NHorizonColor = "horizon_color";
    static readonly StringName NGroundColor = "ground_color";
    static readonly StringName NSunColor = "sun_color";
    static readonly StringName NSunDir = "sun_dir";
    static readonly StringName NMoonDir = "moon_dir";
    static readonly StringName NDaylight = "daylight";
    static readonly StringName NStars = "stars";

    public Node3D? Player;

    DirectionalLight3D sun = null!;
    Godot.Environment env = null!;
    readonly ShaderMaterial sky = new() { Shader = SkyShader };
    readonly Rain rain = new();
    readonly Fireflies fireflies = new();
    readonly Mist mist = new();
    readonly FallingLeaves leaves = new();
    readonly SoundScape sound = new();
    // The soundscape, for settings such as the menu's music slider (Sound.MusicVolume).
    public SoundScape Sound => sound;

    // Report sheep here (position, running) and they bleat; see SheepVoices.
    public readonly SheepVoices Sheep = new();
    // The same for red deer: a bark when a herd bolts, hoof thuds, and stags roaring at autumn dusk (audio-4).
    public readonly DeerVoices Deer = new();
    public readonly HenVoices Hens = new();

    // The clock: day zero began at dayStart, and the server is clockOffset ahead of us.
    long dayStartMicros, clockOffsetMicros;
    int dayLength = WorldTime.DayLengthSeconds;
    bool synced;
    float timeOfDay = -1f, previewDays;
    float? fixedTime, dayMinutes;

    // What is shown eases toward what the server says.
    float cloud = 0.2f, rainAmount, wind = 0.3f, wetness;
    float targetCloud = 0.2f, targetRain, targetWind = 0.3f, targetWetness;
    float? argCloud, argRain, argWind, argWetness;
    // Standing water after rain, eased; --puddles=1 holds it for screenshots.
    float puddleLevel;
    float? argPuddles;
    int previewWeather;

    // F6 steps through these (cloud, rain, wind, wetness): the server's weather,
    // then each kind in shared/Weathers.cs at the middle of its range, the ground
    // as wet as that rain soon makes it.
    static readonly (float, float, float, float)[] PreviewWeathers =
        new[] { default((float, float, float, float)) }.Concat(Weathers.All.Select(k =>
        {
            float rain = (k.RainMin + k.RainMax) * 0.5f;
            return ((k.CloudMin + k.CloudMax) * 0.5f, rain, (k.WindMin + k.WindMax) * 0.5f, Mathf.Min(1f, rain * 1.6f));
        })).ToArray();

    // Lightning: a flash lights the sky and the land, and thunder follows.
    float flash, nextStrike = 20f;

    // Whether a spot is under a roof; Main points this at the building system.
    public Func<Vector3, bool>? Sheltered;

    readonly System.Collections.Generic.Dictionary<ulong, (float X, float Z, long FuelUntil)> fires = new();
    Vector2 cloudOffset;
    // Which way the wind blows (x, z), shared with shaders as wind_dir. Fixed for now;
    // the clouds drift with it.
    public static readonly Vector2 WindDirection = new(0.6f, 0.8f);
    static readonly StringName NWindDir = "wind_dir";
    static readonly StringName NFogTint = "fog_tint";

    // How wooded it is around the player, eased over a few seconds: where the sun
    // shafts between the trunks show.
    float woods;

    Vector2? debugPosition;

    public float TimeOfDay => timeOfDay;

    public override void _Ready()
    {
        var parent = GetParent();
        sun = parent.GetChildren().OfType<DirectionalLight3D>().First();
        env = parent.GetChildren().OfType<WorldEnvironment>().First().Environment;
        env.Sky.SkyMaterial = sky;
        env.Sky.ProcessMode = Sky.ProcessModeEnum.Incremental;
        env.Sky.RadianceSize = Sky.RadianceSizeEnum.Size128;
        sky.SetShaderParameter(NClouds, Look.Clouds);
        sun.ShadowEnabled = true;

        AddChild(rain);
        AddChild(fireflies);
        AddChild(mist);
        AddChild(leaves);
        ReadyWater();
        AddChild(sound);
        Sheep.Bleat = sound.Bleat;
        Deer.Cry = sound.DeerCry;
        ReadySeasons();
        ReadyFronts();
        ReadyGrade();
        ReadyFrost();
        ReadyImpacts();
        ReadyStorms();
        ReadyHomes();
        ReadySignals();

        // Previews are for testing, so release builds leave them out.
        if (OS.IsDebugBuild())
        {
            Bind("preview_time", Key.F5);
            Bind("preview_weather", Key.F6);
        }
        Bind("toggle_music", Key.M);

        fixedTime = ArgFloat("time-of-day");
        dayMinutes = ArgFloat("day-minutes");
        argCloud = ArgFloat("cloud");
        argRain = ArgFloat("rain");
        argWind = ArgFloat("wind");
        argWetness = ArgFloat("wetness");
        argPuddles = ArgFloat("puddles");
        if (Arg("debug-position")?.Split(',') is [var px, var pz]
            && float.TryParse(px, System.Globalization.CultureInfo.InvariantCulture, out var x)
            && float.TryParse(pz, System.Globalization.CultureInfo.InvariantCulture, out var z))
            debugPosition = new Vector2(x, z);
        // --debug-clear=seconds bares the ground around the player after that long,
        // as placing a building does, to check that it eases over.
        // --render-stats prints draw calls and objects once the world has loaded.
        if (Arg("render-stats") != null)
            GetTree().CreateTimer(9).Timeout += () => GD.Print(
                $"render-stats draw_calls={Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)} " +
                $"objects={Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame)} " +
                $"primitives={Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)} " +
                RenderBreakdown());
        if (ArgFloat("debug-clear") is { } clearAfter)
            GetTree().CreateTimer(clearAfter).Timeout += () =>
            {
                if (parent.GetChildren().OfType<Terrain>().FirstOrDefault() is { } terrain) DebugClear(terrain);
            };
        if (argRain is > 0f)
        {
            argCloud ??= 0.95f;
            argWind ??= 0.8f;
            argWetness ??= Math.Min(1f, argRain.Value * 1.2f);
        }

        // Until the server answers, run our own clock from late afternoon.
        dayStartMicros = LocalMicros() - (long)(dayLength * 0.62 * 1_000_000);
        Update(0f, true);
    }

    // Called once the connection is up.
    public void Connect(DbConnection conn)
    {
        conn.Db.WorldClock.OnInsert += (_, c) => OnClock(c);
        conn.Db.WorldClock.OnUpdate += (_, _, c) => OnClock(c);
        conn.Db.Weather.OnInsert += (_, w) => OnWeather(w);
        conn.Db.Weather.OnUpdate += (_, _, w) => OnWeather(w);
        ConnectSeasons(conn);
        ConnectImpacts(conn);
        ConnectHomes(conn);
        // Sheep bleat (only sheep: deer and other animals get their own voices);
        // Herd subscribes to the animal rows near the player.
        conn.Db.Animal.OnInsert += (_, a) => HearAnimal(a);
        conn.Db.Animal.OnUpdate += (_, was, a) =>
        {
            HearAnimal(a);
            if (a.Eggs > was.Eggs && AnimalDefs.Get((byte)a.Species).Model == "hen") Hens.Laid(a.X, a.Z);
        };
        conn.Db.Animal.OnDelete += (_, a) => { Sheep.Gone(a.Id); Deer.Gone(a.Id); Hens.Gone(a.Id); };
        // A blow landing on an animal: a thud, or a crack for a sneak-attack crit (audio-4).
        conn.Db.AnimalHit.OnInsert += (_, h) => HearHit(h);
        // A bow shot: the twang now, the arrow's thunk when it lands (audio-6).
        conn.Db.ArrowShot.OnInsert += (_, shot) => HearArrow(shot);
        // Drinking, eating, plucking and picking up, for everyone, timed to the motions (audio-7).
        conn.Db.PlayerAct.OnInsert += (_, act) => HearAct(conn, act);
        // Campfires crackle; FireView subscribes to the fires near the player.
        conn.Db.Fire.OnInsert += (_, f) => fires[f.Id] = (f.X, f.Z, f.FuelUntil.MicrosecondsSinceUnixEpoch);
        conn.Db.Fire.OnUpdate += (_, _, f) => fires[f.Id] = (f.X, f.Z, f.FuelUntil.MicrosecondsSinceUnixEpoch);
        conn.Db.Fire.OnDelete += (_, f) => fires.Remove(f.Id);
        HearFireCaps(conn);
        conn.SubscriptionBuilder()
            .OnError((_, e) => GD.PushWarning($"Weather subscription: {e.Message}"))
            .Subscribe(Subscriptions.Atmosphere);
        KeyHelp.Extra = () => TimeCommands.HelpFor(conn);
        WorkSounds.Connect(conn, sound);
        ToolSounds.Connect(conn);
    }

    void OnClock(WorldClock c)
    {
        dayStartMicros = c.DayStart.MicrosecondsSinceUnixEpoch;
        dayLength = (int)c.DayLengthSeconds;
        // ServerNow is refreshed every few seconds, so a reading can be stale but
        // never early: the largest offset seen is the closest to the truth.
        long sample = c.ServerNow.MicrosecondsSinceUnixEpoch - LocalMicros();
        if (!synced || sample > clockOffsetMicros || Math.Abs(sample - clockOffsetMicros) > 300_000_000L)
            clockOffsetMicros = sample;
        synced = true;
    }

    void OnWeather(Weather w)
    {
        targetCloud = w.Cloud;
        targetRain = w.Rain;
        targetWind = w.Wind;
        targetWetness = w.Wetness;
        OnWeatherAhead(w);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (InputMap.HasAction("preview_weather") && e.IsActionPressed("preview_weather")) previewWeather = (previewWeather + 1) % PreviewWeathers.Length;
        if (e.IsActionPressed("toggle_music")) sound.MusicOn = !sound.MusicOn;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        Look.Tick(dt);
        // A whole day in about 45 seconds while T is held.
        if (InputMap.HasAction("preview_time") && Input.IsActionPressed("preview_time")) previewDays += dt / 45f;
        if (debugPosition is { } d && Player != null)
            Player.Position = new Vector3(d.X, Player.Position.Y, d.Y);
        Update(dt, false);
        Footsteps();
    }

    // --- Time and weather -----------------------------------------------------

    void Update(float dt, bool snap)
    {
        int length = dayMinutes is { } m ? Math.Max(1, (int)(m * 60)) : dayLength;
        float target = fixedTime ?? WorldTime.TimeOfDay(LocalMicros() + clockOffsetMicros, dayStartMicros, length);
        target = Frac(target + previewDays);
        if (snap || timeOfDay < 0f) timeOfDay = target;
        else
        {
            // Glide rather than jump when the server clock first arrives.
            float diff = Frac(target - timeOfDay + 0.5f) - 0.5f;
            timeOfDay = Mathf.Abs(diff) < 0.0005f ? target : Frac(timeOfDay + diff * (1f - Mathf.Exp(-3f * dt)));
        }

        (float c, float r, float w, float wet) = previewWeather > 0
            ? PreviewWeathers[previewWeather]
            : (argCloud ?? targetCloud, argRain ?? targetRain, argWind ?? targetWind, argWetness ?? targetWetness);
        float k = snap ? 1f : 1f - Mathf.Exp(-dt / 6f);
        cloud = Mathf.Lerp(cloud, c, k);
        rainAmount = Mathf.Lerp(rainAmount, r, k);
        wind = Mathf.Lerp(wind, w, k);
        wetness = Mathf.Lerp(wetness, wet, snap ? 1f : 1f - Mathf.Exp(-dt / 10f));

        // Clouds drift with the wind, the same way the grass leans.
        cloudOffset -= WindDirection * (1.5f + wind * 5f) * dt;
        RenderingServer.GlobalShaderParameterSet(NWetness, wetness);
        // Puddles stand once the ground is well soaked, and frozen ground holds none.
        float puddleTarget = Mathf.SmoothStep(0.45f, 0.9f, wetness) * (1f - Mathf.SmoothStep(0.3f, 0.8f, SnowCover));
        puddleLevel = Mathf.Lerp(puddleLevel, argPuddles ?? puddleTarget, snap ? 1f : 1f - Mathf.Exp(-dt / 20f));
        RenderingServer.GlobalShaderParameterSet(NPuddles, puddleLevel);
        RenderingServer.GlobalShaderParameterSet(NWindStrength, wind);
        // Grass and trees push harder exactly when the wind is heard to gust.
        RenderingServer.GlobalShaderParameterSet(NWindGust, sound.Gust);
        RenderingServer.GlobalShaderParameterSet(NRainAmount, rainAmount);
        RenderingServer.GlobalShaderParameterSet(NCloudCover, cloud);
        RenderingServer.GlobalShaderParameterSet(NCloudOffset, cloudOffset);
        RenderingServer.GlobalShaderParameterSet(NWindDir, WindDirection);

        ApplyLight();
        RenderingServer.GlobalShaderParameterSet(NFogTint, env.FogLightColor);

        var focus = Player is { } p && p.IsInsideTree() ? p.GlobalPosition : Vector3.Zero;
        RenderingServer.GlobalShaderParameterSet(NPlayerPosition, focus);
        if (GetViewport()?.GetCamera3D() is { } camera)
        {
            RenderingServer.GlobalShaderParameterSet(NViewCameraPosition, camera.GlobalPosition);
            terrain ??= GetParent()?.GetChildren().OfType<Terrain>().FirstOrDefault();
            terrain?.ShowGhosts(camera.GlobalPosition, focus);
            Sheep.Tick(dt, focus, camera.GlobalBasis.X);
            Deer.RoarTime = StagRoarTime();
            Deer.Tick(dt, focus, camera.GlobalBasis.X);
            Hens.NightTime = Mathf.SmoothStep(0.7f, 0.85f, timeOfDay < 0f ? 0.5f : timeOfDay) + Mathf.SmoothStep(0.3f, 0.2f, timeOfDay < 0f ? 0.5f : timeOfDay);
            Hens.Tick(dt);
        }
        float daylight = WorldTime.Daylight(timeOfDay);
        UpdateSeasons(dt, focus, wind, env.FogLightColor * (0.3f + 0.7f * WorldTime.Daylight(timeOfDay)));
        rain.Follow(focus, rainAmount, wind, env.FogLightColor.Lerp(new Color(0.8f, 0.82f, 0.85f) * (0.3f + 0.7f * daylight), 0.4f));
        // Dawn mist glows with the same light as the fog: warm at sunrise.
        float sunLow = Mathf.SmoothStep(-0.02f, 0.08f, WorldTime.SunHeight(timeOfDay)) * (1f - Mathf.SmoothStep(0.25f, 0.5f, WorldTime.SunHeight(timeOfDay)));
        var towardSun = WorldTime.SunHeight(timeOfDay) > -0.06f ? sun.GlobalBasis.Z : -sun.GlobalBasis.Z;
        mist.Follow(focus, Mist.Amount(timeOfDay, wind, rainAmount, wetness), env.FogLightColor.Lerp(sun.LightColor, 0.3f).Lerp(Colors.White, 0.2f),
            towardSun, sun.LightColor * sunLow * (1f - Mathf.Clamp(cloud * 1.2f - 0.2f, 0f, 1f)), wind, dt);
        fireflies.Follow(focus, Mathf.SmoothStep(0.85f, 1f, 1f - daylight) * (1f - Mathf.Clamp(rainAmount * 3f, 0f, 1f)));
        Night = 1f - daylight;
        UpdateHomes(dt, focus, Night, env.FogLightColor);
        UpdateSignals(dt, focus, Night);
        sound.Daylight = daylight;
        sound.Rain = rainAmount;
        sound.Wind = wind;
        sound.Wetness = wetness;
        sound.Cover = Sheltered?.Invoke(focus) == true ? 1f : 0f;
        sound.TimeOfDay = timeOfDay;
        float wooded = Woodedness(focus);
        woods += (wooded - woods) * Mathf.Min(1f, dt / 3f);
        leaves.Follow(focus, Year, wind, woods, WindDirection);
        UpdateWater(dt, focus, env.FogLightColor.Lerp(sun.LightColor, 0.4f));
        UpdateFrost(dt);
        UpdateImpacts(dt);
        sound.Foliage = wooded;
        Lightning(dt, focus);
        if (GetViewport()?.GetCamera3D() is { } ear)
        {
            Sfx.Listen(focus, ear.GlobalBasis.X);
            FireSound(focus, ear.GlobalBasis.X);
            WaterSound(dt, focus, ear.GlobalBasis.X);
            WorkSounds.Place(dt, focus, ear.GlobalBasis.X);
            ToolSounds.Tick(dt);
        }
    }

    // --- Sun, sky and fog -----------------------------------------------------

    // How the world looks at points through the day, before weather. Times are
    // fractions of a day; the late-afternoon key is the look the game started with.
    readonly record struct Mood(
        float At, Color Top, Color Horizon, Color Ground, Color Fog, Color Mist, Color Ambient,
        float FogDensity, float MistDensity, float AmbientEnergy, float Exposure);

    static readonly Mood[] Moods =
    {
        new(0.00f, new(0.025f, 0.04f, 0.09f), new(0.07f, 0.10f, 0.18f), new(0.03f, 0.04f, 0.05f), new(0.10f, 0.13f, 0.22f), new(0.30f, 0.38f, 0.58f), new(0.18f, 0.24f, 0.40f), 0.007f, 0.006f, 0.5f, 1.35f),
        new(0.21f, new(0.07f, 0.09f, 0.20f), new(0.30f, 0.24f, 0.32f), new(0.06f, 0.06f, 0.07f), new(0.26f, 0.24f, 0.32f), new(0.55f, 0.50f, 0.60f), new(0.20f, 0.22f, 0.32f), 0.009f, 0.010f, 0.55f, 1.3f),
        new(0.26f, new(0.28f, 0.40f, 0.60f), new(0.88f, 0.74f, 0.68f), new(0.20f, 0.21f, 0.20f), new(0.58f, 0.61f, 0.67f), new(0.80f, 0.83f, 0.88f), new(0.15f, 0.17f, 0.21f), 0.007f, 0.006f, 0.48f, 1.08f),
        new(0.32f, new(0.30f, 0.47f, 0.68f), new(0.86f, 0.80f, 0.70f), new(0.24f, 0.28f, 0.22f), new(0.66f, 0.70f, 0.72f), new(0.90f, 0.90f, 0.88f), new(0.08f, 0.09f, 0.08f), 0.007f, 0.009f, 0.4f, 1.0f),
        new(0.50f, new(0.27f, 0.46f, 0.72f), new(0.76f, 0.82f, 0.86f), new(0.24f, 0.28f, 0.22f), new(0.64f, 0.71f, 0.77f), new(0.88f, 0.90f, 0.92f), new(0.06f, 0.07f, 0.07f), 0.005f, 0.005f, 0.4f, 0.95f),
        new(0.68f, new(0.30f, 0.45f, 0.62f), new(0.80f, 0.74f, 0.62f), new(0.24f, 0.28f, 0.22f), new(0.62f, 0.68f, 0.70f), new(0.86f, 0.88f, 0.86f), new(0.0f, 0.0f, 0.0f), 0.006f, 0.007f, 0.38f, 1.0f),
        new(0.745f, new(0.26f, 0.32f, 0.52f), new(0.92f, 0.64f, 0.46f), new(0.20f, 0.19f, 0.17f), new(0.66f, 0.58f, 0.54f), new(0.88f, 0.80f, 0.74f), new(0.12f, 0.11f, 0.14f), 0.007f, 0.008f, 0.42f, 1.05f),
        new(0.79f, new(0.07f, 0.09f, 0.21f), new(0.34f, 0.22f, 0.32f), new(0.05f, 0.05f, 0.07f), new(0.22f, 0.20f, 0.30f), new(0.50f, 0.44f, 0.58f), new(0.18f, 0.20f, 0.32f), 0.008f, 0.008f, 0.55f, 1.3f),
        new(1.00f, new(0.025f, 0.04f, 0.09f), new(0.07f, 0.10f, 0.18f), new(0.03f, 0.04f, 0.05f), new(0.10f, 0.13f, 0.22f), new(0.30f, 0.38f, 0.58f), new(0.18f, 0.24f, 0.40f), 0.007f, 0.006f, 0.5f, 1.35f),
    };

    static Mood MoodAt(float t)
    {
        int i = 0;
        while (i < Moods.Length - 2 && t > Moods[i + 1].At) i++;
        Mood a = Moods[i], b = Moods[i + 1];
        float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - a.At) / (b.At - a.At), 0f, 1f));
        return new Mood(t, a.Top.Lerp(b.Top, f), a.Horizon.Lerp(b.Horizon, f), a.Ground.Lerp(b.Ground, f),
            a.Fog.Lerp(b.Fog, f), a.Mist.Lerp(b.Mist, f), a.Ambient.Lerp(b.Ambient, f),
            Mathf.Lerp(a.FogDensity, b.FogDensity, f), Mathf.Lerp(a.MistDensity, b.MistDensity, f),
            Mathf.Lerp(a.AmbientEnergy, b.AmbientEnergy, f), Mathf.Lerp(a.Exposure, b.Exposure, f));
    }

    static Color Grey(Color c, float amount, float brightness)
    {
        float l = (c.R * 0.3f + c.G * 0.59f + c.B * 0.11f) * brightness;
        return c.Lerp(new Color(l, l, l * 1.04f), amount);
    }

    void ApplyLight()
    {
        float t = timeOfDay;
        var mood = MoodAt(t);
        float sunHeight = WorldTime.SunHeight(t);
        float daylight = WorldTime.Daylight(t);
        float overcast = Mathf.Clamp(cloud * 1.1f - 0.1f, 0f, 1f);
        // How much the rain closes the world in: a little in light rain, a lot only
        // in a storm.
        float storm = 0.3f * rainAmount + 0.7f * Mathf.SmoothStep(0.6f, 1f, rainAmount);

        // The sun crosses the sky once a day; the moon follows on the opposite side.
        // The light swaps between them while both are below the horizon and dark.
        bool isDay = sunHeight > -0.06f;
        float sunUp = Mathf.SmoothStep(-0.06f, 0.1f, sunHeight);
        float moonUp = Mathf.SmoothStep(0.06f, 0.25f, -sunHeight);
        float azimuth = -40f + (t - 0.68f) * 360f;
        // The moon rides high enough as soon as it is up that early night isn't
        // lit only by grazing light.
        float moonElevation = 16f + Mathf.Max(-sunHeight, 0f) * 32f;
        Vector3 sunDir = Toward(sunHeight * 55f, azimuth), moonDir = Toward(moonElevation, azimuth + 180f);
        float elevation = Mathf.Max(isDay ? sunHeight * 55f : moonElevation, 3f);
        sun.Rotation = new Vector3(Mathf.DegToRad(-elevation), Mathf.DegToRad(isDay ? azimuth : azimuth + 180f), 0);

        Color low = new(1.0f, 0.64f, 0.38f), mid = new(1.0f, 0.86f, 0.66f), high = new(1.0f, 0.95f, 0.87f);
        float h = Mathf.Clamp(sunHeight, 0f, 1f);
        Color sunColor = h < 0.4f ? low.Lerp(mid, Mathf.SmoothStep(0f, 0.4f, h)) : mid.Lerp(high, Mathf.SmoothStep(0.4f, 1f, h));
        Color moonColor = new(0.56f, 0.66f, 1.0f);
        sun.LightColor = Grey(isDay ? sunColor.Lerp(new Color(0.9f, 0.94f, 1f), 0.45f * WinterLight) : moonColor, overcast * 0.6f, 1f);
        sun.LightEnergy = isDay ? 2.4f * sunUp * (1f - 0.72f * overcast) : 0.55f * moonUp * (1f - 0.7f * overcast);
        sun.LightVolumetricFogEnergy = isDay ? (0.6f + 1.0f * Mathf.SmoothStep(0.05f, 0.35f, sunHeight)) * (1f - 0.5f * overcast) : 1.2f;
        sun.ShadowOpacity = 1f - 0.6f * overcast;
        sun.LightEnergy *= 1f - 0.5f * storminess;

        // Sun shafts in the woods: at a low sun on a fair day the air between the
        // trunks thickens a little and scatters forward, so the light slants through
        // the canopy in beams. Gone at midday, under cloud and in rain.
        float lowSun = Mathf.SmoothStep(0.02f, 0.12f, sunHeight) * (1f - Mathf.SmoothStep(0.3f, 0.55f, sunHeight));
        float fair = (1f - overcast) * (1f - overcast) * (1f - Mathf.Clamp(rainAmount * 2f, 0f, 1f));
        float shafts = woods * lowSun * fair;
        sun.LightVolumetricFogEnergy += 1.4f * shafts;

        // Overcast skies go grey and flat; rain darkens and thickens the air.
        float grey = overcast * 0.75f;
        // A thunderstorm goes darker still, toward a cold slate.
        float dim = (1f - 0.35f * storm) * (1f - 0.3f * storminess);
        Color top = Grey(mood.Top, grey, 1.05f) * dim, horizon = Grey(mood.Horizon, grey, 1.0f) * dim;
        sky.SetShaderParameter(NTopColor, top);
        sky.SetShaderParameter(NHorizonColor, horizon);
        sky.SetShaderParameter(NGroundColor, Grey(mood.Ground, grey, 1f) * dim);
        sky.SetShaderParameter(NSunColor, sunColor * (1f - overcast * 0.8f));
        sky.SetShaderParameter(NSunDir, sunDir);
        sky.SetShaderParameter(NMoonDir, moonDir);
        sky.SetShaderParameter(NDaylight, daylight);
        sky.SetShaderParameter(NCloudCover, cloud);
        sky.SetShaderParameter(NCloudOffset, cloudOffset);
        sky.SetShaderParameter(NStars, (1f - daylight) * (1f - overcast));
        UpdateFronts((float)GetProcessDeltaTime(), sunHeight, overcast);

        env.FogLightColor = Grey(mood.Fog, grey, 0.95f) * dim;
        env.FogDensity = mood.FogDensity * (1f + 2.4f * storm + 0.3f * overcast);
        env.VolumetricFogAlbedo = Grey(mood.Mist, grey, 1f) * (1f - 0.25f * storm);
        env.VolumetricFogDensity = mood.MistDensity * (1f + 2.2f * storm + 1.6f * shafts);
        env.VolumetricFogAnisotropy = 0.6f + 0.25f * shafts;
        env.AmbientLightColor = mood.Ambient;
        env.AmbientLightEnergy = mood.AmbientEnergy * (1f + 0.35f * overcast);
        env.TonemapExposure = mood.Exposure * (1f + 0.12f * overcast);
        env.AdjustmentSaturation = 1f - 0.2f * overcast - 0.1f * (1f - daylight) - 0.12f * WinterLight;
        env.AmbientLightEnergy *= 1f + 0.25f * snowCover;
        env.AmbientLightEnergy *= 1f - 0.3f * storminess;
        env.FogLightColor = env.FogLightColor.Lerp(new Color(0.3f, 0.34f, 0.38f) * dim, 0.35f * storminess);
        env.GlowIntensity = 0.6f + 0.4f * (1f - daylight);

        Grade(sunHeight, daylight, overcast);

        if (flash > 0.001f)
        {
            var bolt = new Color(0.75f, 0.8f, 1f);
            sky.SetShaderParameter(NTopColor, top.Lerp(bolt, flash * 0.6f));
            sky.SetShaderParameter(NHorizonColor, horizon.Lerp(bolt, flash * 0.75f));
            env.AmbientLightEnergy += flash * 1.6f;
            env.FogLightColor = env.FogLightColor.Lerp(bolt, flash * 0.5f);
        }
    }

    // The campfires you can hear: louder the closer and the better they burn,
    // panned toward the nearest. Matches FireView's fade to embers.
    void FireSound(Vector3 listener, Vector3 right)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        float total = 0f, nearest = 0f, pan = 0f, stoke = 0f;
        foreach (var (id, (x, z, fuelUntil)) in fires)
        {
            float left = (fuelUntil - now) / 1_000_000f;
            if (left <= 0f) continue;
            var offset = new Vector3(x - listener.X, 0f, z - listener.Z);
            float d = offset.Length();
            if (d > 18f) continue;
            float g = Mathf.Clamp(left / 60f, 0.15f, 1f) * Mathf.Pow(1f - d / 18f, 2f);
            total += g;
            if (g > nearest)
            {
                nearest = g;
                // Stoked: fuel left against what a fire holds at most (a well-fed fire roars).
                stoke = Stoke(id, left);
                pan = d < 0.5f ? 0f : offset.Normalized().Dot(new Vector3(right.X, 0f, right.Z).Normalized());
            }
        }
        sound.Fire = Mathf.Min(total, 1f);
        sound.FirePan = pan;
        sound.FireStoke = stoke;
    }

    // How much of night it is, 0 full day to 1 full night, for other lanes that
    // change with the dark (e.g. campfire light reaching about 14 m at night instead
    // of 10 m by day, Justin's pick). Updated every frame.
    public static float Night { get; private set; }

    // How bright a campfire burns from moment to moment: a slow breathing as the
    // flames rise and settle, quicker licks over it, and now and then a dip as a
    // log shifts. Around 1 on average. FireView drives its light with this.
    public static float FireFlicker(float time)
    {
        float breathe = 0.08f * Mathf.Sin(time * 1.7f) + 0.05f * Mathf.Sin(time * 2.9f + 0.7f);
        float licks = 0.06f * Mathf.Sin(time * 11.3f) + 0.05f * Mathf.Sin(time * 17.9f + 1.3f) + 0.03f * Mathf.Sin(time * 29.1f + 2.1f);
        float shift = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 0.37f + 1.1f) * Mathf.Sin(time * 0.53f)), 6f) * 0.25f;
        return 0.92f + breathe + licks - shift;
    }

    void DebugClear(Terrain terrain)
    {
        if (Player == null) return;
        int cx = Mathf.RoundToInt(Player.GlobalPosition.X), cz = Mathf.RoundToInt(Player.GlobalPosition.Z);
        int r = Mathf.RoundToInt(ArgFloat("debug-clear-radius") ?? 3f);
        var before = terrain.Cleared;
        terrain.Cleared = (x, z) => before?.Invoke(x, z) == true || (x - cx) * (x - cx) + (z - cz) * (z - cz) <= r * r;
        for (int x = cx - r; x <= cx + r; x++)
            for (int z = cz - r; z <= cz + r; z++)
                terrain.Refresh(x, z);
    }

    // A unit vector pointing at something this many degrees above the horizon,
    // turned the same way a light with this yaw would be.
    static Vector3 Toward(float elevationDeg, float azimuthDeg) =>
        Basis.FromEuler(new Vector3(Mathf.DegToRad(-elevationDeg), Mathf.DegToRad(azimuthDeg), 0)).Z;

    Terrain? terrain;

    // How wooded it is around a point, 0 open to 1 deep woods, from the same forest
    // density the terrain plants its trees by (Forest.Density, env-1). Sampled every few metres out to
    // about 12 m; the mixer eases between values, so walking into the trees fades
    // the leaves in.
    static float Woodedness(Vector3 at)
    {
        int cx = World.CellOf(at.X), cz = World.CellOf(at.Z);
        float sum = 0f;
        int count = 0;
        for (int dx = -12; dx <= 12; dx += 4)
            for (int dz = -12; dz <= 12; dz += 4)
            {
                int x = cx + dx, z = cz + dz;
                count++;
                sum += Forest.Density(x, z);
            }
        return Mathf.SmoothStep(0.1f, 0.6f, sum / count);
    }

    // --- Helpers ----------------------------------------------------------------

    static long LocalMicros() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;

    static float Frac(float x) => x - Mathf.Floor(x);

    static void Bind(string action, Key key)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    // Visible meshes, lights and particles grouped by which top-level node they sit
    // under, for finding where draw calls come from.
    string RenderBreakdown()
    {
        var counts = new System.Collections.Generic.Dictionary<string, int>();
        if (GetParent() is not { } main) return "";
        foreach (var top in main.GetChildren())
        {
            int n = 0;
            var stack = new System.Collections.Generic.Stack<Node>();
            stack.Push(top);
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node is VisualInstance3D v && v.IsVisibleInTree()) n++;
                foreach (var c in node.GetChildren()) stack.Push(c);
            }
            if (n > 0) { string key = top.Name + ":" + top.GetType().Name; counts[key] = (counts.TryGetValue(key, out int had) ? had : 0) + n; }
        }
        return string.Join(" ", counts.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}"));
    }

    static string? Arg(string name)
    {
        foreach (var arg in OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()))
            if (arg.StartsWith($"--{name}="))
                return arg[(name.Length + 3)..];
        return null;
    }

    static float? ArgFloat(string name) =>
        float.TryParse(Arg(name), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    static readonly Shader SkyShader = new()
    {
        Code = """
            shader_type sky;

            uniform vec3 top_color : source_color;
            uniform vec3 horizon_color : source_color;
            uniform vec3 ground_color : source_color;
            uniform vec3 sun_color : source_color;
            uniform vec3 sun_dir = vec3(0.0, 1.0, 0.0);
            uniform vec3 moon_dir = vec3(0.0, -1.0, 0.0);
            uniform float daylight = 1.0;
            uniform float cloud_cover = 0.2;
            uniform vec2 cloud_offset;
            uniform float stars = 0.0;
            uniform sampler2D clouds : repeat_enable, filter_linear_mipmap;
            // Weather on its way (AtmosphereFronts): where it comes from, how cloudy
            // and wet it will be, and how strongly it shows yet; and a rainbow.
            uniform vec3 front_dir = vec3(-0.6, 0.0, -0.8);
            uniform float front_cloud = 0.2;
            uniform float front_rain = 0.0;
            uniform float front_amount = 0.0;
            uniform float rainbow = 0.0;
            // The prototype colour grade (AtmosphereGrade), 0 off.
            uniform float grade = 0.0;
            // Thunderstorms (AtmosphereStorms): how stormy, and lightning inside
            // the clouds toward flash_dir.
            uniform float storm = 0.0;
            uniform float flash = 0.0;
            uniform vec3 flash_dir = vec3(0.0, 0.25, -1.0);

            float hash(vec3 p) {
                p = fract(p * 0.3183099 + 0.1);
                p *= 17.0;
                return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            void sky() {
                vec3 d = normalize(EYEDIR);
                float up = clamp(d.y, 0.0, 1.0);
                vec3 col = mix(horizon_color, top_color, pow(up, 0.45));
                col = mix(col, ground_color, 1.0 - smoothstep(-0.25, 0.0, d.y));

                // A soft glow around the sun, and its disc.
                float s = max(dot(d, sun_dir), 0.0);
                float sun_vis = smoothstep(-0.15, 0.05, sun_dir.y);
                col += sun_color * (pow(s, 5.0) * 0.3 + pow(s, 60.0) * 0.5) * sun_vis;
                col += sun_color * smoothstep(0.9993, 0.9997, s) * 6.0 * step(-0.02, sun_dir.y);

                float night = 1.0 - daylight;
                float m = max(dot(d, moon_dir), 0.0);
                col += vec3(0.85, 0.9, 1.0) * smoothstep(0.99955, 0.9998, m) * 2.0 * night;
                col += vec3(0.25, 0.3, 0.45) * pow(m, 24.0) * 0.3 * night;
                // A pale ring around the moon when thin cloud veils it.
                float veil = smoothstep(0.25, 0.5, cloud_cover) * (1.0 - smoothstep(0.75, 0.95, cloud_cover));
                col += vec3(0.75, 0.8, 0.9) * exp(-pow((m - 0.927) / 0.008, 2.0)) * veil * night * 0.08 * step(0.0, moon_dir.y);

                // How much of this direction belongs to the coming weather: the
                // upwind side, low in the sky.
                vec2 flat_dir = normalize(d.xz + vec2(0.0001));
                float upwind = smoothstep(0.1, 0.85, dot(flat_dir, normalize(front_dir.xz))) * (1.0 - smoothstep(0.05, 0.55, d.y)) * front_amount;

                if (d.y > 0.0) {
                    if (stars > 0.0) {
                        // A band of the galaxy arching across a clear night: faint,
                        // mottled, and thick with extra stars.
                        vec3 axis = normalize(vec3(0.35, 0.45, 0.82));
                        float band = exp(-pow(dot(d, axis) / 0.2, 2.0));
                        float dust = texture(clouds, d.xz / (d.y + 0.4) * 0.35 + vec2(0.13, 0.71)).r;
                        float high = smoothstep(0.0, 0.3, d.y);
                        col += vec3(0.50, 0.55, 0.72) * band * smoothstep(0.3, 0.8, dust) * 0.09 * stars * high;

                        vec3 p = d * 260.0;
                        float r = hash(floor(p));
                        float threshold = 0.985 - 0.01 * band;
                        float star = step(threshold, r) * smoothstep(0.35, 0.0, length(fract(p) - 0.5)) * (r - threshold) * 66.0;
                        // Each star twinkles at its own pace and has its own tint,
                        // from pale blue to warm white.
                        float twinkle = 0.75 + 0.25 * sin(TIME * (1.5 + r * 3.0) + r * 90.0);
                        vec3 tint = mix(vec3(0.80, 0.88, 1.0), vec3(1.0, 0.92, 0.80), fract(r * 37.0));
                        col += tint * star * twinkle * stars * high * 1.4;
                    }
                    // The same cloud layer whose shadows drift over the ground.
                    vec2 uv = (d.xz / (d.y + 0.03) * 120.0 + cloud_offset) * 0.01;
                    float n = texture(clouds, uv).r;
                    float cover = mix(cloud_cover, max(cloud_cover, front_cloud), upwind);
                    float edge = 1.0 - cover;
                    float c = smoothstep(edge - 0.08, edge + 0.22, n) * smoothstep(0.0, 0.18, d.y);
                    float lit = 0.25 + 0.75 * daylight;
                    vec3 cloud_col = mix(horizon_color * 1.1, vec3(0.92, 0.92, 0.95) * lit + sun_color * 0.15 * sun_vis, 0.5 + 0.5 * n);
                    cloud_col *= 1.0 - 0.45 * cover * smoothstep(0.4, 0.8, n);
                    // Rain clouds on the way are darker underneath.
                    cloud_col *= 1.0 - 0.5 * front_rain * upwind;
                    // Storm clouds hang heavy and dark, bruised blue-grey underneath.
                    cloud_col = mix(cloud_col, cloud_col * vec3(0.55, 0.6, 0.68), storm * smoothstep(0.3, 0.7, n));
                    // With the grade on, clouds near a low sun glow with warm rims.
                    float sun_low = smoothstep(-0.05, 0.05, sun_dir.y) * (1.0 - smoothstep(0.2, 0.45, sun_dir.y));
                    cloud_col += sun_color * pow(s, 4.0) * (1.0 - smoothstep(0.35, 0.8, n)) * 1.4 * sun_low * grade;
                    col = mix(col, cloud_col, c * 0.9);

                    // Lightning lights the clouds from inside, brightest toward the strike.
                    float toward = pow(max(dot(d, flash_dir), 0.0), 5.0);
                    col += vec3(0.72, 0.78, 1.0) * flash * toward * (0.35 + 1.4 * c * n);

                    // Grey curtains of rain hanging from them down to the horizon.
                    float around = atan(d.x, d.z);
                    float streak = texture(clouds, vec2(around * 2.0 + cloud_offset.x * 0.002, d.y * 0.6 + TIME * 0.004)).r;
                    float curtain = front_rain * upwind * (1.0 - smoothstep(0.0, 0.16, d.y)) * smoothstep(0.35, 0.75, streak);
                    col = mix(col, horizon_color * mix(0.55, 0.8, daylight), curtain * 0.7);

                    // A rainbow 42 degrees from the point opposite the sun, red outside.
                    if (rainbow > 0.001) {
                        float r = dot(d, -sun_dir);
                        float t = (r - 0.743) / 0.022;
                        float band = exp(-t * t);
                        vec3 hue = clamp(vec3(1.0 - abs(t + 0.7) * 1.4, 1.0 - abs(t) * 1.4, 1.0 - abs(t - 0.7) * 1.4), 0.0, 1.0);
                        col += hue * band * rainbow * 0.22 * smoothstep(0.0, 0.08, d.y) * (1.0 - c * 0.7);
                    }
                }
                COLOR = col;
            }
            """,
    };
}
