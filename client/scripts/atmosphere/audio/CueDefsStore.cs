namespace LifeGame;

// Things put into and taken out of baskets and storage jars (audio-21).
public static partial class CueDefs
{
    // Wicker: a dry rustle and a soft thump of something set down.
    public static readonly CueDef BasketPut = new("store.basket.put", 0.3f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.06f, 0.132f, 1200f, 4200f, 0.008f),
        new CueLayer(Src.Tone, 0.03f, 0.04f, 0.148f, 170f, 100f, 0.004f),
    });

    // A hand lifting something out through the weave: a shorter, rising rustle.
    public static readonly CueDef BasketTake = new("store.basket.take", 0.25f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.05f, 0.332f, 1500f, 5000f, 0.02f),
        new CueLayer(Src.Noise, 0.04f, 0.02f, 0.144f, 600f, 1600f, 0.004f),
    });

    // Fired clay: a clink and a hollow knock as something goes in under the lid.
    public static readonly CueDef JarPut = new("store.jar.put", 0.35f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.05f, 0.058f, 1250f, 1050f, 0.002f),
        new CueLayer(Src.Tone, 0.01f, 0.07f, 0.086f, 240f, 170f, 0.004f),
        new CueLayer(Src.Noise, 0f, 0.012f, 0.052f, 2500f, 7000f, 0.001f),
    });

    // The lid scrapes and something comes out: a soft clay scrape and a lighter clink.
    public static readonly CueDef JarTake = new("store.jar.take", 0.35f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.07f, 0.150f, 700f, 2600f, 0.02f),
        new CueLayer(Src.Tone, 0.06f, 0.04f, 0.112f, 1100f, 950f, 0.002f),
    });
}
