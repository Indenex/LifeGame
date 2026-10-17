using Godot;

namespace LifeGame;

// Justin's cutesy picks (D-313) #8 and #10, atmosphere-36: what a soft, friendly
// camera lens adds.
// - Soft glow and light leaks: the bloom spreads wider and warmer (the same
//   amount of glow, moved to the softer levels and tinted honey), so the sun,
//   fires and lamps sit in a gentle haze. At golden hour pastel peach, rose and
//   lilac light leaks in from the screen edge on the sun's side. Subtle, and the
//   fog cap (atmosphere-34) still limits the bloom afterwards.
// - Gentle vignette: the corners darken very softly, warm at dusk, cool at night,
//   barely there by day.
// Options toggles both (graphics soft_glow / vignette); --soft-glow=0 and
// --vignette=0 turn them off for comparison.
//
// Modeling's soft rim light (Painted.Rim, D-313 #1) multiplies the light colour by
// the global rim_tint, set here by time of day. The sun already warms toward dusk,
// so the tint is relative to it: Modeling's honey by day, a touch pinker at golden
// hour, rose just before sunset, and cooler under the moon. rgb is the colour, a
// scales Painted.RimStrength (1 by day, up to about 1.3 backlit at golden hour,
// less at night and under cloud).
public partial class Atmosphere
{
    public static bool SoftGlow = true, Vignette = true;

    CanvasLayer? lens;
    ColorRect? leakRect, vignetteRect;
    ShaderMaterial? leakMat, vignetteMat;
    ImageTexture? warmGlow;
    readonly float[] glowLevels = new float[7];
    // The soft glow's weights: the tight levels down, the wide ones up.
    static readonly float[] SoftLevels = { 0f, 0.5f, 0.8f, 1.1f, 1.0f, 0.6f, 0f };

    static readonly StringName NLeakDir = "leak_dir", NLeakAmount = "amount", NTint = "tint", NRimTint = "rim_tint", NHalo = "halo";

    void ReadyLens()
    {
        SoftGlow = Settings.Get("graphics", "soft_glow", 1) != 0;
        Vignette = Settings.Get("graphics", "vignette", 1) != 0;
        if (ArgFloat("soft-glow") is { } g) SoftGlow = g > 0f;
        if (ArgFloat("vignette") is { } v) Vignette = v > 0f;

        for (int i = 0; i < glowLevels.Length; i++) glowLevels[i] = env.GetGlowLevel(i);

        // Under the HUD (layer 2) and above the world.
        lens = new CanvasLayer { Layer = 1, Name = "Lens" };
        AddChild(lens);
        vignetteMat = new ShaderMaterial { Shader = VignetteShader };
        leakMat = new ShaderMaterial { Shader = LeakShader };
        vignetteRect = FullScreen(vignetteMat);
        leakRect = FullScreen(leakMat);

        // A flat honey colour as the glow map tints the whole bloom warm.
        var img = Image.CreateEmpty(4, 4, false, Image.Format.Rgb8);
        img.Fill(new Color(1f, 0.86f, 0.70f));
        warmGlow = ImageTexture.CreateFromImage(img);
    }

    ColorRect FullScreen(ShaderMaterial mat)
    {
        var rect = new ColorRect { Material = mat, MouseFilter = Control.MouseFilterEnum.Ignore, Color = Colors.White };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        lens!.AddChild(rect);
        return rect;
    }

