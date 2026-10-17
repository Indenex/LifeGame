using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeGame;

// A small score player for the game's music (audio-8): a Track (key, tempo, melody and
// chords) played by an Arrangement (which instruments play which part). Both are plain data,
// so a new piece or a new mood is an entry, not code. Pure C# with no engine types: SoundPreview
// renders pieces to a WAV from it, and the game can stream it.
//
// Time is counted in eighth notes (a 6/8 bar is six of them). Pitch is in semitones above the
// track's root; the melody is written in scale degrees (1 to 7, ' an octave up, , an octave down).

public enum Inst { Pluck, Flute, Bell, MusicBox, Pad, Square, Pulse, Triangle, Shaker, Wood, Hat }

// One note of a melody or part: start and length in eighths, pitch in semitones above the root.
public readonly record struct ScoreNote(float Start, float Length, int Semis, float Velocity = 1f);

// A chord for one bar: its root as semitones above the key's root, and minor or major.
public readonly record struct Chord(int Root, bool Minor = false);

public sealed class Track
{
    public string Name = "";
    public int RootMidi = 55;          // the key's root; harmony sits here, the melody an octave up
    public float Bpm = 84f;            // dotted quarters a minute in 6/8 (three eighths a beat)
    public int Bars;
    public int BarEighths = 6;
    public List<ScoreNote> Melody = new();
    public List<Chord> Chords = new();

    static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 };

    // "5:2 1':1 3':3 | 2':2 ..." : degree:length in eighths, r for a rest, | between bars (ignored).
    public static List<ScoreNote> ParseMelody(string text)
    {
        var notes = new List<ScoreNote>();
        float at = 0f;
        foreach (var token in text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token == "|") continue;
            var parts = token.Split(':');
            float len = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            if (parts[0] != "r")
            {
                int octave = parts[0].Count(c => c == '\'') - parts[0].Count(c => c == ',');
                int degree = int.Parse(parts[0].Trim('\'', ','));
                notes.Add(new ScoreNote(at, len, Major[degree - 1] + 12 * octave));
            }
            at += len;
        }
        return notes;
    }

    // Chords by roman numeral: I ii iii IV V vi (lower case is minor).
    public static List<Chord> ParseChords(string text)
    {
        var chords = new List<Chord>();
        foreach (var token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            bool minor = char.IsLower(token[0]);
            int degree = token.ToUpperInvariant() switch { "I" => 0, "II" => 1, "III" => 2, "IV" => 3, "V" => 4, "VI" => 5, _ => 0 };
            chords.Add(new Chord(Major[degree], minor));
        }
        return chords;
    }
}

// Which instruments play, and how. Patterns index the chord's four tones (root, third, fifth,
// octave); an entry of -1 is a rest.
public sealed class Arrangement
{
    public string Name = "";
    public float TempoScale = 1f;
    public Inst Melody = Inst.Flute;
    public float MelodyGain = 1f;
    public Inst? Double;               // plays the melody an octave up on its long notes
    public float DoubleGain = 0.35f;
    public Inst? Arp;
    public float ArpGain = 0.5f;
    public int[] ArpPattern = { 0, 1, 2, 3, 2, 1 };
    public int ArpOctave = 0;
    public Inst? Bass;
    public float BassGain = 0.6f;
    public int[] BassPattern = { 0, -1, -1, 2, -1, -1 };
    public bool PadOn;
    public float PadGain = 0.3f;
    public Inst? Tick;                 // light percussion on every eighth
    public float TickGain = 0.2f;
    public Inst? Accent;               // and a soft accent on the second beat
    public float AccentGain = 0.3f;
    public float Reverb = 0.25f;
    public float Brightness = 1f;
}

public static class Score
{
    const int Rate = 44100;

    static float Hz(float midi) => 440f * MathF.Pow(2f, (midi - 69f) / 12f);

