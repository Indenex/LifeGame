using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The painted look for everything that isn't a tree (Justin: "the new painted looks for
// everything", after the Golden Skull art and Dofus): rocks, bushes, props, animals,
// buildings and items. Painted.Of(material) gives a painted twin of any of Look's
// materials, with the same colour, relief and settings, so a family switches over by
// swapping its material.
//
// The shader is Look's painted shader with PAINTED_PROP defined (as PaintedTrees does
// for the trees), so seasons, snow, wetness, falling, the fog rim and the ghost all still
// apply. On top: colour laid in flat strokes with a few lighter dabs on the faces that
// look up, crevices in one darker tone instead of a smooth gradient, a soft ink edge
// round each shape, and light in stepped bands with violet shade (Atmosphere's rules:
// LIGHT_COLOR and ambient stay smooth).
public static class Painted
{
    static readonly Dictionary<Material, ShaderMaterial> Twins = new();
    static readonly Dictionary<Material, ShaderMaterial> Ghosts = new();

    // The painted twin of one of Look's materials (or of a copy of one, such as a tinted
    // structure material). Twins are cached, so the same source always gives the same twin.
    public static ShaderMaterial Of(ShaderMaterial source)
    {
        lock (Twins)
        {
            if (Twins.TryGetValue(source, out var twin)) return twin;
            twin = new ShaderMaterial { Shader = Shader };
            foreach (var uniform in source.Shader.GetShaderUniformList())
            {
                var name = new StringName(uniform.AsGodotDictionary()["name"].AsString());
                twin.SetShaderParameter(name, source.GetShaderParameter(name));
            }
            twin.SetShaderParameter("dabs", Dabs);
            // The painting carries the form now; the relief only marks the crevices.
            twin.SetShaderParameter("bump", 0.35f * (float)source.GetShaderParameter("bump"));
            Twins[source] = twin;
            return twin;
        }
    }

    // The see-through copy of a painted twin whose source fades out of the camera's way.
    public static ShaderMaterial? GhostOf(Material material)
    {
        if (material is not ShaderMaterial twin || twin.Shader != Shader) return null;
        if (!(bool)twin.GetShaderParameter("see_through") && (float)twin.GetShaderParameter("fade_override") < 0f) return null;
        lock (Ghosts)
        {
            if (!Ghosts.TryGetValue(material, out var ghost)) Ghosts[material] = ghost = Look.Ghost(twin, GhostShader);
            return ghost;
        }
    }

    // Look's rock, painted: boulders, outcrops, river stones and pebbles.
    public static ShaderMaterial Rock => rock ??= MakeRock();
    static ShaderMaterial? rock;

    // The cards' stone (Forest_big_stone): pale grey planes where the light falls, deep
    // blue-violet in the shade, crevices only where the relief is deepest.
    static ShaderMaterial MakeRock()
    {
        var twin = (ShaderMaterial)Of(Look.RockSurface).Duplicate();
        twin.SetShaderParameter("base_paint", new Color(0.6f, 0.6f, 0.65f));
        twin.SetShaderParameter("base_mix", 0.5f);
        twin.SetShaderParameter("shade_tint", new Color(0.56f, 0.53f, 0.8f));
        twin.SetShaderParameter("crevice", 0.22f);
        twin.SetShaderParameter("lit_dabs", 0.08f);
        return twin;
    }

    // Look's foliage, painted, for low plants with their own colours in the vertices
    // (heather, gorse): sway and the heather's season code stay as they were.
    public static ShaderMaterial Foliage => Of(Look.Foliage);

    // The bushes' leaves (painted-2): Look's foliage with the shade leaning further
    // violet, more lit dabs on the tops and only the deepest relief as dark gaps.
    public static ShaderMaterial Bush => bush ??= MakeBush();
    static ShaderMaterial? bush;

    static ShaderMaterial MakeBush()
    {
        var twin = (ShaderMaterial)Of(Look.Foliage).Duplicate();
        twin.SetShaderParameter("shade_tint", new Color(0.64f, 0.6f, 0.88f));
        twin.SetShaderParameter("lit_dabs", 0.22f);
        twin.SetShaderParameter("crevice", 0.28f);
        twin.SetShaderParameter("dab_size", 0.9f);
        return twin;
    }

