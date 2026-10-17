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

    // Hit: a blow landing: a dull thud dropping a little in pitch and a slap of cloth and flesh.
    public static readonly CueDef Hit = new("cue.hit", 0.4f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.07f, 0.15f, 130f, 108f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.03f, 0.24f, 600f, 1800f, 0.004f),
    });

    // Crit: a sneak attack's bonus: the same thud, a sharp crack and a short bright ring.
    public static readonly CueDef Crit = new("cue.crit", 0.7f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.09f, 0.16f, 130f, 108f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.012f, 0.115f, 2000f, 6000f, 0.004f),
        new CueLayer(Src.Tone, 0.02f, 0.14f, 0.04f, 1480f, 1480f, 0.004f),
    }, Send: 0.1f);

    // Jump: the push-off: a quick scuff of the ground and cloth and a small lift of breath.
    public static readonly CueDef Jump = new("cue.jump", 0.3f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.05f, 0.3f, 900f, 2400f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.1f, 0.06f, 20f, 2200f, 0.1f),
    });

    // A wooden bar struck: the fundamental, its bright 3.9x partial, and two quiet echoes after it
    // (the journal's chime, once in Cues). Tones start at phase 0 when their layer starts.
    static void Bar(List<CueLayer> into, float freq, float at, float gain)
    {
        (float Delay, float Echo)[] echoes = { (0f, 1f), (0.23f, 0.22f), (0.47f, 0.1f) };
        foreach (var (delay, echo) in echoes)
        {
            into.Add(new CueLayer(Src.Tone, at + delay, 0.3125f, gain * echo, freq, freq, 0.004f));
            into.Add(new CueLayer(Src.Tone, at + delay, 0.0714f, gain * echo * 0.25f, freq * 3.9f, freq * 3.9f, 0.004f));
        }
    }

    // Learn: one faint wooden note, G5: something new in the journal.
    public static readonly CueDef Learn = new("cue.learn", 1.6f, Bars(0.017f, (783.99f, 0f, 0.55f)), Send: 0.6f);

    // Skill up: D5 then A5 on a wooden bar, an open fifth: a skill earned a new title.
    public static readonly CueDef SkillUp = new("cue.skillup", 2.2f, Bars(0.027f, (587.33f, 0f, 1f), (880f, 0.16f, 0.8f)), Send: 0.6f);

    static CueLayer[] Bars(float scale, params (float Freq, float At, float Gain)[] notes)
    {
        var layers = new List<CueLayer>();
        foreach (var (freq, at, gain) in notes) Bar(layers, freq, at, gain * scale);
        return layers.ToArray();
    }

    // Pickup: leaves and cloth brushing, then a soft knock in the bag.
    public static readonly CueDef Pickup = new("cue.pickup", 0.4f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.05f, 0.39f, 1800f, 4200f, 0.004f),
        new CueLayer(Src.Tone, 0.07f, 0.03f, 0.19f, 140f, 140f, 0.004f),
    });

    // Eat (the cue, not the act): three bites, each a short crunchy burst.
    public static readonly CueDef EatBites = new("cue.eat", 0.9f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.035f, 0.47f, 1000f, 3000f, 0.004f),
        new CueLayer(Src.Noise, 0.28f, 0.03f, 0.38f, 1000f, 3000f, 0.004f),
        new CueLayer(Src.Noise, 0.55f, 0.03f, 0.31f, 1000f, 3000f, 0.004f),
    });

    // Drink (the cue, for the pot): two gulps, each a drop in pitch, over a little slosh.
    public static readonly CueDef DrinkGulps = new("cue.drink", 0.8f, new[]
    {
        new CueLayer(Src.Tone, 0.05f, 0.07f, 0.11f, 260f, 110f, 0.004f),
        new CueLayer(Src.Tone, 0.38f, 0.07f, 0.11f, 260f, 110f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.15f, 0.2f, 20f, 900f, 0.004f),
    });

    // Fire lit: a soft whoosh as the flame takes, opening up as it grows.
    public static readonly CueDef FireLit = new("cue.firelit", 1f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.45f, 0.8f, 20f, 700f, 0.4f),
        new CueLayer(Src.Noise, 0.25f, 0.3f, 0.39f, 300f, 2800f, 0.3f),
    });

    // Bow draw: wood and sinew under strain, an irregular slow creak.
    public static readonly CueDef BowDraw = new("cue.bowdraw", 0.8f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.4f, 0.23f, 450f, 850f, 0.25f),
        new CueLayer(Src.Noise, 0.1f, 0.35f, 0.14f, 650f, 1100f, 0.25f),
    });

    // Bow shot: the string's twang (a few damped low tones) and the whoosh of the arrow leaving.
    public static readonly CueDef BowShot = new("cue.bowshot", 0.6f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.2f, 0.08f, 165f, 165f, 0.002f),
        new CueLayer(Src.Tone, 0f, 0.11f, 0.04f, 330f, 330f, 0.002f),
        new CueLayer(Src.Tone, 0f, 0.07f, 0.02f, 495f, 495f, 0.002f),
        new CueLayer(Src.Noise, 0.01f, 0.1f, 0.22f, 900f, 2800f, 0.004f),
    }, Send: 0.1f);

    // Arrow into an animal: a dull, wet thunk.
    public static readonly CueDef ArrowFlesh = new("cue.arrowflesh", 0.4f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.07f, 0.14f, 120f, 70f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.04f, 0.25f, 20f, 800f, 0.004f),
    });

    // Arrow into a tree: a sharp thock and the shaft humming for a moment.
    public static readonly CueDef ArrowWood = new("cue.arrowwood", 0.5f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.012f, 0.4f, 900f, 2400f, 0.004f),
        new CueLayer(Src.Tone, 0f, 0.08f, 0.12f, 380f, 380f, 0.004f),
        new CueLayer(Src.Tone, 0f, 0.22f, 0.03f, 940f, 940f, 0.004f),
    }, Send: 0.2f);

    // Arrow into the ground: a dull thud and a little grit.
    public static readonly CueDef ArrowEarth = new("cue.arrowearth", 0.4f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.06f, 0.14f, 85f, 85f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.05f, 0.18f, 20f, 1000f, 0.004f),
    });

    // --- Water (audio-12): wading, splashing and floating. Louder and lower the deeper it is.

    // Wade, ankle deep (under 0.3 m): a light splash and a few drops.
    public static readonly CueDef WadeShallow = new("water.wade.shallow", 0.4f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.08f, 0.25f, 700f, 4000f, 0.006f),
        new CueLayer(Src.Noise, 0.03f, 0.06f, 0.08f, 2500f, 7000f, 0.004f),
        new CueLayer(Src.Tone, 0.02f, 0.05f, 0.05f, 350f, 900f, 0.004f),
    });

    // Wade, knee deep (0.3 to 0.65 m): a heavier slosh with a swirl behind it.
    public static readonly CueDef WadeKnee = new("water.wade.knee", 0.65f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.16f, 0.3f, 200f, 2500f, 0.015f),
        new CueLayer(Src.Noise, 0.08f, 0.2f, 0.2f, 100f, 900f, 0.08f),
        new CueLayer(Src.Noise, 0.2f, 0.07f, 0.06f, 2500f, 6500f, 0.004f),
    });

    // Wade, waist deep (over 0.65 m): a slow, deep push through the water.
    public static readonly CueDef WadeDeep = new("water.wade.deep", 0.9f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.25f, 0.35f, 80f, 1500f, 0.03f),
        new CueLayer(Src.Noise, 0.1f, 0.3f, 0.25f, 60f, 600f, 0.12f),
        new CueLayer(Src.Tone, 0.25f, 0.08f, 0.04f, 200f, 120f, 0.01f),
    });

    // Splash in: something (or you) hits the water: a crack of spray, a low whump and a sheet of drops.
    public static readonly CueDef SplashIn = new("water.splash", 1.0f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.14f, 0.27f, 300f, 5000f, 0.004f),
        new CueLayer(Src.Tone, 0f, 0.12f, 0.12f, 140f, 70f, 0.006f),
        new CueLayer(Src.Noise, 0.1f, 0.25f, 0.07f, 2000f, 7000f, 0.05f),
        new CueLayer(Src.Noise, 0.3f, 0.1f, 0.036f, 2500f, 7000f, 0.02f),
    }, Send: 0.1f);

    // Climb out: the water running off, a few drips.
    public static readonly CueDef Drip = new("water.drip", 0.7f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.15f, 0.12f, 300f, 2200f, 0.03f),
        new CueLayer(Src.Noise, 0.2f, 0.04f, 0.05f, 2500f, 7000f, 0.003f),
        new CueLayer(Src.Tone, 0.22f, 0.04f, 0.03f, 500f, 1200f, 0.003f),
    });

    // Paddle: one soft stroke while afloat and moving, a swish and a trickle off the hand.
    public static readonly CueDef Paddle = new("water.paddle", 0.9f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.25f, 0.13f, 300f, 2200f, 0.12f),
        new CueLayer(Src.Noise, 0.35f, 0.15f, 0.05f, 1500f, 5000f, 0.05f),
        new CueLayer(Src.Tone, 0.4f, 0.08f, 0.04f, 200f, 130f, 0.01f),
    });

    // Float: afloat and still, the water lapping softly at you.
    public static readonly CueDef Lap = new("water.lap", 1.4f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.5f, 0.06f, 100f, 900f, 0.4f),
    });

    public static CueDef? ForCue(Cue cue) => cue switch
    {
        Cue.Place => Place,
        Cue.Hit => Hit,
        Cue.Crit => Crit,
        Cue.Jump => Jump,
        Cue.Pickup => Pickup,
        Cue.Eat => EatBites,
        Cue.Drink => DrinkGulps,
        Cue.SkillUp => SkillUp,
        Cue.Learn => Learn,
        Cue.FireLit => FireLit,
        Cue.BowDraw => BowDraw,
        Cue.BowShot => BowShot,
        Cue.ArrowFlesh => ArrowFlesh,
        Cue.ArrowWood => ArrowWood,
        Cue.ArrowEarth => ArrowEarth,
        _ => null,
    };
}
