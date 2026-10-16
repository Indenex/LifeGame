using Godot;

namespace LifeGame;

// The player's own settings (volume, keys, camera), in user://settings.cfg. Loaded on
// first use; every change saves at once, so nothing is lost if the game closes.
public static class Settings
{
    const string Path = "user://settings.cfg";
    static ConfigFile? file;

    static ConfigFile File
    {
        get
        {
            if (file != null) return file;
            file = new ConfigFile();
            file.Load(Path);
            return file;
        }
    }

    public static T Get<[MustBeVariant] T>(string section, string key, T fallback) =>
        File.HasSectionKey(section, key) ? File.GetValue(section, key).As<T>() : fallback;

    public static bool Has(string section, string key) => File.HasSectionKey(section, key);

    public static void Set(string section, string key, Variant value)
    {
        File.SetValue(section, key, value);
        File.Save(Path);
    }

    public static void Clear(string section)
    {
        if (File.HasSection(section)) File.EraseSection(section);
        File.Save(Path);
    }
}
