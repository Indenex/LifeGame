using System;
using LifeGame;

static partial class AudioScenes
{
    // Rest scene: sit (1 s), get up (3 s), lie on the ground (5 s), get up (8 s), lie on a mat (10 s), get up (13 s).
    [Scene("rest")]
    static void Rest(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        if (At(1)) mix.Play(CueDefs.RestSit);
        if (At(3)) mix.Play(CueDefs.RestUp);
        if (At(5)) mix.Play(CueDefs.RestLie);
        if (At(8)) mix.Play(CueDefs.RestUp);
        if (At(10)) mix.Play(CueDefs.RestLieMat);
        if (At(13)) mix.Play(CueDefs.RestUp);
    }
}
