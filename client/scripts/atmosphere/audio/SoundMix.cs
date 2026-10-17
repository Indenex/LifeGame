using System;
using System.Collections.Generic;

namespace LifeGame;

public enum Ground { Grass, Earth, Water, Clay, Floor, Sand, Snow }

// Short sounds for things that just happened to you (SoundMix.Cue, audio-3).
public enum Cue { Pickup, Eat, Drink, SkillUp, Learn, FireLit, Place, Hit, Crit, Jump, BowDraw, BowShot, ArrowFlesh, ArrowWood, ArrowEarth }

// What your hands are doing, for the work sounds (SoundMix.Working).
public enum Work { None, Knap, Drill, Fibre, Lash, Clay, Rasp, Carve }

// The whole soundscape, synthesized as it plays: wind, rain, birds by day,
// crickets and owls by night, footsteps, and a slow generative score in a
// forest-sized reverb. Plain C# with no engine types, so tools/SoundPreview can
// render it to a file without the game.
public sealed class SoundMix
{
    public readonly int Rate;

    // Set by the atmosphere every frame. All run from 0 to 1.
    public float Daylight = 1f, Rain, Wind = 0.3f, Wetness;
    // 1 when the listener is under a roof. Everything outdoors is heard through
    // the walls, duller and quieter, easing over about a second.
    public float Cover;
    // 0 midnight, 0.25 sunrise, 0.5 noon, 0.75 sunset: when the birds sing.
    public float TimeOfDay = 0.5f;
    // How wooded it is around the listener, 0 open grass to 1 deep woods: how
    // much of the wind is heard in leaves rather than grass.
    public float Foliage;
    // The nearest campfires: how loud (0 to 1, from distance and how well it burns)
    // and where (pan -1 left to 1 right).
    public float Fire, FirePan;
    // Open water nearby: how close and how much of it (0 none, 1 at the edge of a
    // pond), and which side it lies (pan -1 left to 1 right).
    public float Water, WaterPan;
    // A running stream nearby, the same way: how close (0 none, 1 at its bank) and
    // which side (audio-2, env-2's streams).
    public float Stream, StreamPan;
    // How far through the year it is, 0 to 1 (Seasons.YearFraction): 0 the first day
    // of spring, 0.25 summer, 0.5 autumn, 0.75 winter. Sets which creatures are
    // about, what the trees have on them and how cold the wind sounds (audio-2).
    public float Year = 0.3f;
    // Snow lying on the ground, 0 to 1: the grass goes quiet under it.
    public float SnowCover;
    public bool MusicOn = true;
    // The synthesized score. Off since audio-8: the game plays recorded music (MusicPlayer).
    public bool SynthMusic;
    // The sound sliders (Options page, D-097), each 0 to 1. 0.5, where every new
    // player starts, is the mix as designed; see SliderGain. Effects are your
    // footsteps and everyone's work; ambience is weather, wildlife, sheep, fire and
    // water. In the game the master slider works on Godot's Master bus instead, so
    // MasterLevel stays 0.5 there; SoundPreview uses it.
    public float MasterLevel = 0.5f, MusicLevel = 0.5f, EffectsLevel = 0.5f, AmbienceLevel = 0.5f;
    // Master level. Raised from 1.3 (audio-1): Justin heard the mix as quiet.
    public float Volume = 1.7f;

    Noise noise = null!;
    WindVoice wind;
    FoliageVoice foliage;
    RainVoice rain;
    ThunderVoice thunder;
    FireVoice fire;
    Birds birds;
    NightVoices night;
    WaterVoice water;
    Frogs frogs;
    DayInsects insects;
    Cues cues;
    StreamVoice stream;
    Deer deer;
    Steps steps;
    WorkVoice work;
    // Other players at work nearby, by player id (D-072).
    readonly Dictionary<uint, WorkVoice> others = new();
    int seed;
    readonly List<uint> quiet = new();
    Sheep sheep;
    Music music;
    Reverb reverb;
    float[] sendL = Array.Empty<float>(), sendR = Array.Empty<float>();
    float[] outL = Array.Empty<float>(), outR = Array.Empty<float>();
    float[] stepL = Array.Empty<float>(), stepR = Array.Empty<float>();
    float[] workL = Array.Empty<float>(), workR = Array.Empty<float>();
    // How loud the surroundings are (smoothed RMS), so footsteps sit just above them.
    float ambience = 0.01f;
    // Slider gains, eased so a dragged slider never clicks.
    float masterGain = 1f, effectsGain = 1f, ambienceGain = 1f;
    Lowpass wallL, wallR;
    float cover;

    public SoundMix(int rate, int seed = 1)
    {
        Rate = rate;
        this.seed = seed;
        Build();
    }

    // Makes every voice and filter from scratch. Called at the start, and again if the
    // output ever goes non-finite, so one bad value can never silence the game for good.
    void Build()
    {
        var rate = Rate;
        var rng = new Random(seed);
        noise = new Noise((uint)seed * 2654435761u + 1u);
        wind = new WindVoice(rate, noise);
        foliage = new FoliageVoice(rate, noise);
        rain = new RainVoice(rate, noise, rng);
        thunder = new ThunderVoice(rate, noise, rng);
        fire = new FireVoice(rate, noise, rng);
        birds = new Birds(rate, rng);
        night = new NightVoices(rate, rng);
        water = new WaterVoice(rate, rng);
        frogs = new Frogs(rate, rng);
        insects = new DayInsects(rate, noise, rng);
        cues = new Cues(rate, noise, rng);
        stream = new StreamVoice(rate, noise, rng);
        deer = new Deer(rate, noise, rng);
        steps = new Steps(rate, noise, rng);
        work = new WorkVoice(rate, noise, rng);
        sheep = new Sheep(rate, rng);
        music = new Music(rate, rng);
        reverb = new Reverb(rate);
        others.Clear();
        quiet.Clear();
        wallL = default;
        wallR = default;
        cover = 0f;
        ambience = 0.01f;
    }

    // Reports a fault in the synth (an exception, or a block that went non-finite and
    // was silenced and rebuilt). The game logs it; SoundPreview's soak test prints it.
    public static Action<string>? Fault;
    public int Faults { get; private set; }
    // For the soak test: make the next block bad, to prove the mix recovers.
    internal bool InjectNaN, InjectThrow;

    void Report(string what)
    {
        Faults++;
        if (Faults <= 5) Fault?.Invoke(what);
    }

    static float Fin(float x, float fallback = 0f) => float.IsFinite(x) ? x : fallback;
    static float Unit(float x) => Math.Clamp(Fin(x), 0f, 1f);
    static float Side(float x) => Math.Clamp(Fin(x), -1f, 1f);

    // Inputs come from the game; a NaN or a wild value must not get into a filter.
    void Sane()
    {
        Daylight = Unit(Daylight); Rain = Unit(Rain); Wind = Unit(Wind); Wetness = Unit(Wetness); Cover = Unit(Cover);
        TimeOfDay = Fin(TimeOfDay, 0.5f); TimeOfDay -= MathF.Floor(TimeOfDay);
        Foliage = Unit(Foliage); Fire = Unit(Fire); FirePan = Side(FirePan);
        Water = Unit(Water); WaterPan = Side(WaterPan); Stream = Unit(Stream); StreamPan = Side(StreamPan);
        Year = Fin(Year, 0.3f); SnowCover = Unit(SnowCover);
        MasterLevel = Unit(MasterLevel); MusicLevel = Unit(MusicLevel); EffectsLevel = Unit(EffectsLevel); AmbienceLevel = Unit(AmbienceLevel);
    }

    static bool Finite(float[] a, int n)
    {
        for (int i = 0; i < n; i++) if (!float.IsFinite(a[i])) return false;
        return true;
    }

    // How hard the wind is gusting right now, 0 to 1, so grass and trees can move
    // with what is heard. Written by the synth thread; a stale read is harmless.
    public float Gust => wind.Gust;

    public void Step(Ground ground, float wetness) => steps.Trigger(ground, Unit(wetness));

    // Coming down from a jump: both feet at once on whatever is underfoot, heavier
    // and lower the harder the landing (strength 0 to 1).
    public void Land(Ground ground, float strength, float gain = 1f, float pan = 0f)
    {
        strength = Unit(strength); gain = Math.Clamp(Fin(gain), 0f, 4f); pan = Side(pan);
        steps.Trigger(ground, Wetness, (1.1f + 0.9f * strength) * gain, 0.85f - 0.2f * strength, Math.Clamp(pan - 0.2f, -1f, 1f));
        steps.Trigger(ground, Wetness, (1.0f + 0.8f * strength) * gain, 0.95f - 0.2f * strength, Math.Clamp(pan + 0.2f, -1f, 1f));
    }

    // Something that just happened to you: picked something up, ate, drank, got
    // better at a skill, learned something, a fire caught, set something down.
    // gain and pan (-1 left to 1 right) place it in the world, e.g. another player's jump.
    public void Play(CueDef def, float gain = 1f, float pan = 0f) => cues.Trigger(def, Math.Clamp(Fin(gain), 0f, 4f), Side(pan));

    public void Play(Cue cue, float gain = 1f, float pan = 0f)
    {
        gain = Math.Clamp(Fin(gain), 0f, 4f);
        pan = Side(pan);
        // Cues that have been converted to data play from their CueDef.
        if (CueDefs.ForCue(cue) is { } def) cues.Trigger(def, gain, pan);
        else cues.Trigger(cue, gain, pan);
    }

    // The work in your hands: its sound runs until Working(Work.None). WorkDone
    // closes it with a thunk, a snap if it failed, or a breath of flame for a drill.
    public void Working(Work kind) => work.Set(kind);
    public void WorkDone(Work kind, bool failed) => work.Done(kind, failed);

    // The same for another player's work, placed by OtherPlace: gain from distance
    // (already well under your own) and pan from -1 (left) to 1 (right).
    public void OtherWorking(uint player, Work kind)
    {
        if (!others.TryGetValue(player, out var voice))
        {
            if (kind == Work.None) return;
            // Silent until OtherPlace says where they are.
            others[player] = voice = new WorkVoice(Rate, noise, new Random(unchecked((int)player * 7919 + 13))) { Gain = 0f };
        }
        voice.Set(kind);
    }

    public void OtherWorkDone(uint player, Work kind, bool failed)
    {
        if (others.TryGetValue(player, out var voice)) voice.Done(kind, failed);
    }

    public void OtherPlace(uint player, float gain, float pan)
    {
        if (!others.TryGetValue(player, out var voice)) return;
        voice.Gain = Math.Clamp(Fin(gain), 0f, 4f);
        voice.Side = Side(pan);
    }

    // Which sound a craft process makes, from its id ("knap.flake" -> Knap).
    public static Work WorkFor(string processId) => processId switch
    {
        "hide.scrape" => Work.Rasp,
        _ => processId.Split('.')[0] switch
        {
            "knap" => Work.Knap,
            "fire" => Work.Drill,
            "cord" or "weave" or "gather" => Work.Fibre,
            "lash" or "haft" or "hide" => Work.Lash,
            "clay" => Work.Clay,
            "carve" => Work.Carve,
            _ => Work.None,
        },
    };

    // A sheep somewhere around the listener: pan from -1 (left) to 1 (right),
    // gain from distance, startled when the flock bolts.
    public void Bleat(float pan, float gain, bool startled, bool lamb = false) => sheep.Trigger(Side(pan), Math.Clamp(Fin(gain), 0f, 4f), startled, lamb);

    // A deer somewhere around the listener (pan -1 to 1, gain from distance, far 0 to 1).
    public void DeerCry(DeerCall call, float pan, float gain, float far) => deer.Trigger(call, Side(pan), Math.Clamp(Fin(gain), 0f, 4f), Unit(far));

    // Thunder after a flash: delay in seconds (sound is slower than light), gain
    // from 0 to 1, near from 0 (a distant rumble) to 1 (a crack overhead).
    public void Thunder(float delay, float gain, float near) => thunder.Trigger(Math.Clamp(Fin(delay), 0f, 30f), Math.Clamp(Fin(gain), 0f, 2f), Unit(near));

    // Never throws and never returns a non-finite sample: a fault is reported once, the
    // block is silenced and the whole mix is rebuilt, so the sound comes back by itself.
    public void Render(float[] left, float[] right, int count)
    {
        Sane();
        string stage = "start";
        try
        {
            RenderCore(left, right, count, ref stage);
            if (InjectThrow) { InjectThrow = false; throw new InvalidOperationException("injected"); }
            if (InjectNaN) { InjectNaN = false; left[0] = float.NaN; }
            if (Finite(left, count) && Finite(right, count)) return;
            Report($"SoundMix produced a non-finite block after stage '{stage}'; rebuilt the mix");
        }
        catch (Exception e)
        {
            Report($"SoundMix.Render threw after stage '{stage}': {e}");
        }
        Array.Clear(left, 0, count);
        Array.Clear(right, 0, count);
        Build();
    }

