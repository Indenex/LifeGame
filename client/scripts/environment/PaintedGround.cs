using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// A prototype of the ground in Justin's art references (docs/art-reference 02 and
// 03): thick painted grass, flowers, clover, ferns and mushrooms drawn as crossed
// alpha cards from his licensed Golden Skull foliage pack (client/assets/environment,
// NOTICE.md); darker olive earth under the grass so the gaps read as shade; bare dirt
// on the deer trails; twigs and old stumps scattered through. Only with
// --ground=painted on the command line, until Justin picks (D-154). The cards sway
// in the same wind as the grass and fade out by distance like the tufts.
public static class PaintedGround
{
    public static readonly bool On = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ground=painted");

    // Three painted tones for grass (sRGB, before the grass shader's warm tips).
    static readonly Color Lit = new(0.66f, 0.70f, 0.26f), Mid = new(0.47f, 0.55f, 0.19f), Shade = new(0.30f, 0.38f, 0.15f);
    // The earth under the grass and on paths.
    static readonly Color UnderGrass = new(0.30f, 0.30f, 0.15f), PathDirt = new(0.47f, 0.35f, 0.22f);

    static Mesh? blades, mushroom, twig, grassClump;

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
    static ShaderMaterial? cardMaterial, grassCardMaterial;

    // One sprite as two crossed cards, about `size` metres across its larger side.
    public static Mesh Card(int index, float size = 0.4f) => cards[index] ??= BuildCard(index, size);

    // A square metre of thick painted grass: four tufts, each a crossed pair of
    // cards of a different grass sprite, at their own heights and turns.
    public static Mesh GrassClump => grassClump ??= BuildGrassClump();

    // Cards for scattered plants, and for the grass (which fades out by 48 m, like
    // the tufts it replaces, so chunks without grass never show).
    public static ShaderMaterial CardMaterial => cardMaterial ??= MakeCardMaterial(0f);
    public static ShaderMaterial GrassCardMaterial => grassCardMaterial ??= MakeCardMaterial(48f);

    public static Mesh Blades => blades ??= BuildBlades();
    public static Mesh Mushroom => mushroom ??= BuildMushroom();
    public static Mesh Twig => twig ??= BuildTwig();

    // The ground colour: today's tile colour pulled darker and toward olive earth, so
    // it reads as shade between the blades; worn paths go to bare dirt.
    public static Color Ground(Color tile, float trail) =>
        tile.Darkened(0.2f).Lerp(UnderGrass, 0.35f).Lerp(PathDirt, Mathf.SmoothStep(0.25f, 0.7f, trail));

    // A card's tint on a tile: near white so the painted sprite keeps its colours,
    // nudged toward the tile's hue (darker under trees, browner on the heath).
    public static Color CardTint(Color tile) =>
        Colors.White.Lerp(tile * (1f / Mathf.Max(tile.V, 0.05f)), 0.22f).Darkened(0.1f * (1f - tile.V / 0.6f));

    // The same instances, tinted near white (painted cards bring their own colours).
    public static List<(Transform3D, Color, Color)> Whiten(List<(Transform3D, Color, Color)> items) =>
        items.ConvertAll(i => (i.Item1, Colors.White.Lerp(i.Item2, 0.15f), i.Item3));

    // A grass clump's colour on a tile, at its lit tone (the blades bake their mid and
    // shade tones below it): brighter where the meadow is lush, and still darker
    // under trees, on the heath and in the wet (as the tile colour says).
    public static Color Grass(Color tile, float lush) =>
        Lit.Darkened(0.12f * (1f - Mathf.Clamp(lush * 1.2f - 0.15f, 0f, 1f))).Lerp(tile * (Lit.V / Mathf.Max(tile.V, 0.05f)), 0.3f);

