namespace LifeGame;

// Rabbit snare (audio-38): soft, never violent. A twig springs and a cord whips; setting and
// collecting are a quiet rustle and a cord creak.
public static partial class CueDefs
{
    // The snare catches: a small wooden snap and a short whip of cord.
    public static readonly CueDef SnareSnap = new("snare.snap", 0.4f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.012f, 0.240f, 900f, 420f, 0.001f),
        new CueLayer(Src.Noise, 0f, 0.010f, 0.320f, 1800f, 6000f, 0.001f),
        new CueLayer(Src.Noise, 0.02f, 0.05f, 0.144f, 2500f, 7500f, 0.015f),
    });

    // Setting it: leaves rustle while the cord creaks as it is drawn tight.
    public static readonly CueDef SnareSet = new("snare.set", 0.9f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.14f, 0.288f, 1200f, 5000f, 0.06f),
        new CueLayer(Src.Noise, 0.28f, 0.10f, 0.216f, 1500f, 5500f, 0.05f),
        new CueLayer(Src.Tone, 0.12f, 0.13f, 0.108f, 240f, 330f, 0.05f),
    });

    // Collecting it: a shorter rustle and a softer creak as the cord is gathered.
    public static readonly CueDef SnareCollect = new("snare.collect", 0.7f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.12f, 0.284f, 1300f, 5200f, 0.05f),
        new CueLayer(Src.Tone, 0.08f, 0.10f, 0.097f, 320f, 250f, 0.04f),
    });
}
