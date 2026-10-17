using System;
using LifeGame;

static partial class AudioScenes
{
    // Pound scene: six blows, 1.2 a second, from 1 s.
    [Scene("pound")]
    static void Pound(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        for (int i = 0; i < 6; i++) if (At(1.0 + i / 1.2)) mix.Play(CueDefs.PoundThud);
    }
}
