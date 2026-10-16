// Prints every skill effect at ranks 0, 15, 50 and 100 and checks the shared curve:
// start and end values unchanged, effects move monotonically, and the first 15% of
// ranks give about 40% of the benefit. Exit code 1 on any failure.
using LifeGame.Shared;

int[] ranks = { 0, 10, 15, 25, 50, 100 };
int failures = 0;
Console.WriteLine($"{"effect",-22}" + string.Join("", ranks.Select(r => $"{"r" + r,10}")));
foreach (var e in Progression.Effects)
{
    var vals = ranks.Select(r => Progression.Effect(e.Kind, r)).ToArray();
    Console.WriteLine($"{e.Name,-22}" + string.Join("", vals.Select(v => $"{v,10:0.##}")) + $"  {e.Unit}");
    if (MathF.Abs(vals[0] - e.Lo) > 0.001f || MathF.Abs(vals[^1] - e.Hi) > 0.001f) { Console.WriteLine("  FAIL: ends"); failures++; }
    float share = (vals[2] - e.Lo) / (e.Hi - e.Lo);
    if (share < 0.38f || share > 0.42f) { Console.WriteLine($"  FAIL: rank 15 gives {share:P0}, want ~40%"); failures++; }
    for (int i = 1; i < vals.Length; i++)
        if ((vals[i] - vals[i - 1]) * (e.Hi - e.Lo) < 0) { Console.WriteLine("  FAIL: not monotonic"); failures++; }
}
Console.WriteLine($"QualityCap 0/15/50/100: {Skills.QualityCap(0)}/{Skills.QualityCap(15)}/{Skills.QualityCap(50)}/{Skills.QualityCap(100)}");
Console.WriteLine($"Fires.MaxSecondsFor 0/100: {Fires.MaxSecondsFor(0)}/{Fires.MaxSecondsFor(100)}");
if (Skills.QualityCap(0) != 35 || Skills.QualityCap(100) != 100) failures++;
if (Skills.SpecFor(Skills.Kill)?.Id != "stalking" || Skills.SpecFor(Skills.Sneak)?.Id != "stalking") { Console.WriteLine("FAIL: stalking mapping"); failures++; }
if (Progression.Pace != 1f) Console.WriteLine($"Pace {Progression.Pace}: first gain {Skills.Gain(0, false)} points");
if (Skills.SpecFor(Skills.Shoot)?.Id != "archery") { Console.WriteLine("FAIL: archery mapping"); failures++; }
Console.WriteLine(failures == 0 ? "OK" : $"{failures} failure(s)");
return failures == 0 ? 0 : 1;
