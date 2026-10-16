using Godot;

namespace LifeGame;

// Low mist that gathers in the hollows around dawn and burns off by mid-morning.
// Two thin layers float just above the low ground; each is only as thick as the
// air between it and the ground below, so the mist pools in dips and by the water
// and thins to nothing on the rises. It drifts slowly with the wind.
public partial class Mist : Node3D
{
    // Heights of the two layers: low ground sits under them, rises poke through.
    static readonly float[] Layers = { -0.45f, -0.1f };
    const float Size = 140f;

    readonly ShaderMaterial material = new() { Shader = MistShader };
    readonly MeshInstance3D[] planes = new MeshInstance3D[Layers.Length];
    float drift;
    Vector2 offset;

    static readonly StringName NAmount = "amount", NColor = "mist_color", NOffset = "drift", NFocus = "focus";

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

    // amount 0 to 1; color is the light the mist glows with (the fog's light).
    public void Follow(Vector3 focus, float amount, Color color, float wind, float dt)
    {
        drift += dt;
        offset += new Vector2(0.6f, 0.8f) * (0.15f + wind * 0.8f) * dt;
        bool show = amount > 0.005f;
        for (int i = 0; i < planes.Length; i++)
        {
            planes[i].Visible = show;
            planes[i].GlobalPosition = new Vector3(Mathf.Round(focus.X), Layers[i], Mathf.Round(focus.Z));
        }
        if (!show) return;
        material.SetShaderParameter(NAmount, amount);
        material.SetShaderParameter(NColor, color);
        material.SetShaderParameter(NOffset, offset);
        material.SetShaderParameter(NFocus, new Vector2(focus.X, focus.Z));
    }

    // How misty it is at this time of day (0 midnight, 0.25 sunrise): gathering in
    // the last of the night, thickest just after sunrise, gone by mid-morning.
    // Wind stirs it away and rain beats it down.
    public static float Amount(float timeOfDay, float wind, float rain) =>
        Mathf.SmoothStep(0.16f, 0.24f, timeOfDay) * (1f - Mathf.SmoothStep(0.29f, 0.38f, timeOfDay))
        * Mathf.Clamp(1.3f - wind * 1.6f, 0f, 1f) * (1f - 0.6f * Mathf.Clamp(rain * 2f, 0f, 1f));

    static readonly Shader MistShader = new()
    {
        Code = """
            shader_type spatial;
            render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;

            uniform float amount = 0.0;
            uniform vec4 mist_color : source_color = vec4(0.85, 0.87, 0.88, 1.0);
            uniform vec2 drift;
            uniform vec2 focus;
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

                float pool = smoothstep(0.0, 1.4, thick);
                // Fade toward the edge of the layer so it never shows a border.
                float edge = 1.0 - smoothstep(45.0, 68.0, length(world_pos.xz - focus));
                ALBEDO = mist_color.rgb;
                ALPHA = clamp(amount * pool * (0.15 + 0.85 * wisps) * edge * 0.42, 0.0, 0.6);
            }
            """,
    };
}
