using System;
using LifeGame;

static partial class AudioScenes
{
    // Rabbit scene: rustling (1, 2.5 s), a bolt thump (4 s), a squeak (6 s).
    [Scene("rabbit")]
    static void Rabbit(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        if (At(1)) mix.Play(CueDefs.RabbitRustle);
        if (At(2.5)) mix.Play(CueDefs.RabbitRustle);
        if (At(4)) mix.Play(CueDefs.RabbitThump);
        if (At(6)) mix.Play(CueDefs.RabbitSqueak);
    }
}
