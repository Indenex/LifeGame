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
// Command line: --time-of-day=0.8 --rain=1 --wetness=1 --cloud=0.5 --wind=0.5
// --day-minutes=4 (a shorter day, for watching the whole cycle), and
// --debug-position=x,z to hold the player somewhere for screenshots.
public partial class Atmosphere : Node3D
{
    // Cached names: a string here would build a new StringName every frame.
    static readonly StringName NClouds = "clouds";
    static readonly StringName NWetness = "wetness";
    static readonly StringName NWindStrength = "wind_strength";
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
    readonly SoundScape sound = new();
    // The soundscape, for settings such as the menu's music slider (Sound.MusicVolume).
    public SoundScape Sound => sound;

    // Report sheep here (position, running) and they bleat; see SheepVoices.
    public readonly SheepVoices Sheep = new();

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
    int previewWeather;

    // F6 steps through these (cloud, rain, wind, wetness): the server's weather,
    // then clear, drizzle, light rain, steady rain and a storm.
    static readonly (float, float, float, float)[] PreviewWeathers =
    {
        default, (0.1f, 0f, 0.25f, 0f), (0.8f, 0.18f, 0.3f, 0.4f), (0.9f, 0.4f, 0.45f, 0.7f), (0.95f, 0.65f, 0.6f, 1f), (1f, 1f, 0.9f, 1f),
    };

    // Lightning: a flash lights the sky and the land, and thunder follows.
    float flash, nextStrike = 20f;

    // Whether a spot is under a roof; Main points this at the building system.
    public Func<Vector3, bool>? Sheltered;

    readonly System.Collections.Generic.Dictionary<ulong, (float X, float Z, long FuelUntil)> fires = new();
    Vector2 cloudOffset;

    Vector2? debugPosition;
    float stepPhase;
    Vector3 lastPlayerPos;

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
        AddChild(sound);
        Sheep.Bleat = sound.Bleat;

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
        // Sheep bleat; Herd subscribes to the animal rows near the player.
        conn.Db.Animal.OnInsert += (_, a) => Sheep.Seen(a.Id, a.X, a.Z, a.State == AnimalState.Run, a.Growth < 100);
        conn.Db.Animal.OnUpdate += (_, _, a) => Sheep.Seen(a.Id, a.X, a.Z, a.State == AnimalState.Run, a.Growth < 100);
        conn.Db.Animal.OnDelete += (_, a) => Sheep.Gone(a.Id);
        // Campfires crackle; FireView subscribes to the fires near the player.
        conn.Db.Fire.OnInsert += (_, f) => fires[f.Id] = (f.X, f.Z, f.FuelUntil.MicrosecondsSinceUnixEpoch);
        conn.Db.Fire.OnUpdate += (_, _, f) => fires[f.Id] = (f.X, f.Z, f.FuelUntil.MicrosecondsSinceUnixEpoch);
        conn.Db.Fire.OnDelete += (_, f) => fires.Remove(f.Id);
        conn.SubscriptionBuilder()
            .OnError((_, e) => GD.PushWarning($"Weather subscription: {e.Message}"))
            .Subscribe(new[] { "SELECT * FROM world_clock", "SELECT * FROM weather", "SELECT * FROM world_admin" });
        KeyHelp.Extra = () => TimeCommands.HelpFor(conn);
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
        cloudOffset -= new Vector2(0.6f, 0.8f) * (1.5f + wind * 5f) * dt;
        RenderingServer.GlobalShaderParameterSet(NWetness, wetness);
        RenderingServer.GlobalShaderParameterSet(NWindStrength, wind);
        RenderingServer.GlobalShaderParameterSet(NCloudCover, cloud);
        RenderingServer.GlobalShaderParameterSet(NCloudOffset, cloudOffset);

        ApplyLight();

