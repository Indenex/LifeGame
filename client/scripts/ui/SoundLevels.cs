using System;
using Godot;

namespace LifeGame;

// The player's sound levels from the Options page (D-097), 0 to 1, saved in
// user://settings.cfg. New players start every slider at half. Master drives Godot's
// Master bus here; the Audio lane reads Music, Effects (work and other sounds you make)
// and Ambience (wind, rain, birds, water) as gains on its own sound groups.
public static class SoundLevels
{
    public const float Default = 0.5f;

    public static float Master { get; private set; } = Load("master");
    public static float Music { get; private set; } = Load("music");
    public static float Effects { get; private set; } = Load("effects");
    public static float Ambience { get; private set; } = Load("ambience");

    // Any level changed; the Audio lane can listen instead of reading every frame.
    public static event Action? Changed;

    // Players from before the Options page keep the volume they had set.
    static float Load(string key) =>
        Settings.Get("sound", key, key == "master" ? Settings.Get("sound", "volume", Default) : Default);

    public static void ApplyMaster() =>
        AudioServer.SetBusVolumeDb(0, Master <= 0.001f ? -80f : Mathf.LinearToDb(Master));

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
