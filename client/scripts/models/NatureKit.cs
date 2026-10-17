using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Quaternius's Stylized Nature MegaKit (CC0, client/assets/naturekit, License_Standard.txt
// beside it), blended into our look (Justin, 2026-10-17: "closer to the style I want ... merge
// these styles together, or use the models/textures where applicable"). The kit's meshes
// keep their own hand-painted textures and chunky leaf cards; we draw them with our light:
// Painted.Light's soft stepped bands and violet shade warming at golden hour, wind sway on leaves and plants, and autumn turning broadleaf leaves.
//
// NatureKit.Mesh(name) gives the kit model by its file name (CommonTree_1, Pine_3,
// Rock_Medium_2, Fern_1 ...) with our materials on it, or with the kit's own (raw: true) for
// comparison. Meshes are cached and shared, so a MultiMesh can batch them.
public static class NatureKit
{
    public const string Dir = "res://assets/quaternius/nature/";
    // The village kit's props (village-1), drawn with the same blended look.
    public const string VillageDir = "res://assets/quaternius/village/";

    public enum Kind { Leaves, Bark, Rock, Plant, Flower, Grass }

    static readonly Dictionary<(string, bool), Mesh?> Meshes = new();

    // The world's trees drawn from the kit (naturekit-1, behind a setting until Justin picks):
    // --naturekit on the command line, or graphics/naturekit_trees = 1 in the settings.
    static readonly bool Wanted = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--naturekit") >= 0;
    public static bool Trees => Wanted || Settings.Get("graphics", "naturekit_trees", 0) == 1;
    // The world's boulders from the kit: --naturekit too, or graphics/naturekit_rocks = 1.
    public static bool Rocks => Wanted || Settings.Get("graphics", "naturekit_rocks", 0) == 1;

    // Our boulder variants (PropMeshes.Rock: kind v % RockKinds, second form above that) as
    // the kit's six: the first forms are its four medium rocks, the second its two big
    // ones and two mediums again (each placed at its own yaw and size).
    static string RockName(int v) => (v / PropMeshes.RockKinds, v % PropMeshes.RockKinds) switch
    {
        (0, var k) => $"Rock_Medium_{k + 1}",
        (_, 0) => "Rock_Big_1",
        (_, 1) => "Rock_Big_2",
        (_, var k) => $"Rock_Medium_{k - 1}",
    };
    static readonly Dictionary<int, (Mesh Mesh, Material Material, float Scale, float Sink)> RockMeshes = new();

    // Our tree variants drawn as the kit's: broadleaf v as CommonTree_(v+1), pine v as Pine_(v+1).
    static string TreeName(bool pine, int v) => pine ? $"Pine_{v + 1}" : $"CommonTree_{v + 1}";
    static readonly Dictionary<(bool, int), (Mesh Mesh, float Scale)> TreeMeshes = new();

    // Loads the kit trees on the main thread (ChunkWorker.Prewarm), so chunk workers only read.
    static bool prewarmed;
    public static void Prewarm()
    {
        if (prewarmed || !(Trees || Rocks)) return;
        prewarmed = true;
        if (Rocks)
            for (int v = 0; v < PropMeshes.RockVariants; v++)
                if (Mesh(RockName(v)) is { } rock && rock.SurfaceGetMaterial(0) is { } look)
                {
                    // As broad as our boulder of the same variant (its collision is that
                    // size), and sunk a little so it sits in the ground on a slope.
                    var ours = PropMeshes.Rock(v).GetAabb();
                    var kit = rock.GetAabb();
                    float scale = (ours.Size.X + ours.Size.Z) / (kit.Size.X + kit.Size.Z);
                    lock (RockMeshes) RockMeshes[v] = (rock, look, scale, kit.Size.Y * scale * 0.12f);
                }
        if (!Trees) return;
        for (int v = 0; v < PaintedTrees.Variants; v++)
            foreach (bool pine in new[] { false, true })
                if (Mesh(TreeName(pine, v)) is { } mesh)
                {
                    // Stand as tall as our tree of the same scale (PaintedTrees.Height), so
                    // placement, collision and the canopy fade keep their sizes.
                    float tall = mesh.GetAabb().End.Y;
                    lock (TreeMeshes) TreeMeshes[(pine, v)] = (mesh, PaintedTrees.Height / tall);
                }
    }

