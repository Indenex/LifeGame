using System;
using LifeGame;

static partial class AudioScenes
{
    // Clap and shrug scene: five claps from 1 s, a shrug at 4 s (times as in EmoteSounds).
    [Scene("clap")]
    static void Clap(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        for (int i = 0; i < 5; i++) if (At(1.25 + i / 3.2)) mix.Play(CueDefs.EmoteHandClap);
        if (At(4.25)) mix.Play(CueDefs.EmoteSwish, 0.8f);
        if (At(5.05)) mix.Play(CueDefs.EmoteSwish, 0.65f);
    }
}
