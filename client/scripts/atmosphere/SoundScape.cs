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
    public float Fire { set => mix.Fire = value; }
    public float FirePan { set => mix.FirePan = value; }
    public bool MusicOn { get => mix.MusicOn; set => mix.MusicOn = value; }

    public override void _Ready()
    {
        player.Stream = new AudioStreamGenerator { MixRate = mix.Rate, BufferLength = 0.25f };
        AddChild(player);
        player.Play();
        playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
    }

    public void Step(Ground ground, float wetness) => mix.Step(ground, wetness);
    public void Bleat(float pan, float gain, bool startled) => mix.Bleat(pan, gain, startled);
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
