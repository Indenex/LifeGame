namespace LifeGame;

// Meat roasting over a fire (audio-21). Data cues; RoastSounds schedules them.
public static partial class CueDefs
{
    // A short breath of hiss as juice meets the heat.
    public static readonly CueDef RoastSizzle = new("roast.sizzle", 0.45f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.13f, 0.0040f, 3000f, 9000f, 0.012f),
        new CueLayer(Src.Noise, 0.02f, 0.07f, 0.0022f, 1200f, 3200f, 0.01f),
    });

    // A spit of fat: a sharp tick with a little pop under it.
    public static readonly CueDef RoastSpit = new("roast.spit", 0.2f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.008f, 0.0044f, 2500f, 8000f, 0.001f),
        new CueLayer(Src.Tone, 0f, 0.02f, 0.0044f, 520f, 300f, 0.002f),
    });

    // A drop of fat falling on the coals: a spat hiss that dies fast.
    public static readonly CueDef RoastDrip = new("roast.drip", 0.3f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.05f, 0.0050f, 2000f, 7000f, 0.002f),
        new CueLayer(Src.Noise, 0f, 0.015f, 0.0030f, 600f, 1800f, 0.001f),
    });
}
