using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Morning frost (atmosphere-28). Only in autumn, and only on some mornings (Justin):
// each night rolls its own chance from the day number, the same for every player.
// After a cold, clear, still night a pale rime
// settles on the grass, leaves, rocks and bark, glinting as you walk; once the sun
// is up it melts back in patches, gone by mid-morning (sooner in warm air, at once
// in rain). Overcast nights hold the warmth in, so they bring none. The painted and
// grass shaders draw it from one global, frost (0 to 1).
//
// Command line: --frost=0..1 holds it at that amount, for screenshots.
public partial class Atmosphere
{
    static readonly StringName NFrost = "frost", NFrostSeed = "frost_seed";

    // How long a full rime takes to melt in clear sunshine, as a share of a day
    // (about two in-game hours, so it's gone by mid-morning).
    const float FrostMeltDays = 0.08f;

    float frostLevel;
    float? argFrost;

    void ReadyFrost() => argFrost = ArgFloat("frost");

    void UpdateFrost(float dt)
    {
        float sunHeight = WorldTime.SunHeight(timeOfDay);
        float clear = 1f - Mathf.SmoothStep(0.35f, 0.75f, cloud);
        float cold = Mathf.SmoothStep(8f, 1f, airCelsius);
        // Autumn only; winter has snow instead.
        float autumn = Mathf.SmoothStep(0.47f, 0.52f, Year) * (1f - Mathf.SmoothStep(0.72f, 0.76f, Year));
        // Each night (counted toward the morning it ends in) has its own chance and
        // strength, from the day number: frost on a little under half of autumn mornings.
        long sinceStart = LocalMicros() + clockOffsetMicros - dayStartMicros;
        int night = (int)System.Math.Floor(sinceStart / 1_000_000.0 / Mathf.Max(60f, dayLength) + 0.25);
        float tonight = Mathf.SmoothStep(0.5f, 0.62f, World.Noise(night, 0, 1f, 977u)) * (0.55f + 0.45f * World.Noise(night, 1, 1f, 978u));
        RenderingServer.GlobalShaderParameterSet(NFrostSeed, Mathf.PosMod(night * 0.6180339f, 1f) * 97f);
        float rain = Mathf.Clamp(rainAmount * 6f, 0f, 1f);
        if (sunHeight < 0.02f)
        {
            // Night and the first grey of dawn: it forms over a few minutes on a
            // cold clear night, and fades again if cloud or rain comes over.
            float settle = cold * clear * autumn * tonight * (1f - rain) * (1f - Mathf.SmoothStep(0.5f, 0.9f, wind));
            frostLevel = Mathf.Lerp(frostLevel, settle, 1f - Mathf.Exp(-dt / 40f));
        }
        else
        {
            // Once the sun is up it only melts: faster as it climbs, slower under
            // cloud, quickly in air above freezing, at once in the rain.
            float days = dt / Mathf.Max(60f, dayLength);
            float sunMelt = Mathf.SmoothStep(0.02f, 0.3f, sunHeight) * (1f - 0.6f * (1f - clear));
            float warmMelt = Mathf.SmoothStep(2f, 8f, airCelsius);
            frostLevel = Mathf.Max(0f, frostLevel - days / FrostMeltDays * (sunMelt + warmMelt) - dt * rain / 20f);
        }
        RenderingServer.GlobalShaderParameterSet(NFrost, argFrost ?? frostLevel);
    }
}
