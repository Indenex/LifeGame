namespace LifeGame;

// Wild rabbits heard (audio-32): small, dry and quick. No bleating.
public static partial class CueDefs
{
    // The hind-foot thump of an alarmed rabbit: two dull thuds on the ground.
    public static readonly CueDef RabbitThump = new("rabbit.thump", 0.4f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.05f, 0.120f, 95f, 60f, 0.003f),
        new CueLayer(Src.Noise, 0f, 0.02f, 0.060f, 150f, 900f, 0.002f),
        new CueLayer(Src.Tone, 0.13f, 0.05f, 0.102f, 95f, 60f, 0.003f),
        new CueLayer(Src.Noise, 0.13f, 0.02f, 0.048f, 150f, 900f, 0.002f),
    });

    // A tiny sharp squeak when hurt: a thin rising-then-falling chirp.
    public static readonly CueDef RabbitSqueak = new("rabbit.squeak", 0.3f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.05f, 0.096f, 1500f, 2300f, 0.004f),
        new CueLayer(Src.Tone, 0.05f, 0.06f, 0.080f, 2300f, 1400f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.02f, 0.024f, 3000f, 7000f, 0.002f),
    });

    // Nibbling and rustling in the grass: a few small dry ticks.
    public static readonly CueDef RabbitRustle = new("rabbit.rustle", 0.4f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.05f, 0.200f, 1500f, 5000f, 0.025f),
        new CueLayer(Src.Noise, 0.1f, 0.012f, 0.200f, 2500f, 6500f, 0.002f),
        new CueLayer(Src.Noise, 0.19f, 0.012f, 0.160f, 2500f, 6500f, 0.002f),
    });
}
