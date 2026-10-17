using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// A prototype of the ground in Justin's art references (docs/art-reference 02 and
// 03). Justin wants the dense, wind-swept blade grass kept as one lush carpet, but
// restyled: the pack palette (olive-yellow lit tips, muted greens) in two or three
// painted tone bands instead of a smooth gradient, with a darker soft base where it
// meets the ground. Flowers, clover, ferns and mushrooms from his licensed Golden
// Skull foliage pack (client/assets/environment, NOTICE.md) are sparse accents in it,
// drawn as crossed alpha cards; the ground takes the pack's grass-top colour, with
// sandy paths on the deer trails and twigs and old stumps scattered through. Only
// with --ground=painted on the command line, until Justin picks (D-154).
public static class PaintedGround
{
    public static readonly bool On = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ground=painted");

    // The pack's grass tones (Modeling's variant D, from the Golden Skull tiles): the
    // muted mid green each blade is tinted with, and the olive-yellow lit tip the
    // shader paints toward (it holds its own copies, in the band code).
    static readonly Color GrassMid = new(0.45f, 0.55f, 0.24f), GrassLit = new(0.80f, 0.80f, 0.50f);
    // The ground under the carpet, the pack's warm yellow-green a shade darker than the
    // blades, matching their shaded base so the carpet reads fully filled, and the
    // pack's sandy path.
    public static readonly Color MeadowTop = new(0.41f, 0.50f, 0.22f);
    static readonly Color PathSand = new(0.70f, 0.58f, 0.38f);

    static Mesh? twig;
    static ShaderMaterial? grassMaterial;

    // The atlas cells (tools/env-assets/build_foliage_atlas.py): each sprite's width
    // and height as a fraction of its 256 px cell, standing on the cell's bottom.
    readonly record struct Cell(int Index, float W, float H);
    static readonly Cell[] Cells =
    {
        new(0, 0.969f, 0.773f), new(1, 0.969f, 0.789f), new(2, 0.969f, 0.684f), new(3, 0.969f, 0.836f), // grass 01-04
        new(4, 0.969f, 0.652f), new(5, 0.969f, 0.945f), new(6, 0.969f, 0.621f), // grass 05-07
        new(7, 0.969f, 0.840f), new(8, 0.844f, 0.969f), new(9, 0.551f, 0.969f), // flowers 01-03
        new(10, 0.969f, 0.824f), new(11, 0.664f, 0.969f), new(12, 0.969f, 0.895f), // clover 01-03
        new(13, 0.969f, 0.664f), new(14, 0.969f, 0.734f), // ferns 01-02
        new(15, 0.715f, 0.969f), new(16, 0.969f, 0.754f), new(17, 0.824f, 0.969f), new(18, 0.719f, 0.969f), // mushrooms 01-04
    };
    const int AtlasCols = 8, AtlasRows = 4;
    public const int FlowerCards = 7, CloverCards = 10, FernCards = 13, MushroomCards = 15;

    static readonly Mesh?[] cards = new Mesh?[19];
    static ShaderMaterial? cardMaterial;

    // One sprite as two crossed cards, about `size` metres across its larger side.
    public static Mesh Card(int index, float size = 0.4f) => cards[index] ??= BuildCard(index, size);

    // A square metre of thick painted grass: five tufts, each a crossed pair of
    // cards of a different grass sprite, at their own heights and turns.
    // Everything the prototype's chunk builds touch, made on the main thread first
    // (chunks build on worker threads, env-12). Card meshes are cached by sprite, at
    // the size they are first asked for, so these sizes are the ones Terrain uses.
    public static void Prewarm()
    {
        for (int i = 0; i < TuftVariants; i++) BroadTufts(i);
        // Far ground lifts toward the carpet's colour past where the tufts fade (Look).
        RenderingServer.GlobalShaderParameterSet("ground_far_lift", new Vector3(1.18f, 1.14f, 1.02f));
        _ = Twig; _ = GrassMaterial; _ = CardMaterial;
        _ = PropMeshes.Stump; _ = Look.BarkSurface;
        for (int i = 0; i < 4; i++) Card(MushroomCards + i, 0.22f);
        for (int i = 0; i < 3; i++) Card(CloverCards + i, 0.3f);
        Card(FernCards, 0.75f);
        Card(FlowerCards, 0.35f);
        Card(FlowerCards + 1, 0.5f); Card(FlowerCards + 2, 0.5f);
    }

