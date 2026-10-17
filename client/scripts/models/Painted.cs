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

    // The see-through copy of a painted twin on a building (StructureView's cutaway, D-168):
    // Look.StructureGhost's render mode, Painted's look, so a fading wall stays painted.
    // Null for anything that isn't a painted twin.
    public static ShaderMaterial? StructureGhostOf(ShaderMaterial material) =>
        material.Shader == Shader ? Look.Ghost(material, StructureGhostShader) : null;

    // Whether a material is already one of Painted's twins.
    public static bool IsPainted(Material? material) => material is ShaderMaterial m && m.Shader == Shader;

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

    static Shader Shader => shader ??= Look.MakeShader(Code(ghost: null));
    static Shader GhostShader => ghostShader ??= Look.MakeShader(Code(ghost: "render_mode cull_back, depth_draw_always;"));
    static Shader StructureGhostShader => structureGhostShader ??= Look.MakeShader(Code(ghost: "render_mode cull_disabled, depth_prepass_alpha;"));
    static Shader? shader, ghostShader, structureGhostShader;

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

    // ghost: the render mode of a see-through variant, or null for the solid.
    static string Code(string? ghost)
    {
        string code = Look.PaintedCode;
        code = Splice(code, "shader_type spatial;", "shader_type spatial;\n#define PAINTED_PROP" + (ghost != null ? "\n#define GHOST" : ""));
        if (ghost != null) code = Splice(code, "render_mode cull_disabled;", ghost);
        code = Splice(code, "varying float season_kind;", "varying float season_kind;\n" + Declarations + "\n" + RimUniform);
        code = Splice(code, "vec3 color = tint * (1.0 + paint * 2.0);", $$"""
            dab = dab_at(world_pos);
                // Clean colour (D-319): the brush only drifts the colour in broad, soft
                // patches, the faces that look up are a touch lighter in a wide wash, and the
                // whole shape shades softly from its top to its underside.
                float lift = smoothstep(0.35, 0.8, dab) * smoothstep(0.2, 0.9, world_normal.y);
                vec3 color = mix(tint, base_paint * dot(tint, vec3(0.3, 0.59, 0.11)) / 0.45, base_mix) * (1.0 + paint * {{F(Smooth.Paint)}}) * (1.0 + lit_dabs * {{F(Smooth.LitDabs)}} * lift);
                color *= mix(1.0 - {{F(Smooth.TopGradient)}}, 1.0 + {{F(Smooth.TopGradient)}}, smoothstep(-0.6, 0.9, world_normal.y));
            """);
        code = Splice(code, "color *= mix(1.0 - cavity, 1.0 + cavity * 0.35, smoothstep(0.15, 0.75, h));", $$"""
            // Only the deepest hollows darken, gently and broadly: no speckle.
                color *= mix(1.0 - cavity * {{F(Smooth.Crevice)}}, 1.0, smoothstep(crevice - 0.25, crevice + 0.25, h));
            """);
        code = Splice(code, "ALBEDO = clamp(color, 0.0, 1.0);", $$"""
            // A soft, faint darkening round each shape, like a light ink line.
                float rim = 1.0 - clamp(abs(dot(NORMAL, VIEW)), 0.0, 1.0);
                color *= mix(1.0, 0.7, ink * {{F(Smooth.Ink)}} * smoothstep(0.6, 0.95, rim));
                ALBEDO = clamp(color, 0.0, 1.0);
            """);
        // No fine relief: the small bumps bent into the normals and the noise in the AO are
        // what read as pitting.
        code = Splice(code, "NORMAL = normalize(abs(det) * NORMAL - grad * bump * 0.035);", $"NORMAL = normalize(abs(det) * NORMAL - grad * bump * 0.035 * {F(Smooth.Relief)});");
        code = Splice(code, "AO = mix(1.0, smoothstep(0.0, 0.6, h), cavity * 0.6);", $"AO = mix(1.0, smoothstep(0.0, 0.6, h), cavity * 0.6 * {F(Smooth.Relief)});");
        code = Splice(code, "void light() {", "void light() {\n#ifdef PAINTED_PROP\n" + Light + "\n" + Rim + "\n#else\n");
        code = code.TrimEnd();
        if (!code.EndsWith("}")) throw new System.InvalidOperationException("Painted: Look's shader no longer ends with light()");
        return code[..^1] + "#endif\n}\n";
    }

    // The painted uniforms and dab_at(); spliced after Look's varyings. Environment's ground uses it too.
    // D-319, Justin: "the weird pitted look is not cutesy". Every painted family (rocks,
    // bushes, logs, animals, structures, items) is smooth and clean: broad soft colour
    // drift instead of small dabs, no fine relief or crevice speckle, softer light bands,
    // a faint ink edge. These are the one set to tune; 0 relief means no bump at all.
    internal static class Smooth
    {
        public const float Relief = 0f;       // fine bump and AO noise kept (was 1)
        public const float Paint = 0.15f;     // fine brush noise in the colour (was 0.6)
        public const float LitDabs = 0.6f;    // the lighter wash on top faces, times each family's lit_dabs
        public const float DabScale = 4f;     // brush patches this many times larger (was 1)
        public const float Crevice = 0.25f;   // hollow darkening, times cavity (was 1.4, sharp)
        public const float Ink = 0.5f;        // edge darkening, times each family's ink (was 1)
        public const float BandSoft = 0.1f;   // light-band edge width (was 0.03)
        public const float BandBreak = 0.04f; // how much the brush breaks the bands (was 0.16)
        public const float TopGradient = 0.06f; // soft top-to-underside shading
    }

    static string F(float v) => v.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture);

    internal static readonly string Declarations = $$"""
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
        // The smooth look's constants (Painted.Smooth), shared with anything splicing Light.
        const float DAB_SCALE = {{F(Smooth.DabScale)}};
        const float BAND_SOFT = {{F(Smooth.BandSoft)}};
        const float BAND_BREAK = {{F(Smooth.BandBreak)}};

        float dab_at(vec3 p) {
            vec2 a = vec2(p.x + p.y * 0.3, p.z - p.y * 0.2) / (dab_size * DAB_SCALE);
            vec2 b = vec2(p.z * 0.9, p.y * 1.2) / (dab_size * DAB_SCALE) + 0.37;
            return mix(texture(dabs, a).r, texture(dabs, b).r, 0.45);
        }
        """;

    // The soft rim light (D-313, Justin's pick): a thin warm glow round the edges of a shape
    // that face away from the sun, strongest when it is backlit, as the cards outline their
    // figures against the light. Characters (ChibiLook), animals, props, buildings and items
    // all add it; trees and the ground do not. Its colour is the sun's times Atmosphere's
    // global rim_tint (cream by day, peach at golden hour, rose at dusk, cool blue at night;
    // its alpha scales the strength, #612). RimStrength is the one constant to raise.
    // Shaders that splice Rim declare RimUniform once at the top level.
    internal const float RimStrength = 0.5f;
    internal const string RimUniform = "global uniform vec4 rim_tint;";
    internal static readonly string Rim = $$"""
            if (LIGHT_IS_DIRECTIONAL) {
                float rim_edge = smoothstep(0.5, 0.9, 1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0));
                float rim_back = smoothstep(-0.2, 0.8, dot(-VIEW, LIGHT));
                float rim_away = 1.0 - smoothstep(0.0, 0.5, dot(NORMAL, LIGHT));
                DIFFUSE_LIGHT += LIGHT_COLOR * rim_tint.rgb * rim_edge * mix(0.5, 1.0, rim_back) * rim_away * rim_tint.a * {{RimStrength.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture)}} / PI;
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
            float l = wrapped * ATTENUATION * sky + (dab - 0.5) * BAND_BREAK;
            float soft = BAND_SOFT;
            float mid = smoothstep(0.32 - soft, 0.32 + soft, l);
            float lit = smoothstep(0.64 - soft, 0.64 + soft, l);
            // At low sun the shade lifts (it wraps further), but the lit side does not: the
            // golden light is bright enough, and lifting both washed sunlit faces out to a
            // pale peach at dusk (painted-11).
            float band = 0.12 + 0.2 * low_sun * (1.0 - mid) + 0.4 * mid * (1.0 - 0.2 * low_sun) + 0.36 * lit * (1.0 - 0.5 * low_sun);
            vec3 tone = mix(shade_tint, vec3(1.0), mid);
            float reach = LIGHT_IS_DIRECTIONAL ? 1.0 : smoothstep(0.0, 0.25, ATTENUATION);
            DIFFUSE_LIGHT += LIGHT_COLOR * tone * band * reach / PI;
        """;
}
