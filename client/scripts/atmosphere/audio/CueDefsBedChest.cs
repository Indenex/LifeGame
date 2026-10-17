namespace LifeGame;

// A straw bed and a wooden chest (audio-36).
public static partial class CueDefs
{
    // Lying down on a straw bed: a long, dry crackle of straw settling under you, with a soft thump.
    public static readonly CueDef RestLieBed = new("rest.lie.bed", 1.1f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.24f, 0.12f, 400f, 2800f, 0.12f),
        new CueLayer(Src.Noise, 0.08f, 0.04f, 0.13f, 2200f, 7000f, 0.003f),
        new CueLayer(Src.Noise, 0.19f, 0.035f, 0.11f, 2200f, 7000f, 0.003f),
        new CueLayer(Src.Noise, 0.3f, 0.05f, 0.1f, 2000f, 6500f, 0.004f),
        new CueLayer(Src.Noise, 0.45f, 0.04f, 0.08f, 2000f, 6500f, 0.004f),
        new CueLayer(Src.Tone, 0.28f, 0.08f, 0.09f, 100f, 65f, 0.02f),
    });

    // A wooden chest lid: a short dry creak of the hinge and a heavy, hollow clunk as it settles.
    static readonly CueLayer[] ChestLid =
    {
        new CueLayer(Src.Tone, 0f, 0.14f, 0.035f, 280f, 410f, 0.04f),
        new CueLayer(Src.Noise, 0f, 0.12f, 0.028f, 900f, 2400f, 0.05f),
        new CueLayer(Src.Tone, 0.22f, 0.05f, 0.112f, 150f, 90f, 0.004f),
        new CueLayer(Src.Noise, 0.22f, 0.025f, 0.084f, 250f, 1400f, 0.003f),
    };

    // Putting something into a chest: the lid and the thud of the thing set down inside.
    public static readonly CueDef ChestPut = new("store.chest.put", 0.6f, new[]
    {
        ChestLid[0], ChestLid[1], ChestLid[2], ChestLid[3],
        new CueLayer(Src.Tone, 0.1f, 0.04f, 0.070f, 170f, 110f, 0.004f),
    });

    // Taking something out: the lid, a shorter rummage and the lid again.
    public static readonly CueDef ChestTake = new("store.chest.take", 0.6f, new[]
    {
        ChestLid[0], ChestLid[1], ChestLid[2], ChestLid[3],
        new CueLayer(Src.Noise, 0.1f, 0.05f, 0.035f, 800f, 3200f, 0.02f),
    });
}