    void RenderCore(float[] left, float[] right, int count, ref string stage)
    {
        if (sendL.Length < count)
        {
            sendL = new float[count];
            sendR = new float[count];
            outL = new float[count];
            outR = new float[count];
            stepL = new float[count];
            stepR = new float[count];
            workL = new float[count];
            workR = new float[count];
        }
        Array.Clear(left, 0, count);
        Array.Clear(right, 0, count);
        Array.Clear(sendL, 0, count);
        Array.Clear(sendR, 0, count);
        Array.Clear(outL, 0, count);
        Array.Clear(outR, 0, count);

        // The world outside: wind, leaves, animals, thunder and fire. Rain muffles
        // itself under a roof; footsteps and music are always close.
        float night01 = 1f - Daylight;
        (float birdLevel, float chorus) = BirdActivity(TimeOfDay);
        var season = SeasonSound.Of(Year);
        float winterWind = Math.Clamp(Wind + 0.05f * season.Cold, 0f, 1f);
        wind.Render(outL, outR, count, winterWind, Rain, season.Cold);
        foliage.Render(outL, outR, count, winterWind, Rain, Foliage, wind.Gust, season.Leaves, season.Dry, SnowCover);
        thunder.Render(outL, outR, count);
        fire.Render(outL, outR, count, Fire, FirePan, Rain);
        birds.Render(outL, outR, sendL, sendR, count, birdLevel * season.Birds * (1f - 0.9f * Rain) * (1f - 0.5f * Wind), Math.Min(1f, chorus * season.Chorus));
        night.Render(outL, outR, sendL, sendR, count, night01 * (1f - 0.9f * Rain) * (1f - 0.4f * Wind), season.Insects);
        insects.Render(outL, outR, count, season.Summer * Smooth(0.2f, 0.8f, Daylight) * (1f - Rain) * (1f - 0.6f * Wind) * (1f - SnowCover));
        sheep.Render(outL, outR, sendL, sendR, count);
        deer.Render(outL, outR, sendL, sendR, count);
        water.Render(outL, outR, count, Water, WaterPan, Wind, wind.Gust, Rain);
        stream.Render(outL, outR, count, Stream, StreamPan, Rain);
        frogs.Render(outL, outR, sendL, sendR, count, Water * season.Frogs * Smooth(0.15f, 0.6f, night01) * (1f - 0.4f * Wind), WaterPan);
        float coverCoef = Lowpass.Coef(1.2f, Rate), wall = Lowpass.Coef(650f, Rate);
        for (int i = 0; i < count; i++)
        {
            cover += coverCoef * (Cover - cover);
            float ml = wallL.Process(outL[i], wall), mr = wallR.Process(outR[i], wall);
            left[i] += outL[i] * (1f - cover) + ml * cover * 0.55f;
            right[i] += outR[i] * (1f - cover) + mr * cover * 0.55f;
            sendL[i] *= 1f - 0.7f * cover;
            sendR[i] *= 1f - 0.7f * cover;
        }

        rain.Render(left, right, count, Rain, Wetness, Cover);

        float ease = count / (Rate * 0.15f);
        masterGain += Math.Clamp(SliderGain(MasterLevel) - masterGain, -ease, ease);
        effectsGain += Math.Clamp(SliderGain(EffectsLevel) - effectsGain, -ease, ease);
        ambienceGain += Math.Clamp(SliderGain(AmbienceLevel) - ambienceGain, -ease, ease);

        // Footsteps and the work in your hands sit a little above whatever
        // surrounds them: soft on a still dawn, firmer in a storm, never louder
        // than they need to be.
        double power = 0;
        for (int i = 0; i < count; i++) power += left[i] * left[i] + right[i] * right[i];
        float rms = (float)Math.Sqrt(power / (2.0 * count));
        ambience += (1f - MathF.Exp(-count / (float)Rate / 1.5f)) * (rms - ambience);
        // Measured before the ambience slider, so the effects follow the world
        // itself, not how loud the player likes it.
        for (int i = 0; i < count; i++)
        {
            left[i] *= ambienceGain;
            right[i] *= ambienceGain;
            sendL[i] *= ambienceGain;
            sendR[i] *= ambienceGain;
        }
        if (!Finite(left, count) || !Finite(right, count)) { stage = "ambience (wind, foliage, rain, thunder, fire, birds, night, insects, sheep, deer, water, stream, frogs)"; throw new InvalidOperationException("non-finite ambience"); }
        float stepGain = StepGain(ambience) * effectsGain, workGain = WorkGain(ambience) * effectsGain;
        Array.Clear(stepL, 0, count);
        Array.Clear(stepR, 0, count);
        Array.Clear(workL, 0, count);
        Array.Clear(workR, 0, count);
        steps.Render(stepL, stepR, count);
        work.Render(workL, workR, count);
        cues.Render(workL, workR, sendL, sendR, count);
        foreach (var (player, voice) in others)
        {
            voice.Render(workL, workR, count);
            if (voice.Idle) quiet.Add(player);
        }
        foreach (var player in quiet) others.Remove(player);
        quiet.Clear();
        for (int i = 0; i < count; i++)
        {
            left[i] += stepL[i] * stepGain + workL[i] * workGain;
            right[i] += stepR[i] * stepGain + workR[i] * workGain;
        }
        if (!Finite(left, count) || !Finite(right, count)) { stage = "effects (steps, work, cues, others)"; throw new InvalidOperationException("non-finite mix before music"); }
        music.Render(left, right, sendL, sendR, count, MusicOn && SynthMusic, SliderGain(MusicLevel), night01);
        if (!Finite(left, count) || !Finite(right, count)) { stage = "music"; throw new InvalidOperationException("non-finite after music"); }
        reverb.Process(sendL, sendR, left, right, count);
        stage = "reverb and master";

        for (int i = 0; i < count; i++)
        {
            left[i] = SoftClip(left[i] * Volume * masterGain);
            right[i] = SoftClip(right[i] * Volume * masterGain);
        }
    }

    // How busy the birds are at this time of day (0 to 1), and how much of that is
    // the dawn chorus. A full chorus just after sunrise, a quiet midday, a smaller
    // burst of evening song that thins out into dusk, and nothing at night.
    public static (float Level, float Chorus) BirdActivity(float t)
    {
        float awake = Smooth(0.215f, 0.245f, t) * (1f - Smooth(0.765f, 0.795f, t));
        float dawn = MathF.Exp(-Sq((t - 0.27f) / 0.03f));
        float dusk = MathF.Exp(-Sq((t - 0.725f) / 0.025f));
        return (awake * Math.Min(1f, 0.35f + 0.65f * dawn + 0.35f * dusk), awake * dawn);
    }

    // What the time of year does to the ambience, from the year fraction (0 the
    // first day of spring). Warmth matches Seasons.Warmth: -1 mid-winter to 1
    // mid-summer.
    public readonly record struct SeasonSound(float Cold, float Summer, float Leaves, float Dry, float Birds, float Chorus, float Insects, float Frogs)
    {
        public static SeasonSound Of(float year)
        {
            float y = year - MathF.Floor(year);
            float warmth = MathF.Cos((y - 0.375f) * MathF.PI * 2f);
            float spring = MathF.Exp(-Sq(Wrapped(y - 0.12f) / 0.09f));
            // Bare from late autumn through winter, budding again in the first days of spring.
            float bare = y >= 0.5f ? Smooth(0.62f, 0.74f, y) : 1f - Smooth(0f, 0.06f, y);
            // Dry, crisp leaves through autumn, still on the trees and blowing about below.
            float dry = MathF.Exp(-Sq((y - 0.64f) / 0.07f));
            return new SeasonSound(
                Cold: Math.Max(0f, -warmth),
                Summer: Smooth(0.35f, 0.9f, warmth),
                Leaves: 1f - bare,
                Dry: dry,
                // Busiest in spring, a few hardy birds in deep winter.
                Birds: 0.12f + 0.88f * Smooth(-0.6f, 0.2f, warmth) * (1f - 0.45f * dry) + 0.6f * spring,
                Chorus: 0.5f + 1.5f * spring,
                Insects: Smooth(-0.25f, 0.65f, warmth),
                Frogs: Smooth(-0.35f, 0.3f, warmth) * (1f + 0.5f * spring));
        }

        static float Wrapped(float d) => d - MathF.Round(d);
    }

    // Footstep level for an ambience RMS: about 0.17 (-15 dB) over a still dawn bed
    // near -45 dBFS, so steps land about 5 dB above it, rising to full in a storm.
    public static float StepGain(float ambienceRms) => Math.Clamp(0.12f + 9f * ambienceRms, 0.12f, 1f);

    // Work level for an ambience RMS. Work is what you are paying attention to, so
    // it gets its own floor well above the footsteps': about 0.39 (-8 dB) on a
    // still morning bed near -42 dBFS, putting hits clearly over the bed and a
    // little under the music (D-097), rising to 0.7 in a storm.
    public static float WorkGain(float ambienceRms) => Math.Clamp(0.39f + 3.5f * ambienceRms, 0.39f, 0.7f);

    // A slider position (0 to 1) as a gain: silent at 0, 1 (the designed mix) at
    // the default 0.5, and 4 (+12 dB) at the top. Squared, so it feels even to the ear.
    public static float SliderGain(float level) => 4f * Sq(Math.Clamp(level, 0f, 1f));

    static float Sq(float x) => x * x;

    static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    static float SoftClip(float x)
    {
        x = Math.Clamp(x, -3f, 3f);
        return x * (27f + x * x) / (27f + 9f * x * x);
    }

    // --- Building blocks --------------------------------------------------------

    internal sealed class Noise
    {
        uint state;
        public Noise(uint seed) => state = seed == 0 ? 1u : seed;

