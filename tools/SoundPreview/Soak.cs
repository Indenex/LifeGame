using LifeGame;

// Drives a SoundMix like the game does for a long time and checks the output.
static class Soak
{
    const int Rate = 44100, Block = 256; // the game's block size

    // Every public setting at the edges of its range (and just outside), one at a time. With the
    // old unguarded mix this found the root cause of the silence: StreamPan at 0.75 or more
    // (the stream voice pans its two halves at pan plus and minus 0.3, so Pan() took the
    // square root of a negative number and a NaN reached the shared filters and reverb for good).
    public static int Edges()
    {
        SoundMix.Fault = m => Console.WriteLine($"  FAULT: {m}");
        float[] edges = { 0f, 1f, -0f, 1.0000001f, 0.9999999f, -1e-7f, -1f, 1.5f, 2f, 1000f, 0.5f, 0.25f, 0.75f, 0.875f, 0.125f };
        var fields = typeof(SoundMix).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(float)).ToArray();
        var l = new float[Block];
        var r = new float[Block];
        int failures = 0;
        foreach (var f in fields)
            foreach (var e in edges)
            {
                var mix = new SoundMix(Rate, seed: 1) { Rain = 0.6f, Wind = 0.5f, Fire = 0.5f, Water = 0.5f, Stream = 0.5f };
                f.SetValue(mix, e);
                for (int i = 0; i < 400; i++) mix.Render(l, r, Block);
                if (mix.Faults > 0) { Console.WriteLine($"FAILED: {f.Name} = {e:R} made the mix fault"); failures++; }
            }
        for (int k = 0; k <= 2000; k += 7)
        {
            var mix = new SoundMix(Rate, seed: 1) { Rain = 0.3f, TimeOfDay = k / 1000f, Year = k / 1000f - 0.5f };
            for (int i = 0; i < 60; i++) mix.Render(l, r, Block);
            if (mix.Faults > 0) { Console.WriteLine($"FAILED: TimeOfDay/Year sweep at {k / 1000f}"); failures++; }
        }
        Console.WriteLine(failures == 0 ? "ok: edge values" : $"{failures} edge failure(s)");
        return failures == 0 ? 0 : 5;
    }

    public static int Run(double hours, int seed, bool fuzz, bool inject)
    {
        var rng = new Random(seed);
        SoundMix.Fault = m => Console.WriteLine($"FAULT: {m}");
        var mix = new SoundMix(Rate, seed: seed) { MusicOn = true };
        var left = new float[Block];
        var right = new float[Block];
        long blocks = (long)(hours * 3600 * Rate / Block);
        var cues = Enum.GetValues<Cue>();
        var grounds = Enum.GetValues<Ground>();
        var works = Enum.GetValues<Work>();
        var calls = Enum.GetValues<SoundMix.DeerCall>();
        double t = 0, nextEvent = 0, nextWeather = 0;
        float rain = 0, wind = 0.3f, cover = 0, fire = 0;
        long bad = 0, injects = 0;
        double maxAbs = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (long b = 0; b < blocks; b++)
        {
            t = b * (double)Block / Rate;
            if (t >= nextWeather)
            {
                nextWeather = t + 5 + rng.NextDouble() * 120;
                rain = rng.NextDouble() < 0.4 ? (float)rng.NextDouble() : 0f;
                wind = (float)rng.NextDouble();
                cover = rng.NextDouble() < 0.3 ? 1f : 0f;
                fire = rng.NextDouble() < 0.2 ? (float)rng.NextDouble() : 0f;
            }
            // Fast clock: a game day every 6 minutes, a year every 2 hours.
            mix.TimeOfDay = (float)((t / 360.0) % 1.0);
            mix.Daylight = Math.Clamp((float)(0.5 - 0.5 * Math.Cos(mix.TimeOfDay * 2 * Math.PI)), 0f, 1f);
            mix.Year = (float)((t / 7200.0) % 1.0);
            mix.SnowCover = mix.Year > 0.72f ? 0.8f : 0f;
            mix.Rain = rain; mix.Wind = wind; mix.Cover = cover; mix.Fire = fire; mix.FirePan = (float)(rng.NextDouble() * 2 - 1);
            mix.Wetness = rain;
            mix.Foliage = (float)rng.NextDouble();
            if (fuzz && rng.Next(40) == 0) { mix.WaterPan = 1.3f; mix.StreamPan = -2f; mix.FirePan = 1.0000001f; }
            mix.Water = (float)rng.NextDouble(); mix.WaterPan = (float)(rng.NextDouble() * 2 - 1);
            mix.Stream = (float)rng.NextDouble(); mix.StreamPan = (float)(rng.NextDouble() * 2 - 1);
            while (t >= nextEvent)
            {
                nextEvent += 0.05 + rng.NextDouble() * 1.5;
                float pan = (float)(rng.NextDouble() * 2 - 1);
                // Fuzz: now and then hand the mix values a bug elsewhere might produce.
                if (fuzz && rng.Next(8) == 0)
                    pan = new[] { 1.0000001f, -1.0000001f, 2.5f, -7f, float.NaN, float.PositiveInfinity }[rng.Next(6)];
                float g = (float)rng.NextDouble();
                if (fuzz && rng.Next(8) == 0)
                    g = new[] { -1f, 4f, 100f, float.NaN, float.PositiveInfinity, -0f }[rng.Next(6)];
                try
                {
                    switch (rng.Next(12))
                    {
                        case 0: mix.Step(grounds[rng.Next(grounds.Length)], mix.Wetness); break;
                        case 1: mix.Land(grounds[rng.Next(grounds.Length)], (float)rng.NextDouble(), g, pan); break;
                        case 2: mix.Play(cues[rng.Next(cues.Length)], g, pan); break;
                        case 3: mix.Bleat(pan, g, rng.Next(2) == 0, rng.Next(2) == 0); break;
                        case 4: mix.DeerCry(calls[rng.Next(calls.Length)], pan, g, g); break;
                        case 5: mix.Thunder((float)(rng.NextDouble() * 6), g, g); break;
                        case 6: mix.Working(works[rng.Next(works.Length)]); break;
                        case 7: mix.WorkDone(works[rng.Next(works.Length)], rng.Next(2) == 0); break;
                        case 8: mix.OtherWorking((uint)rng.Next(1, 6), works[rng.Next(works.Length)]); break;
                        case 9: mix.OtherWorkDone((uint)rng.Next(1, 6), works[rng.Next(works.Length)], rng.Next(2) == 0); break;
                        case 10: mix.OtherPlace((uint)rng.Next(1, 6), g * 0.4f, pan); break;
                        default: mix.MusicOn = rng.Next(4) != 0; break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"EXCEPTION in event at {t:F1} s: {e}");
                    return 1;
                }
            }
            bool injected = false;
            if (inject && b % 5000 == 2500) { mix.InjectNaN = true; injected = true; }
            if (inject && b % 5000 == 4000) { mix.InjectThrow = true; injected = true; }
            injects += injected ? 1 : 0;
            mix.Render(left, right, Block);
            for (int i = 0; i < Block; i++)
            {
                if (!float.IsFinite(left[i]) || !float.IsFinite(right[i]))
                {
                    if (bad++ == 0) Console.WriteLine($"NON-FINITE output at {t:F2} s (block {b}, sample {i}) year {mix.Year:F3} rain {rain:F2} cover {cover}");
                    break;
                }
                maxAbs = Math.Max(maxAbs, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
            }
            if (bad > 0) { Console.WriteLine("a non-finite sample got out of Render: the guard failed"); return 2; }
        }
        if (inject && mix.Faults != injects) { Console.WriteLine($"FAILED: injected {injects} faults, mix reported {mix.Faults}"); return 4; }
        if (mix.Faults > 0 && !fuzz && !inject) { Console.WriteLine($"FAILED: {mix.Faults} fault(s) without fuzzed inputs: the mix itself produced a bad block"); return 3; }
        Console.WriteLine($"ok{(fuzz ? " (fuzz)" : "")}: {hours} game hours ({blocks} blocks) in {watch.Elapsed.TotalSeconds:F0} s, seed {seed}, max |sample| {maxAbs:F2}, faults {mix.Faults}");
        return 0;
    }
}
