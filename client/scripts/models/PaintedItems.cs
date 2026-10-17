using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Items on the painted look (painted-6): tools and things in the hand, items lying on the
// ground, and so the inventory icons, which ItemIcon renders from the ground mesh. Each
// family of material (wood handles, stone blades, cord, hide, clay, food) gets its own
// tuned twin of Painted.Of. Items are small on screen, so the ink edge is lighter and the
// brush dabs smaller than on rocks and trees.
//
// Held things move with the hand, and the brush is laid in world space, so a held twin
// keeps its dabs too big to see (no crawling over a swinging axe): it keeps the stepped
// light, the violet shade and the ink edge, which carry the look at that size.
public static class PaintedItems
{
    public enum Family { Wood, Stone, Fibre, Hide, Clay, Food, Plain }

    static readonly Dictionary<(Material, Family, bool), ShaderMaterial> Twins = new();
    // Each twin's source, so a painted mesh can be turned back (the "today" row) or
    // repainted for the hand.
    static readonly Dictionary<Material, (Material Source, Family Family)> Sources = new();
    static readonly Dictionary<(Mesh, Family, bool), Mesh> Meshes = new();

    // The painted twin of one of Look's materials for this family, on the ground or held.
    // Anything that isn't Look's (a pot's water) passes through; an item twin is first
    // traced back to its source, so asking again for the hand gives the held twin.
    public static Material Of(Material material, Family family, bool held = false)
    {
        lock (Twins)
        {
            if (Sources.TryGetValue(material, out var traced)) (material, family) = traced;
            if (material is not ShaderMaterial source || source.Shader != LookShader) return material;
            if (Twins.TryGetValue((source, family, held), out var twin)) return twin;
            twin = (ShaderMaterial)Painted.Of(source).Duplicate();
            Tune(twin, family, held);
            Twins[(source, family, held)] = twin;
            Sources[twin] = (source, family);
            return twin;
        }
    }

    // The material a painted item twin was made from (itself if it isn't one).
    public static Material Unpainted(Material material)
    {
        lock (Twins) return Sources.TryGetValue(material, out var traced) ? traced.Source : material;
    }

    // A copy of a mesh with each surface on its painted twin (for the items whose meshes
    // other lanes make: ceramics' pots, survival's berries, the first simple shapes).
    // Cached, so a mesh always gives the same copy and batches stay shared.
    public static Mesh Paint(Mesh source, Family family, bool held = false) =>
        Swap(source, family, held, m => Of(m, family, held));

    // Today's look of a painted mesh: the same surfaces on their source materials.
    public static Mesh Unpaint(Mesh painted) => Swap(painted, Family.Plain, false, Unpainted, cache: false);

    static Mesh Swap(Mesh source, Family family, bool held, System.Func<Material, Material> map, bool cache = true)
    {
        lock (Meshes)
        {
            if (cache && Meshes.TryGetValue((source, family, held), out var done)) return done;
            var mesh = new ArrayMesh();
            for (int s = 0; s < source.GetSurfaceCount(); s++)
            {
                mesh.AddSurfaceFromArrays(source is ArrayMesh array ? array.SurfaceGetPrimitiveType(s) : Mesh.PrimitiveType.Triangles, source.SurfaceGetArrays(s));
                var material = source.SurfaceGetMaterial(s);
                if (material != null) mesh.SurfaceSetMaterial(s, map(material));
            }
            if (cache) Meshes[(source, family, held)] = mesh;
            return mesh;
        }
    }