        // White noise in [-1, 1].
        public float Next()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xFFFFFF) / 8388608f - 1f;
        }
    }

    // One-pole low-pass: cheap, smooth, never unstable.
    internal struct Lowpass
    {
        float y;
        public float Process(float x, float coef) => y += coef * (x - y);
        public static float Coef(float cutoff, int rate) => 1f - MathF.Exp(-2f * MathF.PI * cutoff / rate);
    }

    // State-variable filter for band-pass sounds below about 5 kHz.
    internal struct BandPass
    {
        float low, band;
        public float Process(float x, float cutoff, float damping, int rate)
        {
            float f = 2f * MathF.Sin(MathF.PI * Math.Min(cutoff, rate / 7f) / rate);
            low += f * band;
            float high = x - low - damping * band;
            band += f * high;
            return band;
        }
    }

    static float Pan(float pan, bool left)
    {
        pan = Side(pan);
        return left ? MathF.Sqrt(0.5f * (1f - pan)) : MathF.Sqrt(0.5f * (1f + pan));
    }

    static float Range(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

    // --- Wind -------------------------------------------------------------------

    // Band-passed noise whose pitch and loudness rise and fall with slow gusts.
    sealed class WindVoice
    {
        readonly int rate;
        Noise noise = null!;
        BandPass bandL, bandR;
        Lowpass rumbleL, rumbleR, gustSmooth;
        float gust = 0.5f, gustTarget = 0.5f, level;
        int counter;

        public float Gust => gust;

        public WindVoice(int rate, Noise noise)
        {
            this.rate = rate;
            this.noise = noise;
        }

        // Cold (0 to 1, deep winter): a thinner, higher, more restless wind.
        public void Render(float[] l, float[] r, int n, float wind, float rain, float cold = 0f)
        {
            float rumbleCoef = Lowpass.Coef(120f, rate);
            float gustCoef = Lowpass.Coef(0.15f, rate);
            float levelCoef = Lowpass.Coef(0.5f, rate);
            for (int i = 0; i < n; i++)
            {
                if (counter-- <= 0)
                {
                    counter = rate / 2;
                    gustTarget = Math.Clamp(gustTarget + noise.Next() * (0.35f + 0.08f * cold), 0f, 1f);
                }
                gust = gustSmooth.Process(gustTarget, gustCoef);
                float strength = Math.Clamp(wind + rain * 0.2f, 0f, 1f);
                level += levelCoef * (strength - level);
                float amp = (0.07f + level * 0.2f) * (0.45f + 0.55f * gust);
                float center = (220f + 520f * gust * (0.4f + level)) * (1f + 0.15f * cold);
                float a = bandL.Process(noise.Next(), center, 1.3f, rate);
                float b = bandR.Process(noise.Next(), center * 1.07f, 1.3f, rate);
                float lowA = rumbleL.Process(noise.Next(), rumbleCoef) * 2.5f;
                float lowB = rumbleR.Process(noise.Next(), rumbleCoef) * 2.5f;
                l[i] += (a + lowA * level) * amp;
                r[i] += (b + lowB * level) * amp;
            }
        }
    }

    // --- Leaves and grass -------------------------------------------------------

    // The wind heard in what it moves: a dry, fluttering rustle of leaves in the
    // woods, a softer hiss through open grass. Both swell with the gusts.
    sealed class FoliageVoice
    {
        readonly int rate;
        readonly Noise noise;
        BandPass leafL, leafR, grassL, grassR, twigL, twigR;
        Lowpass flutterL, flutterR, level, wooded;

        public FoliageVoice(int rate, Noise noise)
        {
            this.rate = rate;
            this.noise = noise;
        }

        // Leaves on the trees (0 bare to 1 full) and how dry they are (autumn): dry
        // leaves rattle higher and crisper. Bare winter branches whistle thinly
        // instead, and snow lying on the ground hushes the grass.
        public void Render(float[] l, float[] r, int n, float wind, float rain, float foliage, float gust, float leafCover = 1f, float dry = 0f, float snow = 0f)
        {
            float flutterCoef = Lowpass.Coef(14f + 26f * dry, rate), levelCoef = Lowpass.Coef(0.4f, rate), woodCoef = Lowpass.Coef(0.3f, rate);
            float strength = Math.Clamp(wind + rain * 0.15f, 0f, 1f);
            float target = MathF.Pow(strength, 1.4f) * (0.35f + 0.65f * gust);
            float leafGain = 0.16f * (0.15f + 0.85f * leafCover) * (1f + 0.2f * dry), leafHz = 3400f + 1300f * dry;
            float bare = 1f - leafCover, grassGain = 0.07f * (1f - 0.75f * snow);
            for (int i = 0; i < n; i++)
            {
                float lv = level.Process(target, levelCoef);
                float w = wooded.Process(foliage, woodCoef);
                // Leaves flicker: the rustle comes in quick uneven bursts, sharper when dry.
                float fl = Math.Max(0f, 0.5f - 0.3f * dry + (6f + 6f * dry) * flutterL.Process(noise.Next(), flutterCoef));
                float fr = Math.Max(0f, 0.5f - 0.3f * dry + (6f + 6f * dry) * flutterR.Process(noise.Next(), flutterCoef));
                float leafA = leafL.Process(noise.Next(), leafHz + 900f * gust, 0.9f, rate) * fl;
                float leafB = leafR.Process(noise.Next(), leafHz + 300f + 900f * gust, 0.9f, rate) * fr;
                float grassA = grassL.Process(noise.Next(), 1500f + 700f * gust, 1.1f, rate);
                float grassB = grassR.Process(noise.Next(), 1650f + 700f * gust, 1.1f, rate);
                float leaves = lv * w * leafGain, grass = lv * (1f - 0.6f * w) * grassGain;
                float s = 0f, t = 0f;
                if (bare > 0.01f)
                {
                    // Narrow and high: wind over bare twigs, rising with each gust.
                    float twigs = lv * w * bare * gust * 0.005f;
                    s = twigL.Process(noise.Next(), 1100f + 900f * gust, 0.15f, rate) * twigs;
                    t = twigR.Process(noise.Next(), 1250f + 900f * gust, 0.15f, rate) * twigs;
                }
                l[i] += leafA * leaves + grassA * grass + s;
                r[i] += leafB * leaves + grassB * grass + t;
            }
        }
    }

    // --- Rain -------------------------------------------------------------------

    // Rain is layered by how hard it falls: a soft patter of single drops in a
    // drizzle, a hiss as it settles in, and a low roar in a storm. Wet branches
    // drip during rain and for a while after. Under a roof the open rain goes
    // muffled and drops drum on the thatch overhead instead.
    sealed class RainVoice
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        Lowpass hissLowL, hissLowR, hissTopL, hissTopR, washL, washR, roarL, roarR, muffleL, muffleR;
        Lowpass roofHiL, roofHiR, roofLoL, roofLoR;
        readonly List<Drop> drops = new();
        float level, cover;
        readonly Lowpass[] dropHpL = new Lowpass[3], dropHpR = new Lowpass[3], dropLpL = new Lowpass[3], dropLpR = new Lowpass[3];
        float[][] dropL = { Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>() }, dropR = { Array.Empty<float>(), Array.Empty<float>(), Array.Empty<float>() };

        // Kind: 0 a drop on leaves, 1 a drip off a branch, 2 a tap on the roof. Each is a short
        // burst of noise with no pitch, shaped by its kind's filter, so rain is plain hiss and patter.
        record struct Drop(int Kind, float Decay, float Amp, float Pan, float Age);

        public RainVoice(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Render(float[] l, float[] r, int n, float rain, float wetness, float covered)
        {
            float low = Lowpass.Coef(1400f, rate), top = Lowpass.Coef(7000f, rate), wash = Lowpass.Coef(350f, rate);
            float roarCoef = Lowpass.Coef(160f, rate), muffle = Lowpass.Coef(900f, rate);
            float roofHi = Lowpass.Coef(750f, rate), roofLo = Lowpass.Coef(150f, rate);
            float levelCoef = Lowpass.Coef(0.3f, rate), coverCoef = Lowpass.Coef(0.8f, rate);
            float dt = 1f / rate;
            for (int i = 0; i < n; i++)
            {
                level += levelCoef * (rain - level);
                cover += coverCoef * (covered - cover);
                float hiss = Smooth(0.12f, 0.7f, level), roar = Smooth(0.55f, 1f, level);

                float nl = noise.Next(), nr = noise.Next();
                float hl = hissTopL.Process(nl - hissLowL.Process(nl, low), top);
                float hr = hissTopR.Process(nr - hissLowR.Process(nr, low), top);
                float wl = washL.Process(noise.Next(), wash) * 3f, wr = washR.Process(noise.Next(), wash) * 3f;
                float rl = roarL.Process(noise.Next(), roarCoef) * 6f, rr = roarR.Process(noise.Next(), roarCoef) * 6f;
                float openL = hl * hiss * 0.09f + wl * hiss * 0.06f + rl * roar * 0.1f;
                float openR = hr * hiss * 0.09f + wr * hiss * 0.06f + rr * roar * 0.1f;
                // Under cover the open rain is heard through the walls: duller and quieter.
                l[i] += openL * (1f - cover) + muffleL.Process(openL, muffle) * cover * 0.5f;
                r[i] += openR * (1f - cover) + muffleR.Process(openR, muffle) * cover * 0.5f;

                // Heavy rain on a roof thickens into a steady low drumming: plain noise
                // around 150 to 750 Hz, with no pulse or pitch.
                float heavy = Smooth(0.45f, 1f, level) * cover;
                float bodyL = roofHiL.Process(nl, roofHi) - roofLoL.Process(nl, roofLo);
                float bodyR = roofHiR.Process(nr, roofHi) - roofLoR.Process(nr, roofLo);
                l[i] += bodyL * heavy * 0.12f;
                r[i] += bodyR * heavy * 0.12f;

                // Patter: single drops on leaves, sparse in a drizzle, a crowd in a storm
                // where the hiss carries them.
                float patter = (20f + 380f * level * level) * Smooth(0.02f, 0.15f, level) * (1f - cover);
                if (rng.NextDouble() < patter * dt)
                    drops.Add(new Drop(0, Range(rng, 0.002f, 0.006f),
                        Range(rng, 0.012f, 0.04f) * (0.6f + 0.6f * level), Range(rng, -0.9f, 0.9f), 0f));
                // Drips off the branches: during rain and while the leaves are still wet.
                float drips = (rain > 0.02f ? 1.5f + 3f * level : 0f) + Math.Max(0f, wetness - level) * 4f;
                if (rng.NextDouble() < drips * (1f - cover * 0.7f) * dt)
                    drops.Add(new Drop(1, Range(rng, 0.008f, 0.02f), Range(rng, 0.03f, 0.07f), Range(rng, -0.8f, 0.8f), 0f));
                // On the roof: low, close taps that thicken into a drumming.
                // Softer than the open rain, so stepping inside always quietens it.
                float roof = cover * (20f + 200f * level) * Smooth(0.02f, 0.1f, level);
                if (rng.NextDouble() < roof * dt)
                    drops.Add(new Drop(2, Range(rng, 0.004f, 0.01f), Range(rng, 0.015f, 0.04f), Range(rng, -0.6f, 0.6f), 0f));
            }

            if (dropL[0].Length < n)
                for (int k = 0; k < 3; k++) { dropL[k] = new float[n]; dropR[k] = new float[n]; }
            for (int k = 0; k < 3; k++) { Array.Clear(dropL[k], 0, n); Array.Clear(dropR[k], 0, n); }
            for (int d = drops.Count - 1; d >= 0; d--)
            {
                var drop = drops[d];
                int start = (int)(rng.NextDouble() * n * 0.98);
                float pl = Pan(drop.Pan, true), pr = Pan(drop.Pan, false);
                var sumL = dropL[drop.Kind];
                var sumR = dropR[drop.Kind];
                // Each drop sits at a random spot in this block, then dies away.
                for (int i = drop.Age == 0f ? start : 0; i < n; i++)
                {
                    float env = MathF.Exp(-drop.Age / drop.Decay);
                    if (env < 0.001f) break;
                    float s = noise.Next() * env * drop.Amp;
                    sumL[i] += s * pl;
                    sumR[i] += s * pr;
                    drop.Age += dt;
                }
                if (MathF.Exp(-drop.Age / drop.Decay) < 0.001f) drops.RemoveAt(d);
                else drops[d] = drop;
            }
            // Shape each kind: leaves ring bright (above 2 kHz), drips sit mid (400 to 1500 Hz),
            // the roof is a dull thud (below 700 Hz). None is pitched.
            float[] hpCut = { 2000f, 400f, 0f }, lpCut = { 7000f, 1500f, 700f }, gain = { 1.6f, 1.8f, 2.0f };
            for (int k = 0; k < 3; k++)
            {
                float hp = Lowpass.Coef(Math.Max(hpCut[k], 1f), rate), lp = Lowpass.Coef(lpCut[k], rate);
                for (int i = 0; i < n; i++)
                {
                    float xl = dropL[k][i], xr = dropR[k][i];
                    if (hpCut[k] > 0f) { xl -= dropHpL[k].Process(xl, hp); xr -= dropHpR[k].Process(xr, hp); }
                    l[i] += dropLpL[k].Process(xl, lp) * gain[k];
                    r[i] += dropLpR[k].Process(xr, lp) * gain[k];
                }
            }
        }
    }

    // --- Thunder ----------------------------------------------------------------

    // A crack for a close strike, then a long rolling rumble that swells and fades
    // a few times as it echoes off the hills.
    sealed class ThunderVoice
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        readonly List<Roll> rolls = new();
        Lowpass lowL, lowR, crackL, crackR, snapLow, tearLow;

        record struct Roll(float Delay, float Gain, float Near, float Age, float Length, float Pan, float Seed);

        public ThunderVoice(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Trigger(float delay, float gain, float near) =>
            rolls.Add(new Roll(delay, gain, near, 0f, Range(rng, 5f, 9f), Range(rng, -0.6f, 0.6f), Range(rng, 0f, 100f)));

        public void Render(float[] l, float[] r, int n)
        {
            if (rolls.Count == 0) return;
            float dt = 1f / rate;
            float lowCoef = Lowpass.Coef(110f, rate), crackCoef = Lowpass.Coef(2500f, rate);
            float snapCoef = Lowpass.Coef(2500f, rate), tearCoef = Lowpass.Coef(1800f, rate);
            for (int d = rolls.Count - 1; d >= 0; d--)
            {
                var roll = rolls[d];
                float pl = Pan(roll.Pan, true), pr = Pan(roll.Pan, false);
                for (int i = 0; i < n; i++)
                {
                    roll.Age += dt;
                    float t = roll.Age - roll.Delay;
                    if (t < 0f) continue;
                    float u = t / roll.Length;
                    if (u >= 1f) break;
                    // Rumble: a quick swell, then a few uneven rolls dying away.
                    float swell = Math.Min(1f, t / 0.25f) * MathF.Pow(1f - u, 2f);
                    float wobble = 0.55f + 0.45f * MathF.Sin(t * (2.1f + 0.7f * MathF.Sin(roll.Seed)) + roll.Seed)
                                         * MathF.Sin(t * 0.9f + roll.Seed * 1.7f);
                    float rumbleL = lowL.Process(noise.Next(), lowCoef) * 9f, rumbleR = lowR.Process(noise.Next(), lowCoef) * 9f;
                    float crack = roll.Near * MathF.Exp(-t / 0.18f) * 0.6f;
                    float cl = crackL.Process(noise.Next(), crackCoef) * crack, cr = crackR.Process(noise.Next(), crackCoef) * crack;
                    float g = roll.Gain * swell * wobble * 0.35f;
                    // A close strike (near over 0.5) cracks: a bright snap in the first few
                    // hundredths of a second, then a ripping tear that dies in half a second.
                    float sharp = Smooth(0.5f, 0.9f, roll.Near);
                    float snap = 0f, tear = 0f;
                    if (sharp > 0f && t < 0.7f)
                    {
                        float bright = noise.Next();
                        bright -= snapLow.Process(bright, snapCoef);
                        snap = bright * MathF.Min(1f, t / 0.0015f) * MathF.Exp(-t / 0.025f) * 0.9f;
                        float flutter = 0.5f + 0.5f * MathF.Sin(t * 2f * MathF.PI * (38f + 20f * u) + roll.Seed);
                        tear = tearLow.Process(noise.Next(), tearCoef) * 2.2f * flutter * MathF.Exp(-t / 0.22f);
                    }
                    float sharpGain = roll.Gain * sharp * 0.32f;
                    l[i] += (rumbleL * g + cl * roll.Gain + (snap + tear) * sharpGain) * pl;
                    r[i] += (rumbleR * g + cr * roll.Gain + (snap + tear) * sharpGain) * pr;
                }
                if (roll.Age - roll.Delay >= roll.Length) rolls.RemoveAt(d);
                else rolls[d] = roll;
            }
        }
    }

    // --- Campfire ---------------------------------------------------------------

    // A soft breathing roar under quick crackles, now and then a louder pop as a
    // pocket of sap bursts, and a hiss when rain lands in the flames.
    sealed class FireVoice
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        readonly List<Crack> cracks = new();
        Lowpass roarL, roarR, flutter, hissLow;
        float level, flutterTarget = 0.5f, hissLeft;
        int counter;

        sealed class Crack
        {
            public float Age, Decay, Amp, Cutoff, Pan;
            public SoundMix.BandPass Band;
        }

        public FireVoice(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Render(float[] l, float[] r, int n, float gain, float pan, float rain)
        {
            float dt = 1f / rate;
            float levelCoef = Lowpass.Coef(1.5f, rate), roarCoef = Lowpass.Coef(260f, rate);
            float flutterCoef = Lowpass.Coef(3f, rate), hissCoef = Lowpass.Coef(2500f, rate);
            if (gain < 0.001f && level < 0.001f && cracks.Count == 0) return;
            float pl = Pan(pan * 0.8f, true), pr = Pan(pan * 0.8f, false);
            for (int i = 0; i < n; i++)
            {
                level += levelCoef * (gain - level);
                if (counter-- <= 0)
                {
                    counter = rate / 12;
                    flutterTarget = Math.Clamp(flutterTarget + noise.Next() * 0.4f, 0.1f, 1f);
                }
                float f = flutter.Process(flutterTarget, flutterCoef);
                float roar = level * (0.4f + 0.6f * f) * 0.12f;
                l[i] += roarL.Process(noise.Next(), roarCoef) * 3f * roar * pl;
                r[i] += roarR.Process(noise.Next(), roarCoef) * 3f * roar * pr;

                if (rng.NextDouble() < (5f + 40f * level * f) * level * dt)
                    cracks.Add(new Crack { Decay = Range(rng, 0.0015f, 0.006f), Amp = Range(rng, 0.1f, 0.35f) * level, Cutoff = Range(rng, 1500f, 4500f), Pan = pan * 0.8f + Range(rng, -0.15f, 0.15f) });
                if (rng.NextDouble() < 0.4f * level * dt)
                    cracks.Add(new Crack { Decay = Range(rng, 0.012f, 0.03f), Amp = Range(rng, 0.4f, 0.8f) * level, Cutoff = Range(rng, 500f, 1200f), Pan = pan * 0.8f });

                // Rain on the embers: short sizzles.
                if (hissLeft <= 0f && rng.NextDouble() < rain * 2f * level * dt) hissLeft = Range(rng, 0.1f, 0.35f);
                if (hissLeft > 0f)
                {
                    hissLeft -= dt;
                    float x = noise.Next();
                    float h = (x - hissLow.Process(x, hissCoef)) * level * 0.06f * Math.Min(1f, hissLeft * 8f);
                    l[i] += h * pl;
                    r[i] += h * pr;
                }
            }

            for (int c = cracks.Count - 1; c >= 0; c--)
            {
                var crack = cracks[c];
                int start = crack.Age == 0f ? (int)(rng.NextDouble() * n * 0.98) : 0;
                float cl = Pan(crack.Pan, true), cr = Pan(crack.Pan, false);
                for (int i = start; i < n; i++)
                {
                    float env = MathF.Exp(-crack.Age / crack.Decay);
                    if (env < 0.001f) break;
                    float s = crack.Band.Process(noise.Next(), crack.Cutoff, 0.6f, rate) * env * crack.Amp;
                    l[i] += s * cl;
                    r[i] += s * cr;
                    crack.Age += dt;
                }
                if (MathF.Exp(-crack.Age / crack.Decay) < 0.001f) cracks.RemoveAt(c);
            }
        }
    }

    // --- Birds ------------------------------------------------------------------

    // Short songs from a few kinds of bird, scattered around the listener.
    sealed class Birds
    {
        readonly int rate;
        readonly Random rng;
        readonly List<Song> songs = new();
        float untilNext = 1.5f;

        sealed class Song
        {
            public readonly List<(float Start, float Length, float From, float To)> Notes = new();
            public float Time, Phase, Pan, Gain, Harmonic, Vibrato;
            public float Length => Notes.Count == 0 ? 0f : Notes[^1].Start + Notes[^1].Length;
        }

        public Birds(int rate, Random rng)
        {
            this.rate = rate;
            this.rng = rng;
        }

        // At the dawn chorus (chorus near 1) many more birds sing at once.
        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n, float level, float chorus)
        {
            float dt = 1f / rate;
            untilNext -= n * dt;
            if (untilNext <= 0f)
            {
                untilNext = Range(rng, 1.5f, 7f) / (0.3f + level) / (1f + 2.5f * chorus);
                if (level > 0.05f && songs.Count < 3 + (int)(4f * chorus)) songs.Add(MakeSong(level));
            }

            for (int s = songs.Count - 1; s >= 0; s--)
            {
                var song = songs[s];
                float pl = Pan(song.Pan, true), pr = Pan(song.Pan, false);
                for (int i = 0; i < n; i++)
                {
                    song.Time += dt;
                    float sample = 0f;
                    foreach (var note in song.Notes)
                    {
                        float u = (song.Time - note.Start) / note.Length;
                        if (u < 0f || u >= 1f) continue;
                        float freq = note.From + (note.To - note.From) * u;
                        freq *= 1f + song.Vibrato * MathF.Sin(song.Time * 2f * MathF.PI * 28f);
                        song.Phase += 2f * MathF.PI * freq * dt;
                        float env = MathF.Sin(MathF.PI * u);
                        sample = (MathF.Sin(song.Phase) + song.Harmonic * MathF.Sin(2f * song.Phase)) * env * env;
                        break;
                    }
                    sample *= song.Gain;
                    l[i] += sample * pl;
                    r[i] += sample * pr;
                    sl[i] += sample * pl * 0.5f;
                    sr[i] += sample * pr * 0.5f;
                }
                if (song.Time > song.Length) songs.RemoveAt(s);
            }
        }

        Song MakeSong(float level)
        {
            var song = new Song
            {
                Pan = Range(rng, -0.85f, 0.85f),
                Gain = Range(rng, 0.02f, 0.06f) * level,
                Harmonic = 0.12f,
                Vibrato = 0.004f,
            };
            float t = 0f;
            switch (rng.Next(4))
            {
                case 0: // a wandering warble
                    for (int k = rng.Next(4, 9); k > 0; k--)
                    {
                        float len = Range(rng, 0.07f, 0.17f), from = Range(rng, 2300f, 4200f);
                        song.Notes.Add((t, len, from, from + Range(rng, -600f, 600f)));
                        t += len + Range(rng, 0.025f, 0.07f);
                    }
                    break;
                case 1: // two clear falling whistles, "fee-bee"
                    song.Notes.Add((0f, 0.32f, 3950f, 3850f));
                    song.Notes.Add((0.42f, 0.36f, 3420f, 3330f));
                    song.Harmonic = 0.02f;
                    song.Vibrato = 0.001f;
                    break;
                case 2: // a fast trill
                    float pitch = Range(rng, 3800f, 4800f);
                    for (int k = rng.Next(10, 18); k > 0; k--)
                    {
                        song.Notes.Add((t, 0.035f, pitch, pitch - 500f));
                        t += 0.05f;
                        pitch -= 25f;
                    }
                    break;
                default: // a distant wood pigeon
                    for (int k = rng.Next(3, 6); k > 0; k--)
                    {
                        float len = k == 1 ? 0.25f : 0.42f;
                        song.Notes.Add((t, len, 560f, 480f));
                        t += len + 0.16f;
                    }
                    song.Harmonic = 0.25f;
                    song.Vibrato = 0.002f;
                    song.Gain *= 1.3f;
                    break;
            }
            return song;
        }
    }

    // --- Night ------------------------------------------------------------------

    // Crickets chirping out of step with each other over a faint shimmer of
    // insects in the grass, and now and then an owl.
    sealed class NightVoices
    {
        readonly int rate;
        readonly Random rng;
        readonly (float Freq, float Pan, float Period, float Gain)[] crickets;
        readonly float[] cricketTime, cricketPhase;
        float owlTime = -1f, owlPhase, untilOwl = 12f, owlPan, shimmerTime;
        BandPass shimmerL, shimmerR;
        Lowpass swellL, swellR;
        readonly Noise noise = new(97u);

        public NightVoices(int rate, Random rng)
        {
            this.rate = rate;
            this.rng = rng;
            crickets = new (float, float, float, float)[6];
            cricketTime = new float[crickets.Length];
            cricketPhase = new float[crickets.Length];
            for (int c = 0; c < crickets.Length; c++)
            {
                crickets[c] = (Range(rng, 4200f, 4900f), Range(rng, -0.9f, 0.9f), Range(rng, 0.5f, 0.95f), Range(rng, 0.006f, 0.013f));
                cricketTime[c] = Range(rng, 0f, 1f);
            }
        }

        // Insects (0 to 1) is how many are about this time of year: none in winter,
        // so only the owls are left.
        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n, float level, float insects = 1f)
        {
            float dt = 1f / rate;
            float owls = level;
            level *= insects;
            if (level > 0.01f)
            {
                // The shimmer: high, buzzing, slowly swelling on each side.
                float swell = Lowpass.Coef(0.2f, rate);
                for (int i = 0; i < n; i++)
                {
                    shimmerTime += dt;
                    float buzz = 0.55f + 0.45f * MathF.Sin(shimmerTime * 2f * MathF.PI * 38f);
                    float a = Math.Max(0f, 0.6f + 8f * swellL.Process(noise.Next(), swell));
                    float b = Math.Max(0f, 0.6f + 8f * swellR.Process(noise.Next(), swell));
                    float g = 0.02f * level * buzz;
                    l[i] += shimmerL.Process(noise.Next(), 6300f, 0.25f, rate) * a * g;
                    r[i] += shimmerR.Process(noise.Next(), 6700f, 0.25f, rate) * b * g;
                }
                // The first crickets start at dusk; the rest join as it gets dark.
                for (int c = 0; c < crickets.Length; c++)
                {
                    var (freq, pan, period, gain) = crickets[c];
                    float join = Smooth(c / (float)crickets.Length * 0.8f, c / (float)crickets.Length * 0.8f + 0.2f, level);
                    if (join <= 0f) continue;
                    float pl = Pan(pan, true) * gain * join, pr = Pan(pan, false) * gain * join;
                    for (int i = 0; i < n; i++)
                    {
                        float t = cricketTime[c] = (cricketTime[c] + dt) % period;
                        cricketPhase[c] += 2f * MathF.PI * freq * dt;
                        // Three quick pulses at the start of each period.
                        float pulse = 0f;
                        if (t < 0.096f)
                        {
                            float p = t % 0.032f;
                            if (p < 0.018f)
                            {
                                float e = MathF.Sin(MathF.PI * p / 0.018f);
                                pulse = e * e;
                            }
                        }
                        float s = MathF.Sin(cricketPhase[c]) * pulse;
                        l[i] += s * pl;
                        r[i] += s * pr;
                    }
                }
            }

            untilOwl -= n * dt;
            if (untilOwl <= 0f)
            {
                untilOwl = Range(rng, 25f, 70f);
                if (owls > 0.5f)
                {
                    owlTime = 0f;
                    owlPan = Range(rng, -0.8f, 0.8f);
                }
            }
            if (owlTime < 0f) return;

            // "Hoo... hoo-hoo-hoooo", soft and far away.
            ReadOnlySpan<(float Start, float Length)> hoots = stackalloc (float, float)[] { (0f, 0.45f), (0.85f, 0.18f), (1.1f, 0.18f), (1.35f, 0.6f) };
            float gl = Pan(owlPan, true) * 0.045f * owls, gr = Pan(owlPan, false) * 0.045f * owls;
            for (int i = 0; i < n; i++)
            {
                owlTime += dt;
                float s = 0f;
                foreach (var (start, length) in hoots)
                {
                    float u = (owlTime - start) / length;
                    if (u < 0f || u >= 1f) continue;
                    owlPhase += 2f * MathF.PI * (390f - 30f * u) * dt;
                    float env = MathF.Sin(MathF.PI * MathF.Min(1f, u * 1.6f)) * (1f - u * 0.3f);
                    s = MathF.Sin(owlPhase) * env;
                    break;
                }
                l[i] += s * gl * 0.5f;
                r[i] += s * gr * 0.5f;
                sl[i] += s * gl;
                sr[i] += s * gr;
            }
            if (owlTime > 2.2f) owlTime = -1f;
        }
    }

    // --- Streams -------------------------------------------------------------------

    // Water running over stones: a soft, steady wash with many small bubbles
    // popping up through it, quick rising blips scattered across the stream's
    // width. Fuller after rain. Fades with distance and comes from the stream's side.
    sealed class StreamVoice
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        readonly List<Bubble> bubbles = new();
        BandPass washL, washR;
        Lowpass swellL, swellR, level, side;
        float untilBubble, heard;

        sealed class Bubble
        {
            public float Freq, Rise, Decay, Gain, Pan, Age, Phase;
        }

        public StreamVoice(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Render(float[] l, float[] r, int n, float near, float pan, float rain)
        {
            float dt = 1f / rate;
            float ease = Lowpass.Coef(0.6f, rate), swell = Lowpass.Coef(0.7f, rate);
            float target = Math.Clamp(near, 0f, 1f) * (1f + 0.3f * rain);
            for (int i = 0; i < n; i++)
            {
                float lv = heard = level.Process(target, ease);
                float p = side.Process(pan, ease);
                if (lv < 0.002f) continue;
                // Two slowly wandering washes, a little apart in pitch.
                float a = 0.6f + 6f * swellL.Process(noise.Next(), swell);
                float b = 0.6f + 6f * swellR.Process(noise.Next(), swell);
                float wl = washL.Process(noise.Next(), 700f + 250f * a, 0.9f, rate) * a;
                float wr = washR.Process(noise.Next(), 820f + 250f * b, 0.9f, rate) * b;
                float g = 0.05f * lv;
                // Spread a little around where the stream lies.
                l[i] += (wl * Pan(p - 0.3f, true) + wr * Pan(p + 0.3f, true)) * g;
                r[i] += (wl * Pan(p - 0.3f, false) + wr * Pan(p + 0.3f, false)) * g;

                untilBubble -= dt;
                if (untilBubble <= 0f)
                {
                    untilBubble = Range(rng, 0.008f, 0.06f) / (0.3f + lv);
                    if (bubbles.Count < 24)
                        bubbles.Add(new Bubble
                        {
                            Freq = Range(rng, 450f, 1900f),
                            Rise = Range(rng, 2f, 6f),
                            Decay = Range(rng, 0.012f, 0.035f),
                            Gain = Range(rng, 0.3f, 1f),
                            Pan = Math.Clamp(p + Range(rng, -0.6f, 0.6f), -1f, 1f),
                        });
                }
            }

            for (int k = bubbles.Count - 1; k >= 0; k--)
            {
                var bub = bubbles[k];
                float pl = Pan(bub.Pan, true), pr = Pan(bub.Pan, false);
                for (int i = 0; i < n; i++)
                {
                    // A bubble's ring rises in pitch as it reaches the surface.
                    bub.Phase += 2f * MathF.PI * bub.Freq * (1f + bub.Rise * bub.Age) * dt;
                    float v = MathF.Sin(bub.Phase) * MathF.Exp(-bub.Age / bub.Decay) * bub.Gain * 0.012f * heard;
                    l[i] += v * pl;
                    r[i] += v * pr;
                    bub.Age += dt;
                }
                if (bub.Age > bub.Decay * 6f) bubbles.RemoveAt(k);
            }
        }
    }

    // --- Summer insects -----------------------------------------------------------

    // A warm summer day: a high, pulsing drone of insects in the grass that swells
    // and fades in slow waves, and now and then a bee that hums past from one side
    // to the other.
    sealed class DayInsects
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        BandPass droneL, droneR;
        Lowpass swellL, swellR, level;
        float time, untilBee = 6f, beeTime = -1f, beePhase, beeFrom, beePitch;

        public DayInsects(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Render(float[] l, float[] r, int n, float target)
        {
            float dt = 1f / rate;
            float swell = Lowpass.Coef(0.12f, rate), ease = Lowpass.Coef(0.3f, rate);
            for (int i = 0; i < n; i++)
            {
                float lv = level.Process(target, ease);
                if (lv < 0.002f) continue;
                time += dt;
                // A fast buzz under slow uneven swells, a little different on each side.
                float buzz = 0.5f + 0.5f * MathF.Sin(time * 2f * MathF.PI * 52f);
                float a = Math.Max(0f, 0.4f + 9f * swellL.Process(noise.Next(), swell));
                float b = Math.Max(0f, 0.4f + 9f * swellR.Process(noise.Next(), swell));
                float g = 0.016f * lv * buzz;
                l[i] += droneL.Process(noise.Next(), 5200f, 0.3f, rate) * a * g;
                r[i] += droneR.Process(noise.Next(), 5600f, 0.3f, rate) * b * g;
            }

            untilBee -= n * dt;
            if (untilBee <= 0f)
            {
                untilBee = Range(rng, 8f, 25f);
                if (target > 0.3f)
                {
                    beeTime = 0f;
                    beeFrom = rng.NextDouble() < 0.5 ? -0.9f : 0.9f;
                    beePitch = Range(rng, 190f, 250f);
                }
            }
            if (beeTime < 0f) return;
            const float Pass = 2.2f;
            for (int i = 0; i < n; i++)
            {
                beeTime += dt;
                float u = beeTime / Pass;
                if (u >= 1f) break;
                // Louder as it passes closest; a slight drop in pitch as it goes by.
                float near = MathF.Sin(MathF.PI * u);
                beePhase += 2f * MathF.PI * beePitch * (1.03f - 0.06f * u) * dt;
                float wob = 1f + 0.15f * MathF.Sin(beeTime * 2f * MathF.PI * 7f);
                float sv = (MathF.Sin(beePhase) + 0.5f * MathF.Sin(2f * beePhase) + 0.25f * MathF.Sin(3f * beePhase)) * near * near * wob * 0.012f * target;
                float pan = beeFrom * (1f - 2f * u);
                l[i] += sv * Pan(pan, true);
                r[i] += sv * Pan(pan, false);
            }
            if (beeTime >= Pass) beeTime = -1f;
        }
    }

    // --- Water ------------------------------------------------------------------

    // A pond's edge: small waves lapping at the bank, coming in uneven sets that
    // grow with the wind, the odd gulp of water under an overhang, and rain plinking
    // on the surface. Everything fades with distance from the water.
    sealed class WaterVoice
    {
        readonly int rate;
        readonly Random rng;
        readonly Noise noise = new(131u);
        BandPass lapL, lapR;
        Lowpass level, side, swellL, swellR;
        readonly List<Bubble> bubbles = new();
        float waveTime, wavePeriod = 2.2f;

        record struct Bubble(float Freq, float Rise, float Decay, float Amp, float Pan, float Age, float Phase);

        public WaterVoice(int rate, Random rng)
        {
            this.rate = rate;
            this.rng = rng;
        }

        public void Render(float[] l, float[] r, int n, float water, float pan, float wind, float gust, float rain)
        {
            float dt = 1f / rate;
            float levelCoef = Lowpass.Coef(0.4f, rate), swell = Lowpass.Coef(0.6f, rate);
            float stir = 0.35f + 0.65f * Math.Clamp(wind + rain * 0.2f, 0f, 1f) * (0.5f + 0.5f * gust);
            for (int i = 0; i < n; i++)
            {
                float lv = level.Process(water, levelCoef);
                float p = side.Process(pan, levelCoef);
                if (lv < 0.002f) continue;
                // Each little wave rises and slaps softly, then drains back.
                waveTime += dt;
                if (waveTime > wavePeriod)
                {
                    waveTime = 0f;
                    wavePeriod = Range(rng, 1.4f, 3.4f) / (0.7f + 0.6f * stir);
                }
                float u = waveTime / wavePeriod;
                float wave = MathF.Pow(MathF.Sin(MathF.PI * Math.Min(1f, u * 1.4f)), 3f) + 0.25f;
                float a = Math.Max(0f, 0.5f + 5f * swellL.Process(noise.Next(), swell));
                float b = Math.Max(0f, 0.5f + 5f * swellR.Process(noise.Next(), swell));
                float g = 0.22f * lv * stir * wave;
                float sl = lapL.Process(noise.Next(), 380f + 260f * wave, 0.7f, rate) * a * g;
                float sr = lapR.Process(noise.Next(), 420f + 260f * wave, 0.7f, rate) * b * g;
                l[i] += sl * Pan(p * 0.7f, true);
                r[i] += sr * Pan(p * 0.7f, false);

                // A gulp now and then as a wave runs under the bank: a bubble whose
                // pitch rises as it closes.
                if (rng.NextDouble() < (0.8f + 3f * stir) * wave * lv * dt)
                    bubbles.Add(new Bubble(Range(rng, 280f, 650f), Range(rng, 0.6f, 1.4f), Range(rng, 0.03f, 0.07f),
                        Range(rng, 0.02f, 0.05f) * lv, p * 0.7f + Range(rng, -0.25f, 0.25f), 0f, 0f));
                // Rain on the water: bright plinks, pitch rising, many in a downpour.
                float plinks = rain > 0.02f ? (8f + 160f * rain * rain) * lv : 0f;
                if (rng.NextDouble() < plinks * dt)
                    bubbles.Add(new Bubble(Range(rng, 900f, 2600f), Range(rng, 0.8f, 1.8f), Range(rng, 0.008f, 0.02f),
                        Range(rng, 0.008f, 0.03f) * lv, p * 0.5f + Range(rng, -0.6f, 0.6f), 0f, 0f));
            }

            for (int k = bubbles.Count - 1; k >= 0; k--)
            {
                var bubble = bubbles[k];
                float pl = Pan(Math.Clamp(bubble.Pan, -1f, 1f), true), pr = Pan(Math.Clamp(bubble.Pan, -1f, 1f), false);
                int start = bubble.Age == 0f ? (int)(rng.NextDouble() * n * 0.98) : 0;
                for (int i = start; i < n; i++)
                {
                    float env = MathF.Exp(-bubble.Age / bubble.Decay);
                    if (env < 0.001f) break;
                    bubble.Phase += 2f * MathF.PI * bubble.Freq * (1f + bubble.Rise * (1f - env)) * dt;
                    float s = MathF.Sin(bubble.Phase) * env * bubble.Amp * Math.Min(1f, bubble.Age * rate / 24f);
                    l[i] += s * pl;
                    r[i] += s * pr;
                    bubble.Age += dt;
                }
                if (MathF.Exp(-bubble.Age / bubble.Decay) < 0.001f) bubbles.RemoveAt(k);
                else bubbles[k] = bubble;
            }
        }
    }

    // Frogs by the water at night: a few voices, each a short rattle of throaty
    // pulses ("rrrup"), answering one another from different spots on the bank.
    sealed class Frogs
    {
        readonly int rate;
        readonly Random rng;
        readonly Frog[] frogs = new Frog[4];
        Lowpass level;

        struct Frog
        {
            public float Pitch, Pan, Until, Time, Phase, Pulses, Gain;
            public bool Calling;
        }

        public Frogs(int rate, Random rng)
        {
            this.rate = rate;
            this.rng = rng;
            for (int f = 0; f < frogs.Length; f++)
                frogs[f] = new Frog { Pitch = Range(rng, 170f, 320f), Pan = Range(rng, -0.6f, 0.6f), Until = Range(rng, 1f, 8f), Gain = Range(rng, 0.5f, 1f) };
        }

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n, float target, float pan)
        {
            float dt = 1f / rate, levelCoef = Lowpass.Coef(0.3f, rate);
            float lv = 0f;
            for (int i = 0; i < n; i++) lv = level.Process(target, levelCoef);
            if (lv < 0.01f) return;
            for (int f = 0; f < frogs.Length; f++)
            {
                ref var frog = ref frogs[f];
                // Fewer frogs call when there is less water near, the first ones first.
                bool awake = lv > f * 0.2f;
                float p = Math.Clamp(pan * 0.6f + frog.Pan, -1f, 1f);
                float gl = Pan(p, true) * 0.05f * lv * frog.Gain, gr = Pan(p, false) * 0.05f * lv * frog.Gain;
                for (int i = 0; i < n; i++)
                {
                    if (!frog.Calling)
                    {
                        frog.Until -= dt;
                        if (frog.Until <= 0f && awake)
                        {
                            frog.Calling = true;
                            frog.Time = 0f;
                            frog.Pulses = MathF.Round(Range(rng, 4f, 9f));
                        }
                        continue;
                    }
                    frog.Time += dt;
                    float length = frog.Pulses * 0.04f;
                    if (frog.Time >= length)
                    {
                        frog.Calling = false;
                        frog.Until = Range(rng, 1.2f, 6f);
                        continue;
                    }
                    // Each pulse is a quick throaty burst; the call rises a little at the end.
                    float u = frog.Time / length;
                    float pulse = MathF.Sin(MathF.PI * (frog.Time % 0.04f) / 0.04f);
                    float env = pulse * pulse * MathF.Sin(MathF.PI * Math.Min(1f, u * 1.1f));
                    frog.Phase += 2f * MathF.PI * frog.Pitch * (1f + 0.12f * u) * dt;
                    float s = (MathF.Sin(frog.Phase) + 0.5f * MathF.Sin(frog.Phase * 2f) + 0.25f * MathF.Sin(frog.Phase * 3f)) * env;
                    l[i] += s * gl;
                    r[i] += s * gr;
                    sl[i] += s * gl * 0.6f;
                    sr[i] += s * gr * 0.6f;
                }
            }
        }
    }

    // --- Footsteps --------------------------------------------------------------

    // Soft swishes on grass, duller thuds on bare earth, a heavy sticky press on
    // clay, a hollow knock on a hut's floor, a grainy shuffle on sand, a crunch in
    // snow, a squelch when the ground is wet and a splash in water. Every step is a
    // little different.
    sealed class Steps
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        readonly List<Step> active = new();
        bool leftFoot;

        sealed class Step
        {
            public Ground Ground;
            public float Wet, Age, Gain, Pitch, Pan, Phase, BubblePhase, Phase2;
            public BandPass Swish, Squelch;
            public Lowpass Dull, SplashLow;
        }

        public Steps(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Trigger(Ground ground, float wetness) => Trigger(ground, wetness, 1f, 1f, leftFoot ? 0.12f : -0.12f, true);

        // A landing sets both feet down at once: heavier (gain) and lower (pitch).
        public void Trigger(Ground ground, float wetness, float weight, float pitch, float pan, bool walking = false)
        {
            if (walking) leftFoot = !leftFoot;
            if (active.Count > 6) active.RemoveAt(0);
            active.Add(new Step
            {
                Ground = ground,
                Wet = wetness,
                Gain = Range(rng, 0.8f, 1.15f) * weight,
                Pitch = Range(rng, 0.85f, 1.15f) * pitch,
                Pan = pan,
            });
        }

        public void Render(float[] l, float[] r, int n)
        {
            float dt = 1f / rate;
            float dull = Lowpass.Coef(900f, rate), splashLow = Lowpass.Coef(700f, rate);
            for (int k = active.Count - 1; k >= 0; k--)
            {
                var step = active[k];
                float pl = Pan(step.Pan, true), pr = Pan(step.Pan, false);
                for (int i = 0; i < n; i++)
                {
                    float t = step.Age;
                    float x = noise.Next();
                    float attack = Math.Min(1f, t / 0.004f);
                    float s;
                    if (step.Ground == Ground.Water)
                    {
                        float hp = x - step.SplashLow.Process(x, splashLow);
                        s = hp * attack * MathF.Exp(-t / 0.09f) * 0.28f;
                        step.BubblePhase += 2f * MathF.PI * (320f + 9000f * Math.Min(t, 0.06f)) * step.Pitch * dt;
                        s += MathF.Sin(step.BubblePhase) * MathF.Exp(-t / 0.05f) * 0.05f;
                    }
                    else if (step.Ground == Ground.Snow)
                    {
                        // Snow packing under the foot: a quick run of tiny crackles
                        // that thins out, over a soft, muffled press.
                        float grain = MathF.Abs(noise.Next()) > 0.82f - 0.5f * Math.Min(1f, t / 0.12f) ? 1f : 0.12f;
                        float crunch = step.Swish.Process(x * grain, 2100f * step.Pitch, 0.7f, rate);
                        float env = attack * Math.Min(1f, t / 0.02f + 0.3f) * MathF.Exp(-t / 0.075f);
                        step.Phase += 2f * MathF.PI * 70f * step.Pitch * dt;
                        float press = MathF.Sin(step.Phase) * MathF.Exp(-t / 0.04f) * attack;
                        s = crunch * env * 0.34f + press * 0.1f;
                    }
                    else if (step.Ground == Ground.Sand)
                    {
                        // Sand giving way: a grainy, hissing shuffle and a soft thud.
                        // Wet sand packs firmer, so it hisses less and thuds more.
                        float grain = MathF.Abs(noise.Next()) > 0.55f ? 1f : 0.35f;
                        float hiss = step.Swish.Process(x * grain, 3600f * step.Pitch, 0.8f, rate);
                        float env = MathF.Exp(-t / 0.05f) + 0.35f * MathF.Exp(-MathF.Max(0f, t - 0.06f) / 0.04f) * (t > 0.06f ? 1f : 0f);
                        step.Phase += 2f * MathF.PI * 75f * step.Pitch * dt;
                        float thud = MathF.Sin(step.Phase) * MathF.Exp(-t / 0.03f) * attack;
                        s = hiss * env * attack * 0.2f * (1f - 0.55f * step.Wet) + thud * (0.12f + 0.1f * step.Wet);
                    }
                    else if (step.Ground == Ground.Floor)
                    {
                        // Packed boards: two body resonances and a short click.
                        step.Phase += 2f * MathF.PI * 185f * step.Pitch * dt;
                        step.Phase2 += 2f * MathF.PI * 455f * step.Pitch * dt;
                        float body = MathF.Sin(step.Phase) * MathF.Exp(-t / 0.055f) + 0.5f * MathF.Sin(step.Phase2) * MathF.Exp(-t / 0.025f);
                        float click = (x - step.Dull.Process(x, dull)) * MathF.Exp(-t / 0.004f);
                        s = (body * 0.2f + click * 0.12f) * attack;
                    }
                    else
                    {
                        step.Phase += 2f * MathF.PI * (step.Ground == Ground.Clay ? 62f : 80f) * step.Pitch * dt;
                        float thump = MathF.Sin(step.Phase) * MathF.Exp(-t / 0.028f) * attack;
                        if (step.Ground == Ground.Grass)
                        {
                            // Leaves of grass brushing the foot, then a second, lighter rustle.
                            float swish = step.Swish.Process(x, 2600f * step.Pitch, 0.9f, rate);
                            float env = MathF.Exp(-t / 0.035f) + 0.4f * MathF.Exp(-MathF.Max(0f, t - 0.05f) / 0.03f) * (t > 0.05f ? 1f : 0f);
                            s = swish * env * attack * 0.22f + thump * 0.16f;
                        }
                        else if (step.Ground == Ground.Clay)
                        {
                            // Heavier and softer than earth, and the foot pulls free
                            // with a small sucking sound, louder when the clay is wet.
                            float d = step.Dull.Process(x, dull * 0.6f);
                            s = d * MathF.Exp(-t / 0.045f) * attack * 0.45f + thump * 0.3f;
                            float pull = t > 0.11f ? MathF.Exp(-(t - 0.11f) / 0.04f) : 0f;
                            float suck = step.Squelch.Process(x, (500f + 3000f * Math.Max(0f, t - 0.11f)) * step.Pitch, 0.3f, rate);
                            s += suck * pull * (0.03f + 0.08f * step.Wet);
                        }
                        else
                        {
                            float d = step.Dull.Process(x, dull);
                            s = d * MathF.Exp(-t / 0.03f) * attack * 0.3f + thump * 0.18f;
                        }
                        if (step.Wet > 0.15f && step.Ground != Ground.Clay)
                        {
                            float center = (1300f - 9000f * Math.Min(t, 0.09f)) * step.Pitch;
                            float squelch = step.Squelch.Process(x, Math.Max(center, 400f), 0.22f, rate);
                            s += squelch * MathF.Exp(-t / 0.05f) * attack * 0.09f * step.Wet;
                        }
                    }
                    s *= step.Gain;
                    l[i] += s * pl;
                    r[i] += s * pr;
                    step.Age += dt;
                }
                if (step.Age > 0.4f) active.RemoveAt(k);
            }
        }
    }

    // --- Cues ---------------------------------------------------------------------

    // Small sounds for things that just happened, on the effects bus with the work
    // sounds: a rustle and soft knock as something goes in your bag, crunching
    // bites, gulps of water, a fire catching with a soft whoosh, a dull knock as
    // something is set down. A skill's new title rings the journal's soft wooden
    // two-note chime, and a new journal entry one faint note, both with a little of
    // the forest's reverb.
    sealed class Cues
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        readonly List<Voice> active = new();

        sealed class Voice
        {
            public Cue Cue;
            public CueDef? Def;                 // a data cue (CueDefs) instead of a Cue enum one
            public float[]? Phases;             // per layer, for tones
            public Lowpass[]? Hp, Lp;           // per layer, for noise
            public float Age, Pitch, Phase, Phase2, Gain = 1f, Pan;
            public BandPass Band;
            public Lowpass Low;
        }

        public Cues(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        // A cue defined as data: its layers are rendered by the generic voice below.
        public void Trigger(CueDef def, float gain = 1f, float pan = 0f)
        {
            if (active.Count > 8) active.RemoveAt(0);
            active.Add(new Voice
            {
                Def = def, Pitch = Range(rng, 0.96f, 1.04f), Gain = gain, Pan = Math.Clamp(pan, -1f, 1f),
                Phases = new float[def.Layers.Length], Hp = new Lowpass[def.Layers.Length], Lp = new Lowpass[def.Layers.Length],
            });
        }

        public void Trigger(Cue cue, float gain = 1f, float pan = 0f)
        {
            if (active.Count > 8) active.RemoveAt(0);
            active.Add(new Voice { Cue = cue, Pitch = Range(rng, 0.92f, 1.08f), Gain = gain, Pan = Math.Clamp(pan, -1f, 1f) });
        }

        // The journal's chime (it was journal/Chime.cs): each note a marimba bar's
        // fundamental and its bright 3.9x partial with a soft mallet, then two quiet
        // echoes so it hangs in the air a moment.
        static readonly (float Freq, float At, float Gain)[] Title = { (587.33f, 0f, 1f), (880f, 0.16f, 0.8f) };
        static readonly (float Freq, float At, float Gain)[] Discovery = { (783.99f, 0f, 0.55f) };

        static float Marimba(float t, (float Freq, float At, float Gain)[] notes) =>
            Bars(t, notes) + 0.22f * Bars(t - 0.23f, notes) + 0.1f * Bars(t - 0.47f, notes);

        static float Bars(float t, (float Freq, float At, float Gain)[] notes)
        {
            float sum = 0f;
            foreach (var (freq, at, gain) in notes)
            {
                float u = t - at;
                if (u < 0f) continue;
                float body = MathF.Sin(MathF.Tau * freq * u) * MathF.Exp(-u * 3.2f);
                float ring = 0.25f * MathF.Sin(MathF.Tau * freq * 3.9f * u) * MathF.Exp(-u * 14f);
                sum += gain * Math.Min(1f, u / 0.004f) * (body + ring);
            }
            return sum;
        }

        // One sample of a data cue: every layer that has started, as a noise band or a gliding tone.
        float DefSample(Voice v, float t, float x, float dt)
        {
            float sum = 0f;
            var layers = v.Def!.Layers;
            for (int j = 0; j < layers.Length; j++)
            {
                var l = layers[j];
                float u = t - l.Start;
                if (u < 0f) continue;
                float env = Math.Min(1f, u / l.Rise) * MathF.Exp(-u / l.Decay);
                if (env < 0.0005f) continue;
                float src;
                if (l.Source == Src.Noise)
                {
                    // Between the cutoffs: low-pass at Hi, then take away what is below Lo.
                    float body = v.Lp![j].Process(x, Lowpass.Coef(l.Hi, rate));
                    src = body - v.Hp![j].Process(body, Lowpass.Coef(l.Lo, rate));
                }
                else
                {
                    float f = l.Hi + (l.Lo - l.Hi) * MathF.Exp(-u / l.Decay);
                    v.Phases![j] += MathF.Tau * f * v.Pitch * dt;
                    src = MathF.Sin(v.Phases[j]);
                }
                sum += src * env * l.Gain;
            }
            return sum;
        }

        static float Burst(float t, float start, float decay) => t < start ? 0f : MathF.Min(1f, (t - start) / 0.004f) * MathF.Exp(-(t - start) / decay);

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n)
        {
            float dt = 1f / rate;
            for (int k = active.Count - 1; k >= 0; k--)
            {
                var v = active[k];
                float pl = Pan(v.Pan, true) * 1.4142f, pr = Pan(v.Pan, false) * 1.4142f;
                float length = v.Def != null ? v.Def.Length : v.Cue switch { Cue.SkillUp => 2.2f, Cue.Learn => 1.6f, Cue.Eat => 0.9f, Cue.Drink => 0.8f, Cue.FireLit => 1f, Cue.Crit => 0.7f, Cue.Jump => 0.3f, Cue.BowDraw => 0.8f, Cue.BowShot => 0.6f, Cue.ArrowWood => 0.5f, _ => 0.4f };
                for (int i = 0; i < n; i++)
                {
                    float t = v.Age, x = noise.Next(), s = 0f, send = 0f;
                    if (v.Def != null)
                    {
                        s = DefSample(v, t, x, dt);
                        send = s * v.Def.Send;
                    }
                    else switch (v.Cue)
                    {
                        case Cue.Pickup:
                        {
                            // Leaves and cloth brushing, then a soft knock in the bag.
                            float rustle = v.Band.Process(x, 2800f * v.Pitch, 0.9f, rate) * Burst(t, 0f, 0.05f);
                            v.Phase += 2f * MathF.PI * 140f * v.Pitch * dt;
                            s = rustle * 0.35f + MathF.Sin(v.Phase) * Burst(t, 0.07f, 0.03f) * 0.17f;
                            break;
                        }
                        case Cue.Eat:
                        {
                            // Three bites, each a short crunchy burst.
                            float bites = Burst(t, 0f, 0.035f) + 0.8f * Burst(t, 0.28f, 0.03f) + 0.65f * Burst(t, 0.55f, 0.03f);
                            float grain = MathF.Abs(noise.Next()) > 0.6f ? 1f : 0.3f;
                            s = v.Band.Process(x * grain, 1700f * v.Pitch, 0.8f, rate) * bites * 0.65f;
                            break;
                        }
                        case Cue.Drink:
                        {
                            // Two gulps, each a short drop in pitch, over a little slosh.
                            float g1 = Burst(t, 0.05f, 0.07f), g2 = Burst(t, 0.38f, 0.07f);
                            float since = t < 0.38f ? t - 0.05f : t - 0.38f;
                            v.Phase += 2f * MathF.PI * (260f - 500f * MathF.Max(0f, since)) * v.Pitch * dt;
                            float slosh = v.Low.Process(x, Lowpass.Coef(900f, rate)) * Burst(t, 0f, 0.15f);
                            s = MathF.Sin(v.Phase) * (g1 + g2) * 0.11f + slosh * 0.2f;
                            break;
                        }
                        case Cue.FireLit:
                        {
                            // A soft whoosh as the flame takes, opening up as it grows.
                            float open = Math.Min(1f, t / 0.35f);
                            float w = v.Low.Process(x, Lowpass.Coef(300f + 2500f * open, rate));
                            s = w * MathF.Sin(MathF.PI * Math.Min(1f, t / 0.9f)) * 0.24f;
                            break;
                        }
                        case Cue.BowDraw:
                        {
                            // Wood and sinew under strain: an irregular, slow creak. Band-passed
                            // noise whose centre wanders and whose loudness stutters, never a steady tone.
                            float wander = 650f + 220f * MathF.Sin(t * 5.1f) + 140f * MathF.Sin(t * 12.7f + 1f);
                            float stutter = Math.Max(0f, MathF.Sin(t * 2f * MathF.PI * 7.3f) * MathF.Sin(t * 2f * MathF.PI * 11.1f + 0.7f));
                            float rub = v.Band.Process(x, wander * v.Pitch, 0.35f, rate);
                            float strain = MathF.Sin(MathF.PI * Math.Min(1f, t / 0.8f));
                            s = rub * (0.25f + stutter) * strain * 0.16f;
                            break;
                        }
                        case Cue.BowShot:
                        {
                            // The string's twang (a few damped low tones, a touch sharp at first) and the
                            // whoosh of the arrow leaving.
                            float glide = 1f + 0.03f * MathF.Exp(-t / 0.04f);
                            v.Phase += 2f * MathF.PI * 165f * v.Pitch * glide * dt;
                            v.Phase2 += 2f * MathF.PI * 330f * v.Pitch * glide * dt;
                            float twang = MathF.Sin(v.Phase) * MathF.Exp(-t / 0.2f) + 0.5f * MathF.Sin(v.Phase2) * MathF.Exp(-t / 0.11f)
                                + 0.25f * MathF.Sin(v.Phase * 3f) * MathF.Exp(-t / 0.07f);
                            float whoosh = v.Band.Process(x, 1900f - 1200f * Math.Min(1f, t / 0.3f), 0.6f, rate) * Burst(t, 0.01f, 0.1f);
                            s = Math.Min(1f, t / 0.002f) * twang * 0.08f + whoosh * 0.22f;
                            send = twang * 0.01f;
                            break;
                        }
                        case Cue.ArrowFlesh:
                        {
                            // Into an animal: a dull, wet thunk.
                            v.Phase += 2f * MathF.PI * (120f - 200f * Math.Min(t, 0.08f)) * v.Pitch * dt;
                            float wet = v.Low.Process(x, Lowpass.Coef(800f, rate)) * Burst(t, 0f, 0.04f);
                            s = MathF.Sin(v.Phase) * Burst(t, 0f, 0.07f) * 0.14f + wet * 0.25f;
                            break;
                        }
                        case Cue.ArrowWood:
                        {
                            // Into a tree: a sharp thock and the shaft humming for a moment.
                            v.Phase += 2f * MathF.PI * 380f * v.Pitch * dt;
                            v.Phase2 += 2f * MathF.PI * 940f * v.Pitch * dt;
                            float knock = v.Band.Process(x, 1500f, 0.7f, rate) * Burst(t, 0f, 0.012f);
                            s = knock * 0.4f + MathF.Sin(v.Phase) * Burst(t, 0f, 0.08f) * 0.12f + MathF.Sin(v.Phase2) * Burst(t, 0f, 0.22f) * 0.03f;
                            send = s * 0.2f;
                            break;
                        }
                        case Cue.ArrowEarth:
                        {
                            // Into the ground: a dull thud and a little grit.
                            v.Phase += 2f * MathF.PI * 85f * v.Pitch * dt;
                            float grit = v.Low.Process(x, Lowpass.Coef(1000f, rate)) * Burst(t, 0f, 0.05f);
                            s = MathF.Sin(v.Phase) * Burst(t, 0f, 0.06f) * 0.14f + grit * 0.18f;
                            break;
                        }
                        case Cue.Learn:
                            // One faint wooden note, G5: something new in the journal.
                            s = Marimba(t, Discovery) * 0.017f;
                            send = s * 0.6f;
                            break;
                        case Cue.SkillUp:
                            // D5 then A5 on a wooden bar, an open fifth: a skill earned a new title.
                            s = Marimba(t, Title) * 0.027f;
                            send = s * 0.6f;
                            break;
                    }
                    s *= v.Gain;
                    send *= v.Gain;
                    l[i] += s * pl;
                    r[i] += s * pr;
                    sl[i] += send * pl;
                    sr[i] += send * pr;
                    v.Age += dt;
                }
                if (v.Age > length) active.RemoveAt(k);
            }
        }
    }

    // --- Work -------------------------------------------------------------------

    // The sound of your own hands at work while a craft runs: stone clicking on
    // stone, a drill whirring into a hiss of smoke, fibres rustling, cord creaking
    // tight, wet pats of clay, a scraper rasping on hide, shavings curling off wood.
    // It ends in a soft thunk when the work comes good, a dull snap when it fails,
    // and a breath of flame when a drill's ember catches.
    sealed class WorkVoice
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        readonly List<Hit> hits = new();
        Work kind, sounding;
        // Whose hands: 1 and centred for your own, quieter and placed for others.
        public float Gain = 1f, Side;
        float level, time, nextHit, strokePhase;
        BandPass friction, hiss;
        Lowpass smokeLow;

        enum HitKind { Knap, Flake, Pat, Rustle, Creak, Shave, Thunk, Snap, Ember }

        sealed class Hit
        {
            public HitKind Kind;
            public float Age, Delay, Gain, Pitch, Pan, Length, Phase, Phase2, Pulse;
            public BandPass Band;
            public Lowpass Low;
        }

        public WorkVoice(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public bool Idle => kind == Work.None && level < 0.0005f && hits.Count == 0;

        public void Set(Work work)
        {
            if (work == kind) return;
            kind = work;
            if (work == Work.None) return;
            sounding = work;
            time = 0f;
            nextHit = Range(rng, 0.05f, 0.2f);
        }

        public void Done(Work work, bool failed)
        {
            if (work == Work.None) return;
            if (failed) Add(HitKind.Snap, 0f, 1f);
            else if (work == Work.Drill) Add(HitKind.Ember, 0f, 1f);
            else Add(HitKind.Thunk, 0f, 1f);
        }

        void Add(HitKind k, float delay, float gain)
        {
            if (hits.Count > 12) hits.RemoveAt(0);
            hits.Add(new Hit
            {
                Kind = k,
                Delay = delay,
                Gain = gain * Range(rng, 0.8f, 1.15f),
                Pitch = Range(rng, 0.88f, 1.12f),
                Pan = Range(rng, -0.15f, 0.15f),
                Length = k == HitKind.Creak ? Range(rng, 0.15f, 0.3f) : k == HitKind.Ember ? 0.9f : 0.5f,
            });
        }

        // The next stroke of whatever is being worked, and how long until the one after.
        float Schedule()
        {
            switch (kind)
            {
                case Work.Knap:
                    Add(HitKind.Knap, 0f, 1f);
                    if (rng.NextDouble() < 0.35) Add(HitKind.Flake, Range(rng, 0.07f, 0.16f), 1f);
                    return Range(rng, 0.7f, 1.3f);
                case Work.Clay:
                    Add(HitKind.Pat, 0f, 0.6f);
                    return Range(rng, 0.42f, 0.8f);
                case Work.Fibre:
                    Add(HitKind.Rustle, 0f, 1f);
                    return Range(rng, 0.22f, 0.55f);
                case Work.Lash:
                    if (rng.NextDouble() < 0.45) Add(HitKind.Creak, 0f, 2f);
                    else Add(HitKind.Rustle, 0f, 1.3f);
                    return Range(rng, 0.35f, 0.8f);
                case Work.Carve:
                    Add(HitKind.Shave, 0f, 1.25f);
                    return Range(rng, 0.5f, 0.9f);
                default:
                    return 1f;
            }
        }

        public void Render(float[] l, float[] r, int n)
        {
            float dt = 1f / rate;
            float fade = 1f - MathF.Exp(-dt / 0.08f);
            float smoke = Lowpass.Coef(2500f, rate);
            float pl0 = Pan(Side, true), pr0 = Pan(Side, false);
            for (int i = 0; i < n; i++)
            {
                level += fade * ((kind != Work.None ? 1f : 0f) - level);
                if (kind != Work.None)
                {
                    time += dt;
                    nextHit -= dt;
                    if (nextHit <= 0f) nextHit = Schedule();
                }
                if (level < 0.0005f) continue;
                float s = 0f;
                if (sounding == Work.Drill)
                {
                    // A shaft twirled back and forth: friction rises and falls with
                    // its speed, and a hiss of smoke grows as the hearth heats up.
                    strokePhase += 2f * MathF.PI * 2.3f * dt;
                    float speed = MathF.Abs(MathF.Sin(strokePhase));
                    float x = noise.Next();
                    float f = friction.Process(x, 600f + 900f * speed, 0.5f, rate);
                    float heat = Math.Min(1f, time / 3f);
                    float h = x - smokeLow.Process(x, smoke);
                    s += f * MathF.Pow(speed, 0.8f) * (0.1f + 0.04f * heat) + h * heat * speed * 0.016f;
                }
                else if (sounding == Work.Rasp)
                {
                    // A scraper drawn over a hide: strokes forward, a lift back.
                    strokePhase += 2f * MathF.PI * 1.1f * dt;
                    float stroke = MathF.Pow(MathF.Max(0f, MathF.Sin(strokePhase)), 1.5f);
                    float x = noise.Next();
                    float b = hiss.Process(x, 1700f + 700f * stroke, 0.55f, rate);
                    s += b * stroke * (0.65f + 0.35f * MathF.Sin(2f * MathF.PI * 37f * time)) * 0.2f;
                }
                s *= level * Gain;
                l[i] += s * pl0;
                r[i] += s * pr0;
            }

            for (int k = hits.Count - 1; k >= 0; k--)
            {
                var hit = hits[k];
                float hp = Math.Clamp(hit.Pan + Side, -1f, 1f);
                float pl = Pan(hp, true), pr = Pan(hp, false);
                for (int i = 0; i < n; i++)
                {
                    if (hit.Delay > 0f)
                    {
                        hit.Delay -= dt;
                        continue;
                    }
                    float t = hit.Age, x = noise.Next(), s = 0f;
                    float attack = Math.Min(1f, t / 0.002f);
                    switch (hit.Kind)
                    {
                        case HitKind.Knap:
                        {
                            // A hard click, the core ringing high for a moment, a knock underneath.
                            float click = (x - hit.Low.Process(x, 0.35f)) * MathF.Exp(-t / 0.003f);
                            hit.Phase += 2f * MathF.PI * 2400f * hit.Pitch * dt;
                            hit.Phase2 += 2f * MathF.PI * 3650f * hit.Pitch * dt;
                            float ring = (MathF.Sin(hit.Phase) + 0.6f * MathF.Sin(hit.Phase2)) * MathF.Exp(-t / 0.022f);
                            float knock = hit.Band.Process(x, 320f * hit.Pitch, 0.4f, rate) * MathF.Exp(-t / 0.02f);
                            s = (click * 0.5f + ring * 0.07f + knock * 0.5f) * attack;
                            break;
                        }
                        case HitKind.Flake:
                        {
                            // A chip skittering away.
                            hit.Phase += 2f * MathF.PI * 4800f * hit.Pitch * dt;
                            s = MathF.Sin(hit.Phase) * MathF.Exp(-t / 0.008f) * 0.05f * attack;
                            break;
                        }
                        case HitKind.Pat:
                        {
                            // Palm on wet clay: a soft slap, a body thump and a little squelch.
                            float slap = hit.Low.Process(x, 0.08f) * MathF.Exp(-t / 0.03f);
                            hit.Phase += 2f * MathF.PI * 95f * hit.Pitch * dt;
                            float thump = MathF.Sin(hit.Phase) * MathF.Exp(-t / 0.04f);
                            float sq = hit.Band.Process(x, Math.Max(450f, (1400f - 9000f * t) * hit.Pitch), 0.25f, rate) * MathF.Exp(-t / 0.05f);
                            s = (slap * 0.9f + thump * 0.18f + sq * 0.07f) * Math.Min(1f, t / 0.004f);
                            break;
                        }
                        case HitKind.Rustle:
                        {
                            // Fibres drawn through the fingers: a swell and a fall.
                            float env = Math.Min(1f, t / 0.03f) * MathF.Exp(-MathF.Max(0f, t - 0.03f) / 0.1f);
                            s = hit.Band.Process(x, 3400f * hit.Pitch, 1.1f, rate) * env * 0.1f;
                            break;
                        }
                        case HitKind.Creak:
                        {
                            // Cord pulled tight: a slow train of tiny clicks through a woody
                            // resonance, speeding up as the knot bites.
                            hit.Pulse += (70f + 80f * t / hit.Length) * hit.Pitch * dt;
                            float impulse = 0f;
                            if (hit.Pulse >= 1f)
                            {
                                hit.Pulse -= 1f;
                                impulse = 1f;
                            }
                            float body = hit.Band.Process(impulse + 0.05f * x, 850f * hit.Pitch, 0.12f, rate);
                            float env = t < hit.Length ? MathF.Sin(MathF.PI * t / hit.Length) : 0f;
                            s = body * env * 0.5f;
                            break;
                        }
                        case HitKind.Shave:
                        {
                            // A blade lifting a curl of wood: a tick, then a falling scrape.
                            float tick = (x - hit.Low.Process(x, 0.3f)) * MathF.Exp(-t / 0.002f);
                            float env = Math.Min(1f, t / 0.01f) * MathF.Exp(-MathF.Max(0f, t - 0.01f) / 0.08f);
                            float b = hit.Band.Process(x, (4200f - 20000f * Math.Min(t, 0.13f)) * hit.Pitch, 0.7f, rate);
                            s = tick * 0.15f + b * env * 0.16f;
                            break;
                        }
                        case HitKind.Thunk:
                        {
                            // Set down, done.
                            hit.Phase += 2f * MathF.PI * 120f * dt;
                            float body = MathF.Sin(hit.Phase) * MathF.Exp(-t / 0.06f);
                            float click = hit.Low.Process(x, 0.25f) * MathF.Exp(-t / 0.008f);
                            s = (body * 0.22f + click * 0.3f) * attack;
                            break;
                        }
                        case HitKind.Snap:
                        {
                            // It cracked wrong: a sharp snap, a short crack and a dull drop.
                            float snap = (x - hit.Low.Process(x, 0.3f)) * MathF.Exp(-t / 0.006f);
                            float crack = hit.Band.Process(x, 1200f, 0.35f, rate) * MathF.Exp(-t / 0.04f);
                            hit.Phase += 2f * MathF.PI * 70f * dt;
                            float drop = t > 0.06f ? MathF.Sin(hit.Phase) * MathF.Exp(-(t - 0.06f) / 0.1f) : 0f;
                            s = (snap * 0.4f + crack * 0.25f + drop * 0.16f) * attack;
                            break;
                        }
                        case HitKind.Ember:
                        {
                            // The ember catches in the tinder: a soft breath of flame.
                            float swell = Math.Min(1f, t / 0.25f) * MathF.Exp(-MathF.Max(0f, t - 0.25f) / 0.25f);
                            float low = hit.Low.Process(x, Lowpass.Coef(300f + 1500f * Math.Min(1f, t / 0.3f), rate));
                            s = low * swell * 0.55f;
                            break;
                        }
                    }
                    s *= hit.Gain * Gain;
                    l[i] += s * pl;
                    r[i] += s * pr;
                    hit.Age += dt;
                }
                if (hit.Age > hit.Length) hits.RemoveAt(k);
            }
        }
    }

    // --- Sheep ------------------------------------------------------------------

    // A buzzy voice shaped by two vowel formants ("baa"), with the quick warble
    // that makes it a bleat. Startled bleats are higher, shorter and sharper.
    sealed class Sheep
    {
        readonly int rate;
        readonly Random rng;
        readonly List<Voice> voices = new();

        sealed class Voice
        {
            public float Pitch, Length, Gain, Pan, Warble, Age, Phase, Formant;
            public BandPass Low, High;
        }

        public Sheep(int rate, Random rng)
        {
            this.rate = rate;
            this.rng = rng;
        }

        // Lambs bleat higher, shorter and softer, through a smaller throat.
        public void Trigger(float pan, float gain, bool startled, bool lamb = false)
        {
            if (voices.Count > 5 || gain < 0.01f) return;
            voices.Add(new Voice
            {
                Pitch = (startled ? Range(rng, 380f, 470f) : Range(rng, 230f, 330f)) * (lamb ? Range(rng, 1.55f, 1.8f) : 1f),
                Length = (startled ? Range(rng, 0.3f, 0.45f) : Range(rng, 0.6f, 1.0f)) * (lamb ? 0.7f : 1f),
                Gain = gain * (startled ? 0.11f : 0.075f) * (lamb ? 0.65f : 1f),
                Pan = Math.Clamp(pan, -1f, 1f),
                Warble = Range(rng, 6f, 8.5f) * (lamb ? 1.25f : 1f),
                Formant = lamb ? 1.35f : 1f,
            });
        }

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n)
        {
            float dt = 1f / rate;
            for (int v = voices.Count - 1; v >= 0; v--)
            {
                var voice = voices[v];
                float pl = Pan(voice.Pan, true), pr = Pan(voice.Pan, false);
                for (int i = 0; i < n && voice.Age < voice.Length; i++)
                {
                    float u = voice.Age / voice.Length;
                    // The pitch sags a little through the call.
                    float freq = voice.Pitch * (1.05f - 0.12f * u) * (1f + 0.02f * MathF.Sin(voice.Age * 2f * MathF.PI * voice.Warble));
                    voice.Phase += freq * dt;
                    voice.Phase -= MathF.Floor(voice.Phase);
                    float saw = 2f * voice.Phase - 1f;
                    float tremolo = 0.6f + 0.4f * MathF.Sin(voice.Age * 2f * MathF.PI * voice.Warble);
                    float env = MathF.Min(1f, voice.Age / 0.05f) * MathF.Min(1f, (voice.Length - voice.Age) / 0.15f);
                    float formants = voice.Low.Process(saw, 750f * voice.Formant, 0.5f, rate) + 0.6f * voice.High.Process(saw, 1650f * voice.Formant, 0.6f, rate);
                    float s = formants * tremolo * env * voice.Gain;
                    l[i] += s * pl;
                    r[i] += s * pr;
                    sl[i] += s * pl * 0.4f;
                    sr[i] += s * pr * 0.4f;
                    voice.Age += dt;
                }
                if (voice.Age >= voice.Length) voices.RemoveAt(v);
            }
        }
    }

    // --- Red deer ---------------------------------------------------------------

    // Deer are quiet. A startled one gives a short hoarse bark; at dusk in autumn a
    // stag lets out a long, deep roar that rolls and cracks and carries a long way;
    // running deer thud softly. Distance rounds the sound off (a low-pass) and
    // sends more of it to the reverb, so a far roar is a low rolling shape.
    public enum DeerCall { Bark, Roar, Hoof }

    sealed class Deer
    {
        readonly int rate;
        readonly Random rng;
        readonly Noise noise;
        readonly List<Voice> voices = new();

        sealed class Voice
        {
            public DeerCall Call;
            public float Pitch, Length, Gain, Pan, Far, Age, Phase, Rough;
            public BandPass Low, High, Body;
            public Lowpass Round;
        }

        public Deer(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        // gain 0 to 1 from distance; far 0 (close) to 1 (a long way off).
        public void Trigger(DeerCall call, float pan, float gain, float far)
        {
            if (voices.Count > 6 || gain < 0.01f) return;
            var v = new Voice { Call = call, Pan = Math.Clamp(pan, -1f, 1f), Far = Math.Clamp(far, 0f, 1f) };
            switch (call)
            {
                case DeerCall.Bark:
                    v.Pitch = Range(rng, 330f, 420f);
                    v.Length = Range(rng, 0.16f, 0.24f);
                    v.Gain = gain * 0.125f;
                    v.Rough = 0.55f;
                    break;
                case DeerCall.Roar:
                    v.Pitch = Range(rng, 78f, 105f);
                    v.Length = Range(rng, 2.6f, 3.6f);
                    v.Gain = gain * 0.05f;
                    v.Rough = 0.8f;
                    break;
                default:
                    v.Pitch = Range(rng, 70f, 95f);
                    v.Length = 0.11f;
                    v.Gain = gain * 0.07f;
                    break;
            }
            voices.Add(v);
        }

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n)
        {
            float dt = 1f / rate;
            for (int k = voices.Count - 1; k >= 0; k--)
            {
                var v = voices[k];
                float pl = Pan(v.Pan, true), pr = Pan(v.Pan, false);
                float round = Lowpass.Coef(6000f - 5200f * v.Far, rate);
                float send = 0.25f + 0.7f * v.Far;
                for (int i = 0; i < n && v.Age < v.Length; i++)
                {
                    float u = v.Age / v.Length;
                    float s;
                    switch (v.Call)
                    {
                        case DeerCall.Bark:
                        {
                            // A sharp cough: a buzzy tone dropping fast, mixed with breath,
                            // through a throat formant, cut off short.
                            float freq = v.Pitch * (1.25f - 0.45f * u);
                            v.Phase += freq * dt;
                            v.Phase -= MathF.Floor(v.Phase);
                            float buzz = 2f * v.Phase - 1f;
                            float src = buzz * 0.7f + noise.Next() * v.Rough * 0.9f;
                            float env = MathF.Min(1f, v.Age / 0.008f) * MathF.Pow(1f - u, 1.6f);
                            s = (v.Low.Process(src, 900f, 0.45f, rate) + 0.7f * v.High.Process(src, 1900f, 0.55f, rate)) * env;
                            break;
                        }
                        case DeerCall.Roar:
                        {
                            // A low voice that swells, bends up then sags, shaken by a fast
                            // growl (about 28 Hz) with rough noise riding on top.
                            float bend = 1f + 0.35f * MathF.Sin(MathF.PI * MathF.Min(1f, u * 1.4f)) - 0.25f * u * u;
                            float growl = 0.55f + 0.45f * MathF.Sin(v.Age * 2f * MathF.PI * (24f + 8f * u));
                            v.Phase += v.Pitch * bend * dt;
                            v.Phase -= MathF.Floor(v.Phase);
                            float saw = 2f * v.Phase - 1f;
                            float src = saw + noise.Next() * v.Rough * 0.5f;
                            float env = MathF.Min(1f, v.Age / 0.35f) * MathF.Min(1f, (v.Length - v.Age) / 0.9f);
                            float body = v.Body.Process(src, 230f + 120f * u, 0.4f, rate) * 1.2f;
                            float mouth = v.Low.Process(src, 520f + 300f * MathF.Sin(MathF.PI * u), 0.5f, rate) * 0.6f;
                            s = (body + mouth) * growl * env;
                            break;
                        }
                        default:
                        {
                            // A dull hoof on soft ground: a short low thump with a little scuff.
                            float env = MathF.Exp(-v.Age / 0.03f) * MathF.Min(1f, v.Age / 0.003f);
                            v.Phase += v.Pitch * (1f - 0.5f * u) * dt;
                            s = (MathF.Sin(v.Phase * 2f * MathF.PI) + 0.4f * v.Body.Process(noise.Next(), 700f, 0.7f, rate)) * env;
                            break;
                        }
                    }
                    s = v.Round.Process(s, round) * v.Gain;
                    l[i] += s * pl;
                    r[i] += s * pr;
                    sl[i] += s * pl * send;
                    sr[i] += s * pr * send;
                    v.Age += dt;
                }
                if (v.Age >= v.Length) voices.RemoveAt(k);
            }
        }
    }

    // --- Music ------------------------------------------------------------------

    // A slow score that comes and goes: warm pad chords, a soft bass, and a few
    // kalimba-like notes picked from a pentatonic scale. It plays for a few
    // minutes, then leaves the forest to itself for a while. Nights are slower
    // and sparser.
    sealed class Music
    {
        const float RootHz = 146.83f; // D3
        static readonly int[] Scale = { 0, 2, 4, 7, 9 };
        static readonly int[][] Chords =
        {
            new[] { 0, 4, 7, 11, 14 },  // D major 9
            new[] { -3, 0, 4, 7 },      // B minor 7
            new[] { -7, -3, 0, 4 },     // G major 7
            new[] { -5, 0, 2, 7 },      // A sus
            new[] { 2, 5, 9, 12 },      // E minor 7
        };
        static readonly int[][] Progressions =
        {
            new[] { 0, 1, 2, 3 },
            new[] { 2, 0, 4, 3 },
            new[] { 0, 2, 1, 3 },
            new[] { 1, 2, 0, 3 },
        };

        readonly int rate;
        readonly Random rng;
        readonly List<Note> notes = new();
        Lowpass padL, padR;
        bool playing;
        float clock, untilChange = 8f, fade, beatClock, chordClock, gain;
        int[] progression = Progressions[0];
        int chordIndex = -1, melodyStep = 7;

        sealed class Note
        {
            public float Freq, Age, Gain, Pan, Phase, Phase2, Release = float.MaxValue;
            public bool Pad;
            public float Detune;
        }

        public Music(int rate, Random rng)
        {
            this.rate = rate;
            this.rng = rng;
        }

        static float Hz(int semitones) => RootHz * MathF.Pow(2f, semitones / 12f);

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n, bool on, float volume, float night)
        {
            float dt = 1f / rate;
            float block = n * dt;
            clock += block;
            untilChange -= block;
            if (untilChange <= 0f)
            {
                playing = !playing;
                untilChange = playing ? Range(rng, 150f, 240f) : Range(rng, 90f, 200f);
                if (playing)
                {
                    progression = Progressions[rng.Next(Progressions.Length)];
                    chordIndex = -1;
                    chordClock = 0f;
                }
            }
            // Fade in over about six seconds and out over eight.
            float wantFade = playing && untilChange > 8f ? 1f : 0f;
            fade += Math.Clamp(wantFade - fade, -block / 8f, block / 6f);
            gain += Math.Clamp((on ? Math.Clamp(volume, 0f, 4f) : 0f) - gain, -block / 1.5f, block / 1.5f);

            float beat = night > 0.5f ? 1.25f : 0.95f;
            if (playing && untilChange > 8f)
            {
                chordClock -= block;
                if (chordClock <= 0f)
                {
                    chordClock = beat * 8f;
                    chordIndex = (chordIndex + 1) % progression.Length;
                    foreach (var note in notes)
                        if (note.Pad && note.Release == float.MaxValue) note.Release = note.Age;
                    var chord = Chords[progression[chordIndex]];
                    for (int c = 0; c < chord.Length; c++)
                        notes.Add(new Note { Freq = Hz(chord[c]), Pad = true, Gain = 0.011f, Pan = -0.5f + c / (float)(chord.Length - 1), Detune = Range(rng, 0.0015f, 0.003f) });
                    notes.Add(new Note { Freq = Hz(chord[0] - 12), Pad = true, Gain = 0.016f, Pan = 0f, Detune = 0.001f });
                }
                beatClock -= block;
                if (beatClock <= 0f)
                {
                    beatClock = beat;
                    if (rng.NextDouble() < (night > 0.5f ? 0.22 : 0.38)) PlayMelodyNote(night);
                }
            }

            float padCoef = Lowpass.Coef(night > 0.5f ? 900f : 1400f, rate);
            for (int i = 0; i < n; i++)
            {
                float pl = 0f, pr = 0f, ml = 0f, mr = 0f;
                foreach (var note in notes)
                {
                    float env;
                    float s;
                    if (note.Pad)
                    {
                        env = Math.Min(1f, note.Age / 2.5f);
                        if (note.Age > note.Release) env *= MathF.Max(0f, 1f - (note.Age - note.Release) / 4f);
                        note.Phase += 2f * MathF.PI * note.Freq * (1f + note.Detune) * dt;
                        note.Phase2 += 2f * MathF.PI * note.Freq * (1f - note.Detune) * dt;
                        s = (MathF.Sin(note.Phase) + MathF.Sin(note.Phase2) + 0.25f * MathF.Sin(2f * note.Phase)) * env * note.Gain;
                        pl += s * Pan(note.Pan, true);
                        pr += s * Pan(note.Pan, false);
                    }
                    else
                    {
                        float t = note.Age;
                        env = Math.Min(1f, t / 0.004f) * MathF.Exp(-t / 0.9f);
                        note.Phase += 2f * MathF.PI * note.Freq * dt;
                        s = (MathF.Sin(note.Phase) + 0.35f * MathF.Sin(2f * note.Phase) * MathF.Exp(-t / 0.25f) + 0.12f * MathF.Sin(3f * note.Phase) * MathF.Exp(-t / 0.12f)) * env * note.Gain;
                        ml += s * Pan(note.Pan, true);
                        mr += s * Pan(note.Pan, false);
                    }
                    note.Age += dt;
                }
                float level = fade * gain * 0.55f;
                float L = padL.Process(pl, padCoef) * level + ml * level;
                float R = padR.Process(pr, padCoef) * level + mr * level;
                l[i] += L * 0.45f;
                r[i] += R * 0.45f;
                sl[i] += L;
                sr[i] += R;
            }
            notes.RemoveAll(note => note.Pad ? note.Age > note.Release + 4f : note.Age > 5f);
        }

        // Wander up and down the scale a step or two at a time, leaning on chord tones.
        void PlayMelodyNote(float night)
        {
            int move = rng.Next(-2, 3);
            if (move == 0) move = rng.Next(2) == 0 ? -1 : 1;
            int low = night > 0.5f ? 3 : 5, high = night > 0.5f ? 10 : 13;
            melodyStep = Math.Clamp(melodyStep + move, low, high);
            int semis = Scale[melodyStep % Scale.Length] + 12 * (melodyStep / Scale.Length);
            notes.Add(new Note { Freq = Hz(semis), Gain = Range(rng, 0.045f, 0.075f), Pan = Range(rng, -0.4f, 0.4f) });
        }
    }

    // --- Reverb -----------------------------------------------------------------

    // A small Freeverb: parallel damped combs into series all-passes, slightly
    // different per side for width. Gives birds and music a forest-sized space.
    sealed class Reverb
    {
        readonly Comb[] combsL, combsR;
        readonly AllPass[] passL, passR;

        sealed class Comb
        {
            readonly float[] buffer;
            int index;
            float store;
            public Comb(int size) => buffer = new float[size];
            public float Process(float x, float feedback, float damp)
            {
                float y = buffer[index];
                store = y * (1f - damp) + store * damp;
                buffer[index] = x + store * feedback;
                if (++index >= buffer.Length) index = 0;
                return y;
            }
        }

        sealed class AllPass
        {
            readonly float[] buffer;
            int index;
            public AllPass(int size) => buffer = new float[size];
            public float Process(float x)
            {
                float b = buffer[index];
                buffer[index] = x + b * 0.5f;
                if (++index >= buffer.Length) index = 0;
                return b - x;
            }
        }

        public Reverb(int rate)
        {
            float scale = rate / 44100f;
            int[] combs = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
            int[] passes = { 556, 441, 341, 225 };
            combsL = Array.ConvertAll(combs, c => new Comb((int)(c * scale)));
            combsR = Array.ConvertAll(combs, c => new Comb((int)((c + 23) * scale)));
            passL = Array.ConvertAll(passes, p => new AllPass((int)(p * scale)));
            passR = Array.ConvertAll(passes, p => new AllPass((int)((p + 23) * scale)));
        }

        public void Process(float[] inL, float[] inR, float[] outL, float[] outR, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float x = (inL[i] + inR[i]) * 0.5f * 0.08f;
                float a = 0f, b = 0f;
                for (int c = 0; c < combsL.Length; c++)
                {
                    a += combsL[c].Process(x, 0.88f, 0.3f);
                    b += combsR[c].Process(x, 0.88f, 0.3f);
                }
                foreach (var p in passL) a = p.Process(a);
                foreach (var p in passR) b = p.Process(b);
                outL[i] += a;
                outR[i] += b;
            }
        }
    }
}
