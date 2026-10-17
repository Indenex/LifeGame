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
    // Boulders, river stones and outcrops in the world (env-16): paler and warmer than
    // Rock, after the art references. Tools, stations and items keep Rock.
    public static readonly Color WorldRock = new(0.64f, 0.61f, 0.55f);
    // Wildflowers in the grass (Environment's undergrowth).
    public static readonly Color FlowerWhite = new(0.95f, 0.93f, 0.84f);
    public static readonly Color FlowerButtercup = new(0.93f, 0.78f, 0.25f);
    public static readonly Color FlowerViolet = new(0.62f, 0.50f, 0.80f);
    public static readonly Color FlowerPink = new(0.88f, 0.55f, 0.62f);
    // Heath on the hilltops and birch wetland by the water (Environment's env-4).
    public static readonly Color BirchBark = new(0.86f, 0.84f, 0.78f);
    public static readonly Color Heather = new(0.42f, 0.38f, 0.33f);
    public static readonly Color Gorse = new(0.30f, 0.36f, 0.14f);
    public static readonly Color Sedge = new(0.46f, 0.48f, 0.22f);
    public static readonly Color HeathGround = new(0.42f, 0.37f, 0.24f);
    public static readonly Color Moss = new(0.20f, 0.31f, 0.12f);
    public static readonly Color Skin = new(0.88f, 0.68f, 0.52f);
    public static readonly Color Hair = new(0.30f, 0.20f, 0.14f);
    public static readonly Color Cream = new(0.97f, 0.94f, 0.87f);
    public static readonly Color Water = new(0.14f, 0.34f, 0.40f, 0.9f);

    public const float WaterLevel = -1.0f;

    static Shader? paintedShader, ghostShader, structureGhostShader, grassShader;
    static ShaderMaterial? ground, bark, rock, foliage, grass, grassTufts;
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
        Add("wind_gust", RenderingServer.GlobalShaderParameterType.Float, 0.5f);
        Add("wind_dir", RenderingServer.GlobalShaderParameterType.Vec2, Atmosphere.WindDirection);
        Add("rain_amount", RenderingServer.GlobalShaderParameterType.Float, 0f);
        Add("cloud_cover", RenderingServer.GlobalShaderParameterType.Float, 0.2f);
        Add("cloud_offset", RenderingServer.GlobalShaderParameterType.Vec2, Vector2.Zero);
        Add("player_position", RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(0, -1000, 0));
        Add("view_camera_position", RenderingServer.GlobalShaderParameterType.Vec3, Vector3.Zero);
        Add("world_seconds", RenderingServer.GlobalShaderParameterType.Float, 1f);
        Add("season_warmth", RenderingServer.GlobalShaderParameterType.Float, 0.3f);
        Add("season_autumn", RenderingServer.GlobalShaderParameterType.Float, 0f);
        Add("snow_cover", RenderingServer.GlobalShaderParameterType.Float, 0f);
        Add("snowing", RenderingServer.GlobalShaderParameterType.Float, 0f);
        Add("season_year", RenderingServer.GlobalShaderParameterType.Float, 0.1f);
        Add("puddles", RenderingServer.GlobalShaderParameterType.Float, 0f);
        // Distant land (Environment's FarLand sets the radius, 0 when off) melts into
        // the fog colour (Atmosphere) over its outer quarter.
        Add("far_land_radius", RenderingServer.GlobalShaderParameterType.Float, 0f);
        Add("fog_tint", RenderingServer.GlobalShaderParameterType.Color, new Color(0.62f, 0.68f, 0.70f));
        // Legs wet from wading (Atmosphere): up to this world height, this wet (0 to 1).
        Add("wet_line", RenderingServer.GlobalShaderParameterType.Float, -1000f);
        Add("wet_amount", RenderingServer.GlobalShaderParameterType.Float, 0f);
        // Morning frost (Atmosphere): 0 none to 1 a full white rime.
        Add("frost", RenderingServer.GlobalShaderParameterType.Float, 0f);
        // Shifts where frost lies thickest from one morning to the next.
        Add("frost_seed", RenderingServer.GlobalShaderParameterType.Float, 0f);
    }

    // Seconds since the game started (from 1), shared with the shaders as
    // world_seconds so changes to the world can ease in over time. The atmosphere
    // advances it every frame.
    public static float Clock { get; private set; } = 1f;
    static readonly StringName WorldSeconds = "world_seconds"; // cached: a string would make a new StringName every frame

    public static void Tick(float delta)
    {
        Clock += delta;
        if (weatherGlobalsAdded) RenderingServer.GlobalShaderParameterSet(WorldSeconds, Clock);
    }

    // Each surface family gets the same shader with its own detail: how fine the
    // surface relief is, how deep it reads, and which way it stretches.
    public static ShaderMaterial Ground => ground ??= WithPuddles(Make(0f, 1f, detailScale: 0.45f, bump: 1.0f, cavity: 0.18f));

    static ShaderMaterial WithPuddles(ShaderMaterial material)
    {
        material.SetShaderParameter("puddles_ok", 1f);
        return material;
    }
    public static ShaderMaterial BarkSurface => bark ??= Make(0f, 1f, detailScale: 2.0f, bump: 1.2f, cavity: 0.3f, stretch: new Vector3(4f, 0.35f, 4f));
    public static ShaderMaterial RockSurface => rock ??= Make(0f, 1f, detailScale: 1.0f, bump: 1.5f, cavity: 0.22f);
    public static ShaderMaterial Foliage => foliage ??= Make(0.08f, 3.0f, detailScale: 1.6f, bump: 1.5f, cavity: 0.3f);

    // Trees between the camera and the player fade softly, so you can always see
    // yourself in the woods. A faded tree is drawn by its ghost: the same surface,
    // see-through, drawn by a second instance that only shows trees in the way.
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
        material == TreeBark ? treeBarkGhost ??= Ghost(TreeBark) : PaintedTrees.GhostOf(material);

    // A see-through copy of any painted surface for a building to fade by hand: set its
    // fade_override from 0 (solid) to 1 (clear) and swap it in for the solid one.
    public static ShaderMaterial StructureGhost(ShaderMaterial source) => Ghost(source, StructureGhostShader);

    static ShaderMaterial Ghost(ShaderMaterial source) => Ghost(source, GhostShader);

    internal static ShaderMaterial Ghost(ShaderMaterial source, Shader shader)
    {
        var ghost = new ShaderMaterial { Shader = shader };
        foreach (var uniform in source.Shader.GetShaderUniformList())
        {
            var name = new StringName(uniform.AsGodotDictionary()["name"].AsString());
            ghost.SetShaderParameter(name, source.GetShaderParameter(name));
        }
        return ghost;
    }

    // Whether something occupying the box hides the player from the camera: the line
    // from the camera to the player's head (or chest, for low eaves) passes through
    // it, or the player stands inside it. For anything the player can be under, such
    // as roofs and walls, to fade the way trees do (Justin's playtest).
    public static bool HidesPlayer(Aabb box, Vector3 camera, Vector3 player) =>
        box.HasPoint(player + new Vector3(0, 0.9f, 0))
        || SegmentHitsBox(camera, player + new Vector3(0, 1.6f, 0), box)
        || SegmentHitsBox(camera, player + new Vector3(0, 0.9f, 0), box);

    // Whether the segment from a to b touches the box (the slab test).
    public static bool SegmentHitsBox(Vector3 a, Vector3 b, Aabb box)
    {
        float t0 = 0f, t1 = 1f;
        var d = b - a;
        var min = box.Position;
        var max = box.End;
        for (int axis = 0; axis < 3; axis++)
        {
            float start = a[axis], step = d[axis];
            if (Mathf.Abs(step) < 1e-6f)
            {
                if (start < min[axis] || start > max[axis]) return false;
                continue;
            }
            float ta = (min[axis] - start) / step, tb = (max[axis] - start) / step;
            if (ta > tb) (ta, tb) = (tb, ta);
            t0 = Mathf.Max(t0, ta);
            t1 = Mathf.Min(t1, tb);
            if (t0 > t1) return false;
        }
        return true;
    }

    // Kept for callers that only need a plain painted surface.
    public static ShaderMaterial Painted => Ground;
    public static ShaderMaterial Swaying => Foliage;

    // Grass has its own shader: blades take their tile's color at the root, so the
    // carpet and the ground read as one surface, and rolling gusts sweep across it.
    public static ShaderMaterial Grass => grass ??= MakeGrass(0f);

    // The same grass for the tufts Environment plants only near the player: they
    // shrink away between about 31 and 48 m, before the chunks that have none.
    public static ShaderMaterial GrassTufts => grassTufts ??= MakeGrass(48f, stylised: true);

    static ShaderMaterial MakeGrass(float fadeFar, bool stylised = false)
    {
        var material = new ShaderMaterial { Shader = GrassShader };
        material.SetShaderParameter("clouds", Clouds);
        material.SetShaderParameter("fade_far", fadeFar);
        material.SetShaderParameter("stylised", stylised ? 1f : 0f);
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
        // The gust heard in the wind right now (0 to 1), so what moves matches the sound.
        global uniform float wind_gust;
        // Which way the wind blows across the ground (unit x, z); clouds drift this way.
        global uniform vec2 wind_dir;
        global uniform float rain_amount;
        global uniform float cloud_cover;
        global uniform vec2 cloud_offset;
        global uniform float world_seconds;
        // How far the distant land reaches round the player (0 when off), and the
        // colour of the fog it melts into at its rim.
        global uniform float far_land_radius;
        global uniform vec3 fog_tint;
        // How far up the player is wet from wading, and how wet (0 dry to 1 just out).
        global uniform float wet_line;
        global uniform float wet_amount;
        // Rime after a cold clear night, 0 none to 1 full; it melts away patchily.
        global uniform float frost;
        global uniform float frost_seed;
        // Rime is a cool blue-white, so the warm dawn light doesn't turn it pink.
        const vec3 FROST = vec3(0.74, 0.84, 0.97);
        // The season (Atmosphere, D-099): warmth -1 deep winter to 1 high summer,
        // autumn 0 green to 1 leaves fully turned, snow lying on the ground and
        // snow falling now, all 0 to 1.
        global uniform float season_warmth;
        global uniform float season_autumn;
        global uniform float snow_cover;
        global uniform float snowing;
        // How far through the year it is: 0 spring, 0.25 summer, 0.5 autumn, 0.75 winter.
        global uniform float season_year;
        // How much standing water there is after rain, 0 dry to 1 puddled; it rises
        // once the ground is soaked and lingers after the rain stops.
        global uniform float puddles;
        const vec3 SNOW = vec3(0.86, 0.89, 0.94);

        // How bare broadleaf trees are: bare through winter, leafing out again early
        // in spring.
        float leaf_drop() {
            return (1.0 - smoothstep(-0.85, -0.35, season_warmth)) * step(0.3, season_autumn);
        }
        uniform sampler2D clouds : repeat_enable, filter_linear_mipmap;

        // How cold this spot runs on a frosty morning, 0 to 1: higher ground and damp
        // hollows by the water more, in broad patches that shift from day to day.
        float frost_local(vec3 p) {
            float n = texture(clouds, p.xz * 0.004 + vec2(frost_seed, frost_seed * 0.37)).r;
            float high = smoothstep(0.0, 25.0, p.y + 1.0) * 0.35;
            float damp = (1.0 - smoothstep(0.0, 1.5, p.y + 1.0)) * 0.3;
            return clamp(n * 0.9 + high + damp - 0.15, 0.0, 1.0);
        }

        float cloud_shadow(vec3 p) {
            if (cloud_cover <= 0.01) return 1.0;
            // Clouds a few dozen meters across, so on a fair day their shadows
            // slide over the land in patches; under full overcast they merge.
            float n = texture(clouds, (p.xz + cloud_offset) * 0.01).r;
            float edge = mix(0.68, 0.2, cloud_cover);
            return 1.0 - smoothstep(edge - 0.1, edge + 0.12, n) * 0.5;
        }
        """;

    static Shader PaintedShader => paintedShader ??= MakeShader(PaintedCode);

    // The same surface drawn transparent, for trees fading out of the way.
    static Shader GhostShader => ghostShader ??= MakeShader(PaintedCode
        .Replace("render_mode cull_disabled;", "render_mode cull_back, depth_prepass_alpha;")
        .Replace("shader_type spatial;", "shader_type spatial;\n#define GHOST"));

    // A building's ghost: open faces stay visible as on the solid, and an opaque depth pass first,
    // so a faded near wall can't hide a far wall that shares its mesh (a brick course, a daub ring).
    static Shader StructureGhostShader => structureGhostShader ??= MakeShader(PaintedCode
        .Replace("render_mode cull_disabled;", "render_mode cull_disabled, depth_prepass_alpha;")
        .Replace("shader_type spatial;", "shader_type spatial;\n#define GHOST\n#define STRUCTURE_GHOST"));

    internal const string PaintedCode = """
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
            // Set on a ghost (never a solid) to fade it by hand, 0 solid to 1 clear, in place of the
            // tree test: buildings drive it while the player is under their roof.
            uniform float fade_override = -1.0;
            // A building ghost cuts away like Project Zomboid (D-168). cut_mode 1 is a wall:
            // it fades on the side facing the camera (cam_dir, flat, from the building's
            // centre cut_origin to the camera) and stays solid on the far side. cut_mode 2
            // is a roof: it clears as fade_override reaches 1, entirely or only round the player
            // (cut_floor). 0 is the trees.
            uniform float cut_mode = 0.0;
            uniform vec3 cut_origin = vec3(0.0);
            uniform vec2 cam_dir = vec2(0.0, 1.0);
            // How much of a wall facing the camera (or of a roof) fades away from the player's
            // circle: nearly all from inside, less (a wall) or none (a roof) from outside.
            uniform float cut_floor = 0.85;
            // 1 on the ground material only: where puddles may gather after rain.
            uniform float puddles_ok = 0.0;
            global uniform vec3 player_position;
            global uniform vec3 view_camera_position;

            // How far a tree has faded because it hides the player: 0 solid, 1 fully in
            // the way. The crown is taken as an upright capsule round the canopy, sized
            // by the tree's own scale, and the tree fades when the line of sight from
            // the camera to the player passes through it anywhere, including right at
            // the player, so a crown overhead fades too (Justin's playtest). Trunk and
            // canopy share a transform, so the whole tree eases in and out together.
            // Only the main camera fades things, so shadows stay whole.
            float fade_amount(mat4 model, vec3 cam) {
                if (distance(cam, view_camera_position) > 0.5) return 0.0;
                float tall = length(model[1].xyz);
                float wide = length(model[0].xyz);
                vec3 base = model[3].xyz;
                vec3 p1 = cam, d1 = player_position + vec3(0.0, 1.0, 0.0) - cam;
                vec3 p2 = base + vec3(0.0, 1.6 * tall, 0.0), d2 = vec3(0.0, 2.8 * tall, 0.0);
                vec3 r = p1 - p2;
                float a = dot(d1, d1), b = dot(d1, d2), c = dot(d1, r), e = dot(d2, d2), f = dot(d2, r);
                float denom = a * e - b * b;
                float s = denom > 1e-5 ? clamp((b * f - c * e) / denom, 0.0, 1.0) : 0.0;
                float t = clamp((b * s + f) / e, 0.0, 1.0);
                s = clamp((b * t - c) / a, 0.0, 1.0);
                float d = distance(p1 + d1 * s, p2 + d2 * t);
                // Only a tree truly across the line fades (env-25): fully within 1.5
                // times its width of the axis, easing out by 2.1.
                return 1.0 - smoothstep(1.5 * wide, 2.1 * wide, d);
            }

            varying vec3 tint;
            varying float fade;
            varying vec3 world_pos;
            varying vec3 world_normal;
            varying float shade;
            varying float wet;
            // Environment's season code per instance (INSTANCE_CUSTOM.w): 1 broadleaf
            // canopy, 2 flower, 3 heather, 0 no seasonal change.
            varying float season_kind;

            void vertex() {
                season_kind = INSTANCE_CUSTOM.w;
                // Broadleaf canopies thin to nothing in winter and grow back in spring.
                if (sway > 0.0 && season_kind > 0.5 && season_kind < 1.5) VERTEX *= 1.0 - 0.85 * leaf_drop();
                fade = fade_override >= 0.0 ? fade_override : see_through ? fade_amount(MODEL_MATRIX, CAMERA_POSITION_WORLD) : 0.0;
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
                        // Falls slow then fast, in a direction seeded by where the tree
                        // stands, so every player sees it fall the same way (D-056).
                        float dir = fract(sin(dot(floor(MODEL_MATRIX[3].xz + 0.5), vec2(12.9898, 78.233))) * 43758.5453) * 6.2832;
                        vec3 axis = normalize(inverse(mat3(MODEL_MATRIX)) * vec3(sin(dir), 0.0, -cos(dir)));
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
                    float gust = (0.45 + 0.45 * wind_gust + 0.15 * sin(TIME * 0.23 + world.x * 0.03)) * (0.5 + wind_strength * 1.6);
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
                // Broadleaf leaves turn gold, orange and red through autumn, each tree
                // its own mix, then brown before they fall.
                if (sway > 0.0 && season_kind > 0.5 && season_kind < 1.5 && season_autumn > 0.0) {
                    float seed = fract(sin(dot(floor(world_pos.xz * 0.25), vec2(12.9898, 78.233))) * 43758.5453);
                    vec3 turned = mix(vec3(0.62, 0.42, 0.08), vec3(0.58, 0.16, 0.06), seed);
                    turned = mix(turned, vec3(0.30, 0.20, 0.11), smoothstep(0.75, 1.0, season_autumn));
                    color = mix(color, turned * (1.0 + paint * 2.0), smoothstep(0.0, 0.6, season_autumn));
                }
                // Heather stays a quiet green-brown most of the year; only through late
                // summer do its flower tips turn a soft, dusty mauve, no brighter than
                // the grass (Justin: no saturated pink). The tips are found by their
                // colour (purple-ish vertex paint), so the mesh's own purple is tamed too.
                if (sway > 0.0 && season_kind > 2.5 && season_kind < 3.5) {
                    float bloom = smoothstep(0.3, 0.38, season_year) * (1.0 - smoothstep(0.48, 0.56, season_year));
                    float tip = smoothstep(0.02, 0.12, min(color.r, color.b) - color.g);
                    float bright = dot(color, vec3(0.3, 0.59, 0.11));
                    vec3 muted = mix(vec3(0.40, 0.38, 0.25), vec3(0.47, 0.41, 0.44), bloom * 0.8);
                    color = mix(color, muted * clamp(bright / 0.5, 0.7, 1.1), tip);
                }
                // Snow settles on whatever faces up, filling the hollows first.
                float snow = snow_cover * smoothstep(0.3, 0.75, world_normal.y) * smoothstep(0.0, 0.5, snow_cover * 1.4 - h * 0.6);
                color = mix(color, SNOW * (1.0 + paint), snow);
                wet *= 1.0 - snow;
                // Morning frost whitens what faces the open sky, thickest on the bumps,
                // and melts back in patches; people, animals and made things (no
                // cavity) stay clear of it. Tiny glints sparkle as you move.
                if (frost > 0.0 && cavity > 0.01) {
                    float patches = texture(detail, world_pos.xz * 0.06).r;
                    float rime = smoothstep(patches - 0.12, patches + 0.12, frost * (0.5 + 0.5 * frost_local(world_pos)) * 1.25 - 0.12);
                    // Only on the raised grain of what faces up, so the colour beneath
                    // still shows through: a dusting, not a snow cover. Bare ground
                    // takes less than rocks and leaves, which the grass covers anyway.
                    // Rocks and leaves take a clear white edge on their upper faces and
                    // raised grain (Justin: "in between"); flat bare ground only a trace.
                    rime *= smoothstep(0.1, 0.6, world_normal.y) * smoothstep(0.25, 0.65, h) * (1.0 - snow);
                    rime *= puddles_ok > 0.5 ? 0.2 : 0.75;
                    vec3 cell = floor(world_pos * 28.0);
                    float glint = step(0.985, fract(sin(dot(cell + floor(view_camera_position * 3.0), vec3(12.9898, 78.233, 37.719))) * 43758.5453));
                    color = mix(color, FROST * (1.0 + paint), rime);
                    color = mix(color, vec3(1.0), glint * min(rime * 2.0, 1.0) * 0.8);
                    wet *= 1.0 - rime;
                }
                // Puddles gather on flat ground in the low spots of a slow pattern,
                // spreading as the ground stays soaked and shrinking as it dries:
                // dark, still and glossy.
                float puddle = 0.0;
                if (puddles_ok > 0.5 && puddles > 0.01) {
                    float low = 1.0 - texture(detail, world_pos.xz * 0.035).r;
                    float edge = 1.0 - 0.42 * puddles;
                    puddle = smoothstep(edge, edge + 0.05, low) * smoothstep(0.93, 0.99, world_normal.y) * (1.0 - snow);
                    color *= mix(1.0, 0.5, puddle);
                    wet = max(wet, puddle);
                }
                ALBEDO = clamp(color, 0.0, 1.0);
                shade = cloud_shadow(world_pos);

                vec3 dpdx = dFdx(VERTEX), dpdy = dFdy(VERTEX);
                vec3 r1 = cross(dpdy, NORMAL), r2 = cross(NORMAL, dpdx);
                float det = dot(dpdx, r1);
                vec3 grad = sign(det) * (dFdx(h) * r1 + dFdy(h) * r2);
                // Relief height is in tenths of a meter at most, so bumps stay subtle.
                NORMAL = normalize(abs(det) * NORMAL - grad * bump * 0.035);

                // The rim of the distant land melts into the fog, so its edge never
                // shows against the sky, even on a clear day.
                // Just out of the water, the player is darker and glossier up to where the water
                // reached, with a soft edge, drying off over time.
                if (wet_amount > 0.0) {
                    float near_player = 1.0 - smoothstep(0.45, 0.65, length(world_pos.xz - player_position.xz));
                    float soaked = wet_amount * near_player * (1.0 - smoothstep(wet_line - 0.04, wet_line + 0.04, world_pos.y));
                    ALBEDO *= mix(1.0, 0.62, soaked);
                    wet = max(wet, soaked);
                }

                if (far_land_radius > 0.0) {
                    float rim = smoothstep(0.75 * far_land_radius, far_land_radius, length(world_pos.xz - player_position.xz));
                    ALBEDO *= 1.0 - rim;
                    EMISSION = fog_tint * rim;
                }

                AO = mix(1.0, smoothstep(0.0, 0.6, h), cavity * 0.6);
                AO_LIGHT_AFFECT = 0.3;
                ROUGHNESS = mix(mix(1.0, 0.75, wet), 0.05, puddle);
                SPECULAR = mix(0.08 * wet, 0.5, puddle);
                // Still water lies flat, whatever the relief of the ground under it.
                NORMAL = normalize(mix(NORMAL, (VIEW_MATRIX * vec4(0.0, 1.0, 0.0, 0.0)).xyz, puddle));
            #ifdef GHOST
                // Faintly see-through all over, and clear in a soft circle around the
                // player, measured on screen.
                vec4 target = VIEW_MATRIX * vec4(player_position + vec3(0.0, 0.9, 0.0), 1.0);
                float apart = length(VERTEX.xy / -VERTEX.z - target.xy / -target.z);
                float window = 1.0 - smoothstep(0.09, 0.3, apart);
                // Fully clear right around the player, so even several layers of
                // canopy never hide them.
            #ifdef STRUCTURE_GHOST
                if (cut_mode > 1.5) {
                    // cut_floor 1 clears the whole roof (player inside); 0 clears only the
                    // circle round the player (player behind the building).
                    ALPHA = 1.0 - fade * mix(cut_floor, 1.0, window);
                } else if (cut_mode > 0.5) {
                    vec2 out_dir = world_pos.xz - cut_origin.xz;
                    float facing = smoothstep(0.05, 0.45, dot(normalize(out_dir + vec2(1e-4)), cam_dir));
                    ALPHA = 1.0 - fade * facing * mix(cut_floor, 1.0, window);
                } else {
                    ALPHA = 1.0 - fade * (0.35 + 0.65 * window);
                }
                if (ALPHA < 0.04) discard;
            #else
                // A tree in the way thins to about 55% (env-25), a little more right round
                // the player. The ghost draws its depth first (depth_prepass_alpha), so only
                // the front-most surface blends: overlapping crowns never stack up.
                float opening = 1.0 - smoothstep(0.04, 0.42, apart);
                ALPHA = 1.0 - fade * (0.45 + 0.2 * opening * opening);
            #endif
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

            // Tufts shrink to nothing between 0.65 x fade_far and fade_far metres
            // from the player (0 = never), so they are gone before the chunks
            // that carry none (GrassTufts).
            uniform float fade_far = 0.0;
            // 1 on the grass tufts only: the art-reference colour bands.
            uniform float stylised = 0.0;
            global uniform vec3 player_position;

            varying vec3 tint;
            varying float tip;
            varying float gust;
            varying vec3 world_pos;
            varying float shade;

            void vertex() {
                if (fade_far > 0.0) {
                    float away = length(MODEL_MATRIX[3].xz - player_position.xz);
                    VERTEX *= 1.0 - smoothstep(0.65 * fade_far, fade_far, away);
                }
                // Flowers (season code 2) bloom only in spring and summer; snow
                // presses all grass down.
                float flowering = INSTANCE_CUSTOM.w > 1.5 ? smoothstep(-0.25, 0.1, season_warmth) * (1.0 - smoothstep(0.1, 0.4, season_autumn)) : 1.0;
                VERTEX *= flowering;
                VERTEX.y *= 1.0 - 0.75 * snow_cover;
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
                float wind = (0.45 + wind_strength * 1.8) * (0.7 + 0.6 * wind_gust);
                vec3 bend = vec3(wind_dir.x, 0.0, wind_dir.y) * (0.06 + gust * 0.16) * wind + vec3(flutter, 0.0, -flutter) * wind;
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
                // The grass tufts lean toward the art references (docs/art-reference 02):
                // muted olive and warm yellow-green, a soft blue-violet shade at the
                // roots, and pale lit tips, in soft tone bands. Shape, height, density
                // and wind are unchanged; ferns, reeds and flowers keep their colours.
                if (stylised > 0.5) {
                    float lum = dot(tint, vec3(0.3, 0.59, 0.11));
                    vec3 olive = mix(tint, vec3(lum * 1.1, lum * 1.05, lum * 0.55), 0.3);
                    vec3 root_shade = olive * vec3(0.62, 0.65, 0.82);
                    vec3 lit_tip = olive * vec3(1.36, 1.3, 0.78);
                    color = mix(root_shade, olive, smoothstep(0.05, 0.4, tip));
                    color = mix(color, lit_tip, smoothstep(0.6, 0.95, tip));
                }
                color += vec3(0.05, 0.05, 0.02) * gust * tip;
                color *= mix(1.0, 0.88, wetness);
                // Grass fades to straw in the cold and wears snow on its tips.
                vec3 straw = vec3(0.42, 0.36, 0.20) * (0.7 + 0.5 * tip);
                color = mix(color, straw, 0.55 * max(0.0, -season_warmth));
                color = mix(color, SNOW, snow_cover * (0.55 + 0.45 * tip));
                // Frost silvers the blades, most at the tips, gone first where it melts.
                if (frost > 0.0) {
                    float patches = texture(clouds, world_pos.xz * 0.035).r;
                    float rime = smoothstep(patches - 0.15, patches + 0.15, frost * (0.5 + 0.5 * frost_local(world_pos)) * 1.3 - 0.15) * (1.0 - snow_cover);
                    // The tips are clearly silvered; the blades stay green below.
                    color = mix(color, FROST, rime * 0.8 * smoothstep(0.35, 0.9, tip));
                }
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

    // Open water: a calm, deep color that only softly catches the light (no mirror
    // shine), slow shimmering ripples that stir with the wind's gusts, rain rings
    // spreading on the surface while it rains, and a soft fade where it meets the bank.
    // Open water (Atmosphere's WaterLook, atmosphere-31): the far plane, and the dense
    // waving surface Atmosphere keeps round the player.
    public static ShaderMaterial WaterSurface
    {
        get
        {
            EnsureWeatherGlobals();
            return water ??= WaterLook.Make(near: false);
        }
    }
    public static ShaderMaterial WaterNear
    {
        get
        {
            EnsureWeatherGlobals();
            return waterNear ??= WaterLook.Make(near: true);
        }
    }
    static ShaderMaterial? water, waterNear;

    // Puts the weather uniforms right after the shader's render mode line.
    internal static Shader MakeShader(string code)
    {
        EnsureWeatherGlobals();
        int split = code.IndexOf('\n', code.IndexOf("render_mode")) + 1;
        return new Shader { Code = code[..split] + WeatherCode + "\n" + code[split..] };
    }
}