    // Look's tree bark and foliage, painted: fallen logs, birches, landmark trees and the
    // canopies not yet replaced by PaintedTrees. Both fade out of the camera's way, so both
    // have ghosts. The leaves lean toward the painted broadleaf's grey-green.
    public static ShaderMaterial Bark => bark ??= MakeBark();
    public static ShaderMaterial Leaves => leaves ??= MakeLeaves();
    static ShaderMaterial? bark, leaves;

    static ShaderMaterial MakeBark()
    {
        var twin = (ShaderMaterial)Of(Look.TreeBark).Duplicate();
        twin.SetShaderParameter("shade_tint", new Color(0.66f, 0.6f, 0.82f));
        twin.SetShaderParameter("crevice", 0.3f);
        return twin;
    }

    static ShaderMaterial MakeLeaves()
    {
        var twin = (ShaderMaterial)Of(Look.TreeFoliage).Duplicate();
        twin.SetShaderParameter("base_paint", new Color(0.3f, 0.38f, 0.3f));
        twin.SetShaderParameter("base_mix", 0.45f);
        twin.SetShaderParameter("crevice", 0.3f);
        twin.SetShaderParameter("lit_dabs", 0.2f);
        return twin;
    }

    static Shader Shader => shader ??= Look.MakeShader(Code(ghost: false));
    static Shader GhostShader => ghostShader ??= Look.MakeShader(Code(ghost: true));
    static Shader? shader, ghostShader;

