namespace LifeGame;

// Clay oil lamps heard (audio-26): a tallow flame is small and quiet.
public static partial class CueDefs
{
    // A breath of flame: a faint soft hiss as the wick flickers.
    public static readonly CueDef LampFlutter = new("lamp.flutter", 0.5f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.14f, 0.096f, 3000f, 8500f, 0.05f),
    });

    // A tiny tick of the wick as it burns.
    public static readonly CueDef LampTick = new("lamp.tick", 0.12f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.008f, 0.360f, 2500f, 7000f, 0.001f),
        new CueLayer(Src.Tone, 0f, 0.012f, 0.090f, 900f, 600f, 0.001f),
    });

    // The wick catching: a soft puff of flame.
    public static readonly CueDef LampLight = new("lamp.light", 0.5f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.12f, 0.140f, 300f, 2200f, 0.03f),
        new CueLayer(Src.Noise, 0.02f, 0.06f, 0.056f, 2500f, 7000f, 0.02f),
    });

    // The flame guttering out: a thin hiss and a last tiny sigh.
    public static readonly CueDef LampOut = new("lamp.out", 0.6f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.18f, 0.096f, 1500f, 6000f, 0.03f),
        new CueLayer(Src.Noise, 0.04f, 0.1f, 0.040f, 300f, 1200f, 0.04f),
    });
}
