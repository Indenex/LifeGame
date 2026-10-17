using System;
using LifeGame;

static partial class AudioScenes
{
    // Bed and chest scene: lie on a straw bed (1 s), get up (4 s), put into a chest (6 s), take out (8 s).
    [Scene("bedchest")]
    static void BedChest(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        if (At(1)) mix.Play(CueDefs.RestLieBed);
        if (At(4)) mix.Play(CueDefs.RestUp);
        if (At(6)) mix.Play(CueDefs.ChestPut);
        if (At(8)) mix.Play(CueDefs.ChestTake);
    }
}
