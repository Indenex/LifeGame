using System;
using System.Collections.Generic;

namespace LifeGame;

public enum Ground { Grass, Earth, Water }

// The whole soundscape, synthesized as it plays: wind, rain, birds by day,
// crickets and owls by night, footsteps, and a slow generative score in a
// forest-sized reverb. Plain C# with no engine types, so tools/SoundPreview can
// render it to a file without the game.
public sealed class SoundMix
{
    public readonly int Rate;

    // Set by the atmosphere every frame. All run from 0 to 1.
    public float Daylight = 1f, Rain, Wind = 0.3f, Wetness;
    public bool MusicOn = true;
    public float Volume = 1.3f;

    readonly Noise noise;
    readonly WindVoice wind;
    readonly RainVoice rain;
    readonly Birds birds;
    readonly NightVoices night;
    readonly Steps steps;
    readonly Music music;
    readonly Reverb reverb;
    float[] sendL = Array.Empty<float>(), sendR = Array.Empty<float>();

    public SoundMix(int rate, int seed = 1)
    {
        Rate = rate;
        var rng = new Random(seed);
        noise = new Noise((uint)seed * 2654435761u + 1u);
        wind = new WindVoice(rate, noise);
        rain = new RainVoice(rate, noise, rng);
        birds = new Birds(rate, rng);
        night = new NightVoices(rate, rng);
        steps = new Steps(rate, noise, rng);
        music = new Music(rate, rng);
        reverb = new Reverb(rate);
    }

    public void Step(Ground ground, float wetness) => steps.Trigger(ground, wetness);

    public void Render(float[] left, float[] right, int count)
    {
        if (sendL.Length < count)
        {
            sendL = new float[count];
            sendR = new float[count];
        }
        Array.Clear(left, 0, count);
        Array.Clear(right, 0, count);
        Array.Clear(sendL, 0, count);
        Array.Clear(sendR, 0, count);

        float night01 = 1f - Daylight;
        wind.Render(left, right, count, Wind, Rain);
        rain.Render(left, right, count, Rain, Wetness);
        birds.Render(left, right, sendL, sendR, count, Daylight * (1f - 0.9f * Rain) * (1f - 0.5f * Wind));
        night.Render(left, right, sendL, sendR, count, night01 * (1f - 0.9f * Rain));
        steps.Render(left, right, count);
        music.Render(left, right, sendL, sendR, count, MusicOn, night01);
        reverb.Process(sendL, sendR, left, right, count);

        for (int i = 0; i < count; i++)
        {
            left[i] = SoftClip(left[i] * Volume);
            right[i] = SoftClip(right[i] * Volume);
        }
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
                float amp = (0.07f + level * 0.32f) * (0.45f + 0.55f * gust);
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

    // --- Rain -------------------------------------------------------------------

    // A soft hiss, a low wash, and individual drops ticking on leaves. After the
    // rain stops, wet branches keep dripping for a while.
    sealed class RainVoice
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        Lowpass hissLowL, hissLowR, hissTopL, hissTopR, washL, washR;
        readonly List<Drop> drops = new();
        float level;

        record struct Drop(float Freq, float Decay, float Amp, float Pan, float Age, float Phase);

        public RainVoice(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Render(float[] l, float[] r, int n, float rain, float wetness)
        {
            float low = Lowpass.Coef(1400f, rate), top = Lowpass.Coef(7000f, rate), wash = Lowpass.Coef(350f, rate);
            float levelCoef = Lowpass.Coef(0.3f, rate);
            float dt = 1f / rate;
            float dropsPerSecond = rain * rain * 500f;
            float dripsPerSecond = Math.Max(0f, wetness - rain) * 4f;
            for (int i = 0; i < n; i++)
            {
                level += levelCoef * (rain - level);
                float nl = noise.Next(), nr = noise.Next();
                float hl = hissTopL.Process(nl - hissLowL.Process(nl, low), top);
                float hr = hissTopR.Process(nr - hissLowR.Process(nr, low), top);
                float wl = washL.Process(noise.Next(), wash) * 3f, wr = washR.Process(noise.Next(), wash) * 3f;
                l[i] += hl * level * 0.16f + wl * level * 0.09f;
                r[i] += hr * level * 0.16f + wr * level * 0.09f;

                if (rng.NextDouble() < dropsPerSecond * dt)
                    drops.Add(new Drop(Range(rng, 1800f, 5200f), Range(rng, 0.003f, 0.01f), Range(rng, 0.01f, 0.05f) * (0.4f + level), Range(rng, -0.9f, 0.9f), 0f, 0f));
                if (rng.NextDouble() < dripsPerSecond * dt)
                    drops.Add(new Drop(Range(rng, 500f, 1100f), Range(rng, 0.02f, 0.04f), Range(rng, 0.03f, 0.07f), Range(rng, -0.8f, 0.8f), 0f, 0f));
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
                    drop.Phase += 2f * MathF.PI * drop.Freq * (1f + 0.6f * env) * dt;
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

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n, float level)
        {
            float dt = 1f / rate;
            untilNext -= n * dt;
            if (untilNext <= 0f)
            {
                untilNext = Range(rng, 1.5f, 7f) / (0.3f + level);
                if (level > 0.05f && songs.Count < 3) songs.Add(MakeSong(level));
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

    // A few crickets chirping out of step with each other, and now and then an owl.
    sealed class NightVoices
    {
        readonly int rate;
        readonly Random rng;
        readonly (float Freq, float Pan, float Period, float Gain)[] crickets;
        readonly float[] cricketTime, cricketPhase;
        float owlTime = -1f, owlPhase, untilOwl = 12f, owlPan;

        public NightVoices(int rate, Random rng)
        {
            this.rate = rate;
            this.rng = rng;
            crickets = new (float, float, float, float)[4];
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
                for (int c = 0; c < crickets.Length; c++)
                {
                    var (freq, pan, period, gain) = crickets[c];
                    float pl = Pan(pan, true) * gain * level, pr = Pan(pan, false) * gain * level;
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

    // --- Footsteps --------------------------------------------------------------

    // Soft swishes on grass, duller thuds on bare earth, a squelch when the ground
    // is wet and a splash in water. Every step is a little different.
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
            public float Wet, Age, Gain, Pitch, Pan, Phase, BubblePhase;
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
                    else
                    {
                        step.Phase += 2f * MathF.PI * 80f * step.Pitch * dt;
                        float thump = MathF.Sin(step.Phase) * MathF.Exp(-t / 0.028f) * attack;
                        if (step.Ground == Ground.Grass)
                        {
                            // Leaves of grass brushing the foot, then a second, lighter rustle.
                            float swish = step.Swish.Process(x, 2600f * step.Pitch, 0.9f, rate);
                            float env = MathF.Exp(-t / 0.035f) + 0.4f * MathF.Exp(-MathF.Max(0f, t - 0.05f) / 0.03f) * (t > 0.05f ? 1f : 0f);
                            s = swish * env * attack * 0.22f + thump * 0.16f;
                        }
                        else
                        {
                            float d = step.Dull.Process(x, dull);
                            s = d * MathF.Exp(-t / 0.03f) * attack * 0.5f + thump * 0.22f;
                        }
                        if (step.Wet > 0.15f)
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

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n, bool on, float night)
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
            gain += Math.Clamp((on ? 1f : 0f) - gain, -block / 1.5f, block / 1.5f);

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
