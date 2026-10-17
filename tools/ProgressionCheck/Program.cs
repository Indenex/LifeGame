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
    if (e.Shape == EffectShape.EvenInHits)
    {
        // 1 / value is linear in rank: equal steps of rank give equal steps of 1 / value.
        float a = 1f / Progression.Effect(e.Kind, 0), b = 1f / Progression.Effect(e.Kind, 20), c = 1f / Progression.Effect(e.Kind, 40);
        if (MathF.Abs((a - b) - (b - c)) > 0.001f) { Console.WriteLine("  FAIL: not even in hits"); failures++; }
        Console.WriteLine("  even-in-hits at 0/20/40/60/80/100: " + string.Join(" / ", new[] { 0, 20, 40, 60, 80, 100 }.Select(r => Progression.Effect(e.Kind, r).ToString("0.00"))));
    }
    else if (share < 0.38f || share > 0.42f) { Console.WriteLine($"  FAIL: rank 15 gives {share:P0}, want ~40%"); failures++; }
    for (int i = 1; i < vals.Length; i++)
        if ((vals[i] - vals[i - 1]) * (e.Hi - e.Lo) < 0) { Console.WriteLine("  FAIL: not monotonic"); failures++; }
}
Console.WriteLine($"QualityCap 0/15/50/100: {Skills.QualityCap(0)}/{Skills.QualityCap(15)}/{Skills.QualityCap(50)}/{Skills.QualityCap(100)}");
Console.WriteLine($"Fires.MaxSecondsFor 0/100: {Fires.MaxSecondsFor(0)}/{Fires.MaxSecondsFor(100)}");
if (Skills.QualityCap(0) != 35 || Skills.QualityCap(100) != 100) failures++;
if (Skills.SpecFor(Skills.Kill)?.Id != "stalking" || Skills.SpecFor(Skills.Sneak)?.Id != "stalking") { Console.WriteLine("FAIL: stalking mapping"); failures++; }
Console.WriteLine($"First gain: general {Skills.Gain(0, false, "toolmaking")}, stalking {Skills.Gain(0, false, "stalking")}, archery {Skills.Gain(0, false, "archery")} points");
if (Skills.Gain(0, false, "archery") >= Skills.Gain(0, false, "toolmaking")) { Console.WriteLine("FAIL: combat not slower"); failures++; }
if (Skills.Gain(0, false, "swimming") >= Skills.Gain(0, false, "toolmaking")) { Console.WriteLine("FAIL: swimming not slow"); failures++; }
if (Progression.Pace != 1f) Console.WriteLine($"Pace {Progression.Pace}: first gain {Skills.Gain(0, false)} points");
if (Skills.SpecFor(Wading.WadeProcess)?.Id != "swimming" || Skills.SpecFor(Wading.FloatProcess)?.Id != "swimming" || Skills.SpecFor(Skills.Repair)?.Id != "toolmaking" || Skills.SpecFor(Skills.Run)?.Id != "endurance") { Console.WriteLine("FAIL: swimming/repair mapping"); failures++; }
if (Wading.SpeedFactor(0.4f) != Wading.SpeedFactor(0.4f, 0) || Wading.SpeedFactor(2f, 100) <= Wading.SpeedFactor(2f, 0)) { Console.WriteLine("FAIL: wading rank effect"); failures++; }
if (Skills.SpecFor(Skills.Shoot)?.Id != "archery") { Console.WriteLine("FAIL: archery mapping"); failures++; }
Console.WriteLine(failures == 0 ? "OK" : $"{failures} failure(s)");
return failures == 0 ? 0 : 1;
