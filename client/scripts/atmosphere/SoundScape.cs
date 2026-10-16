using Godot;

namespace LifeGame;

// Streams the synthesized soundscape to the speakers.
public partial class SoundScape : Node
{
    readonly SoundMix mix = new((int)AudioServer.GetMixRate(), (int)(Time.GetTicksUsec() % 100000));
    readonly AudioStreamPlayer player = new();
    AudioStreamGeneratorPlayback? playback;
    float[] left = new float[1024], right = new float[1024];

    public float Daylight { set => mix.Daylight = value; }
    public float Rain { set => mix.Rain = value; }
    public float Wind { set => mix.Wind = value; }
    public float Wetness { set => mix.Wetness = value; }
    public float Cover { set => mix.Cover = value; }
    public float Fire { set => mix.Fire = value; }
    public float FirePan { set => mix.FirePan = value; }
    // Music settings, remembered between sessions. The volume runs 0 to 1 and eases
    // to a new level over a second or so.
    public bool MusicOn { get => mix.MusicOn; set { mix.MusicOn = value; Save(); } }
    public float MusicVolume { get => mix.MusicVolume; set { mix.MusicVolume = Mathf.Clamp(value, 0f, 1f); Save(); } }

    const string SettingsPath = "user://sound.cfg";

    void Load()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok) return;
        mix.MusicOn = (bool)config.GetValue("music", "on", true);
        mix.MusicVolume = Mathf.Clamp((float)config.GetValue("music", "volume", 1f), 0f, 1f);
    }

    void Save()
    {
        var config = new ConfigFile();
        config.SetValue("music", "on", mix.MusicOn);
        config.SetValue("music", "volume", mix.MusicVolume);
        config.Save(SettingsPath);
    }

    public override void _Ready()
    {
        Load();
        player.Stream = new AudioStreamGenerator { MixRate = mix.Rate, BufferLength = 0.25f };
        AddChild(player);
        player.Play();
        playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    public void Step(Ground ground, float wetness) => mix.Step(ground, wetness);
    public void Bleat(float pan, float gain, bool startled, bool lamb = false) => mix.Bleat(pan, gain, startled, lamb);
    public void Thunder(float delay, float gain, float near) => mix.Thunder(delay, gain, near);

    public override void _Process(double delta)
    {
        if (playback == null) return;
        int available = playback.GetFramesAvailable();
        while (available > 0)
        {
            int n = Mathf.Min(available, 1024);
            mix.Render(left, right, n);
            var frames = new Vector2[n];
            for (int i = 0; i < n; i++) frames[i] = new Vector2(left[i], right[i]);
            playback.PushBuffer(frames);
            available -= n;
        }
    }
}
