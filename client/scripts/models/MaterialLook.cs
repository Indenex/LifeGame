using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Prototype material looks for Justin's art-direction pick (D-154): small
// hand-painted textures per material instead of the fine noisy relief. Showcase
// only until he picks; nothing in game uses this yet.
//   A  32 px painted patterns in a few flat tones, pixel-sharp (Valheim-like).
//   B  the same patterns at 64 px, smoothly filtered (storybook).
//   C  B plus a slight tint shift per placed object (variety, D-166).
//   D  matched to Justin's reference art (docs/art-reference, D-232): light in
//      2 to 3 crisp bands with blue-violet shade, a soft dark rim at the edges,
//      painted strokes in 3 tones (scallops on foliage, bark stripes, stone
//      cracks, grass blades), and the reference palette.
//   E  D with the artist's own painted tiles (Golden Skull iso-starter pack,
//      docs/art-packs, D-235) for wood, bark, stone and ground: each tile's side
//      or top face unskewed into a square and wrapped round the object, its
//      colours kept, shaded by the vertex colours' brightness.
// Colours still come from the vertex colours and albedo, so palettes stay as they are.
public static class MaterialLook
{
    public enum Variant { Current, A, B, C, D, E }
    public enum Pattern { Plain, Wood, Bark, Stone, Leaves, Straw, Wool, Skin, Grass }

    static Shader? shader;
    static readonly Dictionary<(Pattern, int, bool, bool), ImageTexture> textures = new();
    static readonly Dictionary<(Pattern, Variant, Color), ShaderMaterial> materials = new();

    // How many times a pattern repeats per metre, how it is stretched along u and v,
    // and how strongly it shows.
    static (float Scale, Vector2 Stretch, float Strength) Layout(Pattern p) => p switch
    {
        Pattern.Wood => (1.2f, new Vector2(0.5f, 1f), 1f),
        Pattern.Bark => (1.4f, new Vector2(1f, 0.6f), 1f),
        Pattern.Stone => (0.8f, Vector2.One, 1f),
        Pattern.Leaves => (0.6f, Vector2.One, 1.2f),
        Pattern.Straw => (1.1f, new Vector2(1f, 1f), 1f),
        Pattern.Wool => (3.5f, Vector2.One, 0.8f),
        Pattern.Skin => (1.5f, Vector2.One, 0.35f),
        Pattern.Grass => (1.2f, Vector2.One, 1f),
        _ => (0.6f, Vector2.One, 0.45f),
    };

    public static ShaderMaterial For(Pattern pattern, Variant variant, Color? albedo = null)
    {
        var color = albedo ?? Colors.White;
        if (materials.TryGetValue((pattern, variant, color), out var done)) return done;
        shader ??= new Shader { Code = Code };
        var (scale, stretch, strength) = Layout(pattern);
        bool sharp = variant == Variant.A, painted = variant is Variant.D or Variant.E;
        var art = variant == Variant.E ? ArtTexture(pattern) : null;
        if (painted && art == null)
        {
            // The reference palette: stone leans grey-violet, foliage olive.
            // Tuned to the Golden Skull tiles (docs/art-packs): stone grey with a
            // violet lean, bark a purple-brown, wood a warm red-brown.
            if (pattern == Pattern.Stone) color *= new Color(0.92f, 0.9f, 1.08f);
            if (pattern == Pattern.Bark) color *= new Color(0.95f, 0.92f, 1.06f);
            if (pattern == Pattern.Wood) color *= new Color(1.05f, 0.95f, 0.88f);
            // Straw toward the pack's golden hay, a little deeper than today's pale thatch.
            // Muted toward the pack's dusty hay, so the roof sits in the olive palette.
            if (pattern == Pattern.Straw)
            {
                color *= new Color(0.86f, 0.8f, 0.72f);
                float lum = color.R * 0.3f + color.G * 0.59f + color.B * 0.11f;
                color = color.Lerp(new Color(lum * 1.08f, lum * 1.0f, lum * 0.82f), 0.45f) * 0.88f;
            }
            // Figures a touch deeper and warmer, so pale skin and cloth keep their
            // shape under the creamy grade.
            if (pattern == Pattern.Skin) color *= new Color(0.9f, 0.82f, 0.76f);
            // Foliage toward the pack's olive-yellow and muted greens: half way to a
            // warm olive of the same brightness.
            if (pattern == Pattern.Leaves)
            {
                float lum = color.R * 0.3f + color.G * 0.59f + color.B * 0.11f;
                color = color.Lerp(new Color(lum * 1.15f, lum * 1.1f, lum * 0.6f), 0.5f);
            }
            if (pattern == Pattern.Leaves) (scale, stretch) = (0.8f, Vector2.One);
        }
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("albedo", color);
        material.SetShaderParameter(sharp ? "tex_sharp" : "tex_soft", Texture(pattern, sharp ? 32 : 64, sharp || painted, painted));
        material.SetShaderParameter("sharp", sharp);
        material.SetShaderParameter("scale", scale);
        material.SetShaderParameter("stretch", stretch);
        material.SetShaderParameter("strength", strength);
        material.SetShaderParameter("vary", variant is Variant.C or Variant.D ? 0.18f : 0f);
        material.SetShaderParameter("banded", painted);
        if (art != null)
        {
            material.SetShaderParameter("art", art.Value.Texture);
            material.SetShaderParameter("use_art", true);
            material.SetShaderParameter("scale", art.Value.Scale);
            material.SetShaderParameter("stretch", Vector2.One);
        }
        materials[(pattern, variant, color)] = material;
        return material;
    }

