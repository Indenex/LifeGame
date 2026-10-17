using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// How worked clay looks at each stage: dark and glistening when wet, pale and
// chalky when dry, warm terracotta with smoky fire clouds once fired, and a
// broken rim with dark cracks when a firing goes wrong. Lit with the same soft
// wrapped light as the rest of the world.
public static class ClayLook
{
    public static readonly Color Wet = new(0.36f, 0.23f, 0.16f);
    public static readonly Color Dry = new(0.62f, 0.52f, 0.41f);
    public static readonly Color PitFired = new(0.70f, 0.35f, 0.19f);
    public static readonly Color KilnFired = new(0.64f, 0.31f, 0.19f);

    static Shader? shader;
    static ShaderMaterial? water;
    static readonly Dictionary<(int, int, int), ShaderMaterial> Materials = new();

    // moisture 0..1; fired pots ignore it. Materials are shared in a few steps of
    // moisture so a field of drying pots does not need one material each.
    public static ShaderMaterial For(ClayStage stage, FireKind fire, float moisture, ulong id)
    {
        int step = stage is ClayStage.Fired or ClayStage.Cracked ? 0 : Mathf.RoundToInt(Mathf.Clamp(moisture, 0f, 1f) * 10f);
        int seed = (int)(id % 3);
        var key = ((int)stage * 2 + (int)fire, step, seed);
        if (Materials.TryGetValue(key, out var m)) return m;

        m = new ShaderMaterial { Shader = Shader };
        bool fired = stage is ClayStage.Fired or ClayStage.Cracked;
        float wet = fired ? 0f : step / 10f;
        Color baseColor = fired ? (fire == FireKind.Kiln ? KilnFired : PitFired) : Dry.Lerp(Wet, Mathf.Pow(wet, 0.7f));
        m.SetShaderParameter("albedo", baseColor);
        m.SetShaderParameter("wetness", wet);
        // A pit fire smokes the pot in patches; a kiln fires it evenly.
        m.SetShaderParameter("clouds", fired ? (fire == FireKind.Kiln ? 0.15f : 0.75f) : 0f);
        m.SetShaderParameter("broken", stage == ClayStage.Cracked ? 1f : 0f);
        m.SetShaderParameter("seed", seed * 7.3f);
        Materials[key] = m;
        return m;
    }

    static readonly Dictionary<(ShaderMaterial, int, uint, int), ShaderMaterial> StampedMaterials = new();

    // Finds a maker's glyph (slot in the stamp atlas) and colour from their player id (low 16
    // bits); Main fills this in from the player_mark table. Null when they have no mark.
    public static System.Func<uint, (int slot, Color color)?>? MarkLookup;

    // The same clay as `plain` with a maker's mark painted in (ceramics-15): the glyph's slot in the
    // atlas and the stamp colour. The stamp sits at a height that suits the form.
    public static ShaderMaterial Stamped(ShaderMaterial plain, int slot, Color color, ClayForm form)
    {
        uint packed = color.ToRgba32() >> 8;
        var key = (plain, slot, packed, (int)form);
        if (StampedMaterials.TryGetValue(key, out var m)) return m;
        if (StampedMaterials.Count > 300) StampedMaterials.Clear();
        m = new ShaderMaterial { Shader = Shader };
        foreach (var name in new[] { "albedo", "wetness", "clouds", "broken", "seed", "glow" })
            m.SetShaderParameter(name, plain.GetShaderParameter(name));
        m.SetShaderParameter("stamp_atlas", MakerStamp.Atlas);
        m.SetShaderParameter("stamp_glyph", (float)slot);
        m.SetShaderParameter("stamp_grid", new Vector2(MakerStamp.Cols, MakerStamp.Rows));
        m.SetShaderParameter("stamp_color", color);
        float height = PotMesh.Height(form);
        m.SetShaderParameter("stamp_spot", new Vector2(height * 0.55f, Mathf.Min(0.075f, height * 0.3f)));
        StampedMaterials[key] = m;
        return m;
    }

