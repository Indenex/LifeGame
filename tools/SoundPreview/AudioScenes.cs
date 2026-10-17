using System;
using LifeGame;

// Audio-lane preview scenes, kept out of Program.cs so new scenes never conflict with other
// lanes' edits there. Program.cs calls Owns (to leave the walking off) and Render once per block.
static class AudioScenes
{
    static readonly string[] scenes = { "roast", "store", "pot", "emote", "lamp", "clay", "rabbit", "rest", "clap", "deer" };

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
            // Emote scene: wave at 1 s, cheer at 4 s, dance at 7 s, bow at 12 s (times as in EmoteSounds).
            case "emote":
                if (At(1.2)) mix.Play(CueDefs.EmoteSwish, 0.9f);
                if (At(2.65)) mix.Play(CueDefs.EmoteSwish, 0.6f);
                if (At(4.28)) mix.Play(CueDefs.EmoteClap);
                if (At(4.6)) mix.Play(CueDefs.EmoteTap, 0.8f);
                if (At(5.08)) mix.Play(CueDefs.EmoteClap);
                if (At(5.4)) mix.Play(CueDefs.EmoteTap, 0.8f);
                for (int i = 0; i < 7; i++)
                    if (At(7.4 + 0.4 * i)) mix.Play(CueDefs.EmoteTap, i % 2 == 0 ? 1f : 0.65f);
                if (At(12.15)) mix.Play(CueDefs.EmoteSwish, 0.7f);
                if (At(13.25)) mix.Play(CueDefs.EmoteSwish, 0.8f);
                break;
            // Lamp scene: the wick catches (1 s), flutters and ticks, then the flame goes out (14 s).
            case "lamp":
                if (At(1)) mix.Play(CueDefs.LampLight);
                foreach (double t in new[] { 3.0, 7.0, 10.0 }) if (At(t)) mix.Play(CueDefs.LampFlutter);
                foreach (double t in new[] { 5.0, 9.0, 12.0 }) if (At(t)) mix.Play(CueDefs.LampTick);
                if (At(14)) mix.Play(CueDefs.LampOut);
                break;
            // Clay dig scene: a spade in clay (dig, dig, throw) then bare hands in clay.
            case "clay":
                foreach (double t in new[] { 1.0, 3.0 }) if (At(t)) { mix.Play(CueDefs.DigSpade); mix.Play(CueDefs.DigClay); }
                if (At(4.2)) { mix.Play(CueDefs.DirtThrow); mix.Play(CueDefs.ClayThrow); }
                if (At(6.5)) { mix.Play(CueDefs.DigHands); mix.Play(CueDefs.DigClay); }
                if (At(8.0)) mix.Play(CueDefs.DigClay);       // the clay layers alone, for levels
                if (At(9.0)) mix.Play(CueDefs.ClayThrow);
                break;
            // Rabbit scene: rustling (1, 2.5 s), a bolt thump (4 s), a squeak (6 s).
            case "rabbit":
                if (At(1)) mix.Play(CueDefs.RabbitRustle);
                if (At(2.5)) mix.Play(CueDefs.RabbitRustle);
                if (At(4)) mix.Play(CueDefs.RabbitThump);
                if (At(6)) mix.Play(CueDefs.RabbitSqueak);
                break;
            // Clap and shrug scene: five claps from 1 s, a shrug at 4 s (times as in EmoteSounds).
            case "clap":
                for (int i = 0; i < 5; i++) if (At(1.25 + i / 3.2)) mix.Play(CueDefs.EmoteHandClap);
                if (At(4.25)) mix.Play(CueDefs.EmoteSwish, 0.8f);
                if (At(5.05)) mix.Play(CueDefs.EmoteSwish, 0.65f);
                break;
            // Deer grazing scene: crunches every 2 s, a snort at 8 s.
            case "deer":
                for (int i = 0; i < 6; i++) if (At(1.0 + 2.0 * i)) mix.Play(CueDefs.DeerCrunch);
                if (At(8)) mix.Play(CueDefs.DeerSnort);
                break;
            // Rest scene: sit (1 s), get up (3 s), lie on the ground (5 s), get up (8 s), lie on a mat (10 s), get up (13 s).
            case "rest":
                if (At(1)) mix.Play(CueDefs.RestSit);
                if (At(3)) mix.Play(CueDefs.RestUp);
                if (At(5)) mix.Play(CueDefs.RestLie);
                if (At(8)) mix.Play(CueDefs.RestUp);
                if (At(10)) mix.Play(CueDefs.RestLieMat);
                if (At(13)) mix.Play(CueDefs.RestUp);
                break;
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