    // The artist's tile for a pattern, as a square texture, and how many times it
    // repeats per metre. Null when the pack has none for it.
    static readonly Dictionary<Pattern, (ImageTexture Texture, float Scale)?> art = new();
    const string Tiles = "res://../docs/art-packs/golden-skull/iso-starter/Sprites/";

    static (ImageTexture Texture, float Scale)? ArtTexture(Pattern p)
    {
        if (art.TryGetValue(p, out var done)) return done;
        (string File, bool Top, float Scale)? source = p switch
        {
            Pattern.Wood => ("Wood/ISO_Tile_Wood_01.png", false, 1.1f),
            Pattern.Bark => ("Wood/ISO_Tile_Tree_01.png", false, 1.2f),
            Pattern.Stone => ("Stone/ISO_Tile_Stone_01.png", false, 0.8f),
            Pattern.Grass => ("Grass/ISO_Tile_Dirt_01_Grass_01.png", true, 1.6f),
            _ => null,
        };
        (ImageTexture, float)? result = null;
        if (source is { } s)
        {
            var image = Image.LoadFromFile(ProjectSettings.GlobalizePath(Tiles + s.File));
            if (image != null && !image.IsEmpty())
            {
                image.Convert(Image.Format.Rgba8);
                result = (ImageTexture.CreateFromImage(Unskew(image, s.Top)), s.Scale);
            }
        }
        art[p] = result;
        return result;
    }

