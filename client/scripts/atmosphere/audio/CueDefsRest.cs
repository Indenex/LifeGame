namespace LifeGame;

// Sitting and lying down heard (audio-29): cloth, grass and a woven mat, soft and unhurried.
public static partial class CueDefs
{
    // Sitting down: a short rustle of clothes and a light settle.
    public static readonly CueDef RestSit = new("rest.sit", 0.5f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.1f, 0.12f, 600f, 3200f, 0.06f),
        new CueLayer(Src.Tone, 0.12f, 0.05f, 0.08f, 120f, 85f, 0.01f),
    });

    // Lying down on the ground: a longer rustle of cloth on grass and a soft body-weight thump.
    public static readonly CueDef RestLie = new("rest.lie", 0.9f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.2f, 0.14f, 500f, 3000f, 0.1f),
        new CueLayer(Src.Noise, 0.15f, 0.12f, 0.08f, 2000f, 6000f, 0.05f),
        new CueLayer(Src.Tone, 0.3f, 0.07f, 0.1f, 100f, 70f, 0.02f),
    });

    // Lying down on a woven mat: the same, with the dry crackle of reeds under you.
    public static readonly CueDef RestLieMat = new("rest.lie.mat", 0.9f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.2f, 0.12f, 500f, 3000f, 0.1f),
        new CueLayer(Src.Noise, 0.1f, 0.03f, 0.1f, 2500f, 7000f, 0.002f),
        new CueLayer(Src.Noise, 0.22f, 0.03f, 0.08f, 2500f, 7000f, 0.002f),
        new CueLayer(Src.Noise, 0.34f, 0.04f, 0.07f, 2500f, 7000f, 0.002f),
        new CueLayer(Src.Tone, 0.3f, 0.07f, 0.09f, 100f, 70f, 0.02f),
    });

    // Getting up: a quick rustle that rises and a small shuffle of the feet.
    public static readonly CueDef RestUp = new("rest.up", 0.6f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.12f, 0.12f, 700f, 3600f, 0.04f),
        new CueLayer(Src.Tone, 0.2f, 0.04f, 0.07f, 130f, 90f, 0.005f),
    });
}
