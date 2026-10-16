// Renders the game's soundscape to a WAV file without running the game, so the
// ambience and music can be listened to and checked for level and clipping.
//
//   dotnet run -- out.wav [scene:seconds ...] [--no-music]
//
// Scenes: day, drizzle, light, steady, storm (with thunder), roof (steady rain heard
// from under a roof), rain (same as storm), fire (a campfire at night), firerain
// (a campfire in light rain), dusk, night, sheep (a grazing flock that
// bolts halfway; about a third are lambs), dawn (the dawn chorus), midday,
// evening (evening song thinning out), woods (a windy day among trees), meadow
// (the same wind over open grass), hut (steady rain heard from inside, walking
// on the floor), earth, clay (walking on bare earth or wet clay), approach (walking
// up to a pond's edge on a breezy day), pond (standing at the edge), pondrain
// (the same in steady rain), pondnight (at the edge at night, with frogs).
// Work scenes stand still: chop (chopping a log in the woods, then the tree falls
// right there), fellfar (trees felled 15 m and 40 m away), knap, drill (hand-drill
// fire), bowdrill, shape (shaping clay).
// Each scene walks for its first half, on grass unless the scene says otherwise.
// Default: day:40 rain:30 dusk:20 night:40
using LifeGame;

bool music = !args.Contains("--no-music");
args = args.Where(a => a != "--no-music").ToArray();
string path = args.Length > 0 ? args[0] : "soundscape.wav";
var scenes = args.Length > 1 ? args[1..] : new[] { "day:40", "rain:30", "dusk:20", "night:40" };
const int rate = 44100;
var mix = new SoundMix(rate, seed: 7) { MusicOn = music };
var samples = new List<short>();
var left = new float[1024];
var right = new float[1024];
double peak = 0;

