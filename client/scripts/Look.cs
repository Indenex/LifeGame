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

    static Shader? paintedShader, ghostShader, grassShader;
    static ShaderMaterial? ground, bark, rock, foliage, grass;
    static NoiseTexture2D? brush, detail, clouds;

    // Weather every surface reads, set each frame by the atmosphere: how soaked the
    // ground is, how hard the wind blows, how much sky the clouds cover and where
    // their shadows have drifted to.
    static bool weatherGlobalsAdded;

    static void EnsureWeatherGlobals()
    {
        if (weatherGlobalsAdded) return;
        weatherGlobalsAdded = true;
        static void Add(string name, RenderingServer.GlobalShaderParameterType type, Variant value) =>
            RenderingServer.GlobalShaderParameterAdd(name, type, value);
        Add("wetness", RenderingServer.GlobalShaderParameterType.Float, 0f);
        Add("wind_strength", RenderingServer.GlobalShaderParameterType.Float, 0.3f);
        Add("cloud_cover", RenderingServer.GlobalShaderParameterType.Float, 0.2f);
        Add("cloud_offset", RenderingServer.GlobalShaderParameterType.Vec2, Vector2.Zero);
        Add("player_position", RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(0, -1000, 0));
        Add("view_camera_position", RenderingServer.GlobalShaderParameterType.Vec3, Vector3.Zero);
        Add("world_seconds", RenderingServer.GlobalShaderParameterType.Float, 1f);
    }

    // Seconds since the game started (from 1), shared with the shaders as
    // world_seconds so changes to the world can ease in over time. The atmosphere
    // advances it every frame.
    public static float Clock { get; private set; } = 1f;

    public static void Tick(float delta)
    {
        Clock += delta;
        if (weatherGlobalsAdded) RenderingServer.GlobalShaderParameterSet("world_seconds", Clock);
    }

    // Each surface family gets the same shader with its own detail: how fine the
    // surface relief is, how deep it reads, and which way it stretches.
    public static ShaderMaterial Ground => ground ??= Make(0f, 1f, detailScale: 0.45f, bump: 1.0f, cavity: 0.18f);
    public static ShaderMaterial BarkSurface => bark ??= Make(0f, 1f, detailScale: 2.0f, bump: 1.2f, cavity: 0.3f, stretch: new Vector3(4f, 0.35f, 4f));
    public static ShaderMaterial RockSurface => rock ??= Make(0f, 1f, detailScale: 1.0f, bump: 1.5f, cavity: 0.22f);
    public static ShaderMaterial Foliage => foliage ??= Make(0.08f, 3.0f, detailScale: 1.6f, bump: 1.5f, cavity: 0.3f);

    // Trees between the camera and the player fade softly, so you can always see
    // yourself in the woods. A faded tree is drawn by its ghost: the same surface,
    // transparent, drawn by a second instance that only shows trees in the way.
    // Anything drawn with a see-through material needs its ghost drawn too, or a
    // tree in the way would just vanish.
    public static ShaderMaterial TreeBark => treeBark ??= SeeThrough(BarkSurface);
    public static ShaderMaterial TreeFoliage => treeFoliage ??= SeeThrough(Foliage);

    static ShaderMaterial? treeBark, treeFoliage, treeBarkGhost, treeFoliageGhost;

    static ShaderMaterial SeeThrough(ShaderMaterial material)
    {
        var copy = (ShaderMaterial)material.Duplicate();
        copy.SetShaderParameter("see_through", true);
        return copy;
    }

    // The ghost to draw alongside a see-through material, if it has one.
    public static ShaderMaterial? GhostOf(Material material) =>
        material == TreeFoliage ? treeFoliageGhost ??= Ghost(TreeFoliage) :
        material == TreeBark ? treeBarkGhost ??= Ghost(TreeBark) : null;

    static ShaderMaterial Ghost(ShaderMaterial source)
    {
        var ghost = new ShaderMaterial { Shader = GhostShader };
        foreach (var uniform in source.Shader.GetShaderUniformList())
        {
            var name = new StringName(uniform.AsGodotDictionary()["name"].AsString());
            ghost.SetShaderParameter(name, source.GetShaderParameter(name));
        }
        return ghost;
    }

    // Kept for callers that only need a plain painted surface.
    public static ShaderMaterial Painted => Ground;
    public static ShaderMaterial Swaying => Foliage;

    // Grass has its own shader: blades take their tile's color at the root, so the
    // carpet and the ground read as one surface, and rolling gusts sweep across it.
    public static ShaderMaterial Grass => grass ??= MakeGrass();

    static ShaderMaterial MakeGrass()
    {
        var material = new ShaderMaterial { Shader = GrassShader };
        material.SetShaderParameter("clouds", Clouds);
        return material;
    }

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
        material.SetShaderParameter("clouds", Clouds);
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

    // Soft, billowy shapes for the shadows of passing clouds.
    public static NoiseTexture2D Clouds => clouds ??= new NoiseTexture2D
    {
        Width = 512,
        Height = 512,
        Seamless = true,
        GenerateMipmaps = true,
        Noise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            Frequency = 0.006f,
            FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
            FractalOctaves = 5,
            FractalGain = 0.5f,
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

    // Weather shared by both shaders: cloud shadows only dim direct sunlight, and
    // wet surfaces darken and pick up a sheen.
    const string WeatherCode = """
        global uniform float wetness;
        global uniform float wind_strength;
        global uniform float cloud_cover;
        global uniform vec2 cloud_offset;
        global uniform float world_seconds;
        uniform sampler2D clouds : repeat_enable, filter_linear_mipmap;

        float cloud_shadow(vec3 p) {
            if (cloud_cover <= 0.01) return 1.0;
            float n = texture(clouds, (p.xz + cloud_offset) * 0.004).r;
            float edge = 1.0 - cloud_cover;
            return 1.0 - smoothstep(edge - 0.08, edge + 0.22, n) * 0.6;
        }
        """;

    static Shader PaintedShader => paintedShader ??= MakeShader(PaintedCode);

    // The same surface drawn transparent, for trees fading out of the way.
    static Shader GhostShader => ghostShader ??= MakeShader(PaintedCode
        .Replace("render_mode cull_disabled;", "render_mode cull_back, depth_draw_always;")
        .Replace("shader_type spatial;", "shader_type spatial;\n#define GHOST"));

    const string PaintedCode = """
            shader_type spatial;
            render_mode cull_disabled;

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
            uniform bool see_through = false;
            global uniform vec3 player_position;
            global uniform vec3 view_camera_position;

            // How far a tree has faded because it stands between the camera and the
            // player: 0 solid, 1 fully in the way. Measured from the line of sight to
            // the tree's own axis, so the whole tree eases in and out together as
            // either moves. Only the main camera fades things, so shadows stay whole.
            float fade_amount(mat4 model, vec3 cam) {
                if (distance(cam, view_camera_position) > 0.5) return 0.0;
                float size = length(model[1].xyz);
                vec3 base = model[3].xyz;
                vec3 p1 = cam, d1 = player_position + vec3(0.0, 1.0, 0.0) - cam;
                vec3 p2 = base + vec3(0.0, 0.5 * size, 0.0), d2 = vec3(0.0, 3.5 * size, 0.0);
                vec3 r = p1 - p2;
                float a = dot(d1, d1), b = dot(d1, d2), c = dot(d1, r), e = dot(d2, d2), f = dot(d2, r);
                float denom = a * e - b * b;
                float s = denom > 1e-5 ? clamp((b * f - c * e) / denom, 0.0, 1.0) : 0.0;
                float t = clamp((b * s + f) / e, 0.0, 1.0);
                s = clamp((b * t - c) / a, 0.0, 1.0);
                float d = distance(p1 + d1 * s, p2 + d2 * t);
                return (1.0 - smoothstep(0.9 * size, 1.9 * size, d)) * (1.0 - smoothstep(0.82, 0.97, s));
            }

            varying vec3 tint;
            varying float fade;
            varying vec3 world_pos;
            varying vec3 world_normal;
            varying float shade;
            varying float wet;

            void vertex() {
                fade = see_through ? fade_amount(MODEL_MATRIX, CAMERA_POSITION_WORLD) : 0.0;
                // A tree or rock on a cell that was just cleared goes away instead of
                // vanishing: trees topple slowly and settle into the ground, rocks sink.
                // On a cell that comes back they grow in. Custom data: x when it started,
                // y +1 going or -1 coming, z 1 for rocks.
                if (INSTANCE_CUSTOM.x > 0.0) {
                    float age = world_seconds - INSTANCE_CUSTOM.x;
                    if (INSTANCE_CUSTOM.y < 0.0) {
                        VERTEX *= smoothstep(0.0, 1.6, age);
                    } else if (INSTANCE_CUSTOM.z > 0.5) {
                        float k = smoothstep(0.0, 1.6, age);
                        VERTEX.y -= k * 1.4;
                        VERTEX *= 1.0 - k * k;
                    } else {
                        float k = clamp(age / 2.4, 0.0, 1.0);
                        // Falls away from a direction picked per tree, slow then fast.
                        float dir = fract(sin(dot(MODEL_MATRIX[3].xz, vec2(12.9898, 78.233))) * 43758.5453) * 6.2832;
                        vec3 axis = vec3(cos(dir), 0.0, sin(dir));
                        float angle = 1.45 * smoothstep(0.0, 0.7, k) * smoothstep(0.0, 0.7, k);
                        float cs = cos(angle), sn = sin(angle);
                        VERTEX = VERTEX * cs + cross(axis, VERTEX) * sn + axis * dot(axis, VERTEX) * (1.0 - cs);
                        NORMAL = NORMAL * cs + cross(axis, NORMAL) * sn + axis * dot(axis, NORMAL) * (1.0 - cs);
                        VERTEX *= 1.0 - smoothstep(0.6, 1.0, k);
                    }
                }
                vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
                if (sway > 0.0) {
                    float h = clamp(VERTEX.y / sway_height, 0.0, 1.5);
                    float t = TIME * 0.9 + world.x * 0.21 + world.z * 0.17;
                    float gust = (0.6 + 0.4 * sin(TIME * 0.23 + world.x * 0.03)) * (0.5 + wind_strength * 1.6);
                    VERTEX.x += sin(t) * sway * h * h * gust;
                    VERTEX.z += cos(t * 0.73) * sway * 0.6 * h * h * gust;
                }
                world_pos = world;
                world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
                // Palette colors are authored in sRGB.
                tint = pow(COLOR.rgb, vec3(2.2)) * albedo.rgb;
                // Ground that was just cleared or grew back blends from its old color.
                if (CUSTOM0.a > 0.0) {
                    float k = smoothstep(0.0, 1.6, world_seconds - CUSTOM0.a);
                    tint = mix(pow(CUSTOM0.rgb, vec3(2.2)) * albedo.rgb, tint, k);
                }
            #ifdef GHOST
                // The ghost draws only trees in the way; the rest collapse to nothing.
                if (fade < 0.002) VERTEX = vec3(0.0);
            #else
                // A tree in the way is drawn by its ghost instead.
                if (fade >= 0.002) VERTEX = vec3(0.0);
            #endif
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
                // Rain soaks upward-facing ground first; everything else darkens a little.
                wet = wetness * mix(0.45, 1.0, smoothstep(0.4, 0.9, world_normal.y)) * mix(0.75, 1.0, h);
                color *= mix(1.0, 0.8, wet);
                ALBEDO = clamp(color, 0.0, 1.0);
                shade = cloud_shadow(world_pos);

                vec3 dpdx = dFdx(VERTEX), dpdy = dFdy(VERTEX);
                vec3 r1 = cross(dpdy, NORMAL), r2 = cross(NORMAL, dpdx);
                float det = dot(dpdx, r1);
                vec3 grad = sign(det) * (dFdx(h) * r1 + dFdy(h) * r2);
                // Relief height is in tenths of a meter at most, so bumps stay subtle.
                NORMAL = normalize(abs(det) * NORMAL - grad * bump * 0.035);

                AO = mix(1.0, smoothstep(0.0, 0.6, h), cavity * 0.6);
                AO_LIGHT_AFFECT = 0.3;
                ROUGHNESS = mix(1.0, 0.75, wet);
                SPECULAR = 0.08 * wet;
            #ifdef GHOST
                // Faintly see-through all over, and clear in a soft circle around the
                // player, measured on screen.
                vec4 target = VIEW_MATRIX * vec4(player_position + vec3(0.0, 0.9, 0.0), 1.0);
                float apart = length(VERTEX.xy / -VERTEX.z - target.xy / -target.z);
                float window = 1.0 - smoothstep(0.07, 0.24, apart);
                ALPHA = 1.0 - fade * (0.3 + 0.55 * window);
            #endif
            }

            // Wrapped diffuse light, so forms turn softly into shadow, plus a soft
            // glint of sun on wet surfaces.
            void light() {
                float sky = LIGHT_IS_DIRECTIONAL ? shade : 1.0;
                float wrapped = clamp((dot(NORMAL, LIGHT) + 0.3) / 1.3, 0.0, 1.0);
                DIFFUSE_LIGHT += LIGHT_COLOR * wrapped * wrapped * ATTENUATION * sky / PI;
                if (wet > 0.01) {
                    vec3 half_dir = normalize(LIGHT + VIEW);
                    float glint = pow(max(dot(NORMAL, half_dir), 0.0), 24.0) * wet * 0.06;
                    SPECULAR_LIGHT += LIGHT_COLOR * glint * ATTENUATION * sky;
                }
            }
            """;

    static Shader GrassShader => grassShader ??= MakeShader("""
            shader_type spatial;
            render_mode cull_disabled, specular_disabled;

            varying vec3 tint;
            varying float tip;
            varying float gust;
            varying vec3 world_pos;
            varying float shade;

            void vertex() {
                // Grass on a cell just cleared lies down and sinks away; grass on a
                // cell uncleared rises back up.
                if (INSTANCE_CUSTOM.x > 0.0) {
                    float k = smoothstep(0.0, 1.6, world_seconds - INSTANCE_CUSTOM.x);
                    float height = INSTANCE_CUSTOM.y > 0.0 ? 1.0 - k : k;
                    VERTEX.y *= height;
                    VERTEX.xz *= mix(0.7, 1.0, height);
                }
                vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
                world_pos = world;
                tip = UV.y;
                // Long, slow waves rolling across the field, plus a small flutter.
                float wave = sin(dot(world.xz, vec2(0.23, 0.31)) - TIME * 1.3);
                gust = smoothstep(0.2, 1.0, wave);
                float flutter = sin(TIME * 3.1 + world.x * 1.7 + world.z * 2.3) * 0.03;
                float wind = 0.45 + wind_strength * 1.8;
                vec3 bend = vec3(0.6, 0.0, 0.8) * (0.06 + gust * 0.16) * wind + vec3(flutter, 0.0, -flutter) * wind;
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
                color *= mix(1.0, 0.88, wetness);
                ALBEDO = clamp(color, 0.0, 1.0);
                shade = cloud_shadow(world_pos);
                // Both faces of a blade are lit like the ground beneath it.
                NORMAL = normalize((VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz);
                ROUGHNESS = 1.0;
                SPECULAR = 0.0;
            }

            void light() {
                float sky = LIGHT_IS_DIRECTIONAL ? shade : 1.0;
                float wrapped = clamp((dot(NORMAL, LIGHT) + 0.3) / 1.3, 0.0, 1.0);
                DIFFUSE_LIGHT += LIGHT_COLOR * wrapped * wrapped * ATTENUATION * sky / PI;
            }
            """);

    // Puts the weather uniforms right after the shader's render mode line.
    static Shader MakeShader(string code)
    {
        EnsureWeatherGlobals();
        int split = code.IndexOf('\n', code.IndexOf("render_mode")) + 1;
        return new Shader { Code = code[..split] + WeatherCode + "\n" + code[split..] };
    }
}