        var focus = Player is { } p && p.IsInsideTree() ? p.GlobalPosition : Vector3.Zero;
        RenderingServer.GlobalShaderParameterSet(NPlayerPosition, focus);
        if (GetViewport()?.GetCamera3D() is { } camera)
        {
            RenderingServer.GlobalShaderParameterSet(NViewCameraPosition, camera.GlobalPosition);
            terrain ??= GetParent()?.GetChildren().OfType<Terrain>().FirstOrDefault();
            terrain?.ShowGhosts(camera.GlobalPosition, focus);
            Sheep.Tick(dt, focus, camera.GlobalBasis.X);
        }
        float daylight = WorldTime.Daylight(timeOfDay);
        rain.Follow(focus, rainAmount, wind, env.FogLightColor.Lerp(new Color(0.8f, 0.82f, 0.85f) * (0.3f + 0.7f * daylight), 0.4f));
        fireflies.Follow(focus, Mathf.SmoothStep(0.85f, 1f, 1f - daylight) * (1f - Mathf.Clamp(rainAmount * 3f, 0f, 1f)));
        sound.Daylight = daylight;
        sound.Rain = rainAmount;
        sound.Wind = wind;
        sound.Wetness = wetness;
        sound.Cover = Sheltered?.Invoke(focus) == true ? 1f : 0f;
        sound.TimeOfDay = timeOfDay;
        sound.Foliage = Woodedness(focus);
        Lightning(dt);
        if (GetViewport()?.GetCamera3D() is { } ear) FireSound(focus, ear.GlobalBasis.X);
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
        sun.LightColor = Grey(isDay ? sunColor : moonColor, overcast * 0.6f, 1f);
        sun.LightEnergy = isDay ? 2.4f * sunUp * (1f - 0.72f * overcast) : 0.55f * moonUp * (1f - 0.7f * overcast);
        sun.LightVolumetricFogEnergy = isDay ? (0.6f + 1.0f * Mathf.SmoothStep(0.05f, 0.35f, sunHeight)) * (1f - 0.5f * overcast) : 1.2f;
        sun.ShadowOpacity = 1f - 0.6f * overcast;

        // Overcast skies go grey and flat; rain darkens and thickens the air.
        float grey = overcast * 0.75f;
        float dim = 1f - 0.35f * storm;
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

