using Godot;

namespace LifeGame;

// The colour grade for Justin's "closer to Valheim, awe-inspiring lighting"
// direction (atmosphere-25; Justin picked it as the default, 2026-10-17). On by
// default; --grade=0 turns it off for comparison, in-between values blend.
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
// D-313, Justin's art direction: "cutesy, relaxing, with a hint of realism". The
// cosy layer on top keeps today's light and shadows but softens them toward the
// chibi characters: lower contrast and gentle saturation (fresh greens, not yellow
// mud), more fill so shade is a soft lavender-blue rather than deep, a peach-rose
// dusk instead of brown haze, thinner god-ray air, a pastel sky and a gentler night.
// On by default; --cosy=0 shows the earlier look for before/after renders.
//
// God rays have a quality level for slower PCs: 0 off, 1 normal, 2 high (reaching
// further). UX's Options sets Atmosphere.GodRays; --godrays=0|1|2 overrides it.
public partial class Atmosphere
{
    static readonly StringName NGrade = "grade";
    float grade;
    static readonly StringName NCosy = "cosy";
    float cosy;
    GradientTexture1D? cosyCurve;

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

    // The cosy curve: darks lifted to a soft plum (shade never goes black), mids
    // near neutral so greens stay green, lights a creamy peach-white.
    static GradientTexture1D CosyCurve() => new()
    {
        Gradient = new Gradient
        {
            Offsets = new[] { 0f, 0.5f, 0.85f, 1f },
            Colors = new[]
            {
                new Color(0.06f, 0.045f, 0.09f), new Color(0.53f, 0.52f, 0.50f),
                new Color(0.86f, 0.84f, 0.79f), new Color(0.97f, 0.95f, 0.90f),
            },
        },
    };

    void ReadyGrade()
    {
        grade = Mathf.Clamp(ArgFloat("grade") ?? 1f, 0f, 1f);
        cosy = Mathf.Clamp(ArgFloat("cosy") ?? 1f, 0f, 1f);
        GodRays = Mathf.Clamp(Settings.Get("graphics", "light_rays", 1), 0, 2);
        if (ArgFloat("godrays") is { } level) GodRays = Mathf.Clamp((int)level, 0, 2);
    }

    void Grade(float sunHeight, float daylight, float overcast)
    {
        sky.SetShaderParameter(NGrade, grade);
        sky.SetShaderParameter(NCosy, cosy * grade);
        if (grade <= 0f) return;
        float soft = cosy * grade;
        float clear = 1f - overcast;
        float lowSun = Mathf.SmoothStep(-0.02f, 0.1f, sunHeight) * (1f - Mathf.SmoothStep(0.2f, 0.45f, sunHeight)) * clear * grade;
        float night = (1f - daylight) * grade;

        // Lit tips read warm and bright, as in reference 02: a warmer, stronger sun.
        // Cosy: a softer warm-white day sun and a peach-rose low sun, not orange.
        var daySun = new Color(1f, 0.93f, 0.78f).Lerp(new Color(1f, 0.95f, 0.87f), soft);
        var lowSunColor = new Color(1f, 0.70f, 0.40f).Lerp(new Color(1f, 0.70f, 0.56f), soft);
        sun.LightColor = sun.LightColor.Lerp(daySun, 0.25f * grade * daylight).Lerp(lowSunColor, 0.35f * lowSun);
        sun.LightEnergy *= 1f + 0.1f * grade * daylight + 0.15f * lowSun;
        // Moonlight is a readable blue-violet fill, not near-black: the moon (the same
        // light at night) shines brighter, so cabins and people keep their silhouettes.
        sun.LightEnergy *= 1f + 0.6f * night;
        // Shade is purple-grey by day, blue-violet at golden hour, a soft moonlit
        // blue-violet at night.
        // Cosy shade is a lighter lavender-blue by day and a softer moonlit blue.
        env.AmbientLightColor = env.AmbientLightColor
            .Lerp(new Color(0.48f, 0.44f, 0.60f).Lerp(new Color(0.60f, 0.58f, 0.76f), soft), 0.2f * grade * daylight + 0.1f * soft * daylight)
            .Lerp(new Color(0.40f, 0.42f, 0.66f).Lerp(new Color(0.62f, 0.50f, 0.66f), soft), 0.3f * lowSun)
            .Lerp(new Color(0.30f, 0.32f, 0.58f).Lerp(new Color(0.36f, 0.40f, 0.64f), soft), 0.45f * night);
        env.AmbientLightSkyContribution = 0.75f - 0.1f * grade - 0.2f * lowSun - 0.1f * night;
        // Deeper shade: less fill light and stronger contact shadows, so darks hold their
        // value instead of going milky (Environment's env-13 comparison with reference 02).
        // Less fill by day for deep shade; more at night so the dark stays readable.
        // Cosy gives back most of that fill, so shade reads soft and coloured.
        env.AmbientLightEnergy *= (1f - 0.25f * grade * daylight + 0.2f * soft * daylight) * (1f + 0.7f * night + 0.2f * soft * night);
        env.TonemapExposure *= (1f + 0.15f * night) * (1f + 0.05f * soft);
        env.SsaoIntensity = 1.8f + 1.2f * grade - 1.0f * soft;
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
        // Cosy: dawn and dusk air glows peach-rose, not brown, and the day haze is a
        // paler cream.
        env.FogLightColor = env.FogLightColor.Lerp(new Color(0.86f, 0.82f, 0.74f) * (0.6f + 0.4f * daylight), 0.3f * soft * daylight)
            .Lerp(new Color(0.96f, 0.74f, 0.70f), 0.45f * soft * lowSun);
        // Gentle bloom: a softer threshold so fire and bright light glow, swelling at low sun.
        env.GlowIntensity += 0.15f * grade + 0.3f * lowSun;
        env.GlowBloom = 0.08f + 0.06f * grade + 0.1f * lowSun;
        env.GlowHdrThreshold = 1.1f - 0.25f * grade;
        // Faded and warm, but with strong darks: violet-tinted shade, creamy lights.
        // The Golden Skull packs are muted, but lighting already greys them in engine, so
        // saturation stays where it is; the curve's warm mids do the yellowing.
        // Cosy: flatter contrast and a gentle saturation lift on the plum-cream curve.
        // (Halved from 0.1 in atmosphere-37: with the lifted darks it read as milky haze.)
        env.AdjustmentContrast = 1.08f + 0.06f * grade - 0.05f * soft;
        env.AdjustmentSaturation *= 1f + 0.12f * soft;
        env.AdjustmentColorCorrection = grade <= 0.5f ? null : soft > 0.5f ? (cosyCurve ??= CosyCurve()) : (curve ??= FadedCurve());
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
            // Cosy: softer beams through thinner air, never a wall of haze.
            float rays = lowSun * (GodRays >= 2 ? 1.25f : 1f) * (1f - 0.35f * soft);
            sun.LightVolumetricFogEnergy += 1.8f * rays;
            env.VolumetricFogDensity *= 1f + 1.2f * rays;
            env.VolumetricFogAnisotropy = Mathf.Max(env.VolumetricFogAnisotropy, 0.6f + 0.25f * rays);
        }
    }
}
