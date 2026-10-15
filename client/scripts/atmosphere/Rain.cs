using Godot;

namespace LifeGame;

// Rain as soft grey veils rather than individual streaks: three rings of falling
// rain around the player, near, middle and far, each a faint scrolling curtain
// that fades into the ground and the fog. Heavy rain closes the world in.
public partial class Rain : Node3D
{
    static readonly (float Radius, float Height, float Strength, float Scale, float Speed)[] Rings =
    {
        (6f, 18f, 0.55f, 1.4f, 1.15f),
        (12f, 26f, 0.8f, 1.0f, 1.0f),
        (24f, 40f, 1.0f, 0.6f, 0.85f),
    };

    readonly ShaderMaterial[] materials = new ShaderMaterial[Rings.Length];

    public Rain()
    {
        var streaks = new NoiseTexture2D
        {
            Width = 256,
            Height = 256,
            Seamless = true,
            GenerateMipmaps = true,
            Noise = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex, Frequency = 0.08f, FractalOctaves = 2 },
        };
        for (int i = 0; i < Rings.Length; i++)
        {
            var (radius, height, strength, scale, speed) = Rings[i];
            var material = new ShaderMaterial { Shader = VeilShader };
            material.SetShaderParameter("streaks", streaks);
            material.SetShaderParameter("strength", strength);
            material.SetShaderParameter("scale", scale);
            material.SetShaderParameter("speed", speed);
            material.SetShaderParameter("half_height", height / 2f);
            materials[i] = material;
            AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 48, Rings = 1, CapTop = false, CapBottom = false },
                MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Position = new Vector3(0, height / 2f - 4f, 0),
            });
        }
        Visible = false;
    }

    // amount and wind run from 0 to 1; tint is the color of the air.
    public void Follow(Vector3 focus, float amount, float wind, Color tint)
    {
        GlobalPosition = focus;
        Visible = amount > 0.02f;
        foreach (var material in materials)
        {
            material.SetShaderParameter("amount", Mathf.Clamp(amount, 0f, 1f));
            material.SetShaderParameter("slant", 0.05f + wind * 0.25f);
            material.SetShaderParameter("tint", tint);
        }
    }

    static readonly Shader VeilShader = new()
    {
        Code = """
            shader_type spatial;
            render_mode unshaded, cull_front, depth_draw_never, shadows_disabled, fog_disabled;

            uniform sampler2D streaks : repeat_enable, filter_linear_mipmap;
            uniform sampler2D depth_texture : hint_depth_texture, filter_linear;
            uniform vec3 tint : source_color = vec3(0.7);
            uniform float amount = 0.0;
            uniform float strength = 1.0;
            uniform float scale = 1.0;
            uniform float speed = 1.0;
            uniform float slant = 0.1;
            uniform float half_height = 10.0;

            varying vec3 local;

            void vertex() {
                local = VERTEX;
            }

            void fragment() {
                // Measure around the ring from the side facing away from the camera,
                // so the seam sits on the near side, which is never drawn.
                vec2 away = normalize(NODE_POSITION_WORLD.xz - CAMERA_POSITION_WORLD.xz + vec2(0.0001));
                vec2 l = local.xz;
                float angle = atan(away.x * l.y - away.y * l.x, dot(away, l));
                float around = angle * length(l);

                vec2 uv = vec2(around * 0.22 * scale + local.y * slant * 0.22, local.y * 0.018 * scale + TIME * 0.55 * speed);
                float a = texture(streaks, uv).r;
                float b = texture(streaks, uv * vec2(1.9, 1.4) + vec2(0.37, TIME * 0.21 * speed)).r;
                float rain = smoothstep(0.55, 0.85, a) * 0.6 + smoothstep(0.6, 0.9, b) * 0.4;

                // Soft at the top and bottom of the ring, and wherever the ground or a
                // tree is just behind it, so it never shows a hard edge.
                float edge = smoothstep(-half_height, -half_height + 3.0, local.y) * (1.0 - smoothstep(half_height - 6.0, half_height, local.y));
                float depth = texture(depth_texture, SCREEN_UV).r;
                vec4 view = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, depth, 1.0);
                float behind = -view.z / view.w - (-VERTEX.z);
                float soft = clamp(behind / 3.0, 0.0, 1.0);

                ALBEDO = tint;
                ALPHA = amount * strength * (0.05 + rain * 0.16) * edge * soft;
            }
            """,
    };
}