    // The plain material, or one with the maker's mark when the item row says who made it.
    public static ShaderMaterial WithMark(ShaderMaterial plain, uint wear, ClayForm form)
    {
        if (!Marks.Has(wear) || form == ClayForm.Brick || MarkLookup?.Invoke(Marks.Maker(wear)) is not { } mark) return plain;
        return Stamped(plain, mark.slot, mark.color, form);
    }

    // A pot in the fire: smoke-darkened at first, then glowing orange as it fires
    // through. t is how far the firing has got, 0 to 1.
    public static ShaderMaterial Firing(float t, ulong id)
    {
        int step = Mathf.RoundToInt(Mathf.Clamp(t, 0f, 1f) * 10f);
        int seed = (int)(id % 3);
        var key = (100, step, seed);
        if (Materials.TryGetValue(key, out var m)) return m;
        m = new ShaderMaterial { Shader = Shader };
        float s = step / 10f;
        m.SetShaderParameter("albedo", Dry.Darkened(0.55f).Lerp(PitFired.Darkened(0.2f), s));
        m.SetShaderParameter("wetness", 0f);
        m.SetShaderParameter("clouds", 0.9f);
        m.SetShaderParameter("broken", 0f);
        m.SetShaderParameter("glow", 0.4f + 1.4f * Mathf.Sin(s * Mathf.Pi));
        m.SetShaderParameter("seed", seed * 7.3f);
        Materials[key] = m;
        return m;
    }

    public static ShaderMaterial Water => water ??= MakeWater();

