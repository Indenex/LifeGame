// Renders the game's soundscape to a WAV file without running the game, so the
// ambience and music can be listened to and checked for level and clipping.
//
//   dotnet run -- out.wav [scene:seconds ...] [--no-music] [--master=0.5 --music= --effects= --ambience=]
//
// Scenes: day, drizzle, light, steady, storm (with thunder), roof (steady rain heard
// from under a roof), rain (same as storm), fire (a campfire at night), firerain
// (a campfire in light rain), dusk, night, sheep (a grazing flock that
// bolts halfway; about a third are lambs), dawn (the dawn chorus), midday,
// evening (evening song thinning out), woods (a windy day among trees), meadow
// (the same wind over open grass), hut (steady rain heard from inside, walking
// on the floor), earth, clay (walking on bare earth or wet clay), sand, shallows,
// snow (walking on a stream bank, through shallow water, through snow), approach (walking
// up to a pond's edge on a breezy day), pond (standing at the edge), pondrain
// (the same in steady rain), pondnight (at the edge at night, with frogs),
// stream (standing on a stream bank), streamwalk (walking up to one).
// Seasons (audio-2): spring, summer, autumn, winter (snow on the ground), each a
// breezy morning in light woods; add "night" for the same at night (summernight).
// Work scenes stand still on a calm morning and work for all but the last second:
// knap, drill, fibre, lash, pat (clay), rasp, carve; add "-fail" to end in a snap
// (knap-fail). others: you knap while another player drills 8 m to your left
// and a third carves 20 m to the right. still: the same calm morning with no work,
// the bed the work scenes are measured against.
// cues: each feedback sound (Cue: Pickup, Eat, Drink, SkillUp, Learn, FireLit,
// Place) in turn, 2.5 s apart, standing on a calm morning.
// Each scene walks for its first half, on grass unless the scene says otherwise.
// Default: day:40 rain:30 dusk:20 night:40
using LifeGame;

// Soak test (audio-hotfix-1): hours of game time of random weather, seasons and events
// through the mix, checking every block stays finite and nothing throws.
//   dotnet run -c Release -- --music-test (the recorded-music director's rules, with fake tracks)
//   dotnet run -c Release -- --edges      (every setting at its limits; must report no faults)
//   dotnet run -c Release -- --soak=6 [--seed=1] [--fuzz] [--inject]    (6 game hours)
// --fuzz hands the mix NaN, infinite and out-of-range inputs; --inject makes bad blocks and
// exceptions on purpose to prove the mix recovers.
if (args.Contains("--edges")) Environment.Exit(Soak.Edges());
if (args.Contains("--music-test")) Environment.Exit(MusicTest.Run());
if (args.FirstOrDefault(a => a.StartsWith("--soak=")) is { } soakArg)
{
    double hours = double.Parse(soakArg[7..], System.Globalization.CultureInfo.InvariantCulture);
    int soakSeed = args.FirstOrDefault(a => a.StartsWith("--seed=")) is { } sa ? int.Parse(sa[7..]) : 1;
    Environment.Exit(Soak.Run(hours, soakSeed, args.Contains("--fuzz"), args.Contains("--inject")));
}

bool music = !args.Contains("--no-music");
// Slider positions, 0 to 1 (0.5 default): --master=0.7 --music= --effects= --ambience=
float Slider(string name) => args.FirstOrDefault(a => a.StartsWith($"--{name}=")) is { } a
    ? float.Parse(a[(name.Length + 3)..], System.Globalization.CultureInfo.InvariantCulture) : 0.5f;