    // Renders `bars` bars of the track (loops the chords and melody if asked for more),
    // returning stereo samples at 44.1 kHz.
    public static (float[] Left, float[] Right) Render(Track track, Arrangement arr, int bars, float tailSeconds = 3f)
    {
        float eighth = 60f / (track.Bpm * arr.TempoScale * 3f) * (track.BarEighths / 6f);
        float barSeconds = eighth * track.BarEighths;
        int total = (int)((bars * barSeconds + tailSeconds) * Rate);
        var dryL = new float[total];
        var dryR = new float[total];
        var sendL = new float[total];
        var sendR = new float[total];
        var rng = new Random(11);
        var voice = new Synth(rng);

        void Play(Inst inst, float seconds, float length, float midi, float gain, float pan)
        {
            if (gain <= 0.0005f) return;
            voice.Note(inst, seconds, length, midi, gain, pan, arr.Brightness, dryL, dryR, sendL, sendR, arr.Reverb);
        }

        // Melody (and its octave-up double on long notes).
        float melodyBase = track.RootMidi + 12;
        int plays = (bars + track.Bars - 1) / track.Bars; // the melody repeats if more bars are asked for
        for (int rep = 0; rep < plays; rep++)
            foreach (var n in track.Melody)
            {
                float start = n.Start + rep * track.Bars * track.BarEighths;
                if (start >= bars * track.BarEighths) break;
                float t = start * eighth, len = n.Length * eighth;
                Play(arr.Melody, t, len, melodyBase + n.Semis, arr.MelodyGain * n.Velocity, 0.05f);
                if (arr.Double is { } dbl && n.Length >= 3)
                    Play(dbl, t + 0.01f, len, melodyBase + n.Semis + 12, arr.DoubleGain, 0.3f);
            }

        // Chords, one a bar.
        for (int bar = 0; bar < bars; bar++)
        {
            var chord = track.Chords[bar % track.Chords.Count];
            float bt = bar * barSeconds;
            int third = chord.Minor ? 3 : 4;
            int[] tones = { 0, third, 7, 12 };
            if (arr.PadOn)
            {
                foreach (int i in new[] { 0, 1, 2 })
                    Play(Inst.Pad, bt, barSeconds * 1.05f, track.RootMidi + chord.Root + tones[i], arr.PadGain / 3f, -0.2f + 0.2f * i);
            }
            if (arr.Arp is { } arp)
            {
                for (int e = 0; e < track.BarEighths; e++)
                {
                    int idx = arr.ArpPattern[e % arr.ArpPattern.Length];
                    if (idx < 0) continue;
                    float vel = (e % 3 == 0 ? 1f : 0.7f) * arr.ArpGain;
                    Play(arp, bt + e * eighth, eighth * 2.5f, track.RootMidi + 12 * arr.ArpOctave + chord.Root + tones[idx], vel, -0.3f + 0.12f * e);
                }
            }
            if (arr.Bass is { } bass)
            {
                for (int e = 0; e < track.BarEighths; e++)
                {
                    int idx = arr.BassPattern[e % arr.BassPattern.Length];
                    if (idx < 0) continue;
                    Play(bass, bt + e * eighth, eighth * 2.2f, track.RootMidi - 12 + chord.Root + tones[idx], arr.BassGain, -0.05f);
                }
            }
            for (int e = 0; e < track.BarEighths; e++)
            {
                if (arr.Tick is { } tick)
                    Play(tick, bt + e * eighth, eighth, 80f, (e % 3 == 0 ? 1f : 0.55f) * arr.TickGain, e % 2 == 0 ? -0.35f : 0.35f);
                if (arr.Accent is { } accent && e == 3)
                    Play(accent, bt + e * eighth, eighth, 76f, arr.AccentGain, 0.1f);
            }
        }

        // Reverb on the send, then mix.
        var reverb = new Reverb();
        var left = new float[total];
        var right = new float[total];
        reverb.Process(sendL, sendR, left, right, total);
        double peak = 0;
        for (int i = 0; i < total; i++)
        {
            left[i] += dryL[i];
            right[i] += dryR[i];
            peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
        }
        // Gentle fade out over the last second of tail so a clip ends cleanly.
        int fade = Math.Min(total, Rate);
        for (int i = 0; i < fade; i++)
        {
            float g = 1f - (float)i / fade;
            left[total - fade + i] *= g;
            right[total - fade + i] *= g;
        }
        return (left, right);
    }

