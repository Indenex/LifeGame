using System;
using LifeGame;

static partial class AudioScenes
{
    // Pot scene: a thin broth bubbling for 6 s, then a thick stew.
    [Scene("pot")]
    static void Pot(Block s)
    {
        var mix = s.Mix;
        double a = s.A, b = s.B;
        var prng = new Random((int)(a * 100));
        for (double t0 = Math.Floor(a * 20) / 20; t0 < b; t0 += 0.05)
        {
            if (t0 < a) continue;
            bool stew = t0 > 6;
            double r = (stew ? 0.9 : 1.8) * 0.05, roll = prng.NextDouble();
            if (prng.NextDouble() >= r) continue;
            mix.Play(stew ? (roll < 0.55 ? CueDefs.BubbleBig : roll < 0.85 ? CueDefs.BubbleSmall : CueDefs.BubblePop) : (roll < 0.6 ? CueDefs.BubbleSmall : roll < 0.9 ? CueDefs.BubblePop : CueDefs.BubbleBig));
        }
    }
}
