namespace LifeGame;

// Crushing grit and grog (audio-33): a held stone struck down on the pile, again and again.
public static partial class CueDefs
{
    // Stone on stone: a dull knock, with a gritty crumble as the pile gives.
    public static readonly CueDef PoundThud = new("tool.pound", 0.45f, new[]
    {
        new CueLayer(Src.Tone, 0f, 0.04f, 0.135f, 190f, 110f, 0.003f),
        new CueLayer(Src.Noise, 0f, 0.02f, 0.150f, 700f, 3500f, 0.002f),
        new CueLayer(Src.Noise, 0.03f, 0.07f, 0.075f, 2000f, 6500f, 0.01f),
    });
}