    // The tiles are 148 x 164 isometric cubes: a top diamond from (5, 35) across to
    // (143, 35) with its point at (74, 0), and side faces 93 px tall below it. Pick
    // the right side face (or the top) and resample it into a 128 px square, a few
    // pixels in from the painted outline.
    static Image Unskew(Image tile, bool top)
    {
        const int n = 128;
        const float inset = 0.12f;
        var square = Image.CreateEmpty(n, n, false, Image.Format.Rgba8);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = Mathf.Lerp(inset, 1f - inset, (x + 0.5f) / n), v = Mathf.Lerp(inset, 1f - inset, (y + 0.5f) / n);
                Vector2 at = top
                    ? new Vector2(5f, 35f) + u * new Vector2(69f, -35f) + v * new Vector2(69f, 35f)
                    : new Vector2(74f, 70f) + u * new Vector2(69f, -35f) + v * new Vector2(0f, 93f);
                int ix = Mathf.Clamp((int)at.X, 0, tile.GetWidth() - 1), iy = Mathf.Clamp((int)at.Y, 0, tile.GetHeight() - 1);
                var c = tile.GetPixel(ix, iy);
                square.SetPixel(x, y, new Color(c.R, c.G, c.B, 1f));
            }
        square.GenerateMipmaps();
        return square;
    }

    // A pine (PropMeshes.PaintedPine, one unit tall) painted like the card pine:
    // flat bands rather than mottling. Each bough is the card's deep green with one
    // lighter patch along its upper middle, darkening toward the trunk where the
    // tier above shades it, and a soft ink line round its edge and points. The trunk
    // takes the card's bark colours. Lit in soft bands so it sits in the scene's light
    // without fighting the painted shading.
    public static ShaderMaterial CardPine(Texture2D card)
    {
        // Through Look's MakeShader, so it gets the weather (cloud shadows) like the ground.
        var material = new ShaderMaterial { Shader = Look.MakeShader(CardPineCode) };
        material.SetShaderParameter("card", card);
        material.SetShaderParameter("brush", Brush);
        material.SetShaderParameter("clouds", Look.Clouds);
        return material;
    }

    // A tiling noise of soft blobs, read in world space as brush dabs: where it is
    // high, light lands as a dab even across neighbouring boughs, and it breaks the
    // edges of the light bands so they look painted rather than smooth.
    static NoiseTexture2D Brush => brush ??= new NoiseTexture2D
    {
        Width = 256, Height = 256, Seamless = true, GenerateMipmaps = true,
        Noise = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 0.02f, FractalOctaves = 3, FractalGain = 0.45f, Seed = 7 },
    };
    static NoiseTexture2D? brush;

    const string CardPineCode = """
        shader_type spatial;
        render_mode cull_disabled;
        uniform sampler2D card : source_color, filter_linear_mipmap, repeat_disable;
        uniform sampler2D brush : filter_linear_mipmap, repeat_enable;
        // Colours picked from the card.
        uniform vec3 shade_col : source_color = vec3(0.16, 0.2, 0.14);
        uniform vec3 base_col : source_color = vec3(0.23, 0.3, 0.2);
        uniform vec3 lit_col : source_color = vec3(0.33, 0.4, 0.26);
        uniform vec3 ink : source_color = vec3(0.1, 0.11, 0.08);
        uniform vec3 bark_dark : source_color = vec3(0.24, 0.16, 0.19);
        uniform vec3 bark_lit : source_color = vec3(0.39, 0.31, 0.27);
        // Shade leans violet, as a painter's would; lit tips lean warm.
        uniform vec3 shade_tint : source_color = vec3(0.72, 0.7, 0.9);
        uniform vec3 warm_tint : source_color = vec3(1.0, 0.96, 0.86);
        // How many metres one repeat of the brush noise spans.
        uniform float brush_size = 3.5;
        // How much the light follows the whole tree's cone instead of each bough,
        // so light and shade fall in big painted shapes across the tree.
        uniform float cone_light = 0.75;
        varying vec3 obj;
        varying vec3 world_pos;
        varying vec3 world_n;
        varying float dab;
        varying vec3 cone_n;

        // The brush noise at a world point, read on two planes so it sticks to the
        // tree from every side; stretched a little sideways, like short strokes.
        float brush_at(vec3 p) {
            vec2 a = vec2(p.x * 0.8, p.y * 1.3) / brush_size;
            vec2 b = vec2(p.z * 0.8, p.y * 1.3) / brush_size + 0.37;
            return mix(texture(brush, a).r, texture(brush, b).r, 0.5);
        }

        void vertex() {
            obj = VERTEX;
            world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
            world_n = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
            // The cone's own normal at this point: out from the trunk and a little up.
            vec3 out_dir = length(VERTEX.xz) > 1e-4 ? normalize(vec3(VERTEX.x, 0.0, VERTEX.z)) : vec3(0.0, 0.0, 1.0);
            cone_n = normalize((MODELVIEW_MATRIX * vec4(out_dir + vec3(0.0, 0.45, 0.0), 0.0)).xyz);
        }
        void fragment() {
            vec3 col;
            dab = brush_at(world_pos);
            if (UV2.x < 0.5) {
                // Trunk: the card's purple-brown bark in long vertical strokes.
                float ang = atan(obj.x, obj.z);
                float stroke = fract(ang * 1.4 + sin(obj.y * 18.0 + ang * 3.0) * 0.12);
                col = mix(bark_dark, bark_lit, 0.7 * smoothstep(0.55, 0.62, stroke) * (1.0 - smoothstep(0.78, 0.85, stroke)));
            } else if (UV2.x < 1.5) {
                col = shade_col * COLOR.rgb;
            } else {
                float across = abs(UV.x), along = UV.y, rnd = UV2.y;
                // The lighter patch on each bough's upper middle, its edge wavering
                // with the brush, joined by dabs that run across neighbouring boughs.
                float edge = 0.55 + 0.25 * (dab - 0.5) + 0.12 * rnd;
                float patch = (1.0 - smoothstep(edge - 0.04, edge + 0.04, across))
                    * smoothstep(0.2 + 0.12 * rnd, 0.28 + 0.12 * rnd, along)
                    * (1.0 - smoothstep(0.8, 0.86, along));
                float dabs = smoothstep(0.6, 0.63, dab) * smoothstep(0.1, 0.3, along);
                patch = max(patch * 0.85, dabs);
                patch *= FRONT_FACING ? smoothstep(0.05, 0.3, world_n.y) : 0.0;
                col = mix(base_col, lit_col, patch);
                // Warm light on the tips.
                col *= mix(vec3(1.0), warm_tint, smoothstep(0.7, 0.9, along) * patch);
                // Darker toward the sides, and toward the trunk under the tier above.
                col = mix(col, shade_col, 0.65 * smoothstep(0.72, 0.98, across) * (1.0 - patch));
                col = mix(shade_col, col, smoothstep(0.16, 0.4, along + (dab - 0.5) * 0.15));
                if (!FRONT_FACING) col = mix(col, shade_col, 0.6);
                // A soft ink line round the edge and the points.
                float line = smoothstep(0.9, 1.0, across) * smoothstep(0.7, 0.9, along) * 0.5 + smoothstep(0.9, 1.0, along);
                col = mix(col, ink, 0.7 * clamp(line, 0.0, 1.0));
                col *= COLOR.rgb;
            }
            ALBEDO = col;
            if (UV2.x > 0.5) NORMAL = normalize(mix(FRONT_FACING ? NORMAL : -NORMAL, cone_n, cone_light));
            ROUGHNESS = 0.95;
            SPECULAR = 0.0;
        }
        // Light in painted steps (Atmosphere's rules): the wrapped light times the
        // shadow is quantised into shade, mid and lit, with edges broken by the
        // brush; the light's own colour and strength stay smooth. Shade leans
        // violet. Ambient is left to the scene.
        void light() {
            float wrapped = clamp(dot(NORMAL, LIGHT) * 0.6 + 0.4, 0.0, 1.0);
            // Passing cloud shadows move the band edges, as on the ground.
            float sky = LIGHT_IS_DIRECTIONAL ? cloud_shadow(world_pos) : 1.0;
            float l = wrapped * ATTENUATION * sky + (dab - 0.5) * 0.22;
            float soft = 0.025;
            float mid = smoothstep(0.3 - soft, 0.3 + soft, l);
            float lit = smoothstep(0.62 - soft, 0.62 + soft, l);
            // A low floor, so a pine in shadow is dark like the ground under it; the
            // scene's blue-violet ambient carries the shade.
            float band = 0.1 + 0.36 * mid + 0.3 * lit;
            vec3 tone = mix(shade_tint, vec3(1.0), mid);
            // A fire or doorway light fades out with its distance falloff instead of
            // lighting every needle in range evenly.
            float reach = LIGHT_IS_DIRECTIONAL ? 1.0 : smoothstep(0.0, 0.25, ATTENUATION);
            DIFFUSE_LIGHT += LIGHT_COLOR * tone * band * reach / PI;
        }
        """;

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
        // Modeling's own item and station families (ItemModels, StationMeshes).
        if (Mathf.IsEqualApprox(scale, 7f) || Mathf.IsEqualApprox(scale, 8f)) return Pattern.Wood;
        if (Mathf.IsEqualApprox(scale, 4f) || Mathf.IsEqualApprox(scale, 5f)) return Pattern.Stone;
        if (Mathf.IsEqualApprox(scale, 9f)) return Pattern.Straw;
        if (Mathf.IsEqualApprox(scale, 1.6f)) return albedo.R > albedo.G * 1.08f ? Pattern.Straw : Pattern.Leaves;
        if (Mathf.IsEqualApprox(scale, 1f) && bump > 1f) return Pattern.Stone;
        return Pattern.Plain;
    }

    // --- Painted textures: a grey value per texel (0 dark, 1 light), tiling.

    static ImageTexture Texture(Pattern p, int n, bool tones, bool painted)
    {
        if (textures.TryGetValue((p, n, tones, painted), out var done)) return done;
        var image = Image.CreateEmpty(n, n, true, Image.Format.L8);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float v = Mathf.Clamp(painted ? Painted(p, (x + 0.5f) / n, (y + 0.5f) / n) : Value(p, (x + 0.5f) / n, (y + 0.5f) / n), 0f, 1f);
                // A few flat tones, like paint laid down in strokes.
                if (tones) v = painted ? Mathf.Round(v * 2f) / 2f : Mathf.Round(v * 3f) / 3f;
                image.SetPixel(x, y, new Color(v, v, v));
            }
        image.GenerateMipmaps();
        return textures[(p, n, tones, painted)] = ImageTexture.CreateFromImage(image);
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

    // Variant D's strokes, after the reference art: three tones (dark 0, mid 0.5,
    // light 1) with crisp edges.
    static float Painted(Pattern p, float u, float v)
    {
        switch (p)
        {
            case Pattern.Leaves:
            {
                // Rows of downward points: light at the tips, mid above, dark in the
                // shade under the row above.
                float row = Mathf.Floor(v * 4f);
                float fu = Mathf.PosMod(u * 5f + row * 0.5f + MeshKit.Hash((int)row, 80) * 0.3f, 1f);
                float fv = Mathf.PosMod(v * 4f, 1f);
                float edge = 0.32f * Mathf.Abs(2f * fu - 1f) + 0.05f * MeshKit.Hash((int)(u * 5f), (int)row + 81);
                if (fv < edge) return 0f;
                float t = (fv - edge) / (1f - edge);
                return t < 0.45f ? 1f : t < 0.8f ? 0.5f : 0f;
            }
            case Pattern.Bark:
            {
                // Scaly plates, as in the artist's tree tiles: long upright plates
                // outlined dark, each lit along its top.
                float col = Mathf.Floor(u * 5f);
                float fu = Mathf.PosMod(u * 5f, 1f);
                float shift = MeshKit.Hash((int)col, 82);
                float row = Mathf.Floor(v * 2.5f + shift);
                float fv = Mathf.PosMod(v * 2.5f + shift, 1f);
                float wob = (MeshKit.Hash((int)col, (int)row + 83) - 0.5f) * 0.15f;
                if (fu < 0.1f + wob * 0.3f || fu > 0.92f || fv < 0.07f) return 0f;
                return fv > 0.78f ? 1f : 0.5f;
            }
            case Pattern.Stone:
            {
                // Cobbles, as in the artist's stone tiles: dark mortar between, a lit
                // rim along each stone's upper edge, mid inside.
                const int cells = 3;
                float best = 9f, second = 9f;
                Vector2 centre = Vector2.Zero;
                for (int i = -1; i <= cells; i++)
                    for (int j = -1; j <= cells; j++)
                    {
                        int wi = ((i % cells) + cells) % cells, wj = ((j % cells) + cells) % cells;
                        var c = new Vector2((i + 0.2f + 0.6f * MeshKit.Hash(wi, wj + 40)) / cells, (j + 0.2f + 0.6f * MeshKit.Hash(wi + 7, wj + 41)) / cells);
                        float d = c.DistanceTo(new Vector2(u, v));
                        if (d < best) { second = best; best = d; centre = c; }
                        else if (d < second) second = d;
                    }
                float edge = second - best;
                if (edge < 0.02f) return 0f;
                return edge < 0.07f && v > centre.Y ? 1f : 0.5f;
            }
            case Pattern.Wood:
            {
                // Logs and posts, as the artist paints them: long grain strokes
                // along the wood, dark cracks, a lit stroke here and there, and a
                // knot now and then.
                float lane = Mathf.Floor(v * 7f);
                float f = Mathf.PosMod(v * 7f + Noise(u * 2, v * 7, 2, 1) * 0.5f, 1f);
                float run = Mathf.PosMod(u * 1.5f + MeshKit.Hash((int)lane, 87), 1f);
                if (f < 0.12f && run < 0.75f) return 0f;
                if (f > 0.82f && run > 0.3f) return 1f;
                var knot = new Vector2(MeshKit.Hash(3, 88), MeshKit.Hash(4, 88));
                float k = new Vector2(Mathf.PosMod(u - knot.X + 0.5f, 1f) - 0.5f, Mathf.PosMod(v - knot.Y + 0.5f, 1f) - 0.5f).Length();
                if (k < 0.04f) return 0f;
                if (k < 0.06f) return 1f;
                return 0.5f;
            }
            case Pattern.Grass:
            {
                // Short blades scattered over mid green: light blades, dark roots.
                float best = 0.5f;
                for (int k = 0; k < 14; k++)
                {
                    var c = new Vector2(MeshKit.Hash(k, 90), MeshKit.Hash(k, 91));
                    for (int ox = -1; ox <= 1; ox++)
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            float du = u - c.X - ox, dv = v - c.Y - oy;
                            float lean = (MeshKit.Hash(k, 92) - 0.5f) * 0.6f;
                            float x = du - dv * lean;
                            if (dv > 0f && dv < 0.16f && Mathf.Abs(x) < 0.025f * (1f - dv / 0.16f)) best = 1f;
                            else if (dv <= 0f && dv > -0.02f && Mathf.Abs(du) < 0.03f) best = Mathf.Min(best, 0f);
                        }
                }
                return best;
            }
            case Pattern.Straw:
            {
                // Thatch, as on the artist's hay roofs: rows of straw bundles, each
                // with a ragged fringe hanging over the row below, lit above the
                // fringe, dark in the shadow it casts, fine straws in between.
                float row = Mathf.Floor(v * 4f);
                float fv = Mathf.PosMod(v * 4f, 1f);
                float fringe = 0.18f + 0.1f * MeshKit.Hash((int)Mathf.Floor(u * 24f), (int)row + 85);
                if (fv < fringe * 0.55f) return 0f;
                if (fv < fringe) return 1f;
                float straw = Mathf.PosMod(u * 24f + Noise(u * 4, v * 4, 4, 8) * 0.6f, 1f);
                return straw < 0.18f ? 0f : fv > 0.8f ? 0.5f : (MeshKit.Hash((int)Mathf.Floor(u * 24f), 86) > 0.55f ? 1f : 0.5f);
            }
            case Pattern.Skin:
            {
                // Skin, cloth and hair on figures: a few broad, soft brush strokes in
                // two tones, never busy at game distance.
                float stroke = Noise(u * 3 + v * 1.5f, v * 3, 3, 89) + 0.35f * Noise(u * 6, v * 6, 6, 90);
                return stroke > 0.82f ? 1f : 0.5f;
            }
            case Pattern.Wool:
            {
                // Wool in soft rounded tufts: each tuft lit on top, a dark crease below.
                float best = 9f;
                Vector2 c0 = Vector2.Zero;
                for (int k = 0; k < 7; k++)
                {
                    var c = new Vector2(MeshKit.Hash(k, 70), MeshKit.Hash(k, 71));
                    for (int ox = -1; ox <= 1; ox++)
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            float d = new Vector2(u - c.X - ox, v - c.Y - oy).Length();
                            if (d < best) { best = d; c0 = new Vector2(c.X + ox, c.Y + oy); }
                        }
                }
                if (best > 0.24f) return 0f;
                return v > c0.Y + 0.04f ? 1f : 0.5f;
            }
            default:
                return Mathf.Clamp(Value(p, u, v), 0f, 1f);
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
        // Variant D: light in crisp bands, shade leaning blue-violet, dark rims.
        uniform bool banded = false;
        // Variant E: the artist's painted tile, its own colours.
        uniform sampler2D art : source_color, filter_linear_mipmap, repeat_enable;
        uniform bool use_art = false;
        uniform vec3 shade_tint : source_color = vec3(0.55, 0.52, 0.82);

        varying vec3 tint;
        varying vec3 obj;
        varying vec3 obj_normal;
        varying float bright;

        void vertex() {
            // Palette colours are authored in sRGB.
            tint = pow(COLOR.rgb, vec3(2.2)) * albedo.rgb;
            obj = VERTEX;
            obj_normal = NORMAL;
            bright = dot(pow(COLOR.rgb, vec3(2.2)), vec3(0.3, 0.59, 0.11));
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
            if (use_art) {
                // Mirrored repeats hide the tile's edges.
                vec2 a = obj.zy * scale, b = obj.xz * scale, c = obj.xy * scale;
                a = abs(fract(a * 0.5) * 2.0 - 1.0); b = abs(fract(b * 0.5) * 2.0 - 1.0); c = abs(fract(c * 0.5) * 2.0 - 1.0);
                vec3 painted = texture(art, a).rgb * w.x + texture(art, b).rgb * w.y + texture(art, c).rgb * w.z;
                ALBEDO = clamp(painted * clamp(bright * 1.35, 0.5, 1.5), 0.0, 1.0);
            }
            if (banded) {
                // A soft dark line round each shape's silhouette.
                float rim = 1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0);
                ALBEDO *= mix(1.0, 0.62, smoothstep(0.62, 0.86, rim));
            }
            ROUGHNESS = 0.92;
            SPECULAR = banded ? 0.0 : 0.2;
        }

        void light() {
            float l = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
            if (banded) {
                // Shade, mid and lit, with crisp edges; the shade band leans violet.
                float lit = 0.35 * smoothstep(0.06, 0.1, l) + 0.65 * smoothstep(0.42, 0.47, l);
                vec3 tone = mix(shade_tint, vec3(1.0), smoothstep(0.06, 0.1, l));
                DIFFUSE_LIGHT += LIGHT_COLOR * tone * (0.18 + 0.82 * lit) / PI;
            } else {
                DIFFUSE_LIGHT += LIGHT_COLOR * l / PI;
            }
        }
        """;
}
