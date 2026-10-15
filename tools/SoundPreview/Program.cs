// Renders the game's soundscape to a WAV file without running the game, so the
// ambience and music can be listened to and checked for level and clipping.
//
//   dotnet run -- out.wav [scene:seconds ...] [--no-music]
//
// Scenes: day, rain, dusk, night, sheep (a grazing flock that bolts halfway). Each scene walks on grass for its first half.
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
    (float day, float rain, float wind, float wet) = parts[0] switch
    {
        "rain" => (0.8f, 1f, 0.8f, 1f),
        "dusk" => (0.35f, 0f, 0.3f, 0.2f),
        "night" => (0f, 0f, 0.25f, 0.1f),
        _ => (1f, 0f, 0.3f, 0f),
    };
    int total = (int)(seconds * rate), done = 0;
    double sum = 0, nextStep = 0;
    while (done < total)
    {
        int n = Math.Min(1024, total - done);
        // Ease into each scene over a few seconds, as the game does.
        float k = 1f - MathF.Exp(-n / (float)rate / 3f);
        mix.Daylight += (day - mix.Daylight) * k;
        mix.Rain += (rain - mix.Rain) * k;
        mix.Wind += (wind - mix.Wind) * k;
        mix.Wetness += (wet - mix.Wetness) * k;
        double t = done / (double)rate;
        if (t < seconds / 2 && t >= nextStep)
        {
            mix.Step(Ground.Grass, mix.Wetness);
            nextStep = t + 0.36;
        }
        // Sheep scenes: a calm flock, then it bolts halfway through.
        if (parts[0] == "sheep" && Random.Shared.NextDouble() < n / (double)rate * 0.4)
            mix.Bleat((float)(Random.Shared.NextDouble() * 2 - 1), (float)(0.4 + 0.6 * Random.Shared.NextDouble()), false);
        if (parts[0] == "sheep" && done < total / 2 && done + n >= total / 2)
            for (int b = 0; b < 4; b++) mix.Bleat((float)(Random.Shared.NextDouble() * 2 - 1), 0.9f, true);
        mix.Render(left, right, n);
        for (int i = 0; i < n; i++)
        {
            peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
            sum += left[i] * left[i] + right[i] * right[i];
            samples.Add((short)Math.Clamp(left[i] * 32767f, -32768f, 32767f));
            samples.Add((short)Math.Clamp(right[i] * 32767f, -32768f, 32767f));
        }
        done += n;
    }
    Console.WriteLine($"{parts[0],-6} {seconds,4}s  rms {20 * Math.Log10(Math.Sqrt(sum / (2.0 * total)) + 1e-9):F1} dBFS");
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
