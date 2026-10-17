using LifeGame;

// Checks MusicDirector's rules with fake tracks (audio-8), no audio:
//   dotnet run -c Release -- --music-test
static class MusicTest
{
    public static int Run()
    {
        int failures = 0;
        void Check(bool ok, string what) { if (!ok) { Console.WriteLine("FAIL " + what); failures++; } }

        var defs = new[]
        {
            new MusicDef("d1", "x", "day"), new MusicDef("d2", "x", "day"), new MusicDef("d3", "x", "day dusk"),
            new MusicDef("n1", "x", "night rest"), new MusicDef("n2", "x", "dusk night"),
            new MusicDef("r1", "x", "rest"), new MusicDef("w1", "x", "rain"),
        };
        var lengths = defs.ToDictionary(d => d.Id, d => 110f + 17f * d.Id[1]);

        // 1. No tracks listed: nothing ever happens.
        var empty = new MusicDirector(Array.Empty<MusicDef>(), new Random(1));
        for (int i = 0; i < 20000; i++)
            Check(empty.Update(0.5f, new MusicContext(0.5f, 0f, 0f, 0f, true)).Action == MusicAction.None, "empty list stays silent");

        // 2. Three in-game days of changing weather and place; the fake player ends tracks at their length.
        for (int seed = 1; seed <= 5; seed++)
        {
            var dir = new MusicDirector(defs, new Random(seed));
            float now = 0f, playingSince = 0f, endedAt = -1000f, lastStart = -1000f;
            MusicDef? playing = null;
            string? previous = null;
            int starts = 0;
            for (float t = 0f; t < 3 * 1200f; t += 0.5f)
            {
                float day = (t / 1200f) % 1f;
                bool raining = (t % 900f) > 700f;
                bool indoors = (t % 1500f) > 1300f;
                bool enabled = !((t % 2000f) > 1800f);
                var ctx = new MusicContext(day, raining ? 0.8f : 0f, indoors ? 1f : 0f, 0f, enabled);
                var cmd = dir.Update(0.5f, ctx);
                now = t;
                // The track ran out.
                if (playing != null && now - playingSince >= lengths[playing.Id])
                {
                    playing = null;
                    endedAt = now;
                    dir.TrackEnded();
                }
                if (cmd.Action == MusicAction.Start)
                {
                    Check(enabled, $"seed {seed}: started while music is off at {t}");
                    Check(cmd.Track != null && cmd.FadeSeconds >= 3f && cmd.FadeSeconds <= 5f, $"seed {seed}: cross-fade of {cmd.FadeSeconds}s at {t}");
                    if (playing == null)
                        Check(now - endedAt >= 60f || endedAt < 0f || now - lastStart > 60f, $"seed {seed}: silence under a minute before the track at {t}");
                    var moodChain = MusicDirector.Chain(MusicDirector.MoodOf(ctx), ctx.TimeOfDay);
                    Check(moodChain.Any(cmd.Track!.Has) || dir.Mood != MusicDirector.MoodOf(ctx), $"seed {seed}: {cmd.Track!.Id} doesn't fit the mood at {t}");
                    if (previous != null && playing == null) Check(cmd.Track!.Id != previous, $"seed {seed}: {previous} repeated back to back at {t}");
                    playing = cmd.Track;
                    playingSince = now;
                    previous = cmd.Track!.Id;
                    lastStart = now;
                    starts++;
                }
                else if (cmd.Action == MusicAction.Stop)
                {
                    playing = null;
                    endedAt = now;
                }
                Check(!(playing != null && !enabled && now - lastStart > 5f && cmd.Action == MusicAction.None && !enabled), $"seed {seed}: still playing while off at {t}");
            }
            Check(starts >= 6, $"seed {seed}: only {starts} tracks started in three days");
            Console.WriteLine($"seed {seed}: {starts} tracks in three game days");
        }

        // 3. The gap after a track ends is at least a minute.
        var d2 = new MusicDirector(defs, new Random(9));
        for (int i = 0; i < 400; i++)
        {
            d2.TrackEnded();
            Check(d2.GapLeft >= 60f, "gap after a track under a minute: " + d2.GapLeft);
        }

        // 4. Mood rules.
        Check(MusicDirector.MoodOf(new MusicContext(0.5f, 0f, 0f, 0f, true)) == Mood.Day, "noon is day");
        Check(MusicDirector.MoodOf(new MusicContext(0.75f, 0f, 0f, 0f, true)) == Mood.Dusk, "sunset is dusk");
        Check(MusicDirector.MoodOf(new MusicContext(0.95f, 0f, 0f, 0f, true)) == Mood.Night, "late evening is night");
        Check(MusicDirector.MoodOf(new MusicContext(0.5f, 0.9f, 0f, 0f, true)) == Mood.Rain, "rain by day is rain");
        Check(MusicDirector.MoodOf(new MusicContext(0.5f, 0.9f, 1f, 0f, true)) == Mood.Rest, "indoors is rest even in rain");
        Check(MusicDirector.MoodOf(new MusicContext(0.95f, 0f, 0f, 0.6f, true)) == Mood.Rest, "by a fire at night is rest");
        Check(MusicDirector.MoodOf(new MusicContext(0.5f, 0f, 0f, 0.6f, true)) == Mood.Day, "by a fire at noon is still day");

        Console.WriteLine(failures == 0 ? "ok: music director rules" : $"{failures} failure(s)");
        return failures == 0 ? 0 : 6;
    }
}
