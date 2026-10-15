using Godot;

namespace LifeGame;

// The art direction in one place. Simple low-poly shapes under rich, moody
// light: deep greens, soft wrapped shading, and subtle world-space color
// variation so no two surfaces look like copies.
public static class Look
{
    public static readonly Color GrassLow = new(0.30f, 0.46f, 0.13f);
    public static readonly Color GrassHigh = new(0.48f, 0.58f, 0.17f);
    public static readonly Color GrassBlade = new(0.55f, 0.64f, 0.22f);
    public static readonly Color Clay = new(0.64f, 0.43f, 0.31f);
    public static readonly Color Earth = new(0.32f, 0.25f, 0.19f);
    public static readonly Color DugEarth = new(0.32f, 0.25f, 0.19f);
    public static readonly Color Shore = new(0.80f, 0.74f, 0.52f);
    public static readonly Color Bark = new(0.33f, 0.22f, 0.20f);
    public static readonly Color Pine = new(0.13f, 0.22f, 0.15f);
    public static readonly Color Leaf = new(0.25f, 0.36f, 0.13f);
    public static readonly Color LeafLight = new(0.42f, 0.50f, 0.20f);
    public static readonly Color LeafWarm = new(0.66f, 0.50f, 0.18f);
    public static readonly Color Rock = new(0.46f, 0.47f, 0.46f);
    public static readonly Color Skin = new(0.88f, 0.68f, 0.52f);
    public static readonly Color Hair = new(0.30f, 0.20f, 0.14f);
    public static readonly Color Cream = new(0.97f, 0.94f, 0.87f);
    public static readonly Color Water = new(0.16f, 0.30f, 0.34f, 0.9f);

    public const float WaterLevel = -1.0f;

    static Shader? paintedShader, grassShader;
    static ShaderMaterial? ground, bark, rock, foliage, grass;
    static NoiseTexture2D? brush, detail;

    // Each surface family gets the same shader with its own detail: how fine the
    // surface relief is, how deep it reads, and which way it stretches.
    public static ShaderMaterial Ground => ground ??= Make(0f, 1f, detailScale: 0.45f, bump: 1.0f, cavity: 0.18f);
    public static ShaderMaterial BarkSurface => bark ??= Make(0f, 1f, detailScale: 2.0f, bump: 1.2f, cavity: 0.3f, stretch: new Vector3(4f, 0.35f, 4f));
    public static ShaderMaterial RockSurface => rock ??= Make(0f, 1f, detailScale: 1.0f, bump: 1.5f, cavity: 0.22f);
    public static ShaderMaterial Foliage => foliage ??= Make(0.08f, 3.0f, detailScale: 1.6f, bump: 1.5f, cavity: 0.3f);

    // Kept for callers that only need a plain painted surface.
    public static ShaderMaterial Painted => Ground;
    public static ShaderMaterial Swaying => Foliage;

    // Grass has its own shader: blades take their tile's color at the root, so the
    // carpet and the ground read as one surface, and rolling gusts sweep across it.
    public static ShaderMaterial Grass => grass ??= new ShaderMaterial { Shader = GrassShader };

