using System;
using LifeGame;

static partial class AudioScenes
{
    // Clay dig scene: a spade in clay (dig, dig, throw) then bare hands in clay.
    [Scene("clay")]
    static void Clay(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        foreach (double t in new[] { 1.0, 3.0 }) if (At(t)) { mix.Play(CueDefs.DigSpade); mix.Play(CueDefs.DigClay); }
        if (At(4.2)) { mix.Play(CueDefs.DirtThrow); mix.Play(CueDefs.ClayThrow); }
        if (At(6.5)) { mix.Play(CueDefs.DigHands); mix.Play(CueDefs.DigClay); }
        if (At(8.0)) mix.Play(CueDefs.DigClay);       // the clay layers alone, for levels
        if (At(9.0)) mix.Play(CueDefs.ClayThrow);
    }
}
