using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Prototype material looks for Justin's art-direction pick (D-154): small
// hand-painted textures per material instead of the fine noisy relief. Showcase
// only until he picks; nothing in game uses this yet.
//   A  32 px painted patterns in a few flat tones, pixel-sharp (Valheim-like).
//   B  the same patterns at 64 px, smoothly filtered (storybook).
//   C  B plus a slight tint shift per placed object (variety, D-166).
// Colours still come from the vertex colours and albedo, so palettes stay as they are.
public static class MaterialLook
{
    public enum Variant { Current, A, B, C }
    public enum Pattern { Plain, Wood, Bark, Stone, Leaves, Straw, Wool, Skin }

    static Shader? shader;
    static readonly Dictionary<(Pattern, int, bool), ImageTexture> textures = new();
    static readonly Dictionary<(Pattern, Variant, Color), ShaderMaterial> materials = new();

    // How many times a pattern repeats per metre, how it is stretched along u and v,
    // and how strongly it shows.
    static (float Scale, Vector2 Stretch, float Strength) Layout(Pattern p) => p switch
    {
        Pattern.Wood => (1.4f, new Vector2(0.3f, 1f), 1f),
        Pattern.Bark => (1.6f, new Vector2(1f, 0.35f), 1f),
        Pattern.Stone => (0.8f, Vector2.One, 1f),
        Pattern.Leaves => (0.6f, Vector2.One, 1.2f),
        Pattern.Straw => (0.8f, new Vector2(1f, 1f), 1f),
        Pattern.Wool => (3.5f, Vector2.One, 0.8f),
        Pattern.Skin => (1.5f, Vector2.One, 0.25f),
        _ => (0.6f, Vector2.One, 0.45f),
    };

    public static ShaderMaterial For(Pattern pattern, Variant variant, Color? albedo = null)
    {
        var color = albedo ?? Colors.White;
        if (materials.TryGetValue((pattern, variant, color), out var done)) return done;
        shader ??= new Shader { Code = Code };
        var (scale, stretch, strength) = Layout(pattern);
        bool sharp = variant == Variant.A;
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("albedo", color);
        material.SetShaderParameter(sharp ? "tex_sharp" : "tex_soft", Texture(pattern, sharp ? 32 : 64, sharp));
        material.SetShaderParameter("sharp", sharp);
        material.SetShaderParameter("scale", scale);
        material.SetShaderParameter("stretch", stretch);
        material.SetShaderParameter("strength", strength);
        material.SetShaderParameter("vary", variant == Variant.C ? 0.18f : 0f);
        materials[(pattern, variant, color)] = material;
        return material;
    }

    // Which painted pattern stands in for one of today's materials, read from the
    // relief settings each surface family uses in Look and StructureModel.
    public static Pattern Classify(Material? material, out Color albedo)
    {
        albedo = Colors.White;
        if (material is not ShaderMaterial m) return Pattern.Plain;
        if (m.GetShaderParameter("albedo").VariantType == Godot.Variant.Type.Color) albedo = m.GetShaderParameter("albedo").AsColor();
        float scale = m.GetShaderParameter("detail_scale").VariantType == Godot.Variant.Type.Float ? (float)m.GetShaderParameter("detail_scale") : 1f;
        var stretch = m.GetShaderParameter("detail_stretch").VariantType == Godot.Variant.Type.Vector3 ? m.GetShaderParameter("detail_stretch").AsVector3() : Vector3.One;
        float bump = m.GetShaderParameter("bump").VariantType == Godot.Variant.Type.Float ? (float)m.GetShaderParameter("bump") : 0f;
        if (stretch.Y < 0.9f) return Pattern.Wood;
        if (Mathf.IsEqualApprox(scale, 1.6f)) return albedo.R > albedo.G * 1.08f ? Pattern.Straw : Pattern.Leaves;
        if (Mathf.IsEqualApprox(scale, 1f) && bump > 1f) return Pattern.Stone;
        return Pattern.Plain;
    }

    // --- Painted textures: a grey value per texel (0 dark, 1 light), tiling.