    // The kit tree for one of our tree variants and the extra scale it stands at, or null.
    public static (Mesh Mesh, float Scale)? Tree(bool pine, int variant)
    {
        // Chunks built on the main thread (the view bench) before any worker started.
        if (!prewarmed && OS.GetThreadCallerId() == OS.GetMainThreadId()) Prewarm();
        lock (TreeMeshes) return TreeMeshes.TryGetValue((pine, variant), out var t) ? t : null;
    }

    // The kit boulder for one of our rock variants: its mesh, the material Terrain batches it
    // under, and the scale and sink it stands at (Placed), or null.
    public static (Mesh Mesh, Material Material, float Scale, float Sink)? Rock(int variant)
    {
        if (!prewarmed && OS.GetThreadCallerId() == OS.GetMainThreadId()) Prewarm();
        lock (RockMeshes) return RockMeshes.TryGetValue(variant, out var r) ? r : null;
    }

    // One placement scaled by a kit model's extra scale and sunk by `sink` metres (per unit
    // of the placement's own scale).
    public static (Transform3D, Color, Color) Placed((Transform3D, Color, Color) item, float scale, float sink)
    {
        var basis = item.Item1.Basis;
        return (new Transform3D(basis * Basis.FromScale(Vector3.One * scale), item.Item1.Origin - basis.Y * sink), item.Item2, item.Item3);
    }

    // Whether a chunk batch's mesh is one of the kit trees (TreeFall draws it falling).
    public static bool IsTree(Mesh mesh)
    {
        if (!Trees) return false;
        lock (TreeMeshes)
            foreach (var t in TreeMeshes.Values)
                if (t.Mesh == mesh) return true;
        return false;
    }

    // Terrain's tree placements scaled by a kit tree's extra scale.
    public static List<(Transform3D, Color, Color)> Scaled(List<(Transform3D, Color, Color)> items, float scale) =>
        items.ConvertAll(i => (new Transform3D(i.Item1.Basis * Basis.FromScale(Vector3.One * scale), i.Item1.Origin), i.Item2, i.Item3));
    static readonly Dictionary<(Texture2D?, string), ShaderMaterial> Materials = new();

    // The model's mesh with the blended look (or the kit's own materials), null if the asset
    // isn't there (not imported yet).
    public static Mesh? Mesh(string name, bool raw = false)
    {
        lock (Meshes)
        {
            if (Meshes.TryGetValue((name, raw), out var cached)) return cached;
            var mesh = Load(name.StartsWith("Prop_") ? VillageDir : Dir, name, raw);
            Meshes[(name, raw)] = mesh;
            return mesh;
        }
    }

