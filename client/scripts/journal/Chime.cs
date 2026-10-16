using System;
using Godot;

namespace LifeGame;

// A soft wooden chime for quiet milestones in the journal: two notes when a skill
// earns a new title, one faint note when something new goes in the journal.
// Synthesized once at startup (a struck-wood tone with a short, airy echo), so
// it needs no audio files and never sounds like a game-show ding.
public partial class Chime : Node
{
    const int Rate = 44100;
    readonly AudioStreamPlayer player = new() { VolumeDb = -16f };
    AudioStreamWav? title, discovery;

    public override void _Ready()
    {
        AddChild(player);
        // D5 then A5: an open fifth, unresolved and calm.
        title = Render(new[] { (587.33f, 0f, 1f), (880f, 0.16f, 0.8f) }, 2.2f);
        discovery = Render(new[] { (783.99f, 0f, 0.55f) }, 1.6f);
    }

    // A skill crossed a rank band (Novice to Apprentice and so on).
    public void Title() => Play(title, -16f);

    // Something new went in the journal.
    public void Discovery() => Play(discovery, -22f);

    void Play(AudioStreamWav? stream, float db)
    {
        if (stream == null) return;
        player.Stream = stream;
        player.VolumeDb = db;
        player.Play();
    }

    // Each note: a fundamental and the bright 3.9x partial of a marimba bar, with
    // a soft mallet attack, then two quiet echoes so it hangs in the air a moment.
    static AudioStreamWav Render((float freq, float at, float gain)[] notes, float seconds)
    {
        int n = (int)(seconds * Rate);
        var mix = new float[n];
        foreach (var (freq, at, gain) in notes)
        {
            int start = (int)(at * Rate);
            for (int i = start; i < n; i++)
            {
                float t = (i - start) / (float)Rate;
                float attack = Math.Min(1f, t / 0.004f);
                float body = MathF.Sin(MathF.Tau * freq * t) * MathF.Exp(-t * 3.2f);
                float ring = 0.25f * MathF.Sin(MathF.Tau * freq * 3.9f * t) * MathF.Exp(-t * 14f);
                mix[i] += gain * attack * (body + ring);
            }
        }
        foreach (var (delay, level) in new[] { (0.23f, 0.22f), (0.47f, 0.1f) })
        {
            int d = (int)(delay * Rate);
            for (int i = n - 1; i >= d; i--) mix[i] += mix[i - d] * level;
        }

        float peak = 0.0001f;
        foreach (var s in mix) peak = Math.Max(peak, Math.Abs(s));
        var data = new byte[n * 2];
        for (int i = 0; i < n; i++)
        {
            // Fade the very end so the buffer never clicks off.
            float fade = Math.Min(1f, (n - i) / (0.05f * Rate));
            short v = (short)(mix[i] / peak * fade * 0.8f * short.MaxValue);
            data[i * 2] = (byte)v;
            data[i * 2 + 1] = (byte)(v >> 8);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = Rate, Stereo = false, Data = data };
    }
}