    // Instruments: each Note adds one note into the dry buffers and the reverb send.
    sealed class Synth
    {
        readonly Random rng;
        public Synth(Random rng) => this.rng = rng;

        public void Note(Inst inst, float start, float length, float midi, float gain, float pan, float bright,
            float[] dl, float[] dr, float[] sl, float[] sr, float reverb)
        {
            float f = Hz(midi);
            float tail = inst switch
            {
                Inst.Pluck => 1.4f, Inst.Bell => 2.2f, Inst.MusicBox => 1.6f, Inst.Flute => 0.25f,
                Inst.Pad => 0.8f, Inst.Square or Inst.Pulse => 0.15f, Inst.Triangle => 0.3f, _ => 0.2f,
            };
            int first = (int)(start * Rate);
            int count = (int)((length + tail) * Rate);
            float pl = MathF.Sqrt(0.5f * (1f - pan)), pr = MathF.Sqrt(0.5f * (1f + pan));
            float phase = 0f, vibPhase = (float)rng.NextDouble() * 6f;
            float lp = 0f, lp2 = 0f;
            float breathState = 0f;
            for (int k = 0; k < count; k++)
            {
                int i = first + k;
                if (i < 0 || i >= dl.Length) break;
                float t = k / (float)Rate;
                float s;
                switch (inst)
                {
                    case Inst.Pluck:
                    {
                        // A plucked string: harmonics that die faster the higher they are.
                        s = 0f;
                        for (int h = 1; h <= 7; h++)
                        {
                            float amp = 1f / (h * 0.9f + 0.2f) * (h == 2 ? 0.8f : 1f);
                            s += amp * MathF.Sin(MathF.Tau * f * h * t) * MathF.Exp(-t * (1.6f + 1.3f * h) / bright);
                        }
                        s *= 0.5f * MathF.Min(1f, t / 0.002f);
                        break;
                    }
                    case Inst.Flute:
                    {
                        // A soft recorder: a sine with a little second and third, breath, and vibrato that grows.
                        float vib = 1f + 0.004f * MathF.Min(1f, t / 0.3f) * MathF.Sin(MathF.Tau * 5.3f * t + vibPhase);
                        phase += MathF.Tau * f * vib / Rate;
                        float env = MathF.Min(1f, t / 0.045f) * (t < length ? 1f : MathF.Exp(-(t - length) / 0.07f));
                        float breath = (float)(rng.NextDouble() * 2 - 1);
                        breathState += 0.25f * (breath - breathState);
                        s = (MathF.Sin(phase) + 0.22f * MathF.Sin(2f * phase) + 0.07f * bright * MathF.Sin(3f * phase) + 0.15f * breathState * (1f + (t < 0.08f ? 2f : 0f))) * env * 0.6f;
                        break;
                    }
                    case Inst.Bell:
                    {
                        // A glockenspiel: a bright bar with inharmonic overtones.
                        s = MathF.Sin(MathF.Tau * f * t) * MathF.Exp(-t * 1.6f)
                          + 0.45f * MathF.Sin(MathF.Tau * f * 2.76f * t) * MathF.Exp(-t * 4.5f)
                          + 0.2f * bright * MathF.Sin(MathF.Tau * f * 5.4f * t) * MathF.Exp(-t * 9f);
                        s *= 0.5f * MathF.Min(1f, t / 0.001f);
                        break;
                    }
                    case Inst.MusicBox:
                    {
                        // A music-box comb: a pure tone with a ringing upper partial, a quick tick at the start.
                        s = MathF.Sin(MathF.Tau * f * t) * MathF.Exp(-t * 2.4f)
                          + 0.35f * MathF.Sin(MathF.Tau * f * 3.01f * t) * MathF.Exp(-t * 6f)
                          + 0.12f * bright * MathF.Sin(MathF.Tau * f * 5.02f * t) * MathF.Exp(-t * 11f);
                        s *= 0.55f * MathF.Min(1f, t / 0.0008f);
                        break;
                    }
                    case Inst.Pad:
                    {
                        float env = MathF.Min(1f, t / 0.35f) * (t < length ? 1f : MathF.Exp(-(t - length) / 0.3f));
                        s = (MathF.Sin(MathF.Tau * f * t) + 0.5f * MathF.Sin(MathF.Tau * f * 1.004f * t) + 0.18f * MathF.Sin(MathF.Tau * f * 2f * t)) * env * 0.5f;
                        break;
                    }
                    case Inst.Square:
                    case Inst.Pulse:
                    {
                        // A chip voice: a pulse wave softened by a one-pole low-pass.
                        float duty = inst == Inst.Square ? 0.5f : 0.25f;
                        phase += f / Rate;
                        phase -= MathF.Floor(phase);
                        float raw = phase < duty ? 0.6f : -0.6f;
                        lp += 0.35f * (raw - lp);
                        float env = MathF.Min(1f, t / 0.003f) * (t < length ? MathF.Exp(-t * 1.2f) : MathF.Exp(-(t - length) / 0.03f));
                        s = lp * env;
                        break;
                    }
                    case Inst.Triangle:
                    {
                        phase += f / Rate;
                        phase -= MathF.Floor(phase);
                        float tri = 4f * MathF.Abs(phase - 0.5f) - 1f;
                        float env = MathF.Min(1f, t / 0.004f) * (t < length ? MathF.Exp(-t * 3.5f) : MathF.Exp(-(t - length) / 0.04f));
                        s = tri * env * 0.8f;
                        break;
                    }
                    case Inst.Shaker:
                    {
                        float n = (float)(rng.NextDouble() * 2 - 1);
                        lp += 0.5f * (n - lp);
                        s = (n - lp) * MathF.Exp(-t / 0.035f) * 0.5f * MathF.Min(1f, t / 0.003f);
                        break;
                    }
                    case Inst.Hat:
                    {
                        float n = (float)(rng.NextDouble() * 2 - 1);
                        lp += 0.7f * (n - lp);
                        s = (n - lp) * MathF.Exp(-t / 0.012f) * 0.5f;
                        break;
                    }
                    default: // Wood: a hollow knock
                    {
                        s = MathF.Sin(MathF.Tau * (900f - 350f * MathF.Min(t, 0.03f) / 0.03f) * t) * MathF.Exp(-t / 0.035f) * 0.6f;
                        break;
                    }
                }
                s *= gain;
                dl[i] += s * pl;
                dr[i] += s * pr;
                sl[i] += s * pl * reverb;
                sr[i] += s * pr * reverb;
                lp2 = s;
            }
        }
    }

