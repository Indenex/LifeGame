using System.Linq;
using Godot;

namespace LifeGame;

// Painted ground prototypes (env-27, Justin: "the painted look on everything"), two takes
// for his side-by-side pick (D-154), chosen with --ground= for renders; without it the
// ground is drawn as before.
//   --ground=painted  A: the ground in the painted trees' and props' language. Broad warm
//                     and cool colour patches, soft value bands with brush-broken edges,
//                     directional strokes, stepped light with violet shade (Painted.Light).
//   --ground=tiles    B+: the Golden Skull tile paintings (client/assets/environment/golden-skull),
//                     laid by ground type with anti-tiling: two grids, the second half a cell
//                     off, each cell turned and shifted at random and blended across the cell
//                     edges; two variants per type picked by the region; types blended by a
//                     brush-broken height; the ground's own colours laid over the tiles'
//                     detail, and the same stepped light on top.
// Both keep the grass blades' shape and number (D-256); the grass only takes the stepped light.
public static class PaintedTerrain
{
    public enum Style { Old, Painted, Tiles, Botw, Ghibli }

    public static readonly Style Chosen = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs())
        .FirstOrDefault(a => a.StartsWith("--ground="))?["--ground=".Length..] switch
    {
        "painted" => Style.Painted,
        "tiles" => Style.Tiles,
        "botw" => Style.Botw,
        "ghibli" => Style.Ghibli,
        _ => Style.Old,
    };

    // The terrain's material for the chosen style.
    public static ShaderMaterial Ground => ground ??= Chosen is Style.Old or Style.Botw ? Look.Ground : Make(Chosen == Style.Tiles);
    static ShaderMaterial? ground;

    // Switches the grass tufts onto the same stepped light, for the painted styles.
    public static void Prewarm()
    {
        _ = Ground;
        if (Chosen is Style.Painted or Style.Tiles or Style.Ghibli) Look.GrassTufts.SetShaderParameter("painted_light", 1f);
        // env-31 prototypes: the grass carries the look.
        if (Chosen == Style.Botw) Look.GrassTufts.SetShaderParameter("look", 1f);
        if (Chosen == Style.Ghibli) Look.GrassTufts.SetShaderParameter("look", 2f);
    }

    static ShaderMaterial Make(bool tiles)
    {
        var material = new ShaderMaterial { Shader = Look.MakeShader(Code(tiles)) };
        var source = Look.Ground;
        foreach (var uniform in source.Shader.GetShaderUniformList())
        {
            var name = new StringName(uniform.AsGodotDictionary()["name"].AsString());
            material.SetShaderParameter(name, source.GetShaderParameter(name));
        }
        material.SetShaderParameter("dabs", Dabs);
        material.SetShaderParameter("ink", 0f);
        material.SetShaderParameter("dab_size", 4.5f);
        material.SetShaderParameter("shade_tint", new Color(0.72f, 0.7f, 0.88f));
        material.SetShaderParameter("brush_strength", 0.07f);
        material.SetShaderParameter("bump", 0.4f);
        // Facing the sun, the ground would take the props' stepped light too bright; this
        // keeps it at the old ground's level, the same scale the grass uses.
        float level = Mathf.Pow(Look.PaintedGroundLevel, 1f / 2.2f);
        material.SetShaderParameter("albedo", new Color(level, level, level));
        if (tiles) material.SetShaderParameter("ground_tiles", Tiles());
        return material;
    }

    static NoiseTexture2D Dabs => dabs ??= new NoiseTexture2D
    {
        Width = 256, Height = 256, Seamless = true, GenerateMipmaps = true,
        Noise = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 0.03f, FractalOctaves = 3, FractalGain = 0.45f, Seed = 23 },
    };
    static NoiseTexture2D? dabs;

    // The eight tile paintings as layers (grass, dirt, sand, stone; two of each), so each
    // wraps on its own. Loaded from the file as is: prototype only.
    static Texture2DArray Tiles()
    {
        var atlas = Image.LoadFromFile(ProjectSettings.GlobalizePath("res://assets/environment/golden-skull/ground_atlas.png"));
        var layers = new Godot.Collections.Array<Image>();
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 2; col++)
            {
                var layer = atlas.GetRegion(new Rect2I(col * 128, row * 128, 128, 128));
                layer.GenerateMipmaps();
                layers.Add(layer);
            }
        var array = new Texture2DArray();
        array.CreateFromImages(layers);
        return array;
    }

    static string Code(bool tiles)
    {
        string code = Look.PaintedCode;
        code = Painted.Splice(code, "shader_type spatial;", "shader_type spatial;\n#define PAINTED_GROUND" + (tiles ? "\n#define TILE_GROUND" : ""));
        code = Painted.Splice(code, "varying float season_kind;", "varying float season_kind;\n" + Painted.Declarations + Declarations);
        code = Painted.Splice(code, "vec3 color = tint * (1.0 + paint * 2.0);", tiles ? TileColor : Chosen == Style.Ghibli ? SoftPaint : PaintColor);
        code = Painted.Splice(code, "void light() {", "void light() {\n#ifdef PAINTED_GROUND\n" + Painted.Light + "\n#else\n");
        code = code.TrimEnd();
        return code[..^1] + "#endif\n}\n";
    }

    const string Declarations = """
        uniform sampler2DArray ground_tiles : source_color, filter_linear_mipmap, repeat_enable;
        uniform float tile_size = 2.4;

        float ground_hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }

        // One ground type's tile painting at p (metres), without a visible repeat: two grids,
        // the second half a cell off, each cell turned and shifted at random, weighted to its
        // centre so the seams of one grid fall where the other is whole.
        vec3 tile_layer(float type, vec2 p, float variant) {
            vec3 sum = vec3(0.0);
            float total = 0.0;
            for (int k = 0; k < 2; k++) {
                vec2 q = p / tile_size + (k == 1 ? vec2(0.5) : vec2(0.0));
                vec2 cell = floor(q);
                vec2 f = fract(q) - 0.5;
                float turn = ground_hash(cell + float(k) * 17.0) * 6.2832;
                mat2 r = mat2(vec2(cos(turn), sin(turn)), vec2(-sin(turn), cos(turn)));
                vec2 uv = r * q + vec2(ground_hash(cell + 3.1), ground_hash(cell + 7.7));
                vec3 c = texture(ground_tiles, vec3(uv, type * 2.0 + variant)).rgb;
                float w = max(1.0 - smoothstep(0.15, 0.5, max(abs(f.x), abs(f.y))), 0.002);
                sum += c * w;
                total += w;
            }
            return sum / total;
        }
        """;

    // A: broad warm and cool patches, soft value bands with a broken edge, strokes turned
    // with the patches.
    const string PaintColor = """
        dab = dab_at(world_pos);
                float big = texture(dabs, world_pos.xz / 16.0).r;
                float band = texture(dabs, world_pos.xz / 5.0 + 0.37).r;
                vec3 base = mix(tint * vec3(0.8, 0.94, 1.1), tint * vec3(1.18, 1.06, 0.78), smoothstep(0.36, 0.64, big));
                base *= mix(0.84, 1.12, smoothstep(0.45, 0.55, band + (dab - 0.5) * 0.18));
                float turn = big * 3.1416;
                vec2 sp = mat2(vec2(cos(turn), sin(turn)), vec2(-sin(turn), cos(turn))) * world_pos.xz;
                float stroke = texture(dabs, sp * vec2(0.9, 0.22)).r;
                vec3 color = base * (1.0 + (stroke - 0.5) * 0.5 + paint * 0.4);
        """;

    // The Ghibli prototype's ground (env-31): A's patches, but soft wide blends and gentle
    // value changes, nothing blotchy.
    static string SoftPaint => PaintColor
        .Replace("smoothstep(0.45, 0.55, band + (dab - 0.5) * 0.18)", "smoothstep(0.2, 0.8, band + (dab - 0.5) * 0.1)")
        .Replace("mix(0.84, 1.12,", "mix(0.93, 1.06,")
        .Replace("(stroke - 0.5) * 0.5", "(stroke - 0.5) * 0.18");

    // B+: grass, dirt and sand tile paintings by how green, bright or brown the ground's
    // own colour is, blended by a brush-broken height; the ground's colour laid over the
    // tiles' detail (each tile's average, from its smallest mip, divided out).
    const string TileColor = """
        dab = dab_at(world_pos);
                float lum = dot(tint, vec3(0.3, 0.59, 0.11));
                float grass_w = smoothstep(-0.015, 0.025, tint.g - max(tint.r, tint.b));
                float sand_w = smoothstep(0.28, 0.42, lum) * (1.0 - grass_w);
                float dirt_w = max(1.0 - grass_w - sand_w, 0.0);
                vec3 wts = clamp(vec3(grass_w, dirt_w, sand_w) + (vec3(dab, 1.0 - dab, texture(dabs, world_pos.xz / 3.0).r) - 0.5) * 0.3, 0.0, 1.0);
                wts = pow(wts, vec3(3.0));
                wts /= max(wts.x + wts.y + wts.z, 1e-4);
                float variant = step(0.5, texture(dabs, world_pos.xz / 40.0).r);
                vec3 tiled = vec3(0.0);
                vec3 avg = vec3(0.0);
                for (int t = 0; t < 3; t++) {
                    float w = t == 0 ? wts.x : t == 1 ? wts.y : wts.z;
                    if (w < 0.01) continue;
                    tiled += tile_layer(float(t), world_pos.xz, variant) * w;
                    avg += textureLod(ground_tiles, vec3(0.5, 0.5, float(t) * 2.0 + variant), 7.0).rgb * w;
                }
                vec3 color = mix(tiled, tint * tiled / max(avg, vec3(0.02)), 0.55) * (1.0 + paint * 0.3);
        """;
}
