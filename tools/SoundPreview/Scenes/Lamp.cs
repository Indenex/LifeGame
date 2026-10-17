using System;
using LifeGame;

static partial class AudioScenes
{
    // Lamp scene: the wick catches (1 s), flutters and ticks, then the flame goes out (14 s).
    [Scene("lamp")]
    static void Lamp(Block s)
    {
        var mix = s.Mix;
        bool At(double x) => s.At(x);
        if (At(1)) mix.Play(CueDefs.LampLight);
        foreach (double t in new[] { 3.0, 7.0, 10.0 }) if (At(t)) mix.Play(CueDefs.LampFlutter);
        foreach (double t in new[] { 5.0, 9.0, 12.0 }) if (At(t)) mix.Play(CueDefs.LampTick);
        if (At(14)) mix.Play(CueDefs.LampOut);
    }
}
