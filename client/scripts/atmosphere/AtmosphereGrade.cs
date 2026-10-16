using Godot;

namespace LifeGame;

// A prototype colour grade for Justin's "closer to Valheim, awe-inspiring
// lighting" direction (atmosphere-25, options a + c to the PM). Off by default
// until Justin picks (D-154); --grade=1 turns it on fully, in-between values blend.
//
// At golden hour the sun turns honey-warm and a little stronger while the shadows
// go cool blue-teal (more of the ambient light from a cool colour, less from the
// sky), and the bloom swells. Nights lean deeper blue. The sky gives the clouds
// near a low sun warm glowing rims.
public partial class Atmosphere
{
    static readonly StringName NGrade = "grade";
    float grade;

    void ReadyGrade() => grade = Mathf.Clamp(ArgFloat("grade") ?? 0f, 0f, 1f);

    void Grade(float sunHeight, float daylight, float overcast)
    {
        sky.SetShaderParameter(NGrade, grade);
        if (grade <= 0f) return;
        float clear = 1f - overcast;
        float lowSun = Mathf.SmoothStep(-0.02f, 0.1f, sunHeight) * (1f - Mathf.SmoothStep(0.2f, 0.45f, sunHeight)) * clear * grade;
        float night = (1f - daylight) * grade;

        sun.LightColor = sun.LightColor.Lerp(new Color(1f, 0.70f, 0.40f), 0.35f * lowSun);
        sun.LightEnergy *= 1f + 0.15f * lowSun;
        env.AmbientLightColor = env.AmbientLightColor.Lerp(new Color(0.30f, 0.42f, 0.64f), 0.55f * lowSun).Lerp(new Color(0.12f, 0.18f, 0.40f), 0.35f * night);
        env.AmbientLightSkyContribution = 0.75f - 0.35f * lowSun - 0.15f * night;
        env.GlowIntensity += 0.35f * lowSun;
        env.GlowBloom = 0.08f + 0.12f * lowSun;
        env.AdjustmentSaturation += 0.08f * grade;
        env.AdjustmentContrast = 1.08f + 0.05f * grade;
    }
}
