using System;
using System.Collections.Generic;

namespace LifeGame;

public enum Ground { Grass, Earth, Water, Clay, Floor }

// The whole soundscape, synthesized as it plays: wind, rain, birds by day,
// crickets and owls by night, footsteps, and a slow generative score in a
// forest-sized reverb. Plain C# with no engine types, so tools/SoundPreview can
// render it to a file without the game.
public sealed partial class SoundMix
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
    public bool MusicOn = true;
    // The music's own level, 0 to 1, under the master volume (the menu's music slider).
    public float MusicVolume = 1f;
    public float Volume = 1.3f;

    readonly Noise noise;
    readonly WindVoice wind;
    readonly FoliageVoice foliage;
    readonly RainVoice rain;
    readonly ThunderVoice thunder;
    readonly FireVoice fire;
    readonly Birds birds;
    readonly NightVoices night;
    readonly WaterVoice water;
    readonly Frogs frogs;
    readonly Steps steps;
    readonly Sheep sheep;
    readonly Music music;
    readonly Reverb reverb;
    float[] sendL = Array.Empty<float>(), sendR = Array.Empty<float>();
    float[] outL = Array.Empty<float>(), outR = Array.Empty<float>();
    float[] stepL = Array.Empty<float>(), stepR = Array.Empty<float>();
    // How loud the surroundings are (smoothed RMS), so footsteps sit just above them.
    float ambience = 0.01f;
    Lowpass wallL, wallR;
    float cover;

    public SoundMix(int rate, int seed = 1)
    {
        Rate = rate;
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
        steps = new Steps(rate, noise, rng);
        work = new WorkVoice(rate, noise, rng);
        felling = new Felling(rate, noise, rng);
        sheep = new Sheep(rate, rng);
        music = new Music(rate, rng);
        reverb = new Reverb(rate);
    }

    // How hard the wind is gusting right now, 0 to 1, so grass and trees can move
    // with what is heard. Written by the synth thread; a stale read is harmless.
    public float Gust => wind.Gust;

    public void Step(Ground ground, float wetness) => steps.Trigger(ground, wetness);

    // A sheep somewhere around the listener: pan from -1 (left) to 1 (right),
    // gain from distance, startled when the flock bolts.
    public void Bleat(float pan, float gain, bool startled, bool lamb = false) => sheep.Trigger(pan, gain, startled, lamb);

    // Thunder after a flash: delay in seconds (sound is slower than light), gain
    // from 0 to 1, near from 0 (a distant rumble) to 1 (a crack overhead).
    public void Thunder(float delay, float gain, float near) => thunder.Trigger(delay, gain, near);

    public void Render(float[] left, float[] right, int count)
    {
        if (sendL.Length < count)
        {
            sendL = new float[count];
            sendR = new float[count];
            outL = new float[count];
            outR = new float[count];
            stepL = new float[count];
            stepR = new float[count];
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
        wind.Render(outL, outR, count, Wind, Rain);
        foliage.Render(outL, outR, count, Wind, Rain, Foliage, wind.Gust);
        thunder.Render(outL, outR, count);
        fire.Render(outL, outR, count, Fire, FirePan, Rain);
        birds.Render(outL, outR, sendL, sendR, count, birdLevel * (1f - 0.9f * Rain) * (1f - 0.5f * Wind), chorus);
        night.Render(outL, outR, sendL, sendR, count, night01 * (1f - 0.9f * Rain) * (1f - 0.4f * Wind));
        sheep.Render(outL, outR, sendL, sendR, count);
        water.Render(outL, outR, count, Water, WaterPan, Wind, wind.Gust, Rain);
        frogs.Render(outL, outR, sendL, sendR, count, Water * Smooth(0.15f, 0.6f, night01) * (1f - 0.4f * Wind), WaterPan);
        felling.Render(outL, outR, sendL, sendR, count);
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

        // Footsteps sit a little above whatever surrounds them: soft on a still
        // dawn, firmer in a storm, never louder than they need to be.
        double power = 0;
        for (int i = 0; i < count; i++) power += left[i] * left[i] + right[i] * right[i];
        float rms = (float)Math.Sqrt(power / (2.0 * count));
        ambience += (1f - MathF.Exp(-count / (float)Rate / 1.5f)) * (rms - ambience);
        float stepGain = StepGain(ambience);
        Array.Clear(stepL, 0, count);
        Array.Clear(stepR, 0, count);
        steps.Render(stepL, stepR, count);
        // The work in the player's hands sits on the same close bus as their feet.
        work.Render(stepL, stepR, count);
        for (int i = 0; i < count; i++)
        {
            left[i] += stepL[i] * stepGain;
            right[i] += stepR[i] * stepGain;
        }
        music.Render(left, right, sendL, sendR, count, MusicOn, MusicVolume, night01);
        reverb.Process(sendL, sendR, left, right, count);

        for (int i = 0; i < count; i++)
        {
            left[i] = SoftClip(left[i] * Volume);
            right[i] = SoftClip(right[i] * Volume);
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

    // Footstep level for an ambience RMS: about 0.17 (-15 dB) over a still dawn bed
    // near -45 dBFS, so steps land about 5 dB above it, rising to full in a storm.
    public static float StepGain(float ambienceRms) => Math.Clamp(0.12f + 9f * ambienceRms, 0.12f, 1f);

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

    static float Pan(float pan, bool left) => left ? MathF.Sqrt(0.5f * (1f - pan)) : MathF.Sqrt(0.5f * (1f + pan));

    static float Range(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

    // --- Wind -------------------------------------------------------------------

    // Band-passed noise whose pitch and loudness rise and fall with slow gusts.
    sealed class WindVoice
    {
        readonly int rate;
        readonly Noise noise;
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

        public void Render(float[] l, float[] r, int n, float wind, float rain)
        {
            float rumbleCoef = Lowpass.Coef(120f, rate);
            float gustCoef = Lowpass.Coef(0.15f, rate);
            float levelCoef = Lowpass.Coef(0.5f, rate);
            for (int i = 0; i < n; i++)
            {
                if (counter-- <= 0)
                {
                    counter = rate / 2;
                    gustTarget = Math.Clamp(gustTarget + noise.Next() * 0.35f, 0f, 1f);
                }
                gust = gustSmooth.Process(gustTarget, gustCoef);
                float strength = Math.Clamp(wind + rain * 0.2f, 0f, 1f);
                level += levelCoef * (strength - level);
                float amp = (0.07f + level * 0.2f) * (0.45f + 0.55f * gust);
                float center = 220f + 520f * gust * (0.4f + level);
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
        BandPass leafL, leafR, grassL, grassR;
        Lowpass flutterL, flutterR, level, wooded;

        public FoliageVoice(int rate, Noise noise)
        {
            this.rate = rate;
            this.noise = noise;
        }

        public void Render(float[] l, float[] r, int n, float wind, float rain, float foliage, float gust)
        {
            float flutterCoef = Lowpass.Coef(14f, rate), levelCoef = Lowpass.Coef(0.4f, rate), woodCoef = Lowpass.Coef(0.3f, rate);
            float strength = Math.Clamp(wind + rain * 0.15f, 0f, 1f);
            float target = MathF.Pow(strength, 1.4f) * (0.35f + 0.65f * gust);
            for (int i = 0; i < n; i++)
            {
                float lv = level.Process(target, levelCoef);
                float w = wooded.Process(foliage, woodCoef);
                // Leaves flicker: the rustle comes in quick uneven bursts.
                float fl = Math.Max(0f, 0.5f + 6f * flutterL.Process(noise.Next(), flutterCoef));
                float fr = Math.Max(0f, 0.5f + 6f * flutterR.Process(noise.Next(), flutterCoef));
                float leafA = leafL.Process(noise.Next(), 3400f + 900f * gust, 0.9f, rate) * fl;
                float leafB = leafR.Process(noise.Next(), 3700f + 900f * gust, 0.9f, rate) * fr;
                float grassA = grassL.Process(noise.Next(), 1500f + 700f * gust, 1.1f, rate);
                float grassB = grassR.Process(noise.Next(), 1650f + 700f * gust, 1.1f, rate);
                float leaves = lv * w * 0.16f, grass = lv * (1f - 0.6f * w) * 0.07f;
                l[i] += leafA * leaves + grassA * grass;
                r[i] += leafB * leaves + grassB * grass;
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
        readonly List<Drop> drops = new();
        float level, cover;

        record struct Drop(float Freq, float Decay, float Amp, float Pan, float Age, float Phase, float Bend);

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

                // Patter: single drops on leaves, sparse in a drizzle, a crowd in a storm
                // where the hiss carries them.
                float patter = (20f + 380f * level * level) * Smooth(0.02f, 0.15f, level) * (1f - cover);
                if (rng.NextDouble() < patter * dt)
                    drops.Add(new Drop(Range(rng, 1800f, 5200f), Range(rng, 0.003f, 0.009f),
                        Range(rng, 0.012f, 0.04f) * (0.6f + 0.6f * level), Range(rng, -0.9f, 0.9f), 0f, 0f, 0.6f));
                // Drips off the branches: during rain and while the leaves are still wet.
                float drips = (rain > 0.02f ? 1.5f + 3f * level : 0f) + Math.Max(0f, wetness - level) * 4f;
                if (rng.NextDouble() < drips * (1f - cover * 0.7f) * dt)
                    drops.Add(new Drop(Range(rng, 500f, 1100f), Range(rng, 0.02f, 0.04f), Range(rng, 0.03f, 0.07f), Range(rng, -0.8f, 0.8f), 0f, 0f, 0.6f));
                // On the roof: low, close taps that thicken into a drumming.
                // Softer than the open rain, so stepping inside always quietens it.
                float roof = cover * (20f + 200f * level) * Smooth(0.02f, 0.1f, level);
                if (rng.NextDouble() < roof * dt)
                    drops.Add(new Drop(Range(rng, 180f, 480f), Range(rng, 0.006f, 0.014f), Range(rng, 0.015f, 0.04f), Range(rng, -0.6f, 0.6f), 0f, 0f, 0.2f));
            }

            for (int d = drops.Count - 1; d >= 0; d--)
            {
                var drop = drops[d];
                int start = (int)(rng.NextDouble() * n * 0.98);
                float pl = Pan(drop.Pan, true), pr = Pan(drop.Pan, false);
                // Each drop sits at a random spot in this block, then rings down.
                for (int i = drop.Age == 0f ? start : 0; i < n; i++)
                {
                    float env = MathF.Exp(-drop.Age / drop.Decay);
                    if (env < 0.001f) break;
                    // Drips fall in pitch as they ring, like water off a leaf.
                    drop.Phase += 2f * MathF.PI * drop.Freq * (1f + drop.Bend * env) * dt;
                    float s = MathF.Sin(drop.Phase) * env * drop.Amp;
                    l[i] += s * pl;
                    r[i] += s * pr;
                    drop.Age += dt;
                }
                if (MathF.Exp(-drop.Age / drop.Decay) < 0.001f) drops.RemoveAt(d);
                else drops[d] = drop;
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
        Lowpass lowL, lowR, crackL, crackR;

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
                    l[i] += (rumbleL * g + cl * roll.Gain) * pl;
                    r[i] += (rumbleR * g + cr * roll.Gain) * pr;
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

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n, float level)
        {
            float dt = 1f / rate;
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
                if (level > 0.5f)
                {
                    owlTime = 0f;
                    owlPan = Range(rng, -0.8f, 0.8f);
                }
            }
            if (owlTime < 0f) return;

            // "Hoo... hoo-hoo-hoooo", soft and far away.
            ReadOnlySpan<(float Start, float Length)> hoots = stackalloc (float, float)[] { (0f, 0.45f), (0.85f, 0.18f), (1.1f, 0.18f), (1.35f, 0.6f) };
            float gl = Pan(owlPan, true) * 0.045f * level, gr = Pan(owlPan, false) * 0.045f * level;
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
    // clay, a hollow knock on a hut's floor, a squelch when the ground is wet and a
    // splash in water. Every step is a little different.
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

        public void Trigger(Ground ground, float wetness)
        {
            leftFoot = !leftFoot;
            if (active.Count > 6) active.RemoveAt(0);
            active.Add(new Step
            {
                Ground = ground,
                Wet = wetness,
                Gain = Range(rng, 0.8f, 1.15f),
                Pitch = Range(rng, 0.85f, 1.15f),
                Pan = leftFoot ? -0.12f : 0.12f,
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
            gain += Math.Clamp((on ? Math.Clamp(volume, 0f, 1f) : 0f) - gain, -block / 1.5f, block / 1.5f);

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
