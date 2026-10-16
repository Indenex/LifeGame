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
    // Running water along a stream (audio-2a); ponds use Water.
    public float Stream { set => mix.Stream = value; }
    public float StreamPan { set => mix.StreamPan = value; }
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
        SoundMix.Fault = what => Fault(what);
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
    public void Land(Ground ground, float strength, float gain = 1f, float pan = 0f) => events.Enqueue(m => m.Land(ground, strength, gain, pan));
    public void Play(Cue cue, float gain = 1f, float pan = 0f) => events.Enqueue(m => m.Play(cue, gain, pan));
    public void DeerCry(SoundMix.DeerCall call, float pan, float gain, float far) => events.Enqueue(m => m.DeerCry(call, pan, gain, far));
    public void Bleat(float pan, float gain, bool startled, bool lamb = false) => events.Enqueue(m => m.Bleat(pan, gain, startled, lamb));
    public void Thunder(float delay, float gain, float near) => events.Enqueue(m => m.Thunder(delay, gain, near));
    public void Working(Work kind) => events.Enqueue(m => m.Working(kind));
    public void WorkDone(Work kind, bool failed) => events.Enqueue(m => m.WorkDone(kind, failed));
    public void OtherWorking(uint player, Work kind) => events.Enqueue(m => m.OtherWorking(player, kind));
    public void OtherWorkDone(uint player, Work kind, bool failed) => events.Enqueue(m => m.OtherWorkDone(player, kind, failed));
    public void OtherPlace(uint player, float gain, float pan) => events.Enqueue(m => m.OtherPlace(player, gain, pan));

    int faultsLogged;

    // Logged once per fault, at most a few times, so a repeating fault cannot flood the log.
    void Fault(string what)
    {
        if (faultsLogged++ < 5) GD.PushError($"SoundScape: {what}");
    }

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
            // Nothing a single sound does may end this thread: all sound in the game comes
            // through it. A failing event is dropped and logged; SoundMix.Render guards itself.
            while (events.TryDequeue(out var e))
            {
                try { e(mix); }
                catch (Exception ex) { Fault($"a sound event threw: {ex}"); }
            }
            try { mix.Render(left, right, Block); }
            catch (Exception ex)
            {
                Fault($"Render threw: {ex}");
                Array.Clear(left, 0, Block);
                Array.Clear(right, 0, Block);
            }
            long w = written;
            for (int i = 0; i < Block; i++) ring[(w + i) % RingFrames] = new Vector2(left[i], right[i]);
            Interlocked.Add(ref written, Block);
        }
    }

    public override void _Process(double delta)
    {
        // Watchdogs: if the synth thread has died or Godot stopped the player (an audio
        // device change, say), bring them back instead of staying silent for the session.
        if (running && synth is { IsAlive: false })
        {
            Fault("the synth thread had stopped; restarting it");
            synth = new Thread(Synthesize) { IsBackground = true, Name = "SoundScape", Priority = ThreadPriority.AboveNormal };
            synth.Start();
        }
        if (running && !player.Playing)
        {
            Fault("the audio player had stopped; restarting it");
            player.Play();
            playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        }
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