    static ShaderMaterial MakeWater()
    {
        var m = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = """
                    shader_type spatial;
                    render_mode cull_disabled;
                    void fragment() {
                        float ripple = sin(TIME * 2.0 + length(UV - 0.5) * 40.0) * 0.5 + 0.5;
                        ALBEDO = vec3(0.10, 0.20, 0.22) + ripple * 0.02;
                        ROUGHNESS = 0.05;
                        METALLIC = 0.0;
                        SPECULAR = 0.7;
                    }
                    """,
            },
        };
        return m;
    }

    static Shader Shader => shader ??= new Shader
    {
        Code = """
            shader_type spatial;
            render_mode cull_back;

            uniform vec4 albedo : source_color = vec4(1.0);
            uniform float wetness = 0.0;
            uniform float clouds = 0.0;
            uniform float broken = 0.0;
            uniform float seed = 0.0;
            uniform float glow = 0.0;
            // A maker's mark (ceramics-15): the glyph's cell in the stamp atlas, or -1 for none.
            uniform sampler2D stamp_atlas : filter_linear;
            uniform float stamp_glyph = -1.0;
            uniform vec2 stamp_grid = vec2(8.0, 7.0);
            uniform vec3 stamp_color : source_color = vec3(0.7, 0.4, 0.12);
            uniform vec2 stamp_spot = vec2(0.095, 0.075); // height of the stamp's centre, and its half size

            varying vec3 local_pos;
            varying vec3 local_normal;
            varying float height;

            void vertex() {
                local_pos = VERTEX;
                local_normal = NORMAL;
                height = UV.y;
            }

            float hash(vec3 p) {
                p = fract(p * 0.3183099 + 0.1);
                p *= 17.0;
                return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(vec3 x) {
                vec3 i = floor(x), f = fract(x);
                f = f * f * (3.0 - 2.0 * f);
                return mix(mix(mix(hash(i), hash(i + vec3(1, 0, 0)), f.x),
                               mix(hash(i + vec3(0, 1, 0)), hash(i + vec3(1, 1, 0)), f.x), f.y),
                           mix(mix(hash(i + vec3(0, 0, 1)), hash(i + vec3(1, 0, 1)), f.x),
                               mix(hash(i + vec3(0, 1, 1)), hash(i + vec3(1, 1, 1)), f.x), f.y), f.z);
            }

            float fbm(vec3 p) {
                return noise(p) * 0.55 + noise(p * 2.1) * 0.3 + noise(p * 4.3) * 0.15;
            }

            void fragment() {
                vec3 p = local_pos * 22.0 + vec3(seed);

                // A broken pot loses a jagged bite out of its rim.
                if (broken > 0.5) {
                    float bite = fbm(vec3(local_pos.xz * 9.0, seed));
                    float edge = 1.0 - height;
                    if (bite > 0.58 && edge < (bite - 0.58) * 2.2) discard;
                }

                vec3 color = albedo.rgb;
                // Fine grain from the temper, and finger marks.
                float grain = noise(p * 1.6);
                color *= 0.95 + grain * 0.08;
                color *= 0.96 + fbm(vec3(p.x * 0.3, p.y * 2.5, p.z * 0.3)) * 0.08;

                // A maker's mark pressed into the outside of the pot, twice, on opposite sides, so
                // one is usually in view: dark in the dent, with a thin lighter lip.
                if (stamp_glyph >= 0.0 && dot(local_normal.xz, local_pos.xz) > 0.0) {
                    float radius = max(length(local_pos.xz), 0.001);
                    float angle = atan(local_pos.z, local_pos.x);
                    for (int side = 0; side < 2; side++) {
                        float centre = float(side) * 3.14159265;
                        float da = angle - centre;
                        da = atan(sin(da), cos(da));
                        vec2 uv = vec2(-da * radius / stamp_spot.y, -(local_pos.y - stamp_spot.x) / stamp_spot.y) * 0.5 + 0.5;
                        if (uv.x > 0.0 && uv.x < 1.0 && uv.y > 0.0 && uv.y < 1.0) {
                            float col = mod(stamp_glyph, stamp_grid.x);
                            float row = floor(stamp_glyph / stamp_grid.x);
                            vec2 cell = vec2((col + uv.x) / stamp_grid.x, (row + uv.y) / stamp_grid.y);
                            float ink = texture(stamp_atlas, cell).a;
                            // A pale lip round the dent, like light catching it, so dark colours still read.
                            vec2 step_uv = vec2(0.05, 0.05) / stamp_grid;
                            float near = max(max(texture(stamp_atlas, cell + vec2(step_uv.x, 0.0)).a, texture(stamp_atlas, cell - vec2(step_uv.x, 0.0)).a),
                                             max(texture(stamp_atlas, cell + vec2(0.0, step_uv.y)).a, texture(stamp_atlas, cell - vec2(0.0, step_uv.y)).a));
                            float lip = clamp(near - ink, 0.0, 1.0);
                            color = mix(color, color * 0.6 + vec3(0.42, 0.38, 0.32), lip * 0.7);
                            // Painted in the maker's colour, a touch darker where it sits in the clay.
                            color = mix(color, stamp_color * (0.92 + grain * 0.1), ink * 0.94);
                        }
                    }
                }

                // Smoke clouds from an open fire: soft dark patches, heavier low down.
                float cloud = smoothstep(0.38, 0.7, fbm(p * 0.25)) * clouds;
                cloud = max(cloud, smoothstep(0.35, 0.0, height) * clouds * 0.5);
                color = mix(color, vec3(0.13, 0.10, 0.08), cloud);

                // Dark cracks running down from the break.
                if (broken > 0.5) {
                    float n = fbm(p * 0.8);
                    float crack = 1.0 - smoothstep(0.0, 0.012, abs(n - 0.5));
                    crack *= smoothstep(0.1, 0.7, height);
                    color = mix(color, vec3(0.08, 0.06, 0.05), crack);
                }

                ALBEDO = color;
                // Wet clay glistens; dry and fired clay is matte.
                ROUGHNESS = mix(0.95, 0.32, wetness);
                SPECULAR = mix(0.15, 0.6, wetness);
                AO = mix(1.0, 0.75, smoothstep(0.2, 0.0, height));
                // Heat glowing through from the embers, strongest low down, flickering.
                if (glow > 0.0) {
                    float flicker = 0.75 + 0.25 * sin(TIME * 5.0 + seed + local_pos.y * 30.0);
                    float heat = glow * flicker * mix(1.0, 0.35, height) * (0.6 + 0.4 * fbm(p * 0.5 + vec3(TIME * 0.3)));
                    EMISSION = vec3(1.0, 0.36, 0.08) * heat;
                }
            }
            """,
    };
}
