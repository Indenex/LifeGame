namespace LifeGame;

// Deer at graze heard (audio-30): quiet, close-range, non-vocal-calls (the bark, roar and hooves
// are in SoundMix.Deer).
public static partial class CueDefs
{
    // Tearing a mouthful of grass and a couple of chews.
    public static readonly CueDef DeerCrunch = new("deer.crunch", 0.5f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.05f, 0.280f, 600f, 2800f, 0.025f),
        new CueLayer(Src.Noise, 0.14f, 0.018f, 0.240f, 1500f, 5500f, 0.002f),
        new CueLayer(Src.Noise, 0.26f, 0.018f, 0.180f, 1500f, 5500f, 0.002f),
    });

    // A short nasal snort as the head comes up: a puff of breath with a soft rattle in it.
    public static readonly CueDef DeerSnort = new("deer.snort", 0.5f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.1f, 0.180f, 400f, 2200f, 0.012f),
        new CueLayer(Src.Noise, 0.03f, 0.04f, 0.084f, 1200f, 4000f, 0.01f),
        new CueLayer(Src.Tone, 0.02f, 0.06f, 0.036f, 220f, 150f, 0.01f),
    });
}
