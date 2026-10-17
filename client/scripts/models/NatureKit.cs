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

    public enum Kind { Leaves, Bark, Rock, Plant, Flower }

    static readonly Dictionary<(string, bool), Mesh?> Meshes = new();

    // The world's trees drawn from the kit (naturekit-1, behind a setting until Justin picks):
    // --naturekit on the command line, or graphics/naturekit_trees = 1 in the settings.
    static readonly bool Wanted = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--naturekit") >= 0;
    public static bool Trees => Wanted || Settings.Get("graphics", "naturekit_trees", 0) == 1;

    // Our tree variants drawn as the kit's: broadleaf v as CommonTree_(v+1), pine v as Pine_(v+1).
    static string TreeName(bool pine, int v) => pine ? $"Pine_{v + 1}" : $"CommonTree_{v + 1}";
    static readonly Dictionary<(bool, int), (Mesh Mesh, float Scale)> TreeMeshes = new();

    // Loads the kit trees on the main thread (ChunkWorker.Prewarm), so chunk workers only read.
    public static void Prewarm()
    {
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
        lock (TreeMeshes) return TreeMeshes.TryGetValue((pine, variant), out var t) ? t : null;
    }

    // Terrain's tree placements scaled by a kit tree's extra scale.
    public static List<(Transform3D, Color, Color)> Scaled(List<(Transform3D, Color, Color)> items, float scale) =>
        items.ConvertAll(i => (new Transform3D(i.Item1.Basis * Basis.FromScale(Vector3.One * scale), i.Item1.Origin), i.Item2, i.Item3));
    static readonly Dictionary<(Texture2D?, Kind), ShaderMaterial> Materials = new();

    // The model's mesh with the blended look (or the kit's own materials), null if the asset
    // isn't there (not imported yet).
    public static Mesh? Mesh(string name, bool raw = false)
    {
        lock (Meshes)
        {
            if (Meshes.TryGetValue((name, raw), out var cached)) return cached;
            var mesh = Load(name, raw);
            Meshes[(name, raw)] = mesh;
            return mesh;
        }
    }

    static Mesh? Load(string name, bool raw)
    {
        string path = Dir + name + ".gltf";
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
                    found.SurfaceSetMaterial(s, Blended(source, KindOf(name, source)));
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

    static Kind KindOf(string model, Material? material)
    {
        string m = material?.ResourceName ?? "";
        if (m.StartsWith("Bark")) return Kind.Bark;
        if (m.StartsWith("Rocks") || m.StartsWith("PathRocks")) return Kind.Rock;
        if (m.StartsWith("Flowers") || m.StartsWith("Mushrooms")) return Kind.Flower;
        if (model.StartsWith("CommonTree") || model.StartsWith("Pine") || model.StartsWith("TwistedTree") || model.StartsWith("Bush")) return Kind.Leaves;
        return Kind.Plant;
    }

    // Our material for one of the kit's: its texture, our light.
    public static ShaderMaterial Blended(Material? source, Kind kind)
    {
        var texture = (source as BaseMaterial3D)?.AlbedoTexture;
        lock (Materials)
        {
            if (Materials.TryGetValue((texture, kind), out var done)) return done;
            var m = new ShaderMaterial { Shader = Shader };
            if (texture != null) m.SetShaderParameter("albedo_tex", texture);
            m.SetShaderParameter("has_tex", texture != null);
            m.SetShaderParameter("albedo", (source as BaseMaterial3D)?.AlbedoColor ?? Colors.White);
            bool cut = kind is Kind.Leaves or Kind.Plant or Kind.Flower;
            m.SetShaderParameter("cutout", cut ? 0.5f : 0f);
            // Leaves sway from the crown, plants from their roots.
            m.SetShaderParameter("sway", kind switch { Kind.Leaves => 0.07f, Kind.Plant => 0.05f, Kind.Flower => 0.03f, _ => 0f });
            m.SetShaderParameter("sway_height", kind == Kind.Leaves ? 6f : 0.6f);
            m.SetShaderParameter("autumn", kind == Kind.Leaves ? 1f : 0f);
            // Leaf cards: the crown is lit as one soft mass rather than card by card.
            m.SetShaderParameter("crown_normals", kind == Kind.Leaves ? 1f : 0f);
            m.SetShaderParameter("rim", kind is Kind.Rock ? 1f : 0f);
            m.SetShaderParameter("shade_tint", kind switch
            {
                Kind.Rock => new Color(0.66f, 0.64f, 0.76f),
                Kind.Bark => new Color(0.68f, 0.6f, 0.82f),
                _ => new Color(0.62f, 0.66f, 0.86f),
            });
            Materials[(texture, kind)] = m;
            return m;
        }
    }

    static Shader? shader;
    static Shader Shader => shader ??= new Shader { Code = Code };

    static readonly string Code = """
        shader_type spatial;
        render_mode cull_disabled, depth_draw_opaque;
        """ + Painted.RimUniform + """

        global uniform float wind_strength;
        global uniform float wind_gust;
        global uniform float season_autumn;
        global uniform float wetness;
        uniform sampler2D albedo_tex : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
        uniform bool has_tex = true;
        uniform vec4 albedo : source_color = vec4(1.0);
        uniform float cutout = 0.0;
        uniform float sway = 0.0;
        uniform float sway_height = 1.0;
        uniform float autumn = 0.0;
        uniform float crown_normals = 0.0;
        uniform float rim = 0.0;
        uniform vec3 shade_tint : source_color = vec3(0.66, 0.64, 0.8);
        const float BAND_SOFT = 0.1;
        const float BAND_BREAK = 0.0;
        varying vec3 world_pos;
        varying float tree_seed;

        void vertex() {
            vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
            vec3 origin = MODEL_MATRIX[3].xyz;
            if (sway > 0.0) {
                float h = clamp(VERTEX.y / sway_height, 0.0, 1.5);
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
            vec3 color = tex.rgb * albedo.rgb;
            if (cutout > 0.0 && tex.a < cutout) discard;
            if (autumn > 0.0 && season_autumn > 0.0) {
                // Each tree its own mix of gold, orange and red, brown before they fall;
                // the kit's own light-dark leaf painting is kept in the turn.
                float lum = dot(color, vec3(0.3, 0.59, 0.11));
                vec3 turned = mix(vec3(0.95, 0.62, 0.12), vec3(0.85, 0.24, 0.08), tree_seed) * (0.55 + lum * 1.4);
                turned = mix(turned, vec3(0.42, 0.28, 0.15) * (0.6 + lum), smoothstep(0.75, 1.0, season_autumn));
                color = mix(color, turned, smoothstep(0.0, 0.6, season_autumn));
            }
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
        }
        """;
}