    void UpdateLens(float sunHeight, float daylight, float overcast, Vector3 sunDir)
    {
        if (lens == null) return;
        float clear = 1f - overcast;
        float golden = Mathf.SmoothStep(-0.04f, 0.06f, sunHeight) * (1f - Mathf.SmoothStep(0.15f, 0.35f, sunHeight));
        float dusk = golden * clear;
        float night = 1f - daylight;

        // Soft glow: the same bloom, spread over the wider levels and tinted warm.
        env.GlowMap = SoftGlow ? warmGlow : null;
        env.GlowMapStrength = SoftGlow ? 0.45f : 0f;
        for (int i = 0; i < glowLevels.Length; i++) env.SetGlowLevel(i, SoftGlow ? SoftLevels[i] : glowLevels[i]);
        sky.SetShaderParameter(NHalo, SoftGlow ? 1f : 0f);

        // Light leaks from the screen edge nearest the low sun, only when it's near
        // the view and the sky is clear.
        float leak = 0f;
        var dir = Vector2.Zero;
        if (SoftGlow && dusk > 0.001f && GetViewport()?.GetCamera3D() is { } cam)
        {
            var forward = -cam.GlobalBasis.Z;
            float facing = sunDir.Dot(forward);
            if (facing > -0.2f)
            {
                var side = new Vector2(sunDir.Dot(cam.GlobalBasis.X), -sunDir.Dot(cam.GlobalBasis.Y));
                dir = side.LengthSquared() > 1e-6f ? side.Normalized() : new Vector2(0f, -1f);
                leak = dusk * Mathf.SmoothStep(-0.2f, 0.6f, facing);
            }
        }
        leakRect!.Visible = leak > 0.002f;
        leakMat!.SetShaderParameter(NLeakDir, dir);
        leakMat.SetShaderParameter(NLeakAmount, leak);

        // Vignette: barely there by day, warm at dusk, cool at night.
        var tint = new Color(0.16f, 0.12f, 0.12f)
            .Lerp(new Color(0.34f, 0.18f, 0.12f), dusk)
            .Lerp(new Color(0.06f, 0.08f, 0.20f), night);
        float strength = 0.10f + 0.10f * dusk + 0.14f * night;
        vignetteRect!.Visible = Vignette;
        vignetteMat!.SetShaderParameter(NTint, tint);
        vignetteMat.SetShaderParameter(NLeakAmount, strength);

        // Rim tint, on top of the light colour, for Modeling's rim light.
        var rim = new Color(1f, 0.86f, 0.66f)
            .Lerp(new Color(1f, 0.80f, 0.72f), golden * clear)
            .Lerp(new Color(1f, 0.72f, 0.82f), golden * clear * (1f - Mathf.SmoothStep(0f, 0.08f, sunHeight)))
            .Lerp(new Color(0.78f, 0.86f, 1f), night);
        rim.A = (1f + 0.3f * golden * clear) * (1f - 0.3f * night) * (1f - 0.5f * overcast);
        RenderingServer.GlobalShaderParameterSet(NRimTint, rim);
    }

    static readonly Shader VignetteShader = new()
    {
        Code = """
            shader_type canvas_item;
            uniform vec3 tint : source_color;
            uniform float amount = 0.1;

            void fragment() {
                vec2 p = (UV - 0.5) * vec2(SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x, 1.0);
                float r = length(p) * 1.25;
                COLOR = vec4(tint, smoothstep(0.55, 1.25, r) * amount);
            }
            """,
    };

    // Added on top: three soft pastel blobs drifting slowly along the sun's edge.
    static readonly Shader LeakShader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode blend_add;
            uniform vec2 leak_dir = vec2(0.0, -1.0);
            uniform float amount = 0.0;

            void fragment() {
                vec2 aspect = vec2(SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x, 1.0);
                vec2 p = (UV - 0.5) * aspect;
                vec2 edge = leak_dir * 0.62 * aspect;
                vec2 along = vec2(-leak_dir.y, leak_dir.x);
                float drift = sin(TIME * 0.05) * 0.06;
                vec3 col = vec3(0.0);
                col += vec3(1.0, 0.72, 0.52) * exp(-pow(length(p - edge - along * (0.18 + drift)) / 0.34, 2.0));
                col += vec3(1.0, 0.62, 0.70) * exp(-pow(length(p - edge + along * (0.12 - drift)) / 0.28, 2.0));
                col += vec3(0.78, 0.66, 1.0) * exp(-pow(length(p - edge * 1.15 + along * 0.42) / 0.22, 2.0)) * 0.6;
                COLOR = vec4(col * amount * 0.12, 1.0);
            }
            """,
    };
}
