namespace LifeGame;

// Emotes heard (audio-25): cloth, hands and feet only, no voices.
public static partial class CueDefs
{
    // An arm sweeping through the air: a soft rising swish of cloth.
    public static readonly CueDef EmoteSwish = new("emote.swish", 0.45f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.12f, 0.240f, 700f, 3200f, 0.08f),
        new CueLayer(Src.Noise, 0.05f, 0.08f, 0.100f, 200f, 900f, 0.05f),
    });

    // Two quick claps of the hands.
    public static readonly CueDef EmoteClap = new("emote.clap", 0.35f, new[]
    {
        new CueLayer(Src.Noise, 0f, 0.012f, 0.420f, 1500f, 6000f, 0.001f),
        new CueLayer(Src.Tone, 0f, 0.02f, 0.070f, 800f, 600f, 0.001f),
        new CueLayer(Src.Noise, 0.11f, 0.012f, 0.364f, 1500f, 6000f, 0.001f),
        new CueLayer(Src.Tone, 0.11f, 0.02f, 0.063f, 800f, 600f, 0.001f),
    });

    // A foot set down on the beat: a soft dull tap on the ground.
    public static readonly CueDef EmoteTap = new("emote.tap", 0.2f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.04f, 0.1f, 130f, 85f, 0.003f),
        new CueLayer(Src.Noise, 0f, 0.02f, 0.1f, 300f, 1500f, 0.002f),
    });
}