foreach (var scene in scenes)
{
    var parts = scene.Split(':');
    float seconds = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 30f;
    (float day, float rain, float wind, float wet, float time) = parts[0] switch
    {
        "drizzle" => (0.85f, 0.2f, 0.3f, 0.4f, 0.45f),
        "light" => (0.8f, 0.4f, 0.45f, 0.7f, 0.45f),
        "steady" or "roof" or "hut" => (0.75f, 0.65f, 0.6f, 1f, 0.45f),
        "rain" or "storm" => (0.6f, 1f, 0.9f, 1f, 0.45f),
        "dusk" => (0.35f, 0f, 0.3f, 0.2f, 0.76f),
        "fire" => (0f, 0f, 0.2f, 0.1f, 0.9f),
        "firerain" => (0.1f, 0.4f, 0.4f, 0.7f, 0.85f),
        "night" => (0f, 0f, 0.25f, 0.1f, 0.95f),
        "dawn" => (0.55f, 0f, 0.15f, 0.3f, 0.27f),
        "midday" => (1f, 0f, 0.25f, 0f, 0.5f),
        "evening" => (0.6f, 0f, 0.2f, 0f, 0.73f),
        "woods" or "meadow" => (1f, 0f, 0.8f, 0f, 0.5f),
        "chop" or "fellfar" => (1f, 0f, 0.35f, 0f, 0.42f),
        "clay" => (0.9f, 0f, 0.3f, 0.8f, 0.4f),
        "approach" or "pond" => (1f, 0f, 0.45f, 0f, 0.42f),
        "pondrain" => (0.75f, 0.6f, 0.55f, 1f, 0.45f),
        "pondnight" => (0f, 0f, 0.2f, 0.1f, 0.93f),
        _ => (1f, 0f, 0.3f, 0f, 0.4f),
    };
    var ground = parts[0] switch { "hut" => Ground.Floor, "clay" => Ground.Clay, "earth" => Ground.Earth, _ => Ground.Grass };
    mix.Foliage = parts[0] is "woods" or "chop" or "fellfar" ? 1f : parts[0] == "meadow" ? 0f : 0.4f;
    bool working = parts[0] is "chop" or "fellfar" or "knap" or "drill" or "bowdrill" or "shape";
    var work = parts[0] switch
    {
        "chop" => Work.Chop, "knap" => Work.Knap, "drill" => Work.HandDrill, "bowdrill" => Work.BowDrill, "shape" => Work.Clay, _ => Work.None,
    };
    // Each job runs from 1 s in until 2 s before the scene ends; a chop ends with the tree falling.
    float jobSeconds = seconds - 3f;
    bool jobStarted = false, jobEnded = false;
    var falls = parts[0] switch
    {
        "chop" => new List<(double At, float Distance)> { (1.3 + jobSeconds, 3f) },
        "fellfar" => new List<(double At, float Distance)> { (2, 15f), (9, 40f) },
        _ => new List<(double At, float Distance)>(),
    };
    int total = (int)(seconds * rate), done = 0;
    double sum = 0, nextStep = 0, scenePeak = 0;
    while (done < total)
    {
        int n = Math.Min(1024, total - done);
        // Ease into each scene over a few seconds, as the game does.
        float k = 1f - MathF.Exp(-n / (float)rate / 3f);
        mix.Daylight += (day - mix.Daylight) * k;
        mix.Rain += (rain - mix.Rain) * k;
        mix.Wind += (wind - mix.Wind) * k;
        mix.Wetness += (wet - mix.Wetness) * k;
        mix.Cover = parts[0] is "roof" or "hut" ? 1f : 0f;
        mix.TimeOfDay += (time - mix.TimeOfDay) * k;
        mix.Fire = parts[0] is "fire" or "firerain" ? 0.85f : 0f;
        mix.FirePan = -0.3f;
        double t = done / (double)rate;
        // Ponds lie a little to the left; walking up to one, it grows from nothing.
        float water = parts[0] == "approach" ? (float)Math.Min(1.0, t / (seconds * 0.8)) : parts[0].StartsWith("pond") ? 1f : 0f;
        if (Environment.GetEnvironmentVariable("NO_WATER") != null) water = 0f;
        mix.Water = water * water;
        mix.WaterPan = -0.4f;
        bool walks = parts[0] == "approach" ? t < seconds * 0.8 : !parts[0].StartsWith("pond") && !working && t < seconds / 2;
        if (work != Work.None && !jobStarted && t >= 1) { mix.Working(work, jobSeconds); jobStarted = true; }
        if (work != Work.None && !jobEnded && t >= 1 + jobSeconds) { mix.Working(Work.None, 0f); jobEnded = true; }
        for (int f = falls.Count - 1; f >= 0; f--)
            if (t >= falls[f].At)
            {
                float d = falls[f].Distance;
                mix.Fell(SoundMix.FellGain(d), 0.5f, SoundMix.FellNear(d));
                falls.RemoveAt(f);
            }
        if (walks && t >= nextStep)
        {
            mix.Step(ground, ground == Ground.Floor ? 0f : mix.Wetness);
            nextStep = t + 0.36;
        }
        // Sheep scenes: a calm flock, then it bolts halfway through.
        if (parts[0] == "sheep" && Random.Shared.NextDouble() < n / (double)rate * 0.4)
            mix.Bleat((float)(Random.Shared.NextDouble() * 2 - 1), (float)(0.4 + 0.6 * Random.Shared.NextDouble()), false, Random.Shared.NextDouble() < 0.35);
        if (parts[0] == "sheep" && done < total / 2 && done + n >= total / 2)
            for (int b = 0; b < 4; b++) mix.Bleat((float)(Random.Shared.NextDouble() * 2 - 1), 0.9f, true, b == 3);
        // Storms: a strike every 10 seconds or so, some close, most far off.
        if (parts[0] is "storm" or "rain" && Random.Shared.NextDouble() < n / (double)rate * 0.1)
        {
            float near = (float)Math.Pow(Random.Shared.NextDouble(), 2);
            mix.Thunder(0.5f + (1f - near) * 5f, 0.4f + 0.6f * near, near);
        }
        mix.Render(left, right, n);
        for (int i = 0; i < n; i++)
        {
            scenePeak = Math.Max(scenePeak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
            peak = Math.Max(peak, scenePeak);
            sum += left[i] * left[i] + right[i] * right[i];
            samples.Add((short)Math.Clamp(left[i] * 32767f, -32768f, 32767f));
            samples.Add((short)Math.Clamp(right[i] * 32767f, -32768f, 32767f));
        }
        done += n;
    }
    Console.WriteLine($"{parts[0],-6} {seconds,4}s  rms {20 * Math.Log10(Math.Sqrt(sum / (2.0 * total)) + 1e-9):F1} dBFS  peak {20 * Math.Log10(scenePeak + 1e-9):F1}");
}
Console.WriteLine($"peak {20 * Math.Log10(peak + 1e-9):F1} dBFS");

using var file = new BinaryWriter(File.Create(path));
int bytes = samples.Count * 2;
file.Write("RIFF"u8);
file.Write(36 + bytes);
file.Write("WAVEfmt "u8);
file.Write(16);
file.Write((short)1);
file.Write((short)2);
file.Write(rate);
file.Write(rate * 4);
file.Write((short)4);
file.Write((short)16);
file.Write("data"u8);
file.Write(bytes);
foreach (var s in samples) file.Write(s);
Console.WriteLine($"wrote {path}");
