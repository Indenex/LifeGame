using Godot;

namespace LifeGame;

// The art direction in one place. The look is hand-painted isometric: warm,
// saturated yellow-greens, soft banded light instead of smooth shading, and
// brush-like color variation laid over every surface in world space.
public static class Look
{
    public static readonly Color GrassLow = new(0.40f, 0.60f, 0.18f);
    public static readonly Color GrassHigh = new(0.62f, 0.74f, 0.22f);
    public static readonly Color GrassBlade = new(0.80f, 0.85f, 0.36f);
    public static readonly Color Clay = new(0.64f, 0.43f, 0.31f);
    public static readonly Color Earth = new(0.37f, 0.28f, 0.23f);
    public static readonly Color DugEarth = new(0.34f, 0.24f, 0.17f);
    public static readonly Color Shore = new(0.80f, 0.74f, 0.52f);
    public static readonly Color Bark = new(0.33f, 0.22f, 0.20f);
    public static readonly Color Pine = new(0.24f, 0.36f, 0.26f);
    public static readonly Color Leaf = new(0.44f, 0.54f, 0.26f);
    public static readonly Color LeafLight = new(0.70f, 0.74f, 0.42f);
    public static readonly Color LeafWarm = new(0.82f, 0.62f, 0.24f);
    public static readonly Color Rock = new(0.58f, 0.58f, 0.56f);
    public static readonly Color Skin = new(0.88f, 0.68f, 0.52f);
    public static readonly Color Hair = new(0.30f, 0.20f, 0.14f);
    public static readonly Color Cream = new(0.97f, 0.94f, 0.87f);
    public static readonly Color Water = new(0.34f, 0.58f, 0.62f, 0.93f);

    public const float WaterLevel = -1.0f;

    static Shader? paintedShader, grassShader;
    static ShaderMaterial? painted, swaying, grass;
    static NoiseTexture2D? brush;

    // Every world surface uses the painted shader. Colors come from the mesh, the
    // multimesh instance, or the albedo parameter.
    public static ShaderMaterial Painted => painted ??= Make(0f, 1f);

    // Foliage that drifts gently in the wind. Higher vertices move more.
    public static ShaderMaterial Swaying => swaying ??= Make(0.08f, 3.0f);

    // Grass has its own shader: blades take their tile's color at the root, so the
    // carpet and the ground read as one surface, and rolling gusts sweep across it.
    public static ShaderMaterial Grass => grass ??= new ShaderMaterial { Shader = GrassShader };

    public static ShaderMaterial Solid(Color color)
    {
        var material = Make(0f, 1f);
        material.SetShaderParameter("albedo", color);
        return material;
    }

    // A primitive mesh with low-poly facets, drawn in one flat color.
    public static Mesh Faceted(PrimitiveMesh mesh, Color color)
    {
        var st = new SurfaceTool();
        st.CreateFrom(mesh, 0);
        st.Deindex();
        st.GenerateNormals();
        var result = st.Commit();
        result.SurfaceSetMaterial(0, Solid(color));
        return result;
    }

    static ShaderMaterial Make(float sway, float swayHeight)
    {
        var material = new ShaderMaterial { Shader = PaintedShader };
        material.SetShaderParameter("sway", sway);
        material.SetShaderParameter("sway_height", swayHeight);
        material.SetShaderParameter("brush", Brush);
        return material;
    }

