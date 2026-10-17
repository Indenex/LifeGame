using Godot;

namespace LifeGame;

// A prototype colour grade for Justin's "closer to Valheim, awe-inspiring
// lighting" direction (atmosphere-25, options a + c to the PM). Off by default
// until Justin picks (D-154); --grade=1 turns it on fully, in-between values blend.
//
// Tuned to Justin's reference images (docs/art-reference/): a slightly faded, warm
// grade (blacks lifted toward violet, highlights a touch creamy, a little less
// contrast), blue-violet shadows all day (so snow is warm white with blue shade),
// gentle bloom that lets fire and other bright light glow, and the far land a bit
// softer out of focus. At golden hour the sun turns honey-warm and the bloom swells;
// nights lean deeper blue. The sky gives the clouds
// near a low sun warm glowing rims. At dawn and dusk the air everywhere, not only
// in the woods, thickens a little and scatters toward the sun, so beams of light
// fan out over the hills (god rays, option b).
//
// God rays have a quality level for slower PCs: 0 off, 1 normal, 2 high (reaching
// further). UX's Options sets Atmosphere.GodRays; --godrays=0|1|2 overrides it.
public partial class Atmosphere
{
    static readonly StringName NGrade = "grade";
    float grade;

    public static int GodRays = 1;
    GradientTexture1D? curve;
    int raysApplied = -1;

    // The faded warm curve: each channel's darks lifted toward violet, its lights
    // eased toward cream with a soft shoulder so pale things (straw, skin) keep their
    // detail instead of clipping, and the mids nudged warm (a little more red and green, less
    // blue), so lit grass reads bright yellow-green as in reference 02.
    static GradientTexture1D FadedCurve() => new()
    {
        Gradient = new Gradient
        {
            Offsets = new[] { 0f, 0.5f, 0.85f, 1f },
            Colors = new[]
            {
                new Color(0.04f, 0.03f, 0.07f), new Color(0.53f, 0.52f, 0.45f),
                new Color(0.84f, 0.82f, 0.74f), new Color(0.95f, 0.93f, 0.86f),
            },
        },
    };

    void ReadyGrade()
    {
        grade = Mathf.Clamp(ArgFloat("grade") ?? 0f, 0f, 1f);
        GodRays = Mathf.Clamp(Settings.Get("graphics", "light_rays", 1), 0, 2);
        if (ArgFloat("godrays") is { } level) GodRays = Mathf.Clamp((int)level, 0, 2);
    }

    void Grade(float sunHeight, float daylight, float overcast)
    {
        sky.SetShaderParameter(NGrade, grade);
        if (grade <= 0f) return;
        float clear = 1f - overcast;
        float lowSun = Mathf.SmoothStep(-0.02f, 0.1f, sunHeight) * (1f - Mathf.SmoothStep(0.2f, 0.45f, sunHeight)) * clear * grade;
        float night = (1f - daylight) * grade;

        // Lit tips read warm and bright, as in reference 02: a warmer, stronger sun.
        sun.LightColor = sun.LightColor.Lerp(new Color(1f, 0.93f, 0.78f), 0.25f * grade * daylight).Lerp(new Color(1f, 0.70f, 0.40f), 0.35f * lowSun);
        sun.LightEnergy *= 1f + 0.1f * grade * daylight + 0.15f * lowSun;
        // Moonlight is a readable blue-violet fill, not near-black: the moon (the same
        // light at night) shines brighter, so cabins and people keep their silhouettes.
        sun.LightEnergy *= 1f + 0.6f * night;
        // Shade is purple-grey by day, blue-violet at golden hour, a soft moonlit
        // blue-violet at night.
        env.AmbientLightColor = env.AmbientLightColor
            .Lerp(new Color(0.48f, 0.44f, 0.60f), 0.2f * grade * daylight)
            .Lerp(new Color(0.40f, 0.42f, 0.66f), 0.3f * lowSun)
            .Lerp(new Color(0.30f, 0.32f, 0.58f), 0.45f * night);
        env.AmbientLightSkyContribution = 0.75f - 0.1f * grade - 0.2f * lowSun - 0.1f * night;
        // Deeper shade: less fill light and stronger contact shadows, so darks hold their
        // value instead of going milky (Environment's env-13 comparison with reference 02).
        // Less fill by day for deep shade; more at night so the dark stays readable.
        env.AmbientLightEnergy *= (1f - 0.25f * grade * daylight) * (1f + 0.7f * night);
        env.TonemapExposure *= 1f + 0.15f * night;
        env.SsaoIntensity = 1.8f + 1.2f * grade;
        env.SsaoRadius = 1.6f + 0.6f * grade;
        // Clear air near the camera by day: thinner distance fog and haze, keeping the
        // thicker air at dawn and dusk for the light rays.
        float dayClear = grade * daylight * (1f - lowSun);
        env.FogDensity *= 1f - 0.4f * dayClear;
        env.VolumetricFogDensity *= 1f - 0.5f * dayClear;
        env.VolumetricFogAmbientInject = 0.4f - 0.25f * grade;
        // The distance fades warm, as in reference 02, rather than to the cool sky: the fog
        // takes less of the sky's colour and leans toward a soft warm beige by day.
        env.FogAerialPerspective = 0.5f - 0.3f * grade;
        env.FogLightColor = env.FogLightColor.Lerp(new Color(0.78f, 0.72f, 0.60f) * (0.6f + 0.4f * daylight), 0.35f * grade * daylight);
        // Gentle bloom: a softer threshold so fire and bright light glow, swelling at low sun.
        env.GlowIntensity += 0.15f * grade + 0.3f * lowSun;
        env.GlowBloom = 0.08f + 0.06f * grade + 0.1f * lowSun;
        env.GlowHdrThreshold = 1.1f - 0.25f * grade;
        // Faded and warm, but with strong darks: violet-tinted shade, creamy lights.
        // The Golden Skull packs are muted, but lighting already greys them in engine, so
        // saturation stays where it is; the curve's warm mids do the yellowing.
        env.AdjustmentContrast = 1.08f + 0.06f * grade;
        env.AdjustmentColorCorrection = grade > 0.5f ? (curve ??= FadedCurve()) : null;
        if (GetViewport()?.GetCamera3D()?.Attributes is CameraAttributesPractical lens)
            lens.DofBlurAmount = 0.05f + 0.03f * grade;

        // God rays: the low sun lights the air across the whole view, not just the woods.
        if (raysApplied != GodRays)
        {
            raysApplied = GodRays;
            env.VolumetricFogLength = GodRays >= 2 ? 112f : 64f;
        }
        if (GodRays > 0)
        {
            float rays = lowSun * (GodRays >= 2 ? 1.25f : 1f);
            sun.LightVolumetricFogEnergy += 1.8f * rays;
            env.VolumetricFogDensity *= 1f + 1.2f * rays;
            env.VolumetricFogAnisotropy = Mathf.Max(env.VolumetricFogAnisotropy, 0.6f + 0.25f * rays);
        }
    }
}
