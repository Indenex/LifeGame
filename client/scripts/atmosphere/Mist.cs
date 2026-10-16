using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Low mist that gathers in the hollows around dawn and burns off by mid-morning,
// and returns faintly on still, damp evenings. Two thin layers float just above
// the low ground around the player (a low percentile of the land's height nearby,
// so a valley among hills fills as well as the pond shores); each is only as thick
// as the air between it and the ground below, so the mist pools in dips, valleys
// and by the water and thins to nothing on the rises. It drifts with the wind.
public partial class Mist : Node3D
{
    // Heights of the layers above the low ground nearby: low ground sits under them,
    // rises poke through. The top one makes the mist knee- to waist-deep in a hollow,
    // so it reads from across a valley, not only underfoot (atmosphere-23).
    static readonly float[] Layers = { 0.5f, 0.95f, 1.5f };
    const float Size = 140f;

    readonly ShaderMaterial material = new() { Shader = MistShader };
    readonly MeshInstance3D[] planes = new MeshInstance3D[Layers.Length];
    float drift, lookIn, low = Look.WaterLevel, lowTarget = Look.WaterLevel;
    Vector2 offset;
    readonly float[] samples = new float[225];

    static readonly StringName NAmount = "amount", NColor = "mist_color", NOffset = "drift", NFocus = "focus";
    static readonly StringName NSunDir = "sun_dir", NGlow = "glow_color";

    public override void _Ready()
    {
        material.SetShaderParameter("noise", Look.Clouds);
        for (int i = 0; i < Layers.Length; i++)
        {
            planes[i] = new MeshInstance3D
            {
                Mesh = new PlaneMesh { Size = new Vector2(Size, Size) },
                MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false,
            };
            AddChild(planes[i]);
        }
    }

    // amount 0 to 1; color is the light the mist glows with (the fog's light);
    // sunDir points toward the sun, and glow is the sunlight that lights the mist up
    // when you look toward it (black when the sun is down).
    public void Follow(Vector3 focus, float amount, Color color, Vector3 sunDir, Color glow, float wind, float dt)
    {
        drift += dt;
        offset += new Vector2(0.6f, 0.8f) * (0.15f + wind * 0.8f) * dt;
        bool show = amount > 0.005f;
        if (show)
        {
            lookIn -= dt;
            if (lookIn <= 0f)
            {
                lookIn = 0.5f;
                lowTarget = LowGround(focus);
            }
            low = Mathf.Lerp(low, lowTarget, 1f - Mathf.Exp(-dt / 3f));
        }
        for (int i = 0; i < planes.Length; i++)
        {
            planes[i].Visible = show;
            planes[i].GlobalPosition = new Vector3(Mathf.Round(focus.X), low + Layers[i], Mathf.Round(focus.Z));
        }
        if (!show) return;
        material.SetShaderParameter(NAmount, amount);
        material.SetShaderParameter(NColor, color);
        material.SetShaderParameter(NOffset, offset);
        material.SetShaderParameter(NFocus, new Vector2(focus.X, focus.Z));
        material.SetShaderParameter(NSunDir, sunDir);
        material.SetShaderParameter(NGlow, glow);
    }

    // The low ground around a point: the 20th percentile of the land's height over a
    // 84 m square (a 15 by 15 grid, 6 m apart), never below the water. The water
    // counts at its surface, so ponds and the stream are low ground too.
    float LowGround(Vector3 focus)
    {
        int cx = World.CellOf(focus.X), cz = World.CellOf(focus.Z), n = 0;
        for (int dx = -7; dx <= 7; dx++)
            for (int dz = -7; dz <= 7; dz++)
                samples[n++] = Math.Max(World.BaseHeight(cx + dx * 6, cz + dz * 6), Look.WaterLevel);
        Array.Sort(samples, 0, n);
        return samples[n / 5];
    }

    // How misty it is at this time of day (0 midnight, 0.25 sunrise): gathering in
    // the last of the night, thickest just after sunrise, gone by mid-morning; and a
    // thinner mist settling after sunset when the ground is damp. Wind stirs it away
    // and rain beats it down.
    public static float Amount(float timeOfDay, float wind, float rain, float wetness = 0f)
    {
        float dawn = Mathf.SmoothStep(0.16f, 0.24f, timeOfDay) * (1f - Mathf.SmoothStep(0.29f, 0.38f, timeOfDay));
        float dusk = Mathf.SmoothStep(0.75f, 0.81f, timeOfDay) * (1f - Mathf.SmoothStep(0.88f, 0.95f, timeOfDay))
            * 0.45f * Mathf.SmoothStep(0.2f, 0.7f, wetness);
        return Mathf.Max(dawn, dusk) * Mathf.Clamp(1.3f - wind * 1.6f, 0f, 1f) * (1f - 0.6f * Mathf.Clamp(rain * 2f, 0f, 1f));
    }

    static readonly Shader MistShader = new()
    {
        Code = """
            shader_type spatial;
            render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;

            uniform float amount = 0.0;
            uniform vec4 mist_color : source_color = vec4(0.85, 0.87, 0.88, 1.0);
            uniform vec2 drift;
            uniform vec2 focus;
            uniform vec3 sun_dir = vec3(0.0, 1.0, 0.0);
            uniform vec3 glow_color : source_color = vec3(0.0);
            uniform sampler2D noise : repeat_enable, filter_linear_mipmap;
            uniform sampler2D depth_texture : hint_depth_texture, filter_linear;
            varying vec3 world_pos;

            void vertex() {
                world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
            }

            void fragment() {
                // The ground below this point of the layer, from the depth buffer.
                float depth = textureLod(depth_texture, SCREEN_UV, 0.0).r;
                vec4 behind = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, depth, 1.0);
                behind.xyz /= behind.w;
                vec3 ground = (INV_VIEW_MATRIX * vec4(behind.xyz, 1.0)).xyz;
                float thick = world_pos.y - ground.y;
                if (thick <= 0.0) discard;

                // Wisps: two scales of slow-moving noise.
                float a = texture(noise, world_pos.xz * 0.018 + drift * 0.02).r;
                float b = texture(noise, world_pos.xz * 0.05 - drift * 0.035 + 0.37).r;
                float wisps = smoothstep(0.3, 0.9, a * 0.6 + b * 0.4);

                float pool = smoothstep(0.0, 1.8, thick);
                // Fade toward the edge of the layer so it never shows a border.
                float edge = 1.0 - smoothstep(45.0, 68.0, length(world_pos.xz - focus));
                // Looking toward a low sun, the mist lights up warm and bright.
                vec3 look = normalize(world_pos - CAMERA_POSITION_WORLD);
                float scatter = pow(max(dot(look, normalize(sun_dir)), 0.0), 6.0);
                ALBEDO = mist_color.rgb + glow_color * scatter * 0.9;
                ALPHA = clamp(amount * pool * (0.2 + 0.8 * wisps) * edge * 0.55 * (1.0 + 0.35 * scatter), 0.0, 0.72);
            }
            """,
    };
}
