using Godot;

namespace LifeGame;

// Streams the synthesized soundscape to the speakers.
public partial class SoundScape : Node
{
    readonly SoundMix mix = new((int)AudioServer.GetMixRate(), (int)(Time.GetTicksUsec() % 100000));
    readonly AudioStreamPlayer player = new();
    AudioStreamGeneratorPlayback? playback;
    const int Chunk = 256;
    readonly float[] left = new float[Chunk], right = new float[Chunk];
    readonly Vector2[] frames = new Vector2[Chunk];

    public float Daylight { set => mix.Daylight = value; }
    public float Rain { set => mix.Rain = value; }
    public float Wind { set => mix.Wind = value; }
    public float Wetness { set => mix.Wetness = value; }
    public float Cover { set => mix.Cover = value; }
    public float TimeOfDay { set => mix.TimeOfDay = value; }
    public float Foliage { set => mix.Foliage = value; }
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
        // Push whole chunks through one reused array, so the mixer makes no garbage.
        // A remainder shorter than a chunk (under 6 ms) waits for the next frame;
        // the 0.25 s buffer stays nearly full either way.
        int available = playback.GetFramesAvailable();
        while (available >= Chunk)
        {
            mix.Render(left, right, Chunk);
            for (int i = 0; i < Chunk; i++) frames[i] = new Vector2(left[i], right[i]);
            playback.PushBuffer(frames);
            available -= Chunk;
        }
    }
}