    static NoiseTexture2D Brush => brush ??= new NoiseTexture2D
    {
        Width = 256,
        Height = 256,
        Seamless = true,
        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
            Frequency = 0.035f,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = 3,
            CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.CellValue,
            DomainWarpEnabled = true,
            DomainWarpAmplitude = 18f,
        },
    };

    static Shader PaintedShader => paintedShader ??= new Shader
    {
        Code = """
            shader_type spatial;
            render_mode cull_disabled, specular_disabled;

            uniform vec4 albedo : source_color = vec4(1.0);
            uniform sampler2D brush : repeat_enable, filter_linear;
            uniform float sway = 0.0;
            uniform float sway_height = 1.0;
            uniform float brush_scale = 0.09;
            uniform float brush_strength = 0.22;

            varying vec3 tint;
            varying vec3 world_pos;

            void vertex() {
                vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
                if (sway > 0.0) {
                    float h = clamp(VERTEX.y / sway_height, 0.0, 1.5);
                    float t = TIME * 0.9 + world.x * 0.21 + world.z * 0.17;
                    float gust = 0.6 + 0.4 * sin(TIME * 0.23 + world.x * 0.03);
                    VERTEX.x += sin(t) * sway * h * h * gust;
                    VERTEX.z += cos(t * 0.73) * sway * 0.6 * h * h * gust;
                }
                world_pos = world;
                // Palette colors are authored in sRGB.
                tint = pow(COLOR.rgb, vec3(2.2)) * albedo.rgb;
            }

            void fragment() {
                // Two layers of world-space brush noise, so neighboring tiles and
                // leaves never look like copies of each other.
                vec2 uv = world_pos.xz * brush_scale + vec2(world_pos.y * 0.21);
                float a = texture(brush, uv).r;
                float b = texture(brush, uv * 2.7 + vec2(0.31, 0.77)).r;
                float paint = (a - 0.5) * brush_strength + (b - 0.5) * brush_strength * 0.6;
                ALBEDO = clamp(tint * (1.0 + paint * 2.0), 0.0, 1.0);
                ROUGHNESS = 1.0;
                SPECULAR = 0.0;
            }

            // Soft banded light: a lit tone, a mid tone and a shadow tone, laid
            // down the way a painter would.
            void light() {
                float ndl = dot(NORMAL, LIGHT);
                float wrapped = clamp((ndl + 0.25) / 1.25, 0.0, 1.0);
                float bands = smoothstep(0.12, 0.2, wrapped) * 0.55 + smoothstep(0.5, 0.58, wrapped) * 0.45;
                float shadow = smoothstep(0.25, 0.6, ATTENUATION);
                DIFFUSE_LIGHT += LIGHT_COLOR * bands * shadow / PI;
            }
            """,
    };

    static Shader GrassShader => grassShader ??= new Shader
    {
        Code = """
            shader_type spatial;
            render_mode cull_disabled, specular_disabled;

            varying vec3 tint;
            varying float tip;
            varying float gust;

            void vertex() {
                vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
                tip = UV.y;
                // Long, slow waves rolling across the field, plus a small flutter.
                float wave = sin(dot(world.xz, vec2(0.23, 0.31)) - TIME * 1.3);
                gust = smoothstep(0.2, 1.0, wave);
                float flutter = sin(TIME * 3.1 + world.x * 1.7 + world.z * 2.3) * 0.03;
                vec3 bend = vec3(0.6, 0.0, 0.8) * (0.06 + gust * 0.16) + vec3(flutter, 0.0, -flutter);
                vec3 local = inverse(mat3(MODEL_MATRIX)) * bend;
                VERTEX.xz += local.xz * tip * tip;
                VERTEX.y -= gust * 0.06 * tip * tip;
                NORMAL = vec3(0.0, 1.0, 0.0);
                tint = pow(COLOR.rgb, vec3(2.2));
            }

            void fragment() {
                // Roots match the ground; tips catch the sun and turn warm.
                vec3 warm = tint * vec3(1.3, 1.3, 1.0) + vec3(0.02, 0.02, 0.0);
                vec3 color = mix(tint, warm, smoothstep(0.0, 1.0, tip));
                color += vec3(0.05, 0.05, 0.02) * gust * tip;
                ALBEDO = clamp(color, 0.0, 1.0);
                // Both faces of a blade are lit like the ground beneath it.
                NORMAL = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
                ROUGHNESS = 1.0;
                SPECULAR = 0.0;
            }

            void light() {
                float wrapped = clamp((dot(NORMAL, LIGHT) + 0.25) / 1.25, 0.0, 1.0);
                float bands = smoothstep(0.12, 0.2, wrapped) * 0.55 + smoothstep(0.5, 0.58, wrapped) * 0.45;
                float shadow = smoothstep(0.25, 0.6, ATTENUATION);
                DIFFUSE_LIGHT += LIGHT_COLOR * bands * shadow / PI;
            }
            """,
    };
}