    static NoiseTexture2D Dabs => dabs ??= new NoiseTexture2D
    {
        Width = 256, Height = 256, Seamless = true, GenerateMipmaps = true,
        Noise = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 0.03f, FractalOctaves = 3, FractalGain = 0.45f, Seed = 11 },
    };
    static NoiseTexture2D? dabs;

    // Replaces the one place anchor appears in a shader's code; throws if it appears
    // anywhere else or not at all, so a change to Look's shader fails at startup, not silently.
    internal static string Splice(string text, string anchor, string replacement)
    {
        int at = text.IndexOf(anchor, System.StringComparison.Ordinal);
        if (at < 0 || text.IndexOf(anchor, at + anchor.Length, System.StringComparison.Ordinal) >= 0)
            throw new System.InvalidOperationException($"Painted: Look's shader changed near '{anchor}'");
        return text[..at] + replacement + text[(at + anchor.Length)..];
    }

    static string Code(bool ghost)
    {
        string code = Look.PaintedCode;
        code = Splice(code, "shader_type spatial;", "shader_type spatial;\n#define PAINTED_PROP" + (ghost ? "\n#define GHOST" : ""));
        if (ghost) code = Splice(code, "render_mode cull_disabled;", "render_mode cull_back, depth_draw_always;");
        code = Splice(code, "varying float season_kind;", "varying float season_kind;\n" + Declarations);
        code = Splice(code, "vec3 color = tint * (1.0 + paint * 2.0);", """
            dab = dab_at(world_pos);
                // Flat strokes: the brush only nudges the colour, and the faces that look up
                // catch a few lighter dabs, as a painter lights from above.
                float lift = smoothstep(0.56, 0.6, dab) * smoothstep(0.3, 0.7, world_normal.y);
                vec3 color = mix(tint, base_paint * dot(tint, vec3(0.3, 0.59, 0.11)) / 0.45, base_mix) * (1.0 + paint * 0.6) * (1.0 + lit_dabs * lift);
            """);
        code = Splice(code, "color *= mix(1.0 - cavity, 1.0 + cavity * 0.35, smoothstep(0.15, 0.75, h));", """
            // Crevices in one darker tone, their edge broken by the brush.
                color *= mix(1.0 - cavity * 1.4, 1.0 + cavity * 0.2, smoothstep(crevice - 0.03, crevice + 0.03, h + (dab - 0.5) * 0.12));
            """);
        code = Splice(code, "ALBEDO = clamp(color, 0.0, 1.0);", """
            // A soft dark line round each shape, like the ink round the card art.
                float rim = 1.0 - clamp(abs(dot(NORMAL, VIEW)), 0.0, 1.0);
                color *= mix(1.0, 0.62, ink * smoothstep(0.66, 0.9, rim));
                ALBEDO = clamp(color, 0.0, 1.0);
            """);
        code = Splice(code, "void light() {", "void light() {\n#ifdef PAINTED_PROP\n" + Light + "\n" + Rim + "\n#else\n");
        code = code.TrimEnd();
        if (!code.EndsWith("}")) throw new System.InvalidOperationException("Painted: Look's shader no longer ends with light()");
        return code[..^1] + "#endif\n}\n";
    }

    // The painted uniforms and dab_at(); spliced after Look's varyings. Environment's ground uses it too.
    internal const string Declarations = """
        uniform sampler2D dabs : filter_linear_mipmap, repeat_enable;
        uniform vec3 shade_tint : source_color = vec3(0.74, 0.72, 0.9);
        uniform float lit_dabs = 0.14;
        uniform float ink = 1.0;
        uniform float dab_size = 1.6;
        // A palette colour the surface leans toward (the cards' own stone grey, say), by base_mix.
        uniform vec3 base_paint : source_color = vec3(0.6, 0.6, 0.6);
        uniform float base_mix = 0.0;
        // Relief below this reads as a crevice, in one darker tone.
        uniform float crevice = 0.39;
        varying float dab;

        float dab_at(vec3 p) {
            vec2 a = vec2(p.x + p.y * 0.3, p.z - p.y * 0.2) / dab_size;
            vec2 b = vec2(p.z * 0.9, p.y * 1.2) / dab_size + 0.37;
            return mix(texture(dabs, a).r, texture(dabs, b).r, 0.45);
        }
        """;

    // The soft rim light (D-313, Justin's pick): a thin warm glow round the edges of a shape
    // that face away from the sun, strongest when it is backlit, as the cards outline their
    // figures against the light. Characters (ChibiLook), animals, props, buildings and items
    // all add it; trees and the ground do not. It takes the sun's own colour, so it warms
    // at dawn and dusk with Atmosphere's light. RimStrength is the one constant to raise.
    internal const float RimStrength = 0.5f;
    internal static readonly string Rim = $$"""
            if (LIGHT_IS_DIRECTIONAL) {
                float rim_edge = smoothstep(0.5, 0.9, 1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0));
                float rim_back = smoothstep(-0.2, 0.8, dot(-VIEW, LIGHT));
                float rim_away = 1.0 - smoothstep(0.0, 0.5, dot(NORMAL, LIGHT));
                DIFFUSE_LIGHT += LIGHT_COLOR * vec3(1.0, 0.86, 0.66) * rim_edge * mix(0.5, 1.0, rim_back) * rim_away * {{RimStrength.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture)}} / PI;
            }
        """;

    // Stepped light as on the painted trees: shade, mid and lit with brush-broken edges,
    // the shade leaning violet, a lift at low sun, fires and doorways fading with distance.
    internal const string Light = """
            float sky = LIGHT_IS_DIRECTIONAL ? shade : 1.0;
            float elevation = (INV_VIEW_MATRIX * vec4(LIGHT, 0.0)).y;
            float low_sun = LIGHT_IS_DIRECTIONAL ? 1.0 - smoothstep(0.2, 0.55, elevation) : 0.0;
            float wrap = mix(0.3, 0.6, low_sun);
            float wrapped = clamp(dot(NORMAL, LIGHT) * (1.0 - wrap) + wrap, 0.0, 1.0);
            float l = wrapped * ATTENUATION * sky + (dab - 0.5) * 0.16;
            float soft = 0.03;
            float mid = smoothstep(0.32 - soft, 0.32 + soft, l);
            float lit = smoothstep(0.64 - soft, 0.64 + soft, l);
            float band = 0.12 + 0.2 * low_sun + 0.4 * mid + 0.36 * lit;
            vec3 tone = mix(shade_tint, vec3(1.0), mid);
            float reach = LIGHT_IS_DIRECTIONAL ? 1.0 : smoothstep(0.0, 0.25, ATTENUATION);
            DIFFUSE_LIGHT += LIGHT_COLOR * tone * band * reach / PI;
        """;
}