    // One square metre of thick grass: three clumps of broad, tapering, arching
    // blades. Each blade is a lit, mid or shade tone (baked in the vertex colour,
    // multiplied by the instance colour), with its left half a touch lighter than
    // its right, like a painted leaf.
    static Mesh BuildBlades()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        int blade = 0;
        for (int c = 0; c < 3; c++)
        {
            float cx = (H(c, 0, 600) - 0.5f) * 0.6f, cz = (H(c, 1, 600) - 0.5f) * 0.6f;
            int count = 4 + (int)(H(c, 2, 600) * 3f);
            for (int b = 0; b < count; b++, blade++)
            {
                float angle = b * Mathf.Tau / count + H(blade, 3, 601) * 0.9f;
                var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var side = new Vector3(-dir.Z, 0, dir.X);
                var root = new Vector3(cx, -0.03f, cz) + dir * (0.02f + H(blade, 4, 601) * 0.08f);
                float length = 0.26f + H(blade, 5, 601) * 0.28f;
                float width = 0.045f + H(blade, 6, 601) * 0.03f;
                float lean = 0.25f + H(blade, 7, 601) * 0.35f;
                float roll = H(blade, 8, 601);
                var tone = roll < 0.3f ? Shade : roll < 0.7f ? Mid : Lit;
                // Vertex colours are relative to the instance colour (the lit tone), so
                // they stay at or below 1 (vertex colours are stored in 8 bits).
                var baked = new Color(tone.R / Lit.R, tone.G / Lit.G, tone.B / Lit.B).Darkened(0.06f);
                Vector3 At(float t) => root + dir * (lean * length * t * t) + Vector3.Up * (length * (t - 0.18f * t * t));
                float Width(float t) => width * Mathf.Pow(1f - t, 0.8f);
                const int segments = 3;
                for (int s = 0; s < segments; s++)
                {
                    float t0 = s / (float)segments, t1 = (s + 1) / (float)segments;
                    Vector3 a = At(t0), b2 = At(t1);
                    Vector3 w0 = side * Width(t0), w1 = side * Width(t1);
                    Tri(st, a - w0, t0, baked.Lightened(0.06f), a + w0, t0, baked.Darkened(0.06f), b2 + w1, t1, baked.Darkened(0.06f));
                    Tri(st, a - w0, t0, baked.Lightened(0.06f), b2 + w1, t1, baked.Darkened(0.06f), b2 - w1, t1, baked.Lightened(0.06f));
                }
            }
        }
        return st.Commit();
    }

    // A small mushroom about 12 cm tall: a pale stem under a rounded russet cap. The
    // instance colour only varies them a little.
    static Mesh BuildMushroom()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        Terrain.Append(st, new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.018f, Height = 0.09f, RadialSegments = 6, Rings = 1 }, Terrain.Xf(0, 0.045f, 0), new Color(0.9f, 0.86f, 0.76f));
        Terrain.Append(st, new SphereMesh { Radius = 0.045f, Height = 0.045f, RadialSegments = 8, Rings = 3, IsHemisphere = true }, Terrain.Xf(0, 0.085f, 0), new Color(0.66f, 0.38f, 0.2f));
        return st.Commit();
    }

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

    static Mesh BuildGrassClump()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int k = 0; k < 4; k++)
        {
            var cell = Cells[(int)(H(k, 0, 610) * 7f)];
            var at = new Vector3((H(k, 1, 610) - 0.5f) * 0.7f, -0.03f, (H(k, 2, 610) - 0.5f) * 0.7f);
            AddCard(st, cell, at, 0.55f + H(k, 3, 610) * 0.3f, H(k, 4, 610) * Mathf.Pi);
        }
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
            vec3 color = texel.rgb * tint;
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

    static float H(int a, int b, uint salt) => Terrain.Hash01(a, b, salt);

    static void Tri(SurfaceTool st, Vector3 a, float ta, Color ca, Vector3 b, float tb, Color cb, Vector3 c, float tc, Color cc)
    {
        Vertex(st, a, ta, ca);
        Vertex(st, b, tb, cb);
        Vertex(st, c, tc, cc);
    }

    static void Vertex(SurfaceTool st, Vector3 v, float tip, Color color)
    {
        st.SetNormal(Vector3.Up);
        st.SetUV(new Vector2(0, tip));
        st.SetColor(color);
        st.AddVertex(v);
    }
}