    static Mesh? Load(string dir, string name, bool raw)
    {
        string path = dir + name + ".gltf";
        if (!ResourceLoader.Exists(path) || GD.Load<PackedScene>(path) is not { } packed)
        {
            GD.PushWarning($"NatureKit: {path} could not be loaded.");
            return null;
        }
        var scene = packed.Instantiate();
        var found = Find(scene);
        scene.Free();
        if (found == null) return null;
        if (!raw)
        {
            // Our materials on the imported mesh itself, so it keeps the LODs Godot made for it.
            for (int s = 0; s < found.GetSurfaceCount(); s++)
                if (found.SurfaceGetMaterial(s) is var source && source is not ShaderMaterial)
                    found.SurfaceSetMaterial(s, Blended(source, name));
            return found;
        }
        // The kit's own look: a copy, its materials as they came.
        var copy = new ArrayMesh();
        for (int s = 0; s < found.GetSurfaceCount(); s++)
        {
            copy.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, found.SurfaceGetArrays(s));
            copy.SurfaceSetMaterial(s, found.SurfaceGetMaterial(s));
        }
        return copy;
    }

    static ArrayMesh? Find(Node node)
    {
        if (node is MeshInstance3D { Mesh: ArrayMesh m }) return m;
        foreach (var child in node.GetChildren())
            if (Find(child) is { } found) return found;
        return null;
    }

    static Kind KindOf(string model, string material)
    {
        if (material.StartsWith("Bark") || material.StartsWith("MI_WoodTrim")) return Kind.Bark;
        if (material.StartsWith("MI_RockTrim")) return Kind.Rock;
        if (material.StartsWith("MI_Vine")) return Kind.Leaves;
        if (material.StartsWith("Rocks") || material.StartsWith("PathRocks")) return Kind.Rock;
        if (material.StartsWith("Flowers") || material.StartsWith("Mushrooms")) return Kind.Flower;
        if (material.StartsWith("Grass")) return Kind.Grass;
        if (Leaf(model) != null) return Kind.Leaves;
        return Kind.Plant;
    }

    // The kit's own colours, from its Godot material instances (naturekit-godot.zip,
    // Materials/MI_*.tres). Its tree leaves are not painted by their texture (that gives only
    // the card's outline): they are one green fading to a cool blue-green low down, with a
    // fresnel glow of light through the leaves. Some kinds also stand their cards up to face
    // the camera, which is what makes their crowns read as soft round clumps.
    readonly record struct LeafLook(Color Top, Color Low, Color Glow, float GlowStrength, float GlowBlend, bool Cards);
    static readonly Color KitLow = new(0.13f, 0.33f, 0.25f);
    static readonly LeafLook CommonLeaves = new(new(0.474f, 0.633f, 0.089f), KitLow, new(0.459f, 0.722f, 0f), 2f, 0.2f, false);
    static readonly LeafLook BushLeaves = new(new(0.348f, 0.658f, 0.216f), KitLow, new(0.539f, 0.733f, 0.092f), 2f, 0.2f, true);
    static readonly LeafLook PineLeaves = new(new(0.111f, 0.321f, 0f), KitLow, new(0.65f, 0.7f, 0f), 1.946f, 0.085f, false);
    static readonly LeafLook GiantPineLeaves = new(new(0f, 0.729f, 0.45f), KitLow, new(0f, 0.743f, 0.802f), 3.088f, -0.044f, false);
    // The kit paints birches orange and twisted trees red all year; ours are green until
    // autumn turns them (the PM's default while Justin picks; the kit's colours are in the
    // comments to switch back).
    static readonly LeafLook BirchLeaves = new(new(0.56f, 0.7f, 0.18f), KitLow, new(0.62f, 0.76f, 0.1f), 1.836f, 0.25f, true); // kit: (1, 0.252, 0.066), glow (1, 0.495, 0.127), 0.335
    static readonly LeafLook TwistedLeaves = new(new(0.3f, 0.54f, 0.1f), KitLow, new(0.5f, 0.7f, 0.05f), 1.51f, 0.3f, true); // kit: (0.767, 0, 0.029), glow (0.547, 0, 0.131), 0.61

    // The village kit's vines (MI_Vine: its two greens).
    static readonly LeafLook VineLeaves = new(new(0.423f, 0.467f, 0.006f), new(0.257f, 0.398f, 0f), new(0.45f, 0.6f, 0f), 1.5f, 0.1f, false);

    static LeafLook? Leaf(string model) =>
        model.StartsWith("CommonTree") || model.StartsWith("Bush_Common") ? CommonLeaves
        : model.StartsWith("GiantPine") ? GiantPineLeaves
        : model.StartsWith("Pine") ? PineLeaves
        : model.StartsWith("Birch") ? BirchLeaves
        : model.StartsWith("TwistedTree") ? TwistedLeaves
        : model.StartsWith("Bush_") ? BushLeaves
        : null; // ferns, plants and clover keep their painted texture

    // Bark darkens toward the kit's tint at the foot (vertex colour red is 0 there).
    static Color BarkFoot(string material) => material switch
    {
        "Bark_Birch" => new(0.289f, 0.354f, 0f),
        "Bark_DeadTree" => new(0f, 0f, 0f),
        "Bark_Pine" => new(0.545f, 0.659f, 0f),
        "Bark_TwistedTree" => new(0.219f, 0.271f, 0f),
        "MI_WoodTrim" or "MI_WoodTrim_Wear" => Colors.White, // village timber: its texture as painted
        _ => new(0.235f, 0.29f, 0f),
    };

    // Grass blades: bottom to top colour by vertex colour, a broad world tint, and wind lines.
    static (Color Bottom, Color Top, Color Line, Color World) GrassOf(string model) =>
        model.StartsWith("Grass_Wide") ? (new(0f, 0.247f, 0.018f), new(0.233f, 0.719f, 0f), new(0.354f, 0.64f, 0f), new(0.193f, 0.512f, 0f))
        : model.StartsWith("Grass_Wispy") ? (new(0.244f, 0.193f, 0f), new(0.665f, 0.615f, 0f), new(0.692f, 0.525f, 0f), new(0.501f, 0.428f, 0f))
        : (new(0.09f, 0.239f, 0f), new(0.545f, 0.659f, 0f), new(0.549f, 0.588f, 0f), new(0f, 0.514f, 0.306f));

    static Texture2D? noiseWind, noisePerlin;
    static Texture2D? Noise(ref Texture2D? cache, string file) =>
        cache ??= ResourceLoader.Exists(Dir + "textures/" + file) ? GD.Load<Texture2D>(Dir + "textures/" + file) : null;

    // Our material for one of the kit's on the given model: the kit's colours, our light.
    public static ShaderMaterial Blended(Material? source, string model)
    {
        var standard = source as BaseMaterial3D;
        var texture = standard?.AlbedoTexture;
        string name = source?.ResourceName ?? "";
        var kind = KindOf(model, name);
        var leaf = kind == Kind.Leaves ? (name.StartsWith("MI_Vine") ? VineLeaves : Leaf(model)) : null;
        string look = kind switch
        {
            Kind.Leaves => leaf == CommonLeaves ? "common" : model.Split('_')[0],
            Kind.Grass => model.StartsWith("Grass_Wide") ? "wide" : model.StartsWith("Grass_Wispy") ? "wispy" : "common",
            _ => kind.ToString(),
        };
        lock (Materials)
        {
            if (Materials.TryGetValue((texture, look + name), out var done)) return done;
            var m = new ShaderMaterial { Shader = Shader };
            if (texture != null) m.SetShaderParameter("albedo_tex", texture);
            m.SetShaderParameter("has_tex", texture != null);
            // The village kit's timber is a bright orange pine; ours is a weathered brown.
            m.SetShaderParameter("albedo", name.StartsWith("MI_WoodTrim") ? new Color(0.8f, 0.7f, 0.62f) : standard?.AlbedoColor ?? Colors.White);
            bool cut = kind is Kind.Leaves or Kind.Plant or Kind.Flower;
            m.SetShaderParameter("cutout", cut ? 0.5f : 0f);
            m.SetShaderParameter("mode", kind switch { Kind.Leaves => 1, Kind.Grass => 2, Kind.Bark => 3, Kind.Plant or Kind.Flower => 4, _ => 0 });
            if (leaf is { } l)
            {
                m.SetShaderParameter("leaf_top", l.Top);
                m.SetShaderParameter("leaf_low", l.Low);
                m.SetShaderParameter("glow_color", l.Glow);
                m.SetShaderParameter("glow_strength", l.GlowStrength);
                m.SetShaderParameter("glow_blend", l.GlowBlend);
                m.SetShaderParameter("cards", l.Cards ? 1f : 0f);
            }
            if (kind == Kind.Bark) m.SetShaderParameter("bark_foot", BarkFoot(name));
            if (kind is Kind.Bark or Kind.Rock)
            {
                if (standard is { NormalEnabled: true, NormalTexture: { } normal })
                {
                    m.SetShaderParameter("normal_tex", normal);
                    m.SetShaderParameter("has_normal", true);
                }
            }
            if (kind == Kind.Grass)
            {
                var g = GrassOf(model);
                m.SetShaderParameter("grass_bottom", g.Bottom);
                m.SetShaderParameter("grass_top", g.Top);
                m.SetShaderParameter("grass_line", g.Line);
                m.SetShaderParameter("grass_world", g.World);
                if (Noise(ref noiseWind, "Noise_Wind.png") is { } wind) m.SetShaderParameter("wind_noise", wind);
                if (Noise(ref noisePerlin, "Noise_Perlin.png") is { } perlin) m.SetShaderParameter("world_noise", perlin);
            }
            // Leaves sway from the crown, plants from their roots.
            m.SetShaderParameter("sway", kind switch { Kind.Leaves => 0.07f, Kind.Plant or Kind.Grass => 0.05f, Kind.Flower => 0.03f, _ => 0f });
            m.SetShaderParameter("sway_height", kind == Kind.Leaves ? 6f : 0.6f);
            m.SetShaderParameter("autumn", kind == Kind.Leaves ? 1f : 0f);
            // Leaf cards: the crown is lit as one soft mass rather than card by card.
            m.SetShaderParameter("crown_normals", kind == Kind.Leaves ? 1f : 0f);
            m.SetShaderParameter("rim", kind is Kind.Rock ? 1f : 0f);
            // Rocks take half the hue of the world's stone tint and each one's own darkening.
            m.SetShaderParameter("tint_blend", kind is Kind.Rock ? 0.5f : 0f);
            m.SetShaderParameter("tint_ref", Look.WorldRock.Luminance);
            m.SetShaderParameter("shade_tint", kind switch
            {
                Kind.Rock => new Color(0.66f, 0.64f, 0.76f),
                Kind.Bark => new Color(0.68f, 0.6f, 0.82f),
                _ => new Color(0.62f, 0.66f, 0.86f),
            });
            Materials[(texture, look + name)] = m;
            return m;
        }
    }

    static Shader? shader;
    static Shader Shader => shader ??= new Shader { Code = Code };

    // mode: 0 texture as painted (rocks), 1 kit leaves (flat green gradient, glow, cards),
    // 2 kit grass, 3 kit bark (foot tint, normal map), 4 foliage texture shaded by vertex colour.
    static readonly string Code = """
        shader_type spatial;
        render_mode cull_disabled, depth_draw_opaque;
        """ + Painted.RimUniform + """

        global uniform float wind_strength;
        global uniform float wind_gust;
        global uniform vec2 wind_dir;
        global uniform float season_autumn;
        global uniform float wetness;
        uniform sampler2D albedo_tex : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
        uniform bool has_tex = true;
        uniform vec4 albedo : source_color = vec4(1.0);
        uniform int mode = 0;
        uniform float cutout = 0.0;
        uniform vec3 leaf_top : source_color = vec3(0.474, 0.633, 0.089);
        uniform vec3 leaf_low : source_color = vec3(0.13, 0.33, 0.25);
        uniform vec3 glow_color : source_color = vec3(0.459, 0.722, 0.0);
        uniform float glow_strength = 2.0;
        uniform float glow_blend = 0.2;
        uniform float cards = 0.0;
        uniform vec3 bark_foot : source_color = vec3(0.235, 0.29, 0.0);
        uniform sampler2D normal_tex : hint_normal, filter_linear_mipmap, repeat_enable;
        uniform bool has_normal = false;
        uniform vec3 grass_bottom : source_color = vec3(0.09, 0.239, 0.0);
        uniform vec3 grass_top : source_color = vec3(0.545, 0.659, 0.0);
        uniform vec3 grass_line : source_color = vec3(0.549, 0.588, 0.0);
        uniform vec3 grass_world : source_color = vec3(0.0, 0.514, 0.306);
        uniform sampler2D wind_noise : hint_default_black, filter_linear_mipmap, repeat_enable;
        uniform sampler2D world_noise : hint_default_white, filter_linear_mipmap, repeat_enable;
        uniform float sway = 0.0;
        uniform float sway_height = 1.0;
        uniform float autumn = 0.0;
        uniform float crown_normals = 0.0;
        uniform float rim = 0.0;
        uniform float tint_blend = 0.0;
        uniform float tint_ref = 0.6;
        uniform vec3 shade_tint : source_color = vec3(0.66, 0.64, 0.8);
        const float BAND_SOFT = 0.1;
        const float BAND_BREAK = 0.0;
        varying vec3 world_pos;
        varying float tree_seed;
        varying float height_mask;
        varying float vertex_r;
        varying vec3 glow;

        void vertex() {
            vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
            vec3 origin = MODEL_MATRIX[3].xyz;
            // The kit's leaf colour runs over the first metre of the model's height.
            height_mask = clamp(VERTEX.y, 0.0, 1.0);
            vertex_r = COLOR.r;
            if (cards > 0.0) {
                // The kit's trick: each leaf card also spreads out facing the camera, so a
                // crown reads as soft round clumps from any side.
                vec3 orient = vec3(1.0 - UV.x, 1.0 - UV.y, 0.0) * 2.0 - vec3(1.0);
                vec3 offset = reflect((INV_VIEW_MATRIX * vec4(orient, 0.0)).xyz, INV_VIEW_MATRIX[0].xyz);
                VERTEX += (vec4(offset, 0.0) * MODEL_MATRIX).xyz * cards;
            }
            if (sway > 0.0) {
                float h = mode == 2 ? COLOR.r : clamp(VERTEX.y / sway_height, 0.0, 1.5);
                float t = TIME * 0.9 + world.x * 0.21 + world.z * 0.17;
                float gust = (0.45 + 0.45 * wind_gust + 0.15 * sin(TIME * 0.23 + world.x * 0.03)) * (0.5 + wind_strength * 1.6);
                // Leaves flutter a little on top of the whole crown's sway.
                float flutter = sin(TIME * 3.1 + dot(world, vec3(1.7, 2.3, 1.9))) * 0.25;
                VERTEX.x += (sin(t) + flutter) * sway * h * h * gust;
                VERTEX.z += (cos(t * 0.73) + flutter) * sway * 0.6 * h * h * gust;
            }
            if (crown_normals > 0.0) {
                // Bend each card's normal toward "out from the crown's heart", so the light
                // falls on the canopy as one rounded mass, as a painter would light it.
                vec3 heart = vec3(0.0, max(VERTEX.y * 0.6, 0.5 * sway_height), 0.0);
                NORMAL = normalize(mix(NORMAL, normalize(VERTEX - heart), 0.75 * crown_normals));
            }
            world_pos = world;
            tree_seed = fract(sin(dot(floor(origin.xz * 0.5), vec2(12.9898, 78.233))) * 43758.5453);
        }

        void fragment() {
            vec4 tex = has_tex ? texture(albedo_tex, UV) : vec4(1.0);
            if (cutout > 0.0 && tex.a < cutout) discard;
            vec3 color = tex.rgb * albedo.rgb;
            glow = vec3(0.0);
            if (mode == 1) {
                color = mix(leaf_low, leaf_top, height_mask);
                vec3 through = mix(color, glow_color, glow_strength);
                glow = through * max(height_mask, 0.1) * glow_blend;
            } else if (mode == 2) {
                vec2 uv = world_pos.xz / 50.0 + TIME * 0.03 * (0.4 + wind_strength) * normalize(wind_dir + vec2(1e-4));
                float line = clamp(pow(texture(wind_noise, uv).r, 5.0), 0.0, 1.0) * (0.3 + wind_strength);
                color = mix(grass_bottom, grass_top, vertex_r);
                color = mix(color, grass_line, line) * mix(vec3(1.0), grass_world, texture(world_noise, world_pos.xz / 50.0).r);
                NORMAL *= FRONT_FACING ? 1.0 : -1.0;
            } else if (mode == 3) {
                color = mix(color * bark_foot, color, vertex_r);
            } else if (mode == 4) {
                color *= vertex_r;
            } else if (tint_blend > 0.0) {
                // The instance colour Terrain gives a boulder: its hue (half) and its darkening.
                float lum = max(dot(COLOR.rgb, vec3(0.3, 0.59, 0.11)), 0.05);
                color *= mix(vec3(1.0), COLOR.rgb / lum, tint_blend) * min(lum / tint_ref, 1.0);
            }
            if (autumn > 0.0 && season_autumn > 0.0) {
                // Each tree its own mix of gold, orange and red, brown before they fall;
                // the leaves' light-dark painting is kept in the turn.
                float lum = dot(color, vec3(0.3, 0.59, 0.11));
                vec3 turned = mix(vec3(0.95, 0.62, 0.12), vec3(0.85, 0.24, 0.08), tree_seed) * (0.55 + lum * 1.4);
                turned = mix(turned, vec3(0.42, 0.28, 0.15) * (0.6 + lum), smoothstep(0.75, 1.0, season_autumn));
                color = mix(color, turned, smoothstep(0.0, 0.6, season_autumn));
            }
            if (has_normal) NORMAL_MAP = texture(normal_tex, UV).rgb;
            color *= mix(1.0, 0.8, wetness);
            ALBEDO = color;
            ROUGHNESS = 0.9;
            SPECULAR = 0.0;
        }

        void light() {
            float shade = 1.0;
            float dab = 0.5;
        """ + Painted.Light + """
            if (rim > 0.0) {
        """ + Painted.Rim + """
            }
            if (mode == 1 && LIGHT_IS_DIRECTIONAL) {
                // The kit's light through the leaves at the crown's edges, as sunlight rather than
                // a glow of its own, so it fades with the sun and stays warm at golden hour.
                float fresnel = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 3.0);
                SPECULAR_LIGHT += LIGHT_COLOR * glow * fresnel * mix(0.5, 1.0, ATTENUATION) / PI;
            }
        }
        """;
}
