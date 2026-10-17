using System;
using LifeGame;

static partial class AudioScenes
{
    // Roast scene: browning (0-6 s), ready (6-12 s), charring (12-18 s).
    [Scene("roast")]
    static void Roast(Block s)
    {
        var mix = s.Mix;
        double a = s.A, b = s.B;
        var prng = new Random((int)(a * 100) + 7);
        for (double t0 = Math.Floor(a * 20) / 20; t0 < b; t0 += 0.05)
        {
            if (t0 < a) continue;
            bool ready = t0 > 6, charring = t0 > 12;
            double sizzle = charring ? 0.5 : ready ? 0.7 : 1.1, spit = charring ? 1.1 : ready ? 0.25 : 0.6, drip = ready && !charring ? 0.35 : 0.15;
            if (prng.NextDouble() < sizzle * 0.05) mix.Play(CueDefs.RoastSizzle);
            if (prng.NextDouble() < spit * 0.05) mix.Play(CueDefs.RoastSpit);
            if (prng.NextDouble() < drip * 0.05) mix.Play(CueDefs.RoastDrip);
        }
    }
}
