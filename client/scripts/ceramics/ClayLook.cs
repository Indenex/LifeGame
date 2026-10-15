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
    public static readonly Color KilnFired = new(0.72f, 0.33f, 0.19f);

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

            varying vec3 local_pos;
            varying float height;

            void vertex() {
                local_pos = VERTEX;
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
            }
            """,
    };
}