        env.FogLightColor = Grey(mood.Fog, grey, 0.95f) * dim;
        env.FogDensity = mood.FogDensity * (1f + 2.4f * storm + 0.3f * overcast);
        env.VolumetricFogAlbedo = Grey(mood.Mist, grey, 1f) * (1f - 0.25f * storm);
        env.VolumetricFogDensity = mood.MistDensity * (1f + 2.2f * storm);
        env.AmbientLightColor = mood.Ambient;
        env.AmbientLightEnergy = mood.AmbientEnergy * (1f + 0.35f * overcast);
        env.TonemapExposure = mood.Exposure * (1f + 0.12f * overcast);
        env.AdjustmentSaturation = 1f - 0.2f * overcast - 0.1f * (1f - daylight);
        env.GlowIntensity = 0.6f + 0.4f * (1f - daylight);

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
        float total = 0f, nearest = 0f, pan = 0f;
        foreach (var (x, z, fuelUntil) in fires.Values)
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
                pan = d < 0.5f ? 0f : offset.Normalized().Dot(new Vector3(right.X, 0f, right.Z).Normalized());
            }
        }
        sound.Fire = Mathf.Min(total, 1f);
        sound.FirePan = pan;
    }

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

    float strikeAge = 10f, strikePeak;

    // Storms throw lightning every half minute or so. Most of it is far off: a dull
    // glow in the clouds and a late, low rumble. Now and then a strike is close: a
    // white double flash and a crack right after.
    void Lightning(float dt)
    {
        float stormy = Mathf.SmoothStep(0.75f, 0.95f, rainAmount);
        nextStrike -= dt * stormy;
        if (stormy > 0.05f && nextStrike <= 0f)
        {
            nextStrike = (float)GD.RandRange(12.0, 40.0);
            float near = Mathf.Pow(GD.Randf(), 2f);
            strikePeak = (0.2f + 0.8f * near) * stormy;
            strikeAge = 0f;
            // Sound covers about 340 m a second; distant strikes are 2 to 6 km off.
            sound.Thunder(0.3f + (1f - near) * 5f, (0.35f + 0.65f * near) * stormy, near);
        }
        strikeAge += dt;
        float a = strikeAge;
        flash = strikePeak * (Mathf.Exp(-a / 0.07f) + (a > 0.16f ? 0.7f * Mathf.Exp(-(a - 0.16f) / 0.12f) : 0f));
    }

    // A unit vector pointing at something this many degrees above the horizon,
    // turned the same way a light with this yaw would be.
    static Vector3 Toward(float elevationDeg, float azimuthDeg) =>
        Basis.FromEuler(new Vector3(Mathf.DegToRad(-elevationDeg), Mathf.DegToRad(azimuthDeg), 0)).Z;

    // --- Footsteps --------------------------------------------------------------

    // One step per stride, matching the avatar's leg swing.
    void Footsteps()
    {
        if (Player == null || !Player.IsInsideTree()) return;
        var pos = Player.GlobalPosition;
        var flat = pos - lastPlayerPos;
        flat.Y = 0;
        lastPlayerPos = pos;
        float distance = flat.Length();
        if (distance > 2f || distance < 0.0005f) return;
        float before = stepPhase;
        stepPhase += distance * 2.4f;
        if (Mathf.Floor(before / Mathf.Pi + 0.5f) == Mathf.Floor(stepPhase / Mathf.Pi + 0.5f)) return;

        int cx = World.CellOf(pos.X), cz = World.CellOf(pos.Z);
        terrain ??= GetParent()?.GetChildren().OfType<Terrain>().FirstOrDefault();
        var ground = Sheltered?.Invoke(pos) == true ? Ground.Floor
            : pos.Y < Look.WaterLevel + 0.05f ? Ground.Water
            : World.IsClay(cx, cz) ? Ground.Clay
            : terrain?.Cleared?.Invoke(cx, cz) == true ? Ground.Earth
            : Ground.Grass;
        sound.Step(ground, wetness);
    }

    Terrain? terrain;

    // How wooded it is around a point, 0 open to 1 deep woods, from the same forest
    // density the terrain plants its trees by. Sampled every few metres out to
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
                if (x * x + z * z < 12 * 12 || World.IsClay(x, z)) continue;
                sum += World.Noise(x, z, 24f, 33);
            }
        return Mathf.SmoothStep(0.3f, 0.7f, sum / count);
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

                if (d.y > 0.0) {
                    if (stars > 0.0) {
                        vec3 p = d * 260.0;
                        float r = hash(floor(p));
                        float star = step(0.985, r) * smoothstep(0.35, 0.0, length(fract(p) - 0.5)) * (r - 0.985) * 66.0;
                        col += vec3(star) * stars * smoothstep(0.0, 0.3, d.y) * 1.4;
                    }
                    // The same cloud layer whose shadows drift over the ground.
                    vec2 uv = (d.xz / (d.y + 0.03) * 300.0 + cloud_offset) * 0.004;
                    float n = texture(clouds, uv).r;
                    float edge = 1.0 - cloud_cover;
                    float c = smoothstep(edge - 0.08, edge + 0.22, n) * smoothstep(0.0, 0.18, d.y);
                    float lit = 0.25 + 0.75 * daylight;
                    vec3 cloud_col = mix(horizon_color * 1.1, vec3(0.92, 0.92, 0.95) * lit + sun_color * 0.15 * sun_vis, 0.5 + 0.5 * n);
                    cloud_col *= 1.0 - 0.45 * cloud_cover * smoothstep(0.4, 0.8, n);
                    col = mix(col, cloud_col, c * 0.9);
                }
                COLOR = col;
            }
            """,
    };
}
