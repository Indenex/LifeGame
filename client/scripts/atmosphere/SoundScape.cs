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
    // How hard the wind is gusting in what is heard, 0 to 1.
    public float Gust => mix.Gust;
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
        running = false;
        synth?.Join(500);
    }

    // One-off sounds go through a queue so the synth thread adds them between blocks.
    public void Step(Ground ground, float wetness) => events.Enqueue(m => m.Step(ground, wetness));
    public void Bleat(float pan, float gain, bool startled, bool lamb = false) => events.Enqueue(m => m.Bleat(pan, gain, startled, lamb));
    public void Thunder(float delay, float gain, float near) => events.Enqueue(m => m.Thunder(delay, gain, near));

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
