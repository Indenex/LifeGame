using System;
using LifeGame;

static partial class AudioScenes
{
    // Store scene: basket put and take, jar put and take, 1.5 s apart.
    [Scene("store")]
    static void Store(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        if (At(1)) mix.Play(CueDefs.BasketPut);
        if (At(2.5)) mix.Play(CueDefs.BasketTake);
        if (At(4)) mix.Play(CueDefs.JarPut);
        if (At(5.5)) mix.Play(CueDefs.JarTake);
    }
}
