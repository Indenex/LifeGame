using System;
using Godot;

namespace LifeGame;

// The player's sound levels from the Options page (D-097), 0 to 1, saved in
// user://settings.cfg; the one store for them (agreed with the Audio lane). New players
// start every slider at half, which is the designed mix. Master drives Godot's Master
// bus; Music, Effects (footsteps and all work) and Ambience (weather, birds, sheep,
// fire, water) go to the Audio lane's SoundScape, which eases them in its mixer.
public static class SoundLevels
{
    public const float Default = 0.5f;

    public static float Master { get; private set; } = Load("master");
    public static float Music { get; private set; } = Load("music");
    public static float Effects { get; private set; } = Load("effects");
    public static float Ambience { get; private set; } = Load("ambience");

    // Any level changed; the Audio lane can listen instead of reading every frame.
    public static event Action? Changed;

    // Players from before the Options page keep how loud they had it: the old Volume
    // was a plain bus gain, which is 4*s*s on the new curve.
    static float Load(string key) =>
        Settings.Get("sound", key, key == "master" && Settings.Has("sound", "volume")
            ? Mathf.Sqrt(Settings.Get("sound", "volume", 1f)) / 2f
            : Default);

    // Same curve as the Audio lane's groups, so half is the designed mix (0 dB):
    // gain 4*s*s, silent at 0, capped at +6 dB at the top.
    const float MaxGainDb = 6f;

    public static void ApplyMaster() =>
        AudioServer.SetBusVolumeDb(0, Master <= 0.001f ? -80f : Mathf.Min(Mathf.LinearToDb(4f * Master * Master), MaxGainDb));

    // Hands the three group levels to the soundscape; PauseMenu calls this at startup
    // and on every change.
    public static void ApplyTo(SoundScape? sound)
    {
        if (sound == null) return;
        sound.MusicVolume = Music;
        sound.EffectsVolume = Effects;
        sound.AmbienceVolume = Ambience;
    }

    public static void Set(string key, float value)
    {
        value = Mathf.Clamp(value, 0f, 1f);
        switch (key)
        {
            case "master": Master = value; ApplyMaster(); break;
            case "music": Music = value; break;
            case "effects": Effects = value; break;
            case "ambience": Ambience = value; break;
            default: return;
        }
        Settings.Set("sound", key, value);
        Changed?.Invoke();
    }

    public static float Get(string key) => key switch
    {
        "master" => Master,
        "music" => Music,
        "effects" => Effects,
        "ambience" => Ambience,
        _ => 0f,
    };
}
