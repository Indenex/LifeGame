using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace LifeGame.Spike
{
    // Godot's tonemap and colour adjustments, baked into a URP user LUT so the Unity
    // picture goes through the same curve as the Godot one (Environment.tonemap_*,
    // adjustment_brightness/contrast/saturation and the 1D colour-correction gradient
    // the atmosphere grade sets, D-257).
    //
    // URP applies a user LUT after its own tonemapping, in sRGB. With URP's
    // tonemapping off and postExposure at -3 EV, the LUT sees sRGB(linear / 8): it
    // undoes the /8, then runs Godot's chain (exposure, tonemap, linear->sRGB, BCS,
    // gradient) and returns sRGB, exactly where Godot applies them.
    public static class GodotGrade
    {
        public const float Headroom = 8f;   // scene linear up to 8 survives into the LUT
        public static float HeadroomEv => -Mathf.Log(Headroom, 2f);

        public static Texture2D BuildLut(int size, object env, Texture2D gradient)
        {
            int mode = env.I("tonemap_mode", 3);
            float exposure = env.F("tonemap_exposure", 1f);
            float white = env.F("tonemap_white", 1f);
            bool adjust = env.Get("adjustment_enabled") is true;
            float brightness = env.F("adjustment_brightness", 1f);
            float contrast = env.F("adjustment_contrast", 1f);
            float saturation = env.F("adjustment_saturation", 1f);
            Color[] grad = null;
            if (adjust && gradient != null) grad = gradient.GetPixels(0);

            var tex = new Texture2D(size * size, size, TextureFormat.RGBAHalf, false, true)
            {
                name = "godot_grade_lut",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color[size * size * size];
            float step = 1f / (size - 1);
            for (int b = 0; b < size; b++)
                for (int g = 0; g < size; g++)
                    for (int r = 0; r < size; r++)
                    {
                        var c = new Vector3(Decode(r * step), Decode(g * step), Decode(b * step)) * Headroom;
                        c *= exposure;
                        c = Tonemap(c, mode, white);
                        if (adjust)
                        {
                            // tonemap.glsl: brightness on linear, then sRGB, contrast and
                            // saturation on the sRGB values, clamp, then the gradient.
                            c *= brightness;
                            c = new Vector3(Encode(Mathf.Max(c.x, 0f)), Encode(Mathf.Max(c.y, 0f)), Encode(Mathf.Max(c.z, 0f)));
                            c = Vector3.LerpUnclamped(new Vector3(0.5f, 0.5f, 0.5f), c, contrast);
                            float grey = (c.x + c.y + c.z) * (1f / 3f);
                            c = Vector3.LerpUnclamped(new Vector3(grey, grey, grey), c, saturation);
                            c = new Vector3(Clamp01(c.x), Clamp01(c.y), Clamp01(c.z));
                            if (grad != null)
                                c = new Vector3(Sample(grad, c.x).r, Sample(grad, c.y).g, Sample(grad, c.z).b);
                        }
                        else
                        {
                            c = new Vector3(Encode(Mathf.Max(c.x, 0f)), Encode(Mathf.Max(c.y, 0f)), Encode(Mathf.Max(c.z, 0f)));
                        }
                        c = new Vector3(Clamp01(c.x), Clamp01(c.y), Clamp01(c.z));
                        pixels[g * size * size + b * size + r] = new Color(c.x, c.y, c.z, 1f);
                    }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;
        static float Decode(float c) => c < 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        static float Encode(float c) => c < 0.0031308f ? 12.92f * c : 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;

        // A 1D gradient texture read with linear filtering, as Godot's shader does.
        static Color Sample(Color[] row, float u)
        {
            float x = Clamp01(u) * row.Length - 0.5f;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, row.Length - 1);
            int i1 = Mathf.Clamp(i0 + 1, 0, row.Length - 1);
            return Color.LerpUnclamped(row[i0], row[i1], Clamp01(x - Mathf.Floor(x)));
        }

        // Godot 4.6's tonemappers (servers/rendering/renderer_rd/shaders/effects/tonemap.glsl,
        // white from servers/rendering/storage/environment_storage.cpp).
        static Vector3 Tonemap(Vector3 c, int mode, float white)
        {
            switch (mode)
            {
                case 1: // Reinhard
                    return new Vector3(Reinhard(c.x, white), Reinhard(c.y, white), Reinhard(c.z, white));
                case 2: // Filmic
                {
                    float w = Filmic(white);
                    return new Vector3(Filmic(c.x) / w, Filmic(c.y) / w, Filmic(c.z) / w);
                }
                case 3: // ACES (fitted, MJP's BakingLab with Godot's exposure bias and white)
                    return Aces(c, white);
                default: // Linear (and AgX, which this spike doesn't port)
                    return c;
            }
        }

        // c * (1 + c / white^2) / (1 + c)
        static float Reinhard(float c, float white)
        {
            float ws = white * white;
            return (ws * c + c * c) / (ws * c + ws);
        }

        static float Filmic(float c)
        {
            const float bias = 2f, A = 0.22f, B = 0.30f, C = 0.10f, D = 0.20f, E = 0.01f, F = 0.30f;
            float x = c * bias;
            return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
        }

        static Vector3 Aces(Vector3 c, float white)
        {
            const float bias = 1.8f, A = 0.0245786f, B = 0.000090537f, C = 0.983729f, D = 0.432951f, E = 0.238081f;
            var rrt = new Vector3(
                (0.59719f * c.x + 0.35458f * c.y + 0.04823f * c.z) * bias,
                (0.07600f * c.x + 0.90834f * c.y + 0.01566f * c.z) * bias,
                (0.02840f * c.x + 0.13383f * c.y + 0.83777f * c.z) * bias);
            Vector3 fit = new(
                (rrt.x * (rrt.x + A) - B) / (rrt.x * (C * rrt.x + D) + E),
                (rrt.y * (rrt.y + A) - B) / (rrt.y * (C * rrt.y + D) + E),
                (rrt.z * (rrt.z + A) - B) / (rrt.z * (C * rrt.z + D) + E));
            var odt = new Vector3(
                1.60475f * fit.x - 0.53108f * fit.y - 0.07367f * fit.z,
                -0.10208f * fit.x + 1.10813f * fit.y - 0.00605f * fit.z,
                -0.00327f * fit.x - 0.07276f * fit.y + 1.07602f * fit.z);
            float w = white * bias;
            float wt = (w * (w + A) - B) / (w * (C * w + D) + E);
            return odt / wt;
        }
    }
}
