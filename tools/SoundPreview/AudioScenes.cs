using System;
using LifeGame;

// Audio-lane preview scenes, kept out of Program.cs so new scenes never conflict with other
// lanes' edits there. Program.cs calls Owns (to leave the walking off) and Render once per block.
static class AudioScenes
{
    static readonly string[] scenes = { "roast", "store", "pot" };

    public static bool Owns(string scene) => Array.IndexOf(scenes, scene) >= 0;

    public static void Render(string scene, SoundMix mix, int done, int n, int rate)
    {
        double a = done / (double)rate, b = (done + n) / (double)rate;
        bool At(double x) => a < x && b >= x;
        switch (scene)
        {
            // Roast scene: browning (0-6 s), ready (6-12 s), charring (12-18 s).
            case "roast":
            {
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
                break;
            }
            // Pot scene: a thin broth bubbling for 6 s, then a thick stew.
            case "pot":
            {
                var prng = new Random((int)(a * 100));
                for (double t0 = Math.Floor(a * 20) / 20; t0 < b; t0 += 0.05)
                {
                    if (t0 < a) continue;
                    bool stew = t0 > 6;
                    double r = (stew ? 0.9 : 1.8) * 0.05, roll = prng.NextDouble();
                    if (prng.NextDouble() >= r) continue;
                    mix.Play(stew ? (roll < 0.55 ? CueDefs.BubbleBig : roll < 0.85 ? CueDefs.BubbleSmall : CueDefs.BubblePop) : (roll < 0.6 ? CueDefs.BubbleSmall : roll < 0.9 ? CueDefs.BubblePop : CueDefs.BubbleBig));
                }
                break;
            }
            // Store scene: basket put and take, jar put and take, 1.5 s apart.
            case "store":
                if (At(1)) mix.Play(CueDefs.BasketPut);
                if (At(2.5)) mix.Play(CueDefs.BasketTake);
                if (At(4)) mix.Play(CueDefs.JarPut);
                if (At(5.5)) mix.Play(CueDefs.JarTake);
                break;
        }
    }
}
