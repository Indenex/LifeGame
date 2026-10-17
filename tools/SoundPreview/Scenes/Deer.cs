using System;
using LifeGame;

static partial class AudioScenes
{
    // Deer grazing scene: crunches every 2 s, a snort at 8 s.
    [Scene("deer")]
    static void Deer(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        for (int i = 0; i < 6; i++) if (At(1.0 + 2.0 * i)) mix.Play(CueDefs.DeerCrunch);
        if (At(8)) mix.Play(CueDefs.DeerSnort);
    }
}
