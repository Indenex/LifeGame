using Godot;

namespace LifeGame;

// One hard cap on all the fog together (atmosphere-34, Justin: "fog is awful and
// blinding at some points"). The time of day, weather, woodland sun shafts and the
// god rays each thicken the air a little, and at a low sun they stacked into a
// cream wall where the player 15 m away was barely visible and looking toward the
// sun washed the screen out. After everything else has set the fog each frame,
// this pulls it back so that:
// - the player and anything within about 40 m always read clearly; fog only
//   softens the distance (both fogs together keep at least about 70% of the light
//   at 40 m, even in a storm);
// - looking toward a low sun never blows the screen out: the forward glow of the
//   air, the sun's light in the fog and the bloom are all limited.
// The dawn mist layers have their own cap in Mist.cs.
public partial class Atmosphere
{
    // Extinction per metre: depth fog and volumetric fog, and the two together.
    // (Combined cap lowered from 0.012 in atmosphere-37: distant trees nearly vanished at dawn.)
    const float MaxFogDensity = 0.008f, MaxMistDensity = 0.009f, MaxAirDensity = 0.010f;
    // The follow camera looks down from 20 to 30 m away, so even the player is that
    // deep in the air (atmosphere-39: golden hour read as a murky pink veil over
    // everything). The two fogs together keep at least this much of the light over the
    // camera-to-player distance; the far land still fades.
    const float NearClear = 0.9f, MinAirDensity = 0.003f;
    // How strongly the sun lights the fog, how far forward it scatters, and the bloom.
    const float MaxSunInFog = 1.6f, MaxAnisotropy = 0.7f, MaxGlowBloom = 0.1f, MaxGlowIntensity = 0.9f, MinGlowThreshold = 1.0f;

    void CapFog()
    {
        float fog = Mathf.Min(env.FogDensity, MaxFogDensity);
        float mist = env.VolumetricFogEnabled ? Mathf.Min(env.VolumetricFogDensity, MaxMistDensity) : 0f;
        // Together they still keep the near scene clear.
        float maxAir = MaxAirDensity;
        if (Player is { } p && p.IsInsideTree() && GetViewport()?.GetCamera3D() is { } cam)
        {
            float toPlayer = Mathf.Max(cam.GlobalPosition.DistanceTo(p.GlobalPosition), 1f);
            maxAir = Mathf.Clamp(-Mathf.Log(NearClear) / toPlayer, MinAirDensity, MaxAirDensity);
        }
        if (fog + mist > maxAir)
        {
            float k = maxAir / (fog + mist);
            fog *= k;
            mist *= k;
        }
        env.FogDensity = fog;
        if (env.VolumetricFogEnabled) env.VolumetricFogDensity = mist;
        sun.LightVolumetricFogEnergy = Mathf.Min(sun.LightVolumetricFogEnergy, MaxSunInFog);
        env.VolumetricFogAnisotropy = Mathf.Min(env.VolumetricFogAnisotropy, MaxAnisotropy);
        env.GlowBloom = Mathf.Min(env.GlowBloom, MaxGlowBloom);
        env.GlowIntensity = Mathf.Min(env.GlowIntensity, MaxGlowIntensity);
        env.GlowHdrThreshold = Mathf.Max(env.GlowHdrThreshold, MinGlowThreshold);
    }
}