    // The family for an item's plain surface (and for the meshes other lanes make): what
    // the thing mostly is.
    public static Family FamilyOf(Item item) => item switch
    {
        Item.Stick or Item.Log or Item.DiggingStick or Item.Spear or Item.BowDrill => Family.Wood,
        Item.Stone or Item.Flake or Item.HandAxe or Item.StoneKnife or Item.StoneAxe => Family.Stone,
        Item.Fiber or Item.Tinder or Item.Cordage or Item.Basket or Item.Wool or Item.Feather => Family.Fibre,
        Item.SheepHide or Item.ScrapedHide or Item.Leather or Item.SleepingMat
            or Item.Waterskin or Item.WaterskinOfWater => Family.Hide,
        Item.RawMutton or Item.CookedMutton or Item.BurntMutton or Item.Berries
            or Item.Egg or Item.BoiledEgg or Item.BurntEgg
            or Item.RawChicken or Item.CookedChicken or Item.BurntChicken => Family.Food,
        // By number until the items reach main: clothing (D-074), the deer (animals-14) and wool (animals-8).
        (Item)Garments.WoolTunic or (Item)Garments.FootWraps or (Item)203 or (Item)198 or (Item)199 => Family.Hide,
        (Item)202 or (Item)214 or (Item)215 or (Item)216 or Item.CookedVenison or Item.BurntVenison or Item.Tallow => Family.Food,
        (Item)217 => Family.Hide,
        (Item)197 or (Item)204 or (Item)16 or (Item)17 or (Item)18 => Family.Wood,
        _ => ClayItemLook.MeshFor(item) != null ? Family.Clay : Family.Plain,
    };

    static void Tune(ShaderMaterial twin, Family family, bool held)
    {
        // Small things: a lighter ink edge and finer dabs.
        twin.SetShaderParameter("ink", 0.85f);
        twin.SetShaderParameter("dab_size", 0.32f);
        // Small things keep more of their relief than a boulder does, or a handle's grain
        // and a hide's folds wash out: Painted.Of keeps 0.35 of it, items 0.6.
        twin.SetShaderParameter("bump", (float)twin.GetShaderParameter("bump") * 1.7f);
        switch (family)
        {
            case Family.Wood:
                // Warm brown planes, the shade leaning plum, the grain's crevices a darker tone.
                twin.SetShaderParameter("shade_tint", new Color(0.68f, 0.6f, 0.82f));
                twin.SetShaderParameter("crevice", 0.36f);
                twin.SetShaderParameter("lit_dabs", 0.12f);
                break;
            case Family.Stone:
                // The rocks' stone, a little of the flint's own colour left in.
                twin.SetShaderParameter("base_paint", new Color(0.6f, 0.6f, 0.65f));
                twin.SetShaderParameter("base_mix", 0.3f);
                twin.SetShaderParameter("shade_tint", new Color(0.56f, 0.53f, 0.8f));
                twin.SetShaderParameter("crevice", 0.22f);
                twin.SetShaderParameter("lit_dabs", 0.1f);
                twin.SetShaderParameter("ink", 0.95f);
                break;
            case Family.Fibre:
                twin.SetShaderParameter("shade_tint", new Color(0.7f, 0.66f, 0.84f));
                twin.SetShaderParameter("crevice", 0.3f);
                twin.SetShaderParameter("lit_dabs", 0.18f);
                break;
            case Family.Hide:
                // Soft hide and cloth: few lit dabs, a warm shade, leaning a little toward a
                // tan so pale hide and wool don't burn out to white in the stepped light.
                twin.SetShaderParameter("base_paint", new Color(0.74f, 0.62f, 0.48f));
                twin.SetShaderParameter("base_mix", 0.25f);
                twin.SetShaderParameter("shade_tint", new Color(0.74f, 0.64f, 0.82f));
                twin.SetShaderParameter("crevice", 0.34f);
                twin.SetShaderParameter("lit_dabs", 0.08f);
                break;
            case Family.Clay:
                twin.SetShaderParameter("shade_tint", new Color(0.72f, 0.6f, 0.8f));
                twin.SetShaderParameter("lit_dabs", 0.1f);
                break;
            case Family.Food:
                // Food keeps a glint: more lit dabs, a rosy shade, the lightest ink.
                twin.SetShaderParameter("shade_tint", new Color(0.8f, 0.62f, 0.78f));
                twin.SetShaderParameter("lit_dabs", 0.14f);
                twin.SetShaderParameter("ink", 0.65f);
                break;
        }
        if (held)
        {
            // Dabs too big to see, so nothing crawls over a moving tool.
            twin.SetShaderParameter("dab_size", 60f);
            twin.SetShaderParameter("lit_dabs", 0f);
        }
    }

    static Shader LookShader => lookShader ??= Look.Solid(Colors.White).Shader;
    static Shader? lookShader;
}
