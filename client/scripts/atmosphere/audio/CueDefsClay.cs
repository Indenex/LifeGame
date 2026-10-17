namespace LifeGame;

// Digging in clay (audio-27), played over the ordinary dig cue where the ground is clay.
public static partial class CueDefs
{
    // The blade or stick pulled free of wet clay: a sticky suck and a low wet plop.
    public static readonly CueDef DigClay = new("tool.dig.clay", 0.5f, new[]
    {
        new CueLayer(Src.Noise, 0.02f, 0.1f, 0.2f, 250f, 1400f, 0.03f),
        new CueLayer(Src.Tone, 0.12f, 0.05f, 0.1f, 160f, 360f, 0.01f),
        new CueLayer(Src.Noise, 0.14f, 0.02f, 0.08f, 1200f, 3500f, 0.003f),
    });

    // A load of clay thrown off the spade: a wet slap where it lands.
    public static readonly CueDef ClayThrow = new("tool.dig.claythrow", 0.4f, new[]
    {
        new CueLayer(Src.Noise, 0.08f, 0.05f, 0.132f, 200f, 1600f, 0.004f),
        new CueLayer(Src.Tone, 0.08f, 0.05f, 0.072f, 140f, 90f, 0.004f),
    });
}
