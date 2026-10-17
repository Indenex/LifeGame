using System;
using LifeGame;

static partial class AudioScenes
{
    // Emote scene: wave at 1 s, cheer at 4 s, dance at 7 s, bow at 12 s (times as in EmoteSounds).
    [Scene("emote")]
    static void Emote(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        if (At(1.2)) mix.Play(CueDefs.EmoteSwish, 0.9f);
        if (At(2.65)) mix.Play(CueDefs.EmoteSwish, 0.6f);
        if (At(4.28)) mix.Play(CueDefs.EmoteClap);
        if (At(4.6)) mix.Play(CueDefs.EmoteTap, 0.8f);
        if (At(5.08)) mix.Play(CueDefs.EmoteClap);
        if (At(5.4)) mix.Play(CueDefs.EmoteTap, 0.8f);
        for (int i = 0; i < 7; i++)
            if (At(7.4 + 0.4 * i)) mix.Play(CueDefs.EmoteTap, i % 2 == 0 ? 1f : 0.65f);
        if (At(12.15)) mix.Play(CueDefs.EmoteSwish, 0.7f);
        if (At(13.25)) mix.Play(CueDefs.EmoteSwish, 0.8f);
    }
}