    // A small Schroeder reverb: four combs and two all-passes a side, a little different left and right.
    sealed class Reverb
    {
        static readonly int[] CombL = { 1687, 1601, 2053, 2251 }, CombR = { 1757, 1637, 2111, 2293 };

        public void Process(float[] inL, float[] inR, float[] outL, float[] outR, int n)
        {
            Side(inL, outL, n, CombL);
            Side(inR, outR, n, CombR);
        }

        static void Side(float[] input, float[] output, int n, int[] delays)
        {
            var sum = new float[n];
            foreach (int d in delays)
            {
                var buf = new float[d];
                int p = 0;
                float lp = 0f;
                for (int i = 0; i < n; i++)
                {
                    float y = buf[p];
                    lp += 0.45f * (y - lp);
                    buf[p] = input[i] + lp * 0.84f;
                    p = (p + 1) % d;
                    sum[i] += y * 0.25f;
                }
            }
            // Two all-pass stages for diffusion.
            foreach (int d in new[] { 347, 113 })
            {
                var buf = new float[d];
                int p = 0;
                for (int i = 0; i < n; i++)
                {
                    float x = sum[i];
                    float y = buf[p];
                    buf[p] = x + y * 0.5f;
                    sum[i] = y - x * 0.5f;
                    p = (p + 1) % d;
                }
            }
            for (int i = 0; i < n; i++) output[i] += sum[i];
        }
    }
}