    public static ShaderMaterial Solid(Color color)
    {
        var material = Make(0f, 1f, detailScale: 1f, bump: 0f, cavity: 0f);
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

    static ShaderMaterial Make(float sway, float swayHeight, float detailScale, float bump, float cavity, Vector3? stretch = null)
    {
        var material = new ShaderMaterial { Shader = PaintedShader };
        material.SetShaderParameter("sway", sway);
        material.SetShaderParameter("sway_height", swayHeight);
        material.SetShaderParameter("brush", Brush);
        material.SetShaderParameter("detail", Detail);
        material.SetShaderParameter("detail_scale", detailScale);
        material.SetShaderParameter("detail_stretch", stretch ?? Vector3.One);
        material.SetShaderParameter("bump", bump);
        material.SetShaderParameter("cavity", cavity);
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

    // Fine relief: bumps, cracks and clumps that catch the low sun.
    static NoiseTexture2D Detail => detail ??= new NoiseTexture2D
    {
        Width = 512,
        Height = 512,
        Seamless = true,
        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
            Frequency = 0.02f,
            FractalType = FastNoiseLite.FractalTypeEnum.Ridged,
            FractalOctaves = 5,
            FractalGain = 0.55f,
        },
    };

    static Shader PaintedShader => paintedShader ??= new Shader
    {
        Code = """
            shader_type spatial;
            render_mode cull_disabled, specular_disabled;

            uniform vec4 albedo : source_color = vec4(1.0);
            uniform sampler2D brush : repeat_enable, filter_linear;
            uniform sampler2D detail : repeat_enable, filter_linear_mipmap;
            uniform float sway = 0.0;
            uniform float sway_height = 1.0;
            uniform float brush_scale = 0.09;
            uniform float brush_strength = 0.14;
            uniform float detail_scale = 1.0;
            uniform vec3 detail_stretch = vec3(1.0);
            uniform float bump = 1.0;
            uniform float cavity = 0.4;

            varying vec3 tint;
            varying vec3 world_pos;
            varying vec3 world_normal;

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
                world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
                // Palette colors are authored in sRGB.
                tint = pow(COLOR.rgb, vec3(2.2)) * albedo.rgb;
            }

            // World-space detail projected from three sides, so it wraps any shape
            // without UVs.
            float relief(vec3 p, vec3 n) {
                vec3 w = pow(abs(n), vec3(4.0));
                w /= (w.x + w.y + w.z);
                vec3 q = p * detail_stretch * detail_scale * 0.25;
                return texture(detail, q.zy).r * w.x + texture(detail, q.xz).r * w.y + texture(detail, q.xy).r * w.z;
            }

            void fragment() {
                vec2 uv = world_pos.xz * brush_scale + vec2(world_pos.y * 0.21);
                float a = texture(brush, uv).r;
                float b = texture(brush, uv * 2.7 + vec2(0.31, 0.77)).r;
                float paint = (a - 0.5) * brush_strength + (b - 0.5) * brush_strength * 0.6;

                // Relief darkens the crevices and bends the light around bumps.
                float h = relief(world_pos, world_normal);
                vec3 color = tint * (1.0 + paint * 2.0);
                color *= mix(1.0 - cavity, 1.0 + cavity * 0.35, smoothstep(0.15, 0.75, h));
                ALBEDO = clamp(color, 0.0, 1.0);

                vec3 dpdx = dFdx(VERTEX), dpdy = dFdy(VERTEX);
                vec3 r1 = cross(dpdy, NORMAL), r2 = cross(NORMAL, dpdx);
                float det = dot(dpdx, r1);
                vec3 grad = sign(det) * (dFdx(h) * r1 + dFdy(h) * r2);
                // Relief height is in tenths of a meter at most, so bumps stay subtle.
                NORMAL = normalize(abs(det) * NORMAL - grad * bump * 0.035);

                AO = mix(1.0, smoothstep(0.0, 0.6, h), cavity * 0.6);
                AO_LIGHT_AFFECT = 0.3;
                ROUGHNESS = 1.0;
                SPECULAR = 0.0;
            }

            // Wrapped diffuse light, so forms turn softly into shadow.
            void light() {
                float wrapped = clamp((dot(NORMAL, LIGHT) + 0.3) / 1.3, 0.0, 1.0);
                DIFFUSE_LIGHT += LIGHT_COLOR * wrapped * wrapped * ATTENUATION / PI;
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
                float wrapped = clamp((dot(NORMAL, LIGHT) + 0.3) / 1.3, 0.0, 1.0);
                DIFFUSE_LIGHT += LIGHT_COLOR * wrapped * wrapped * ATTENUATION / PI;
            }
            """,
    };
}