    // Cards for the scattered plants.
    public static ShaderMaterial CardMaterial => cardMaterial ??= MakeCardMaterial(0f);

    public static Mesh Twig => twig ??= BuildTwig();

    // A square metre of the restyled grass: 100 blades in a jittered 10 x 10 grid and
    // at the same heights as today's (Justin: shorter, denser), but each a broad, arching leaf (full width for the lower half, then
    // a tapered point that bends over) rather than a thin spike, as the pack's tufts
    // are drawn. Each blade's vertex shade varies a little, so neighbours read apart.
    public static Mesh BroadTuft => BroadTufts(0);
    public const int TuftVariants = 3;
    static readonly Mesh?[] broadTufts = new Mesh?[TuftVariants];
    public static Mesh BroadTufts(int variant) => broadTufts[variant] ??= BuildBroadTuft(variant);

    static Mesh BuildBroadTuft(int variant)
    {
        uint v = (uint)variant * 40u;
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        const int grid = 10;
        for (int gx = 0; gx < grid; gx++)
            for (int gz = 0; gz < grid; gz++)
            {
                float px = (gx + 0.5f) / grid - 0.5f + (H(gx, gz, 620u + v) - 0.5f) / grid;
                float pz = (gz + 0.5f) / grid - 0.5f + (H(gx, gz, 621u + v) - 0.5f) / grid;
                float angle = H(gx, gz, 622u + v) * Mathf.Pi;
                float height = 0.2f + H(gx, gz, 623u + v) * 0.22f;
                float half = 0.045f + H(gx, gz, 624u + v) * 0.028f;
                var side = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * half;
                var lean = new Vector3(H(gx, gz, 625u + v) - 0.5f, 0, H(gx, gz, 626u + v) - 0.5f).Normalized() * (0.08f + H(gx, gz, 627u + v) * 0.16f);
                var shade = Colors.White.Darkened(H(gx, gz, 628u + v) * 0.32f);
                var root = new Vector3(px, -0.02f, pz);
                var mid = root + lean * 0.35f + Vector3.Up * height * 0.55f;
                var tip = root + lean + Vector3.Up * height * 0.9f;
                // UV.x runs -1 to 1 across the blade (0 at the tip), for the dark rim.
                Blade(st, root - side, new(-1f, 0f), root + side, new(1f, 0f), mid + side * 0.85f, new(1f, 0.55f), shade);
                Blade(st, root - side, new(-1f, 0f), mid + side * 0.85f, new(1f, 0.55f), mid - side * 0.85f, new(-1f, 0.55f), shade);
                Blade(st, mid - side * 0.85f, new(-1f, 0.55f), mid + side * 0.85f, new(1f, 0.55f), tip, new(0f, 1f), shade);
            }
        return st.Commit();
    }

    static void Blade(SurfaceTool st, Vector3 a, Vector2 ua, Vector3 b, Vector2 ub, Vector3 c, Vector2 uc, Color shade)
    {
        foreach (var (v, t) in new[] { (a, ua), (b, ub), (c, uc) })
        {
            st.SetNormal(Vector3.Up);
            st.SetUV(t);
            st.SetColor(shade);
            st.AddVertex(v);
        }
    }

    // The restyled grass (the same tuft mesh and density as today's).
    public static ShaderMaterial GrassMaterial => grassMaterial ??= MakeGrassMaterial();

    // The ground colour: today's tile colour pulled half way to the warm yellow-green (still
    // browner under trees and on the heath), with the deer trails worn to sandy paths.
    public static Color Ground(Color tile, float trail) =>
        tile.Lerp(MeadowTop, 0.5f).Lerp(PathSand, Mathf.SmoothStep(0.25f, 0.7f, trail));

