using System;
using System.Collections.Concurrent;
using System.Threading;
using Godot;

namespace LifeGame;

// Streams the synthesized soundscape to the speakers. The synthesis runs on its own
// thread a little ahead of playback, so it costs the frame nothing and keeps going
// through a hitch; the main thread only hands finished samples to Godot.
public partial class SoundScape : Node
{
    readonly SoundMix mix = new((int)AudioServer.GetMixRate(), (int)(Time.GetTicksUsec() % 100000));
    readonly AudioStreamPlayer player = new();
    AudioStreamGeneratorPlayback? playback;

    // Rendered stereo frames waiting for the speakers. One writer (the synth
    // thread), one reader (the main thread).
    // About 46 ms ahead plus Godot's 0.2 s buffer: about the latency the game had
    // before, and a hitch shorter than that buffer is never heard.
    const int RingFrames = 2048, Block = 256;
    readonly Vector2[] ring = new Vector2[RingFrames];
    long written, read;
    readonly ConcurrentQueue<Action<SoundMix>> events = new();
    Thread? synth;
    volatile bool running;
    readonly Vector2[] chunk = new Vector2[Block];

    // Levels are plain floats the synth thread reads each block; a stale read just
    // eases in one block later.
    public float Daylight { set => mix.Daylight = value; }
    public float Rain { set => mix.Rain = value; }
    public float Wind { set => mix.Wind = value; }
    public float Wetness { set => mix.Wetness = value; }
    public float Cover { set => mix.Cover = value; }
    public float TimeOfDay { set => mix.TimeOfDay = value; }
    public float Foliage { set => mix.Foliage = value; }
    public float Fire { set => mix.Fire = value; }
    public float FirePan { set => mix.FirePan = value; }
    public float Water { set => mix.Water = value; }
    public float WaterPan { set => mix.WaterPan = value; }
    // The time of year (0 to 1, Seasons.YearFraction) and the snow lying on the
    // ground (0 to 1), for the seasonal ambience (audio-2).
    public float Year { set => mix.Year = value; }
    public float SnowCover { set => mix.SnowCover = value; }
    // How hard the wind is gusting in what is heard, 0 to 1.
    public float Gust => mix.Gust;
    // Music on or off, remembered between sessions.
    public bool MusicOn { get => mix.MusicOn; set { mix.MusicOn = value; Save(); } }
    // The Options page sliders (D-097), each 0 to 1, 0.5 the designed mix. UX/UI's
    // SoundLevels owns and saves them and writes them here; the master slider
    // works on Godot's Master bus, so it is not here.
    public float MusicVolume { get => mix.MusicLevel; set => mix.MusicLevel = Mathf.Clamp(value, 0f, 1f); }
    public float EffectsVolume { get => mix.EffectsLevel; set => mix.EffectsLevel = Mathf.Clamp(value, 0f, 1f); }
    public float AmbienceVolume { get => mix.AmbienceLevel; set => mix.AmbienceLevel = Mathf.Clamp(value, 0f, 1f); }

    const string SettingsPath = "user://sound.cfg";

    void Load()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok) return;
        mix.MusicOn = (bool)config.GetValue("music", "on", true);
    }

    void Save()
    {
        var config = new ConfigFile();
        config.SetValue("music", "on", mix.MusicOn);
        config.Save(SettingsPath);
    }

    public override void _Ready()
    {
        Load();
        Sfx.Attach(this);
        player.Stream = new AudioStreamGenerator { MixRate = mix.Rate, BufferLength = 0.2f };
        AddChild(player);
        player.Play();
        playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        running = true;
        synth = new Thread(Synthesize) { IsBackground = true, Name = "SoundScape", Priority = ThreadPriority.AboveNormal };
        synth.Start();
    }

    public override void _ExitTree()
    {
        Sfx.Attach(null);
        running = false;
        synth?.Join(500);
    }

    // One-off sounds go through a queue so the synth thread adds them between blocks.
    public void Step(Ground ground, float wetness) => events.Enqueue(m => m.Step(ground, wetness));
    public void Play(Cue cue) => events.Enqueue(m => m.Play(cue));
    public void Bleat(float pan, float gain, bool startled, bool lamb = false) => events.Enqueue(m => m.Bleat(pan, gain, startled, lamb));
    public void Thunder(float delay, float gain, float near) => events.Enqueue(m => m.Thunder(delay, gain, near));
    public void Working(Work kind) => events.Enqueue(m => m.Working(kind));
    public void WorkDone(Work kind, bool failed) => events.Enqueue(m => m.WorkDone(kind, failed));
    public void OtherWorking(uint player, Work kind) => events.Enqueue(m => m.OtherWorking(player, kind));
    public void OtherWorkDone(uint player, Work kind, bool failed) => events.Enqueue(m => m.OtherWorkDone(player, kind, failed));
    public void OtherPlace(uint player, float gain, float pan) => events.Enqueue(m => m.OtherPlace(player, gain, pan));

    void Synthesize()
    {
        var left = new float[Block];
        var right = new float[Block];
        while (running)
        {
            long ahead = Interlocked.Read(ref written) - Interlocked.Read(ref read);
            if (ahead > RingFrames - Block)
            {
                Thread.Sleep(4);
                continue;
            }
            while (events.TryDequeue(out var e)) e(mix);
            mix.Render(left, right, Block);
            long w = written;
            for (int i = 0; i < Block; i++) ring[(w + i) % RingFrames] = new Vector2(left[i], right[i]);
            Interlocked.Add(ref written, Block);
        }
    }

    public override void _Process(double delta)
    {
        if (playback == null) return;
        // Whole blocks only, so the same array is reused every time.
        long ready = Interlocked.Read(ref written) - read;
        while (ready >= Block && playback.GetFramesAvailable() >= Block)
        {
            for (int i = 0; i < Block; i++) chunk[i] = ring[(read + i) % RingFrames];
            playback.PushBuffer(chunk);
            Interlocked.Add(ref read, Block);
            ready -= Block;
        }
    }
}
