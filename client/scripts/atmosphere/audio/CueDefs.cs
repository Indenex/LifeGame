using System.Collections.Generic;

namespace LifeGame;

// Feedback sounds as data (audio-7, docs/CONTENT.md): a CueDef is a list of layers, each a short
// burst of filtered noise or a gliding tone with an envelope, starting at its own time. One
// generic voice (SoundMix.Cues) plays any of them, so a new sound is an entry here, not code.
// Existing cues in the Cue enum are converted into entries one at a time.

public enum Src { Noise, Tone }

// One layer. Start and Decay are seconds (Decay is the time constant of an exponential fade).
// Noise: heard between the Lo and Hi cutoffs (Hz). Tone: glides from Lo to Hi Hz.
public readonly record struct CueLayer(Src Source, float Start, float Decay, float Gain, float Lo, float Hi, float Rise = 0.003f);

// Length is how long the voice lives, seconds. Send is how much goes to the reverb.
public sealed record CueDef(string Id, float Length, CueLayer[] Layers, float Send = 0f);

public static class CueDefs
{
    static CueLayer Splash(float at, float decay, float gain, float lo = 500f, float hi = 3500f) => new(Src.Noise, at, decay, gain, lo, hi, 0.01f);
    static CueLayer Crunch(float at, float decay, float gain, float lo = 1200f, float hi = 5500f) => new(Src.Noise, at, decay, gain, lo, hi, 0.002f);
    static CueLayer Gulp(float at, float gain) => new(Src.Tone, at, 0.07f, gain, 230f, 110f, 0.01f);
    static CueLayer Thud(float at, float decay, float gain) => new(Src.Noise, at, decay, gain, 60f, 600f, 0.004f);

    // Quick body actions (Animation's player_act, in seconds from the event): the timings match the
    // motions, so the sound lands when the hands do.

    // Drink (2.4 s): hands in the water about 0.35 to 0.75, at the mouth from 1.25, a gulp near 1.4.
    public static readonly CueDef Drink = new("act.drink", 2.4f, new[]
    {
        Splash(0.35f, 0.12f, 0.22f), Splash(0.5f, 0.07f, 0.12f, 800f, 4500f), Splash(0.75f, 0.05f, 0.07f, 1000f, 5000f),
        Gulp(1.4f, 0.12f), Thud(1.4f, 0.05f, 0.1f), Gulp(1.72f, 0.07f),
    });

    // Eat (1.6 s): food at the mouth about 0.5, chewing from 0.6 to 1.1, a swallow.
    public static readonly CueDef Eat = new("act.eat", 1.6f, new[]
    {
        Crunch(0.6f, 0.035f, 0.28f), Crunch(0.78f, 0.03f, 0.23f), Crunch(0.96f, 0.03f, 0.19f),
        Thud(0.6f, 0.05f, 0.08f), Thud(0.78f, 0.05f, 0.07f), Thud(0.96f, 0.05f, 0.06f),
        Gulp(1.2f, 0.06f),
    });

    // Pluck (1.0 s): the flick about 0.35 to 0.55: leaves rustle and a small snap.
    public static readonly CueDef Pluck = new("act.pluck", 1.0f, new[]
    {
        new CueLayer(Src.Noise, 0.35f, 0.12f, 0.18f, 2500f, 8000f, 0.02f),
        new CueLayer(Src.Noise, 0.5f, 0.1f, 0.1f, 2000f, 7000f, 0.01f),
        new CueLayer(Src.Noise, 0.44f, 0.008f, 0.36f, 1500f, 6000f, 0.001f),
    });

    // Pick up (0.45 s): the grab about 0.22: a soft scuff and a little cloth.
    public static readonly CueDef PickUp = new("act.pickup", 0.45f, new[]
    {
        new CueLayer(Src.Noise, 0.22f, 0.07f, 0.25f, 800f, 3500f, 0.01f),
        new CueLayer(Src.Noise, 0.18f, 0.1f, 0.08f, 2000f, 6000f, 0.02f),
    });

    // --- Cues converted from the Cue enum (audio-9 onward): the enum value stays (callers and
    // saved ids don't change) and SoundMix.Play(Cue) plays the entry below instead of old code.

    // Place: something set on the ground: a dull knock and a scuff.
    public static readonly CueDef Place = new("cue.place", 0.4f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.05f, 0.13f, 95f, 95f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.04f, 0.12f, 20f, 1200f, 0.004f),
    });

    public static CueDef? ForCue(Cue cue) => cue switch
    {
        Cue.Place => Place,
        _ => null,
    };
}