float master = Slider("master"), musicLevel = Slider("music"), effects = Slider("effects"), ambienceLevel = Slider("ambience");
args = args.Where(a => !a.StartsWith("--")).ToArray();
string path = args.Length > 0 ? args[0] : "soundscape.wav";
var scenes = args.Length > 1 ? args[1..] : new[] { "day:40", "rain:30", "dusk:20", "night:40" };
const int rate = 44100;
var mix = new SoundMix(rate, seed: 7) { MusicOn = music, MasterLevel = master, MusicLevel = musicLevel, EffectsLevel = effects, AmbienceLevel = ambienceLevel };
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
        "rain" or "storm" or "crack" or "stormroof" => (0.6f, 1f, 0.9f, 1f, 0.45f),
        "dusk" => (0.35f, 0f, 0.3f, 0.2f, 0.76f),
        "fire" => (0f, 0f, 0.2f, 0.1f, 0.9f),
        "firestoked" => (0f, 0f, 0.2f, 0.1f, 0.9f),
        "firerain" => (0.1f, 0.4f, 0.4f, 0.7f, 0.85f),
        "night" => (0f, 0f, 0.25f, 0.1f, 0.95f),
        "dawn" => (0.55f, 0f, 0.15f, 0.3f, 0.27f),
        "midday" => (1f, 0f, 0.25f, 0f, 0.5f),
        "evening" => (0.6f, 0f, 0.2f, 0f, 0.73f),
        "woods" or "meadow" => (1f, 0f, 0.8f, 0f, 0.5f),
        "clay" => (0.9f, 0f, 0.3f, 0.8f, 0.4f),
        "approach" or "pond" or "stream" or "streamwalk" => (1f, 0f, 0.45f, 0f, 0.42f),
        "pondrain" => (0.75f, 0.6f, 0.55f, 1f, 0.45f),
        "pondnight" => (0f, 0f, 0.2f, 0.1f, 0.93f),
        "spring" or "summer" or "autumn" or "winter" => (1f, 0f, 0.45f, 0f, 0.36f),
        "springnight" or "summernight" or "autumnnight" or "winternight" => (0f, 0f, 0.25f, 0.1f, 0.95f),
        _ => (1f, 0f, 0.3f, 0f, 0.4f),
    };
    string name = parts[0].Replace("-fail", "");
    var work = SoundMix.WorkFor(name switch
    {
        "knap" => "knap.flake", "drill" => "fire.hand_drill", "fibre" => "cord.twist", "lash" => "lash.spear",
        "pat" => "clay.brick", "rasp" => "hide.scrape", "carve" => "carve.digging_stick", _ => "",
    });
    if (work != Work.None || name == "still") (day, rain, wind, wet, time) = (0.9f, 0f, 0.15f, 0f, 0.33f);
    if (name == "others")
    {
        (work, day, rain, wind, wet, time) = (Work.Knap, 0.9f, 0f, 0.15f, 0f, 0.33f);
        mix.OtherWorking(2, Work.Drill);
        mix.OtherPlace(2, 0.4f * 0.93f, -0.8f);
        mix.OtherWorking(3, Work.Carve);
        mix.OtherPlace(3, 0.4f * 0.34f, 0.9f);
    }
    // cues: each feedback sound in turn on a calm morning, one every 2.5 s.
    var cueList = Enum.GetValues<Cue>();
    if (name == "cues") (day, rain, wind, wet, time) = (0.9f, 0f, 0.15f, 0f, 0.33f);
    mix.Working(work);
    bool workEnded = false;
    var ground = parts[0] switch { "hut" => Ground.Floor, "clay" => Ground.Clay, "earth" => Ground.Earth, "sand" => Ground.Sand, "shallows" => Ground.Water, "snow" or "winter" => Ground.Snow, _ => Ground.Grass };
    mix.Foliage = parts[0] == "woods" ? 1f : parts[0] == "meadow" ? 0f : 0.4f;
    // Season scenes: a breezy morning (or night) in light woods at the middle of
    // that season; winter has snow on the ground. Other scenes are early summer.
    string seasonName = parts[0] == "deer" ? "deer" : parts[0] == "snow" ? "winter" : parts[0].Replace("night", "");
    if (parts[0] == "deer") (day, time) = (0.35f, 0.78f);
    mix.Year = seasonName switch { "deer" => 0.625f, "spring" => 0.125f, "summer" => 0.375f, "autumn" => 0.625f, "winter" => 0.875f, _ => 0.3f };
    mix.SnowCover = seasonName == "winter" ? 0.8f : 0f;
    if (seasonName is "spring" or "summer" or "autumn" or "winter") mix.Foliage = 0.7f;
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
        mix.Cover = parts[0] is "roof" or "hut" or "stormroof" ? 1f : 0f;
        mix.TimeOfDay += (time - mix.TimeOfDay) * k;
        mix.Fire = parts[0] is "fire" or "firerain" or "firestoked" ? 0.85f : 0f;
    mix.FireStoke = parts[0] == "firestoked" ? 1f : 0f;
        mix.FirePan = -0.3f;
        double t = done / (double)rate;
        // Ponds lie a little to the left; walking up to one, it grows from nothing.
        float water = parts[0] == "approach" ? (float)Math.Min(1.0, t / (seconds * 0.8)) : parts[0].StartsWith("pond") ? 1f : 0f;
        if (Environment.GetEnvironmentVariable("NO_WATER") != null) water = 0f;
        // Streams lie a little to the right; streamwalk walks up to the bank.
        float stream = parts[0] == "stream" ? 1f : parts[0] == "streamwalk" ? (float)Math.Min(1.0, t / (seconds * 0.8)) : 0f;
        mix.Stream = stream * stream;
        mix.StreamPan = 0.4f;
        mix.Water = water * water;
        mix.WaterPan = -0.4f;
        bool walks = parts[0] is "approach" or "streamwalk" ? t < seconds * 0.8 : !parts[0].StartsWith("pond") && parts[0] != "still" && parts[0] != "cues" && parts[0] != "water" && parts[0] != "hens" && parts[0] != "tools" && work == Work.None && t < seconds / 2;
        if (work != Work.None && !workEnded && t >= seconds - 1)
        {
            mix.Working(Work.None);
            mix.WorkDone(work, parts[0].EndsWith("-fail"));
            mix.OtherWorking(2, Work.None);
            mix.OtherWorkDone(2, Work.Drill, false);
            mix.OtherWorking(3, Work.None);
            workEnded = true;
        }
        if (name == "cues")
        {
            int idx = (int)(t / 2.5), prev = (int)((t - n / (double)rate) / 2.5);
            if (t > 0 && idx != prev && idx - 1 < cueList.Length) mix.Play(cueList[idx - 1]);
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
        // Jump scene: hop on grass, a big jump on grass, then a jump on sand.
        if (parts[0] == "jump")
        {
            double a = done / (double)rate, b = (done + n) / (double)rate;
            bool At(double x) => a < x && b >= x;
            if (At(1)) mix.Play(Cue.Jump);
            if (At(1.65)) mix.Land(Ground.Grass, 0.2f);
            if (At(4)) mix.Play(Cue.Jump);
            if (At(4.65)) mix.Land(Ground.Grass, 1f);
            if (At(7)) mix.Play(Cue.Jump);
            if (At(7.65)) mix.Land(Ground.Sand, 0.6f);
            if (At(10)) mix.Step(Ground.Grass, 0f);
        }
        // Water scene: wading ankle, knee and waist deep, a splash in (small, big), a paddle stroke, a lap.
        if (parts[0] == "water")
        {
            double a = done / (double)rate, b = (done + n) / (double)rate;
            bool At(double x) => a < x && b >= x;
            if (At(1)) mix.Play(CueDefs.WadeShallow);
            if (At(2.5)) mix.Play(CueDefs.WadeKnee);
            if (At(4)) mix.Play(CueDefs.WadeDeep);
            if (At(6)) mix.Play(CueDefs.SplashIn, 0.5f);
            if (At(8)) mix.Play(CueDefs.SplashIn, 1.2f);
            if (At(10)) mix.Play(CueDefs.Paddle);
            if (At(12)) mix.Play(CueDefs.Lap);
            if (At(14)) mix.Play(CueDefs.Drip);
        }
        // Hens scene: pecks, a cluck, a run of clucks, a squawk with a flap, a roosting murmur.
        if (parts[0] == "hens")
        {
            double a = done / (double)rate, b = (done + n) / (double)rate;
            bool At(double x) => a < x && b >= x;
            if (At(1)) mix.Play(CueDefs.HenPeck);
            if (At(1.7)) mix.Play(CueDefs.HenPeck);
            if (At(3)) mix.Play(CueDefs.HenCluck);
            if (At(5)) mix.Play(CueDefs.HenClucks);
            if (At(7.5)) mix.Play(CueDefs.HenSquawk);
            if (At(10)) mix.Play(CueDefs.HenRoost);
            if (At(12.5)) mix.Play(CueDefs.HenLay);
        }
        // Tools scene: three chops, a creak and the crash, a stick dig and a hand dig.
        if (parts[0] == "tools")
        {
            double a = done / (double)rate, b = (done + n) / (double)rate;
            bool At(double x) => a < x && b >= x;
            if (At(1)) mix.Play(CueDefs.ChopThunk);
            if (At(2.2)) mix.Play(CueDefs.ChopThunk);
            if (At(3.4)) mix.Play(CueDefs.ChopThunk);
            if (At(5)) mix.Play(CueDefs.TreeCreak);
            if (At(5.95)) mix.Play(CueDefs.TreeCrash);
            if (At(10)) mix.Play(CueDefs.DigStick);
            if (At(11.5)) mix.Play(CueDefs.DigSpade);
            if (At(12.5)) mix.Play(CueDefs.DirtThrow);
            if (At(14)) mix.Play(CueDefs.DigHands);
        }
        // Acts scene: drink, eat, pluck, pick up, 4 s apart.
        if (parts[0] == "acts")
        {
            double a = done / (double)rate, b = (done + n) / (double)rate;
            bool At(double x) => a < x && b >= x;
            if (At(1)) mix.Play(CueDefs.Drink);
            if (At(5)) mix.Play(CueDefs.Eat);
            if (At(8)) mix.Play(CueDefs.Pluck);
            if (At(10)) mix.Play(CueDefs.PickUp);
        }
        // Crack scene: close strikes, then a far one, in a storm.
        if (parts[0] == "crack")
        {
            double a = done / (double)rate, b = (done + n) / (double)rate;
            bool At(double x) => a < x && b >= x;
            if (At(2)) mix.Thunder(0.3f, 1f, 0.95f);
            if (At(9)) mix.Thunder(0.8f, 0.8f, 0.7f);
            if (At(16)) mix.Thunder(4f, 0.6f, 0.1f);
        }
        // Deer scene (autumn dusk): bark close, bark far, roar far, roar near, then a herd bolting.
        if (parts[0] == "deer")
        {
            double a = done / (double)rate, b = (done + n) / (double)rate;
            bool At(double x) => a < x && b >= x;
            if (At(1.5)) mix.DeerCry(SoundMix.DeerCall.Bark, -0.4f, 0.9f, 0f);
            if (At(4)) mix.DeerCry(SoundMix.DeerCall.Bark, 0.7f, 0.4f, 0.5f);
            if (At(6)) mix.DeerCry(SoundMix.DeerCall.Roar, -0.6f, 0.35f, 0.9f);
            if (At(12)) mix.DeerCry(SoundMix.DeerCall.Roar, 0.3f, 0.8f, 0.3f);
            if (a >= 18 && a < 22 && Math.Floor(a / 0.28) != Math.Floor(b / 0.28))
                mix.DeerCry(SoundMix.DeerCall.Hoof, (float)(Random.Shared.NextDouble() * 1.4 - 0.7), (float)(0.5 + 0.4 * Random.Shared.NextDouble()), 0.1f);
            if (At(18)) mix.DeerCry(SoundMix.DeerCall.Bark, 0f, 1f, 0f);
        }
        // Storms: a strike every 10 seconds or so, some close, most far off.
        if (parts[0] is "storm" or "rain" && Random.Shared.NextDouble() < n / (double)rate * 0.1)
        {
            float near = (float)Math.Pow(Random.Shared.NextDouble(), 2);
            mix.Thunder(0.5f + (1f - near) * 5f, 0.4f + 0.6f * near, near);
        }
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
