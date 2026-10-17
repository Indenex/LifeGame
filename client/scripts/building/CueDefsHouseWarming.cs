namespace LifeGame;

// The house-warming (building-27): when friends finish a home together, the helpers nearby
// hear a quiet, warm cheer. Kept in House building's files while no Audio lane runs (PM,
// 2026-10-17); Audio may retune it. Hands, a knock on the new door post and a soft low
// chord that rings into the room, no voices (like the emotes, audio-25).
public static partial class CueDefs
{
    static CueLayer SoftClap(float at, float gain) => new(Src.Noise, at, 0.014f, gain, 1200f, 4800f, 0.002f);

    public static readonly CueDef HouseWarming = new("building.housewarming", 2.6f, new[]
    {
        // Two knocks on the door post.
        new CueLayer(Src.Tone, 0f, 0.05f, 0.09f, 180f, 140f, 0.002f),
        new CueLayer(Src.Noise, 0f, 0.03f, 0.06f, 250f, 1400f, 0.002f),
        new CueLayer(Src.Tone, 0.2f, 0.05f, 0.08f, 175f, 135f, 0.002f),
        new CueLayer(Src.Noise, 0.2f, 0.03f, 0.05f, 250f, 1400f, 0.002f),
        // A few friends' claps, loose and unhurried, fading.
        SoftClap(0.55f, 0.15f), SoftClap(0.68f, 0.11f), SoftClap(0.83f, 0.14f), SoftClap(0.95f, 0.10f),
        SoftClap(1.12f, 0.11f), SoftClap(1.27f, 0.07f), SoftClap(1.45f, 0.05f),
        // A warm low chord (G3, B3, D4) that swells in softly under the claps.
        new CueLayer(Src.Tone, 0.5f, 0.9f, 0.035f, 196f, 196f, 0.12f),
        new CueLayer(Src.Tone, 0.58f, 0.85f, 0.028f, 246.9f, 246.9f, 0.12f),
        new CueLayer(Src.Tone, 0.66f, 0.8f, 0.024f, 293.7f, 293.7f, 0.12f),
    }, Send: 0.35f);
}
