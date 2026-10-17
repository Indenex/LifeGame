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
                    float scale = PaintedTrees.Height / tall;
                    var (world, ghost) = SeeThrough(mesh, scale);
                    lock (TreeMeshes)
                    {
                        TreeMeshes[(pine, v)] = (world, scale);
                        TreeGhosts[world] = ghost;
                    }
                }
    }

    static readonly Dictionary<Mesh, Mesh> TreeGhosts = new();

    // The world's copy of a kit tree, its surfaces fading when the tree hides the player,
    // and its ghost: the same tree drawn see-through, which stands in while it fades
    // (Terrain.AddMulti draws it beside the solid, as Look.GhostOf does for ours). Fading
    // reads the tree's size from its scale, which for a kit tree carries `scale` on top.
    static (Mesh World, Mesh Ghost) SeeThrough(Mesh mesh, float scale)
    {
        var world = (ArrayMesh)mesh.Duplicate();
        var ghost = (ArrayMesh)mesh.Duplicate();
        for (int s = 0; s < world.GetSurfaceCount(); s++)
        {
            if (world.SurfaceGetMaterial(s) is not ShaderMaterial solid) continue;
            var faded = (ShaderMaterial)solid.Duplicate();
            faded.SetShaderParameter("see_through", true);
            faded.SetShaderParameter("fade_size", 1f / scale);
            world.SurfaceSetMaterial(s, faded);
            ghost.SurfaceSetMaterial(s, Look.Ghost(faded, GhostShader));
        }
        return (world, ghost);
    }

    // The see-through copy of a kit tree's world mesh, or null for any other mesh.
    public static Mesh? GhostOf(Mesh mesh)
    {
        lock (TreeMeshes) return TreeGhosts.GetValueOrDefault(mesh);
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

    static readonly Dictionary<Mesh, Mesh> FelledMeshes = new();

    // A kit tree sawn off at the stump, for TreeFall to drop (the stump stays standing):
    // the world copy without the triangles that reach below the cut. Null for other meshes.
    public static Mesh? Felled(Mesh mesh)
    {
        lock (TreeMeshes)
        {
            if (FelledMeshes.TryGetValue(mesh, out var done)) return done;
            float scale = 0f;
            foreach (var t in TreeMeshes.Values) if (t.Mesh == mesh) scale = t.Scale;
            if (scale == 0f || mesh is not ArrayMesh source) return null;
            float cut = LifeGame.Shared.Trees.StumpHeight / scale;
            var felled = new ArrayMesh();
            for (int s = 0; s < source.GetSurfaceCount(); s++)
            {
                var arrays = source.SurfaceGetArrays(s);
                var vertices = arrays[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array();
                var indices = arrays[(int)Godot.Mesh.ArrayType.Index].VariantType == Variant.Type.Nil
                    ? System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Range(0, vertices.Length))
                    : arrays[(int)Godot.Mesh.ArrayType.Index].AsInt32Array();
                var kept = new List<int>(indices.Length);
                for (int i = 0; i + 2 < indices.Length; i += 3)
                    if (vertices[indices[i]].Y >= cut && vertices[indices[i + 1]].Y >= cut && vertices[indices[i + 2]].Y >= cut)
                        kept.AddRange(new[] { indices[i], indices[i + 1], indices[i + 2] });
                // Close the sawn end of the trunk: a disc where its triangles crossed the cut.
                if (source.SurfaceGetMaterial(s) is ShaderMaterial bark && (float)bark.GetShaderParameter("cutout") == 0f)
                    CapCut(arrays, vertices, indices, kept, cut);
                if (kept.Count == 0) continue;
                arrays[(int)Godot.Mesh.ArrayType.Index] = kept.ToArray();
                felled.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
                felled.SurfaceSetMaterial(felled.GetSurfaceCount() - 1, source.SurfaceGetMaterial(s));
            }
            FelledMeshes[mesh] = felled;
            return felled;
        }
    }

    // Adds a flat disc at height cut over the trunk's cross-section there, from the edges of
    // the triangles that cross it, appending its vertices to arrays and its triangles to kept.
    static void CapCut(Godot.Collections.Array arrays, Vector3[] vertices, int[] indices, List<int> kept, float cut)
    {
        var rim = new List<Vector3>();
        for (int i = 0; i + 2 < indices.Length; i += 3)
            for (int e = 0; e < 3; e++)
            {
                Vector3 a = vertices[indices[i + e]], b = vertices[indices[i + (e + 1) % 3]];
                if ((a.Y < cut) == (b.Y < cut)) continue;
                rim.Add(a.Lerp(b, (cut - a.Y) / (b.Y - a.Y)));
            }
        if (rim.Count < 3) return;
        // The main trunk: the crossing points nearest the middle (branches don't reach this low).
        var centre = Vector3.Zero;
        foreach (var p in rim) centre += p;
        centre /= rim.Count;
        float radius = 0f;
        foreach (var p in rim) radius += new Vector2(p.X - centre.X, p.Z - centre.Z).Length();
        radius /= rim.Count;
        if (radius <= 0f) return;
        const int Sides = 10;
        int first = vertices.Length;
        var points = new List<Vector3>(vertices) { centre };
        for (int k = 0; k < Sides; k++)
        {
            float a = k * Mathf.Tau / Sides;
            points.Add(centre + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius * 1.02f);
        }
        for (int k = 0; k < Sides; k++)
            kept.AddRange(new[] { first, first + 1 + (k + 1) % Sides, first + 1 + k });
        // The other arrays grow to match: the disc faces down, with the bark texture's middle.
        int added = points.Count - vertices.Length;
        arrays[(int)Godot.Mesh.ArrayType.Vertex] = points.ToArray();
        void Grow<T>(Godot.Mesh.ArrayType type, T value, System.Func<Variant, T[]> read, System.Func<T[], Variant> write)
        {
            var old = arrays[(int)type];
            if (old.VariantType == Variant.Type.Nil) return;
            var list = new List<T>(read(old));
            for (int n = 0; n < added; n++) list.Add(value);
            arrays[(int)type] = write(list.ToArray());
        }
        Grow(Godot.Mesh.ArrayType.Normal, Vector3.Down, v => v.AsVector3Array(), a => a);
        Grow(Godot.Mesh.ArrayType.TexUV, new Vector2(0.5f, 0.5f), v => v.AsVector2Array(), a => a);
        Grow(Godot.Mesh.ArrayType.Color, Colors.White, v => v.AsColorArray(), a => a);
        Grow(Godot.Mesh.ArrayType.Tangent, 0f, v => v.AsFloat32Array(), a => a);
        if (arrays[(int)Godot.Mesh.ArrayType.Tangent].VariantType != Variant.Type.Nil)
        {
            // Tangents are four floats a vertex: pad the rest of each new one.
            var t = new List<float>(arrays[(int)Godot.Mesh.ArrayType.Tangent].AsFloat32Array());
            t.RemoveRange(t.Count - added, added);
            for (int n = 0; n < added; n++) t.AddRange(new[] { 1f, 0f, 0f, 1f });
            arrays[(int)Godot.Mesh.ArrayType.Tangent] = t.ToArray();
        }
        Grow(Godot.Mesh.ArrayType.TexUV2, Vector2.Zero, v => v.AsVector2Array(), a => a);
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

    const bool TreesTextured = true;

    // Kit trees whose leaves are painted in autumn colours: the green they wear until autumn
    // (near the kit's common tree's leaves, the birch a little fresher) and the mean
    // luminance of their leaf texture, so the painting's light and dark carry into the green.
    static (Color Green, float Reference)? AutumnLeaves(string model) =>
        model.StartsWith("Birch") ? (new Color(0.48f, 0.62f, 0.06f), 0.38f)
        : model.StartsWith("TwistedTree") ? (new Color(0.42f, 0.58f, 0.03f), 0.26f)
        : null;

    // How bright the kit trees' textures sit (Justin, 2026-10-17: "WAY too bright"): their
    // value scaled down and colour eased a little toward grey, into our forest's range. The
    // showcase takes --tree-value=0.85 and so on to compare steps.
    static readonly float TreeValue = Arg("tree-value", 0.7f);
    const float TreeSaturation = 0.88f;

    static float Arg(string name, float fallback)
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--" + name + "=") && float.TryParse(arg[(name.Length + 3)..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                return v;
        return fallback;
    }

    static bool IsTreeModel(string model) =>
        model.StartsWith("CommonTree") || model.StartsWith("Pine") || model.StartsWith("GiantPine")
        || model.StartsWith("Birch") || model.StartsWith("TwistedTree") || model.StartsWith("DeadTree");

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
        // Trees wear the kit's own shading (Justin, 2026-10-17: "I like the middle row better
        // for trees"): plain light rather than our bands and violet shade, the leaves' own
        // normals, and only our wind, autumn and day/night on top. Rocks and plants stay blended.
        bool tree = kind == Kind.Bark || kind == Kind.Leaves && IsTreeModel(model);
        if (tree) look += "_tree";
        // The row he picked is the kit's models with their painted textures as they come
        // (its Standard materials). The kit's Godot shaders paint leaves one flat, lighter
        // green instead (LeafLook); TreesTextured = false switches to that.
        bool textured = tree && TreesTextured;
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
            m.SetShaderParameter("mode", textured ? 0 : kind switch { Kind.Leaves => 1, Kind.Grass => 2, Kind.Bark => 3, Kind.Plant or Kind.Flower => 4, _ => 0 });
            if (leaf is { } l && !textured)
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
            m.SetShaderParameter("crown_normals", kind == Kind.Leaves && !tree ? 1f : 0f);
            m.SetShaderParameter("kit_light", tree);
            if (tree)
            {
                m.SetShaderParameter("tree_value", TreeValue);
                m.SetShaderParameter("tree_saturation", TreeSaturation);
                // Giant pines in our pines' deep green (Justin's pick) rather than the kit's fresher one.
                if (textured && kind == Kind.Leaves && model.StartsWith("GiantPine")) m.SetShaderParameter("albedo", new Color(0.83f, 0.81f, 0.9f));
                // Birches and twisted trees green until autumn (Justin's pick): our green in
                // spring and summer, turning to the kit's own red and orange in the fall.
                if (textured && kind == Kind.Leaves && AutumnLeaves(model) is var (green, reference))
                {
                    m.SetShaderParameter("kit_autumn", true);
                    m.SetShaderParameter("summer_green", green);
                    m.SetShaderParameter("summer_ref", reference);
                }
            }
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

    static Shader? shader, ghostShader;
    static Shader Shader => shader ??= new Shader { Code = Code };
    // The same look drawn see-through, for a tree fading out of the camera's way.
    static Shader GhostShader => ghostShader ??= new Shader
    {
        Code = Code.Replace("shader_type spatial;", "shader_type spatial;\n#define GHOST")
            .Replace("render_mode cull_disabled, depth_draw_opaque;", "render_mode cull_disabled, depth_prepass_alpha;"),
    };

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
        global uniform float world_seconds;
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
        uniform bool see_through = false;
        uniform bool kit_light = false;
        uniform float tree_value = 1.0;
        uniform float tree_saturation = 1.0;
        uniform bool kit_autumn = false;
        uniform vec3 summer_green : source_color = vec3(0.35, 0.48, 0.0);
        uniform float summer_ref = 0.4;
        uniform float fade_size = 1.0;
        global uniform vec3 player_position;
        global uniform vec3 view_camera_position;
        varying float fade;

        // Look's tree fade (fade_amount there): how far a tree hides the player, 0 not at
        // all to 1 right across the line from the camera, for an upright capsule round the
        // crown. A kit tree's own size is its scale times fade_size.
        float fade_amount(mat4 model, vec3 cam) {
            if (distance(cam, view_camera_position) > 0.5) return 0.0;
            float tall = length(model[1].xyz) * fade_size;
            float wide = length(model[0].xyz) * fade_size;
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
            return 1.0 - smoothstep(1.5 * wide, 2.1 * wide, d);
        }
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
            // A tree or rock on a cell just cleared goes away rather than vanishing, and one
            // coming back grows in, as Look's painted trees do. Custom data: x when it
            // started, y +1 going or -1 coming, z 1 for rocks.
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
                    float dir = fract(sin(dot(floor(MODEL_MATRIX[3].xz + 0.5), vec2(12.9898, 78.233))) * 43758.5453) * 6.2832;
                    vec3 axis = normalize(inverse(mat3(MODEL_MATRIX)) * vec3(sin(dir), 0.0, -cos(dir)));
                    float angle = 1.45 * smoothstep(0.0, 0.7, k) * smoothstep(0.0, 0.7, k);
                    float cs = cos(angle), sn = sin(angle);
                    VERTEX = VERTEX * cs + cross(axis, VERTEX) * sn + axis * dot(axis, VERTEX) * (1.0 - cs);
                    NORMAL = NORMAL * cs + cross(axis, NORMAL) * sn + axis * dot(axis, NORMAL) * (1.0 - cs);
                    VERTEX *= 1.0 - smoothstep(0.6, 1.0, k);
                }
            }
            fade = see_through ? fade_amount(MODEL_MATRIX, CAMERA_POSITION_WORLD) : 0.0;
        #ifdef GHOST
            // The ghost draws only a tree in the way; the solid draws the rest.
            if (fade < 0.002) VERTEX = vec3(0.0);
        #else
            if (fade >= 0.002) VERTEX = vec3(0.0);
        #endif
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
            if (kit_autumn) {
                // Leaves painted in autumn colours: green the rest of the year, the paint's own
                // red and orange as the turn, brown before they fall as the others.
                float lum = dot(color, vec3(0.3, 0.59, 0.11));
                vec3 green = summer_green * (lum / summer_ref);
                vec3 turned = mix(color, vec3(0.42, 0.28, 0.15) * (0.6 + lum), smoothstep(0.75, 1.0, season_autumn));
                color = mix(green, turned, smoothstep(0.0, 0.6, season_autumn));
            } else if (autumn > 0.0 && season_autumn > 0.0) {
                // Each tree its own mix of gold, orange and red, brown before they fall;
                // the leaves' light-dark painting is kept in the turn.
                float lum = dot(color, vec3(0.3, 0.59, 0.11));
                vec3 turned = mix(vec3(0.95, 0.62, 0.12), vec3(0.85, 0.24, 0.08), tree_seed) * (0.55 + lum * 1.4);
                turned = mix(turned, vec3(0.42, 0.28, 0.15) * (0.6 + lum), smoothstep(0.75, 1.0, season_autumn));
                color = mix(color, turned, smoothstep(0.0, 0.6, season_autumn));
            }
            if (has_normal) NORMAL_MAP = texture(normal_tex, UV).rgb;
            // Each kit tree a shade lighter or darker than its neighbours, as ours are.
            if (kit_light) {
                color *= 0.9 + 0.1 * tree_seed;
                color = mix(vec3(dot(color, vec3(0.3, 0.59, 0.11))), color, tree_saturation) * tree_value;
            }
            color *= mix(1.0, 0.8, wetness);
            ALBEDO = color;
        #ifdef GHOST
            // As Look's ghosts: thinned to about 55%, a little more round the player on screen.
            vec4 target = VIEW_MATRIX * vec4(player_position + vec3(0.0, 0.9, 0.0), 1.0);
            float apart = length(VERTEX.xy / -VERTEX.z - target.xy / -target.z);
            float opening = 1.0 - smoothstep(0.04, 0.42, apart);
            ALPHA = 1.0 - fade * (0.45 + 0.2 * opening * opening);
        #endif
            ROUGHNESS = 0.9;
            SPECULAR = 0.0;
        }

        void light() {
            if (kit_light) {
                // The kit's own: plain Lambert light, as its M_Leaves and M_Bark have.
                DIFFUSE_LIGHT += LIGHT_COLOR * ATTENUATION * clamp(dot(NORMAL, LIGHT), 0.0, 1.0) / PI;
            } else {
            float shade = 1.0;
            float dab = 0.5;
        """ + Painted.Light + """
            if (rim > 0.0) {
        """ + Painted.Rim + """
            }
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