    static ImageTexture Texture(Pattern p, int n, bool tones)
    {
        if (textures.TryGetValue((p, n, tones), out var done)) return done;
        var image = Image.CreateEmpty(n, n, true, Image.Format.L8);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float v = Mathf.Clamp(Value(p, (x + 0.5f) / n, (y + 0.5f) / n), 0f, 1f);
                // A few flat tones, like paint laid down in strokes.
                if (tones) v = Mathf.Round(v * 3f) / 3f;
                image.SetPixel(x, y, new Color(v, v, v));
            }
        image.GenerateMipmaps();
        return textures[(p, n, tones)] = ImageTexture.CreateFromImage(image);
    }

    // Value noise on a lattice that wraps every `period` cells, so textures tile.
    static float Noise(float x, float y, int period, int seed)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        float L(int i, int j) => MeshKit.Hash(((i % period) + period) % period + seed * 131, ((j % period) + period) % period);
        float a = Mathf.Lerp(L(x0, y0), L(x0 + 1, y0), fx), b = Mathf.Lerp(L(x0, y0 + 1), L(x0 + 1, y0 + 1), fx);
        return Mathf.Lerp(a, b, fy);
    }

    static float Value(Pattern p, float u, float v)
    {
        switch (p)
        {
            case Pattern.Wood:
            {
                // Grain lines running along u, gently wavering.
                float wave = Noise(u * 3, v * 4, 3, 1) * 0.9f + Noise(u * 6, v * 8, 6, 2) * 0.3f;
                float f = Mathf.PosMod(v * 4f + wave, 1f);
                float line = Mathf.SmoothStep(0.0f, 0.12f, f) * (1f - Mathf.SmoothStep(0.85f, 1f, f));
                return 0.45f + 0.4f * line + 0.15f * Noise(u * 2, v * 2, 2, 3);
            }
            case Pattern.Bark:
            {
                // Plates between deep vertical furrows.
                float wobble = Noise(u * 5, v * 2, 5, 4) * 0.5f;
                float f = Mathf.PosMod(u * 5f + wobble, 1f);
                float plate = Mathf.SmoothStep(0.05f, 0.3f, f) * (1f - Mathf.SmoothStep(0.75f, 0.95f, f));
                float breaks = Mathf.SmoothStep(0.25f, 0.4f, Noise(u * 5, v * 3, 5, 5));
                return 0.2f + 0.65f * plate * breaks + 0.15f * Noise(u * 10, v * 6, 10, 6);
            }
            case Pattern.Stone:
            {
                // Soft blotches with a couple of dark cracks between cells.
                const int cells = 3;
                float best = 9f, second = 9f, shade = 0.5f;
                for (int i = -1; i <= cells; i++)
                    for (int j = -1; j <= cells; j++)
                    {
                        int wi = ((i % cells) + cells) % cells, wj = ((j % cells) + cells) % cells;
                        var c = new Vector2((i + MeshKit.Hash(wi, wj + 40)) / cells, (j + MeshKit.Hash(wi + 7, wj + 41)) / cells);
                        float d = c.DistanceTo(new Vector2(u, v));
                        if (d < best) { second = best; best = d; shade = MeshKit.Hash(wi + 3, wj + 42); }
                        else if (d < second) second = d;
                    }
                float crack = Mathf.SmoothStep(0.0f, 0.035f, second - best);
                return (0.5f + 0.3f * shade + 0.2f * Noise(u * 4, v * 4, 4, 7)) * Mathf.Lerp(0.45f, 1f, crack);
            }
            case Pattern.Leaves:
            {
                // Overlapping leaf clusters, lit from above, dark gaps between.
                float best = 0f;
                for (int k = 0; k < 7; k++)
                {
                    var c = new Vector2(MeshKit.Hash(k, 50), MeshKit.Hash(k, 51));
                    for (int ox = -1; ox <= 1; ox++)
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            var d = new Vector2(u - c.X - ox, v - c.Y - oy);
                            float r = 0.22f + MeshKit.Hash(k, 52) * 0.1f;
                            float inside = 1f - d.Length() / r;
                            if (inside > 0f) best = Mathf.Max(best, 0.45f + 0.55f * Mathf.Clamp(inside * 1.6f - d.Y * 1.5f, 0f, 1f));
                        }
                }
                return best > 0f ? best : 0.2f;
            }
            case Pattern.Straw:
            {
                // Fine straws along v, each its own shade, in bundles with ragged ends.
                float column = Mathf.Floor(u * 32f);
                float edge = Mathf.PosMod(u * 32f, 1f);
                float bundle = Mathf.PosMod(v * 2f + MeshKit.Hash((int)Mathf.Floor(u * 8f), 61) * 0.6f, 1f);
                float ends = Mathf.SmoothStep(0f, 0.08f, bundle);
                return (0.5f + 0.4f * MeshKit.Hash((int)column, 60)) * Mathf.Lerp(0.65f, 1f, Mathf.SmoothStep(0f, 0.3f, edge)) * Mathf.Lerp(0.55f, 1f, ends);
            }
            case Pattern.Wool:
            {
                // Little curls: rings round scattered centres.
                float best = 9f;
                for (int k = 0; k < 6; k++)
                {
                    var c = new Vector2(MeshKit.Hash(k, 70), MeshKit.Hash(k, 71));
                    for (int ox = -1; ox <= 1; ox++)
                        for (int oy = -1; oy <= 1; oy++)
                            best = Mathf.Min(best, new Vector2(u - c.X - ox, v - c.Y - oy).Length());
                }
                return 0.55f + 0.45f * Mathf.Cos(best * 40f) * 0.5f + 0.2f;
            }
            case Pattern.Skin:
                return 0.6f + 0.25f * Noise(u * 2, v * 2, 2, 9);
            default:
                return 0.5f + 0.4f * Noise(u * 2, v * 2, 2, 10);
        }
    }

    const string Code = """
        shader_type spatial;
        render_mode cull_disabled;

        uniform vec4 albedo : source_color = vec4(1.0);
        uniform sampler2D tex_sharp : repeat_enable, filter_nearest_mipmap;
        uniform sampler2D tex_soft : repeat_enable, filter_linear_mipmap;
        uniform bool sharp = true;
        uniform float scale = 1.0;
        uniform vec2 stretch = vec2(1.0);
        uniform float strength = 1.0;
        uniform float vary = 0.0;

        varying vec3 tint;
        varying vec3 obj;
        varying vec3 obj_normal;

        void vertex() {
            // Palette colours are authored in sRGB.
            tint = pow(COLOR.rgb, vec3(2.2)) * albedo.rgb;
            obj = VERTEX;
            obj_normal = NORMAL;
            if (vary > 0.0) {
                // Each placed object its own slight shade and warmth.
                vec3 o = MODEL_MATRIX[3].xyz;
                float h = fract(sin(dot(floor(o.xz * 2.0 + 0.5), vec2(12.9898, 78.233))) * 43758.5453);
                float h2 = fract(h * 7.13);
                tint *= 1.0 + (h - 0.5) * vary;
                tint.r *= 1.0 + (h2 - 0.5) * vary * 0.6;
                tint.b *= 1.0 - (h2 - 0.5) * vary * 0.6;
            }
        }

        float paint(vec2 uv) {
            uv *= scale * stretch;
            return sharp ? texture(tex_sharp, uv).r : texture(tex_soft, uv).r;
        }

        void fragment() {
            // Projected from three sides in the object's own space, so it sticks to
            // the object and needs no UVs.
            vec3 w = pow(abs(obj_normal), vec3(8.0));
            w /= (w.x + w.y + w.z);
            float t = paint(obj.zy) * w.x + paint(obj.xz) * w.y + paint(obj.xy) * w.z;
            ALBEDO = clamp(tint * mix(1.0, mix(0.55, 1.2, t), strength), 0.0, 1.0);
            ROUGHNESS = 0.92;
            SPECULAR = 0.2;
        }
        """;
}
