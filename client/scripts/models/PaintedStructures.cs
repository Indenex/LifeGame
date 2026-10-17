using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The painted look for what players build (painted-5): huts, lean-tos, cabins, walls,
// doors, roofs and thatch, the coop, hearths, stations, fences and the fire ring, on
// Painted's shader, so they share the stepped light, violet shade and soft ink edge of
// the painted rocks and trees. StructureModel hands every piece's material through Of
// with the family it belongs to; each source gets its own tuned twin, so per-material
// tints (pale wood, ash, mud) keep working.
public static class PaintedStructures
{
    public enum Family { Plain, Wood, Thatch, Stone, Clay }

    static readonly Dictionary<Material, ShaderMaterial> Twins = new();
    static readonly Dictionary<Material, Material> Sources = new();

    // The painted twin of a structure material. Anything not drawn with Look's shader
    // (a StandardMaterial3D, say) and anything already painted comes back as it is.
    public static Material Of(Material? source, Family family)
    {
        if (source is not ShaderMaterial material || Painted.IsPainted(material)) return source!;
        if (material.GetShaderParameter("bump").VariantType != Variant.Type.Float) return source;
        lock (Twins)
        {
            if (Twins.TryGetValue(material, out var cached)) return cached;
            var twin = (ShaderMaterial)Painted.Of(material).Duplicate();
            Tune(twin, material, family);
            Sources[twin] = material;
            return Twins[material] = twin;
        }
    }

    // The material a painted twin was made from (for the Showcase's before-and-after).
    public static Material Unpainted(Material material)
    {
        lock (Twins) return Sources.TryGetValue(material, out var source) ? source : material;
    }

    // The Quaternius village kit's painted surfaces (building-26, Justin's kit): wood grain on
    // timber, plaster strokes on daub, round stones on footings. Each kit texture keeps its
    // average colour beside it, so hue can blend between the kit's colour and the material's own.
    public enum Surface { WoodLight, WoodDark, Plaster, RoundRocks }

    static readonly Dictionary<(Material, Surface), ShaderMaterial> Surfaced = new();
    // --no-kit draws structures as before the kit, for before-and-after shots.
    static readonly bool NoKit = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--no-kit") >= 0;

    static (string file, Color mean, float scale) KitSurface(Surface surface) => surface switch
    {
        // Sticks and logs carry UVs in metres: one repeat runs 1.6 m along the grain.
        Surface.WoodLight => ("res://assets/quaternius/village/textures/wood_light.png", Color.Color8(146, 100, 56), 1.6f),
        Surface.WoodDark => ("res://assets/quaternius/village/textures/wood_dark.png", Color.Color8(104, 67, 39), 1.6f),
        Surface.Plaster => ("res://assets/quaternius/village/textures/T_Plaster_BaseColor.png", Color.Color8(175, 158, 127), 2.2f),
        _ => ("res://assets/quaternius/village/textures/T_RoundRocks_BaseColor.png", Color.Color8(78, 76, 83), 1.4f),
    };

    // A painted structure material with a kit surface laid over it. mix is how much of the kit
    // shows; hue 1 keeps the material's own colour and takes only the kit's strokes.
    public static Material WithSurface(Material painted, Surface surface, float mix = 1f, float hue = 0f)
    {
        if (NoKit || painted is not ShaderMaterial material || !Painted.IsPainted(material)) return painted;
        lock (Twins)
        {
            if (Surfaced.TryGetValue((painted, surface), out var cached)) return cached;
            var (file, mean, scale) = KitSurface(surface);
            var twin = (ShaderMaterial)material.Duplicate();
            twin.SetShaderParameter("surface_tex", GD.Load<Texture2D>(file));
            twin.SetShaderParameter("surface_mean", mean);
            twin.SetShaderParameter("surface_scale", scale);
            twin.SetShaderParameter("surface_mix", mix);
            twin.SetShaderParameter("surface_hue", hue);
            // The kit's own colour carries the surface; don't pull it toward the family's palette.
            twin.SetShaderParameter("base_mix", (float)twin.GetShaderParameter("base_mix") * hue);
            if (Sources.TryGetValue(material, out var source)) Sources[twin] = source;
            return Surfaced[(painted, surface)] = twin;
        }
    }

    static void Tune(ShaderMaterial twin, ShaderMaterial source, Family family)
    {
        switch (family)
        {
            // Bark and split wood: warm brown planes, violet in the shade, the grain's
            // grooves in one darker tone, a few light dabs along the tops of the logs.
            case Family.Wood:
                twin.SetShaderParameter("base_paint", new Color(0.66f, 0.46f, 0.3f));
                twin.SetShaderParameter("base_mix", 0.3f);
                twin.SetShaderParameter("shade_tint", new Color(0.6f, 0.52f, 0.82f));
                twin.SetShaderParameter("crevice", 0.24f);
                twin.SetShaderParameter("lit_dabs", 0.14f);
                twin.SetShaderParameter("dab_size", 0.9f);
                break;
            // Thatch, lashing and weave: golden straw in small strokes, sunlit dabs on the
            // slopes, barely any crevice tone so the bundles read soft.
            case Family.Thatch:
                // Deeper gold than today's straw, and more of the relief kept, so the bundles
                // still read as straw once the brush has flattened the colour.
                twin.SetShaderParameter("albedo", (Color)source.GetShaderParameter("albedo") * new Color(0.86f, 0.84f, 0.8f));
                twin.SetShaderParameter("bump", 0.75f * (float)source.GetShaderParameter("bump"));
                twin.SetShaderParameter("base_paint", new Color(0.82f, 0.6f, 0.28f));
                twin.SetShaderParameter("base_mix", 0.3f);
                twin.SetShaderParameter("shade_tint", new Color(0.68f, 0.58f, 0.84f));
                twin.SetShaderParameter("crevice", 0.3f);
                twin.SetShaderParameter("lit_dabs", 0.2f);
                twin.SetShaderParameter("dab_size", 0.5f);
                break;
            // Hearth and wall stones: as Painted.Rock, the cards' pale grey and blue-violet.
            case Family.Stone:
                twin.SetShaderParameter("base_paint", new Color(0.6f, 0.6f, 0.65f));
                twin.SetShaderParameter("base_mix", 0.5f);
                twin.SetShaderParameter("shade_tint", new Color(0.56f, 0.53f, 0.8f));
                twin.SetShaderParameter("crevice", 0.22f);
                twin.SetShaderParameter("lit_dabs", 0.08f);
                break;
            // Daub, plaster, brick, tile and mud: their own earthy colours kept, smooth broad
            // strokes, soft crevices.
            case Family.Clay:
                twin.SetShaderParameter("shade_tint", new Color(0.64f, 0.56f, 0.84f));
                twin.SetShaderParameter("crevice", 0.18f);
                twin.SetShaderParameter("lit_dabs", 0.1f);
                twin.SetShaderParameter("dab_size", 1.3f);
                break;
            // Vertex-coloured station and prop meshes: like the animals' coats.
            default:
                twin.SetShaderParameter("shade_tint", new Color(0.62f, 0.56f, 0.86f));
                twin.SetShaderParameter("crevice", 0.15f);
                twin.SetShaderParameter("lit_dabs", 0.12f);
                twin.SetShaderParameter("dab_size", 1.1f);
                break;
        }
    }
}