    // The same instances, tinted near white (painted cards bring their own colours).
    public static List<(Transform3D, Color, Color)> Whiten(List<(Transform3D, Color, Color)> items) =>
        items.ConvertAll(i => (i.Item1, Colors.White.Lerp(i.Item2, 0.15f), i.Item3));

    // A grass tuft's tint on a tile: the pack's muted mid green, a little toward the
    // olive-yellow where the meadow is lush, and still darker under trees, browner on
    // the heath and mossier in the wet (as the tile colour says).
    public static Color Grass(Color tile, float lush) =>
        GrassMid.Lerp(GrassLit, 0.25f * Mathf.Clamp(lush * 1.3f - 0.2f, 0f, 1f))
            .Lerp(tile * (GrassMid.V / Mathf.Max(tile.V, 0.05f)), 0.35f) * Mathf.Clamp(tile.V / 0.45f, 0.75f, 1.05f);

    // A dead twig lying in the grass: a bent stick with a fork, about 40 cm long.
    static Mesh BuildTwig()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var stick = new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.014f, Height = 0.4f, RadialSegments = 5, Rings = 1 };
        Terrain.Append(st, stick, new Transform3D(new Basis(Vector3.Forward, Mathf.Pi / 2 - 0.08f), new Vector3(0, 0.02f, 0)), Colors.White);
        var fork = new CylinderMesh { TopRadius = 0.005f, BottomRadius = 0.009f, Height = 0.16f, RadialSegments = 5, Rings = 1 };
        Terrain.Append(st, fork, new Transform3D(new Basis(Vector3.Up, 0.6f) * new Basis(Vector3.Forward, Mathf.Pi / 2 - 0.25f), new Vector3(0.1f, 0.035f, 0.04f)), new Color(0.9f, 0.9f, 0.9f));
        return st.Commit();
    }

    static Mesh BuildCard(int index, float size)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        AddCard(st, Cells[index], Vector3.Zero, size, 0f);
        return st.Commit();
    }

    // Two quads crossed at right angles through the sprite's root, the sprite's UVs
    // from its atlas cell, and UV2.y running 0 at the root to 1 at the top (sway).
    static void AddCard(SurfaceTool st, Cell cell, Vector3 at, float size, float turn)
    {
        float scale = size / Mathf.Max(cell.W, cell.H);
        float w = cell.W * scale, h = cell.H * scale;
        float u0 = (cell.Index % AtlasCols + 0.5f - cell.W / 2f) / AtlasCols, u1 = (cell.Index % AtlasCols + 0.5f + cell.W / 2f) / AtlasCols;
        float vBottom = (cell.Index / AtlasCols + 1f - 2f / 256f) / AtlasRows, vTop = vBottom - cell.H / AtlasRows;
        for (int q = 0; q < 2; q++)
        {
            float a = turn + q * Mathf.Pi / 2f;
            var side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (w / 2f);
            Vector3 bl = at - side, br = at + side, tl = bl + Vector3.Up * h, tr = br + Vector3.Up * h;
            CardVertex(st, bl, new Vector2(u0, vBottom), 0f); CardVertex(st, br, new Vector2(u1, vBottom), 0f); CardVertex(st, tr, new Vector2(u1, vTop), 1f);
            CardVertex(st, bl, new Vector2(u0, vBottom), 0f); CardVertex(st, tr, new Vector2(u1, vTop), 1f); CardVertex(st, tl, new Vector2(u0, vTop), 1f);
        }
    }

    static void CardVertex(SurfaceTool st, Vector3 v, Vector2 uv, float tip)
    {
        st.SetNormal(Vector3.Up);
        st.SetUV(uv);
        st.SetUV2(new Vector2(0, tip));
        st.SetColor(Colors.White);
        st.AddVertex(v);
    }

    // The cards' shader: the sprite, cut out at its alpha, tinted by the instance,
    // swaying from the root in the grass's wind, lit softly like the grass, straw in
    // the cold and pressed down by snow. Prototype: the real one belongs in Look.
    static ShaderMaterial MakeCardMaterial(float fadeFar)
    {
        _ = Look.Grass; // registers the weather globals the shader reads
        var material = new ShaderMaterial { Shader = cardShader ??= new Shader { Code = CardCode } };
        material.SetShaderParameter("atlas", GD.Load<Texture2D>("res://assets/environment/foliage_atlas.png"));
        material.SetShaderParameter("fade_far", fadeFar);
        return material;
    }

    static Shader? cardShader;

    const string CardCode = """
        shader_type spatial;
        render_mode cull_disabled, specular_disabled;
        uniform sampler2D atlas : source_color, filter_linear_mipmap, repeat_disable;
        uniform float fade_far = 0.0;
        global uniform vec3 player_position;
        global uniform float wind_strength;
        global uniform float wind_gust;
        global uniform vec2 wind_dir;
        global uniform float snow_cover;
        global uniform float season_warmth;
        global uniform float world_seconds;
        varying vec3 tint;
        varying float tip;

        void vertex() {
            if (fade_far > 0.0) {
                float away = length(MODEL_MATRIX[3].xz - player_position.xz);
                VERTEX *= 1.0 - smoothstep(0.65 * fade_far, fade_far, away);
            }
            VERTEX.y *= 1.0 - 0.75 * snow_cover;
            if (INSTANCE_CUSTOM.x > 0.0) {
                float k = smoothstep(0.0, 1.6, world_seconds - INSTANCE_CUSTOM.x);
                VERTEX.y *= INSTANCE_CUSTOM.y > 0.0 ? 1.0 - k : k;
            }
            tip = UV2.y;
            vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
            float gust = smoothstep(0.2, 1.0, sin(dot(world.xz, vec2(0.23, 0.31)) - TIME * 1.3));
            float wind = (0.45 + wind_strength * 1.8) * (0.7 + 0.6 * wind_gust);
            vec3 bend = vec3(wind_dir.x, 0.0, wind_dir.y) * (0.05 + gust * 0.12) * wind;
            VERTEX.xz += (inverse(mat3(MODEL_MATRIX)) * bend).xz * tip * tip;
            NORMAL = vec3(0.0, 1.0, 0.0);
            tint = pow(COLOR.rgb, vec3(2.2));
        }

        void fragment() {
            vec4 texel = texture(atlas, UV);
            if (texel.a < 0.5) discard;
            // Darker at the root, as the painted tufts are, so the carpet has depth.
            vec3 color = texel.rgb * tint * mix(0.55, 0.95, tip);
            vec3 straw = vec3(0.42, 0.36, 0.20) * (0.7 + 0.5 * tip);
            color = mix(color, straw, 0.45 * max(0.0, -season_warmth));
            ALBEDO = color;
            NORMAL = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
            ROUGHNESS = 1.0;
            SPECULAR = 0.0;
        }

        void light() {
            float wrapped = clamp((dot(NORMAL, LIGHT) + 0.3) / 1.3, 0.0, 1.0);
            DIFFUSE_LIGHT += LIGHT_COLOR * wrapped * wrapped * ATTENUATION / PI;
        }
        """;

    // The restyled grass: today's tuft motion (wind waves and flutter, snow, cleared
    // cells lying down, fading out by 48 m), but painted in three tone bands with soft
    // edges: a darker olive-brown base where the blades meet the ground, the tile's
    // muted green, and olive-yellow lit tips. Where the bands fall shifts a little per
    // patch, so they never line up across the field. Prototype; the real one belongs
    // in Look's grass shader, with Atmosphere.
    static ShaderMaterial MakeGrassMaterial()
    {
        _ = Look.Grass; // registers the weather globals the shader reads
        var material = new ShaderMaterial { Shader = new Shader { Code = GrassCode } };
        material.SetShaderParameter("fade_far", 48f);
        return material;
    }

    const string GrassCode = """
        shader_type spatial;
        render_mode cull_disabled, specular_disabled;
        uniform float fade_far = 0.0;
        global uniform vec3 player_position;
        global uniform float wind_strength;
        global uniform float wind_gust;
        global uniform vec2 wind_dir;
        global uniform float snow_cover;
        global uniform float season_warmth;
        global uniform float world_seconds;
        global uniform float wetness;
        varying vec3 tint;
        varying float tip;
        varying float gust;
        varying float patch;
        varying float across;

        void vertex() {
            if (fade_far > 0.0) {
                float away = length(MODEL_MATRIX[3].xz - player_position.xz);
                VERTEX *= 1.0 - smoothstep(0.65 * fade_far, fade_far, away);
            }
            VERTEX.y *= 1.0 - 0.75 * snow_cover;
            // The carpet rises and dips gently in patches a few metres across.
            vec2 at = MODEL_MATRIX[3].xz;
            VERTEX.y *= 0.88 + 0.22 * sin(at.x * 0.41 + sin(at.y * 0.23) * 2.0) * sin(at.y * 0.37 + sin(at.x * 0.19) * 2.0);
            if (INSTANCE_CUSTOM.x > 0.0) {
                float k = smoothstep(0.0, 1.6, world_seconds - INSTANCE_CUSTOM.x);
                float height = INSTANCE_CUSTOM.y > 0.0 ? 1.0 - k : k;
                VERTEX.y *= height;
                VERTEX.xz *= mix(0.7, 1.0, height);
            }
            vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
            tip = UV.y;
            across = UV.x;
            float wave = sin(dot(world.xz, vec2(0.23, 0.31)) - TIME * 1.3);
            gust = smoothstep(0.2, 1.0, wave);
            float flutter = sin(TIME * 3.1 + world.x * 1.7 + world.z * 2.3) * 0.03;
            float wind = (0.45 + wind_strength * 1.8) * (0.7 + 0.6 * wind_gust);
            vec3 bend = vec3(wind_dir.x, 0.0, wind_dir.y) * (0.06 + gust * 0.16) * wind + vec3(flutter, 0.0, -flutter) * wind;
            vec3 local = inverse(mat3(MODEL_MATRIX)) * bend;
            VERTEX.xz += local.xz * tip * tip;
            VERTEX.y -= gust * 0.06 * tip * tip;
            NORMAL = vec3(0.0, 1.0, 0.0);
            tint = pow(COLOR.rgb, vec3(2.2));
            // A per-blade-patch value from the blade's own position, for varying the bands.
            patch = fract(sin(dot(floor(world.xz * 3.0), vec2(12.9898, 78.233))) * 43758.5453);
        }

        void fragment() {
            // The base matches the ground between the blades (PaintedGround.Ground), so a
            // patch without a tuft never shows as a square.
            vec3 base = tint * vec3(0.34, 0.36, 0.5);
            vec3 lit = mix(tint * vec3(1.15, 1.2, 1.0), pow(vec3(0.88, 0.89, 0.62), vec3(2.2)), 0.5 + 0.2 * patch);
            float low = 0.34 + 0.1 * patch, high = 0.6 + 0.12 * patch;
            vec3 color = mix(base, tint, smoothstep(low - 0.04, low + 0.04, tip));
            color = mix(color, lit, smoothstep(high - 0.04, high + 0.04, tip));
            color += vec3(0.04, 0.04, 0.01) * gust * step(high, tip);
            // A soft dark rim along each blade's edges, as the painted leaves have.
            color *= mix(1.0, 0.55, smoothstep(0.5, 0.95, abs(across)));
            color *= mix(1.0, 0.88, wetness);
            vec3 straw = vec3(0.42, 0.36, 0.20) * (0.7 + 0.5 * tip);
            color = mix(color, straw, 0.55 * max(0.0, -season_warmth));
            color = mix(color, vec3(0.86, 0.89, 0.94), snow_cover * (0.55 + 0.45 * tip));
            ALBEDO = clamp(color, 0.0, 1.0);
            NORMAL = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
            ROUGHNESS = 1.0;
            SPECULAR = 0.0;
        }

        void light() {
            float wrapped = clamp((dot(NORMAL, LIGHT) + 0.3) / 1.3, 0.0, 1.0);
            DIFFUSE_LIGHT += LIGHT_COLOR * wrapped * wrapped * ATTENUATION / PI;
        }
        """;

    static float H(int a, int b, uint salt) => Terrain.Hash01(a, b, salt);

}
