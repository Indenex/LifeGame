using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Proper models for items (model-1, D-082): one design per item, used lying on
// the ground (and so for the inventory icon, which ItemIcon renders from the
// ground mesh) and held in the hand.
//
// Long things (sticks, tools) are made as held: the grip at the origin, the
// working end toward -Y, the edge or point facing +Z, the thin side across X.
// That is the frame of the player's ToolR attachment (HumanRig HandR). On the
// ground they are laid on their flat side. Upright things (a stone, a basket,
// meat) are made as they rest on the ground; held, the hand is at their top.
public static class ItemModels
{
    // One surface per kind of material, so wood, stone and plants each get their
    // own relief on the ground. Held items draw plain: the relief is mapped in
    // world space and would crawl over a moving tool.
    sealed class Kit
    {
        public readonly MeshKit Wood, Stone, Plant, Plain;

        public Kit(bool flat, Transform3D xform)
        {
            Wood = New(flat, xform);
            Stone = New(flat, xform);
            Plant = New(flat, xform);
            Plain = New(flat, xform);
        }

        static MeshKit New(bool flat, Transform3D xform) => new(flat) { Skinned = false, Xform = xform };
    }

    static readonly Dictionary<(Item, int), Mesh?> Ground = new(), Held = new();
    static Material? wood, stone, plant, plain;

    // Laid on the ground, resting on y = 0 and centered; null if this item has no
    // model here yet (ItemLook falls back to its own).
    // wear: 0 as new, 1 worn, 2 spent (see WearLevel); only tools that wear use it.
    public static Mesh? GroundMesh(Item item, int wear = 0)
    {
        wear = Mathf.Clamp(wear, 0, 2);
        if (!Ground.TryGetValue((item, wear), out var mesh)) Ground[(item, wear)] = mesh = Make(item, false, wear);
        return mesh;
    }

    // In the hand, in the ToolR frame; items without a model here (the clay pots)
    // use their ground look, held by the top like a pot by its rim.
    public static Mesh HeldMesh(Item item, int wear = 0)
    {
        wear = Mathf.Clamp(wear, 0, 2);
        if (Held.TryGetValue((item, wear), out var mesh) && mesh != null) return mesh;
        mesh = Make(item, true, wear) ?? HungFromTop(ItemLook.MeshFor(item), PaintedItems.FamilyOf(item));
        Held[(item, wear)] = mesh;
        return mesh;
    }

    // A tool's look from its condition (Crafting's Durability.Condition, D-155):
    // "" as new, "worn", "nearly spent" and "spent".
    public static int WearLevel(string condition) => condition switch
    {
        "worn" => 1,
        "nearly spent" or "spent" => 2,
        _ => 0,
    };

    static Mesh HungFromTop(Mesh source, PaintedItems.Family family)
    {
        var mesh = new ArrayMesh();
        var box = source.GetAabb();
        var shift = new Vector3(-box.GetCenter().X, -box.End.Y, -box.GetCenter().Z);
        for (int s = 0; s < source.GetSurfaceCount(); s++)
        {
            var arrays = source.SurfaceGetArrays(s);
            var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            for (int i = 0; i < verts.Length; i++) verts[i] += shift;
            arrays[(int)Mesh.ArrayType.Vertex] = verts;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, PaintedItems.Of(source.SurfaceGetMaterial(s), family, held: true));
        }
        return mesh;
    }

    static Mesh? Make(Item item, bool held, int wear = 0)
    {
        var (build, upright) = Recipe(item);
        if (build == null) return null;
        // Only tools that wear have a worn look; everything else ignores it.
        if (!WearMarks.ContainsKey(item)) wear = 0;
        // Laying down turns the held frame so -Y (the working end) points along +X
        // and the thin side faces up.
        var lay = upright || held ? Transform3D.Identity : new Transform3D(new Basis(Vector3.Back, Mathf.Pi / 2f), Vector3.Zero);
        var kit = new Kit(PlayerModel.Style.Flat, lay);
        build(kit);
        if (wear > 0) WearMarks[item](kit, wear);

        EnsureMaterials();
        var surfaces = new List<(Godot.Collections.Array Arrays, Material Material)>();
        // Use darkens wood and cord: handled, sweaty, weathered.
        // Each surface goes on its family's painted twin (painted-6).
        void Take(MeshKit k, Material groundMaterial, float darken, PaintedItems.Family family)
        {
            if (k.Empty) return;
            var arrays = k.CommitToArrays();
            if (darken < 1f && arrays[(int)Mesh.ArrayType.Color].Obj is Color[] colors)
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    var c = colors[i];
                    float grey = (c.R + c.G + c.B) / 3f;
                    colors[i] = new Color(Mathf.Lerp(grey, c.R, 0.8f) * darken, Mathf.Lerp(grey, c.G, 0.8f) * darken, Mathf.Lerp(grey, c.B, 0.8f) * darken, c.A);
                }
                arrays[(int)Mesh.ArrayType.Color] = colors;
            }
            surfaces.Add((arrays, PaintedItems.Of(held ? plain! : groundMaterial, family, held)));
        }
        Take(kit.Wood, wood!, wear == 0 ? 1f : wear == 1 ? 0.82f : 0.6f, PaintedItems.Family.Wood);
        Take(kit.Stone, stone!, 1f, PaintedItems.Family.Stone);
        Take(kit.Plant, plant!, wear == 0 ? 1f : wear == 1 ? 0.84f : 0.62f, PaintedItems.Family.Fibre);
        Take(kit.Plain, plain!, 1f, PaintedItems.FamilyOf(item));

        // Rest on the ground, centered; held upright things hang from their top.
        var min = Vector3.Inf;
        var max = -Vector3.Inf;
        foreach (var (arrays, _) in surfaces)
            foreach (var v in (Vector3[])arrays[(int)Mesh.ArrayType.Vertex])
            {
                min = min.Min(v);
                max = max.Max(v);
            }
        var center = (min + max) / 2f;
        var shift = !held ? new Vector3(-center.X, -min.Y, -center.Z)
            : upright ? new Vector3(-center.X, -max.Y, -center.Z)
            : Vector3.Zero;

        var mesh = new ArrayMesh();
        foreach (var (arrays, material) in surfaces)
        {
            var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            for (int i = 0; i < verts.Length; i++) verts[i] += shift;
            arrays[(int)Mesh.ArrayType.Vertex] = verts;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, material);
        }
        return mesh;
    }

    static void EnsureMaterials()
    {
        if (plain != null) return;
        plain = Look.Solid(Colors.White);
        wood = Relief(7f, 0.9f, 0.3f);
        stone = Relief(5f, 0.9f, 0.3f);
        plant = Relief(9f, 0.8f, 0.25f);
    }

    static Material Relief(float scale, float bump, float cavity)
    {
        var material = Look.Solid(Colors.White);
        material.SetShaderParameter("detail_scale", scale);
        material.SetShaderParameter("bump", bump);
        material.SetShaderParameter("cavity", cavity);
        return material;
    }

    // --- Palette. Raw materials stay a little light so they stand out in the grass.

    static readonly Color BarkLight = new(0.55f, 0.42f, 0.30f);
    static readonly Color Whittled = new(0.80f, 0.66f, 0.46f);
    static readonly Color Charred = new(0.16f, 0.12f, 0.10f);
    static readonly Color Flint = new(0.44f, 0.45f, 0.48f);
    static readonly Color FlintBrown = new(0.52f, 0.47f, 0.42f);
    static readonly Color Cord = new(0.60f, 0.47f, 0.28f);
    static readonly Color Straw = new(0.80f, 0.70f, 0.46f);
    static readonly Color Weave = new(0.72f, 0.58f, 0.32f);
    static readonly Color Fleece = new(0.86f, 0.83f, 0.74f);
    static readonly Color HideSkin = new(0.86f, 0.74f, 0.60f);
    static readonly Color LeatherBrown = new(0.55f, 0.37f, 0.22f);
    static readonly Color TunicWool = new(0.78f, 0.74f, 0.64f);
    static readonly Color Bone = new(0.92f, 0.88f, 0.80f);

    // The bow and arrow (animals-15) are Crafting's Item 16 and 17; BowItem /
    // ArrowItem can point other ids at the same models. ArrowFlight is the shot
    // in the air.
    public static Item? BowItem, ArrowItem;

    static (Action<Kit>? Build, bool Upright) Recipe(Item item) =>
        BowItem.HasValue && item == BowItem.Value ? (Bow, false)
        : ArrowItem.HasValue && item == ArrowItem.Value ? (Arrow, false)
        : KnownRecipe(item);

    static (Action<Kit>? Build, bool Upright) KnownRecipe(Item item) => item switch
    {
        Item.Stick => (Stick, false),
        Item.Stone => (Stone, true),
        Item.Log => (Log, false),
        Item.Fiber => (Fiber, false),
        Item.Tinder => (Tinder, true),
        Item.Flake => (Flake, false),
        Item.HandAxe => (HandAxe, false),
        Item.Cordage => (Cordage, true),
        Item.Spear => (Spear, false),
        Item.StoneAxe => (StoneAxe, false),
        Item.StoneKnife => (StoneKnife, false),
        Item.DiggingStick => (DiggingStick, false),
        Item.Basket => (Basket, true),
        Item.BowDrill => (BowDrill, false),
        Item.Berries => (Berries, true),
        // Stand-in until Modeling makes a hazelnut (survival-30).
        Item.Hazelnuts => (Hazelnuts, true),
        Item.RawMutton => (k => Mutton(k, new Color(0.74f, 0.30f, 0.30f), new Color(0.93f, 0.87f, 0.77f), Bone, 1f), true),
        Item.CookedMutton => (k => Mutton(k, new Color(0.50f, 0.27f, 0.12f), new Color(0.74f, 0.54f, 0.28f), new Color(0.86f, 0.78f, 0.64f), 0.95f), true),
        Item.BurntMutton => (k => Mutton(k, new Color(0.10f, 0.08f, 0.07f), new Color(0.17f, 0.13f, 0.10f), new Color(0.32f, 0.28f, 0.23f), 0.88f), true),
        Item.Wool => (Wool, true),
        Item.SheepHide => (k => RolledHide(k, true), true),
        Item.ScrapedHide => (k => RolledHide(k, false), true),
        Item.Leather => (Leather, true),
        Item.SleepingMat => (SleepingMat, true),
        // Clothing (D-074), by number until the items reach main.
        (Item)Garments.WoolTunic => (Tunic, true),
        (Item)Garments.FootWraps => (FootWraps, true),
        // Bow and arrow (Crafting's ids, PR #266; animals-15), by number until on main.
        (Item)16 => (Bow, false),
        (Item)17 => (Arrow, false),
        // Red deer (animals-14), by number until the items reach main.
        (Item)202 => (k => Venison(k, new Color(0.40f, 0.12f, 0.11f), new Color(0.46f, 0.15f, 0.13f), new Color(0.88f, 0.84f, 0.76f)), true),
        // Roast and burnt venison (model-20's icon pass: they had no model and drew as a plain blob).
        // Tallow and grog (model-20's icon pass: their stand-in shapes read as plain blobs).
        Item.Tallow => (Tallow, true),
        Item.Grog => (Grog, true),
        Item.CookedVenison => (k => Venison(k, new Color(0.45f, 0.24f, 0.11f), new Color(0.58f, 0.34f, 0.16f), new Color(0.72f, 0.52f, 0.28f)), true),
        Item.BurntVenison => (k => Venison(k, new Color(0.1f, 0.08f, 0.07f), new Color(0.17f, 0.12f, 0.09f), new Color(0.3f, 0.26f, 0.21f)), true),
        (Item)203 => (k => RolledHide(k, true, DeerCoat), true),
        (Item)204 => (AntlerItem, false),
        // Wool (animals-8), by number until the items reach main.
        (Item)197 => (DropSpindle, false),
        (Item)198 => (Yarn, true),
        (Item)199 => (WoolCloth, true),
        // The hen (animals-19, model-7): eggs, a feather and chicken legs.
        Item.Egg => (k => Egg(k, EggShell, false), true),
        Item.Feather => (Feather, true),
        Item.RawChicken => (k => Drumstick(k, new Color(0.9f, 0.72f, 0.64f), new Color(0.96f, 0.86f, 0.76f)), true),
        Item.CookedChicken => (k => Drumstick(k, new Color(0.66f, 0.4f, 0.17f), new Color(0.8f, 0.56f, 0.26f)), true),
        Item.BurntChicken => (k => Drumstick(k, new Color(0.11f, 0.09f, 0.07f), new Color(0.2f, 0.15f, 0.11f)), true),
        // Boiled and burnt eggs (crafting-24).
        Item.BoiledEgg => (k => Egg(k, new Color(0.97f, 0.96f, 0.92f), false), true),
        Item.BurntEgg => (k => Egg(k, new Color(0.13f, 0.11f, 0.09f), true), true),
        // Rabbit (animals-22), by number until the items reach main: raw, roast and burnt, and its fur.
        (Item)214 => (k => Rabbit(k, new Color(0.84f, 0.56f, 0.52f), new Color(0.9f, 0.7f, 0.64f)), true),
        (Item)215 => (k => Rabbit(k, new Color(0.56f, 0.32f, 0.15f), new Color(0.72f, 0.5f, 0.24f)), true),
        (Item)216 => (k => Rabbit(k, new Color(0.11f, 0.09f, 0.07f), new Color(0.2f, 0.15f, 0.11f)), true),
        (Item)217 => (RabbitFur, true),
        // Spade (Crafting's crafting-shovel).
        Item.Spade => (Spade, false),
        // Waterskins (survival-20).
        Item.Waterskin => (k => Waterskin(k, false), true),
        Item.WaterskinOfWater => (k => Waterskin(k, true), true),
        _ => (null, false),
    };

    static float H(int a, int b) => MeshKit.Hash(a, b);

    // A fallen branch, a little crooked, with the stub of a twig.
    static void Stick(Kit k)
    {
        var path = new[] { new Vector3(0, 0.36f, 0), new Vector3(0.008f, 0.05f, 0.01f), new Vector3(-0.01f, -0.3f, -0.012f), new Vector3(0.004f, -0.64f, 0.006f) };
        k.Wood.Paint(BarkLight).Tube(Smooth(path, 4), 0.042f, 6, 0.03f);
        k.Wood.Paint(BarkLight, 0.92f).Tube(new[] { new Vector3(0, -0.12f, 0.005f), new Vector3(0.004f, -0.2f, 0.07f), new Vector3(0.006f, -0.24f, 0.12f) }, 0.016f, 5, 0.008f);
    }

    // A river cobble: rounded but not round.
    static void Stone(Kit k)
    {
        k.Stone.Paint(Look.Rock).Ellipsoid(new Vector3(0, 0.07f, 0), new Vector3(0.14f, 0.08f, 0.115f), 7, 4, new Basis(Vector3.Up, 0.4f));
        k.Stone.Paint(Look.Rock, 0.94f).Ellipsoid(new Vector3(0.05f, 0.075f, 0.03f), new Vector3(0.09f, 0.065f, 0.08f), 6, 3, new Basis(Vector3.Up, 1.3f));
    }

    // A length of trunk: bark round the outside, pale wood and rings at the cut ends.
    static void Log(Kit k)
    {
        const float r = 0.085f;
        k.Wood.Paint(Look.Bark.Lightened(0.1f)).Loft(new Vector3(0, 0.4f, 0), new Vector3(0, -0.4f, 0), new MeshKit.Ring[]
        {
            new(0f, r * 0.97f, r), new(0.5f, r * 1.03f, r * 0.98f, 0.004f), new(1f, r * 0.92f, r * 0.95f),
        }, 8, 0.02f);
        for (int end = -1; end <= 1; end += 2)
        {
            float y = end * (0.4f + 0.002f);
            k.Wood.Paint(Whittled).Ellipsoid(new Vector3(0, y, 0), new Vector3(r * 0.85f, 0.006f, r * 0.85f), 8, 2);
            k.Wood.Paint(Whittled.Darkened(0.25f)).Ellipsoid(new Vector3(0, y * 1.004f, 0), new Vector3(r * 0.45f, 0.006f, r * 0.45f), 8, 2);
        }
        // The stub of a cut-off branch.
        k.Wood.Paint(Look.Bark.Lightened(0.15f)).Tube(new[] { new Vector3(0.06f, 0.1f, 0.02f), new Vector3(0.12f, 0.16f, 0.03f) }, 0.022f, 6, 0.018f);
    }

    // A sheaf of stripped plant fiber, tied near one end.
    static void Fiber(Kit k)
    {
        for (int i = 0; i < 14; i++)
        {
            float a = i * 2.4f;
            var root = new Vector3(Mathf.Cos(a) * 0.012f, 0.22f, Mathf.Sin(a) * 0.018f);
            var tip = new Vector3(Mathf.Cos(a) * (0.03f + H(i, 1) * 0.05f), -0.34f - H(i, 2) * 0.08f, Mathf.Sin(a) * (0.05f + H(i, 3) * 0.06f));
            var color = (i % 3) switch { 0 => Look.LeafWarm.Lightened(0.15f), 1 => Straw, _ => Look.LeafLight.Lightened(0.1f) };
            k.Plant.Paint(color, 0.88f + H(i, 4) * 0.2f).Blade(root, tip, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), 0.026f, 0.01f);
        }
        k.Plant.Paint(Cord, 0.85f).Tube(MeshKit.Circle(new Vector3(0, 0.13f, 0), Vector3.Up, 0.026f, 14), 0.007f, 4, -1f, false);
    }

    // A loose nest of shredded bark and dry grass.
    static void Tinder(Kit k)
    {
        k.Plant.Paint(Straw).Ellipsoid(new Vector3(0, 0.045f, 0), new Vector3(0.075f, 0.045f, 0.075f), 8, 4);
        for (int i = 0; i < 18; i++)
        {
            float a = i * 2.4f + H(i, 1);
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var root = dir * 0.04f + new Vector3(0, 0.03f + H(i, 2) * 0.04f, 0);
            var tip = root + dir * (0.06f + H(i, 3) * 0.04f) + Vector3.Up * (H(i, 4) * 0.05f - 0.01f);
            k.Plant.Paint(i % 2 == 0 ? Straw : Straw.Darkened(0.2f), 0.9f + H(i, 5) * 0.15f).Blade(root, tip, Vector3.Up, 0.03f, 0.012f);
        }
    }

    // A sharp flake knocked off a core: thin, with a ridge down its back.
    // Loose flakes are drawn half again as big as life, so they show in the grass.
    static void Flake(Kit k) =>
        k.Stone.Paint(Flint).Loft(new Vector3(0, 0.045f, 0), new Vector3(0, -0.15f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.009f, 0.033f), new(0.3f, 0.016f, 0.063f), new(0.7f, 0.012f, 0.054f), new(1f, 0.003f, 0.009f),
        }, 5, 0.3f, 0f);

    // A teardrop of knapped stone, worked on both faces.
    static void HandAxe(Kit k) =>
        k.Stone.Paint(FlintBrown).Loft(new Vector3(0, 0.06f, 0), new Vector3(0, -0.21f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.02f, 0.038f), new(0.2f, 0.035f, 0.07f), new(0.55f, 0.038f, 0.078f), new(0.85f, 0.022f, 0.05f), new(1f, 0.005f, 0.01f),
        }, 6, 0.3f, 0f);

    // A hank of twisted cord, coiled.
    static void Cordage(Kit k)
    {
        var coil = MeshKit.Circle(new Vector3(0, 0.012f, 0), Vector3.Up, 0.065f, 48, 2.6f, 0.022f);
        k.Plant.Paint(Cord).Tube(coil, 0.011f, 5);
        var end = coil[^1];
        k.Plant.Paint(Cord, 0.93f).Tube(new[] { end, end + new Vector3(0.03f, -0.004f, 0.03f), end + new Vector3(0.07f, -0.02f, 0.05f) }, 0.011f, 5, 0.008f);
    }

    // A straight shaft with a stone point lashed into its end.
    static void Spear(Kit k)
    {
        k.Wood.Paint(BarkLight.Lightened(0.08f)).Tube(new[] { new Vector3(0, 0.62f, 0), new Vector3(0.004f, -0.2f, 0.002f), new Vector3(0, -1.0f, 0) }, 0.022f, 6, 0.019f);
        k.Stone.Paint(Flint).Loft(new Vector3(0, -0.96f, 0), new Vector3(0, -1.16f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.008f, 0.02f), new(0.3f, 0.011f, 0.032f), new(1f, 0.002f, 0.003f),
        }, 5, 0.2f, 0f);
        k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(0, -0.9f, 0), Vector3.Down, 0.025f, 40, 5f, 0.08f), 0.006f, 4);
    }

    // A hand axe hafted crosswise in a handle and bound tight with cord.
    static void StoneAxe(Kit k)
    {
        k.Wood.Paint(BarkLight.Lightened(0.05f)).Tube(new[] { new Vector3(0, 0.12f, 0), new Vector3(0.003f, -0.2f, 0), new Vector3(0, -0.56f, 0) }, 0.023f, 6, 0.028f);
        k.Stone.Paint(FlintBrown).Loft(new Vector3(0, -0.48f, -0.075f), new Vector3(0, -0.48f, 0.15f), new MeshKit.Ring[]
        {
            new(0f, 0.02f, 0.03f), new(0.3f, 0.032f, 0.046f), new(0.75f, 0.024f, 0.056f), new(1f, 0.004f, 0.062f),
        }, 6, 0.3f, 0f);
        k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(0, -0.4f, 0), Vector3.Down, 0.031f, 32, 4f, 0.05f), 0.006f, 4);
        k.Plant.Paint(Cord, 0.92f).Tube(MeshKit.Circle(new Vector3(0, -0.52f, 0), Vector3.Down, 0.033f, 24, 3f, 0.035f), 0.006f, 4);
        // Two turns round the head itself, holding it to the handle.
        k.Plant.Paint(Cord, 0.96f).Tube(MeshKit.Circle(new Vector3(0, -0.48f, 0.01f), Vector3.Back, 0.05f, 20, 2f, 0.012f), 0.006f, 4);
    }

    // A flake set in a short wooden handle and bound with cord.
    static void StoneKnife(Kit k)
    {
        k.Wood.Paint(BarkLight).Tube(new[] { new Vector3(0, 0.07f, 0), new Vector3(0, -0.07f, 0) }, 0.019f, 6, 0.021f);
        k.Stone.Paint(Flint).Loft(new Vector3(0, -0.055f, 0), new Vector3(0, -0.21f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.007f, 0.019f), new(0.25f, 0.008f, 0.026f, 0.004f), new(0.8f, 0.006f, 0.02f, 0.006f), new(1f, 0.002f, 0.004f, 0.004f),
        }, 5, 0.3f, 0f);
        k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(0, -0.035f, 0), Vector3.Down, 0.022f, 30, 4f, 0.05f), 0.005f, 4);
    }

    // A wooden spade carved from a log: a short straight handle, a flat blade about a
    // forearm long, and a pale stone edge lashed across its tip. Held like the
    // digging stick; the blade's flat faces look along ±X.
    static void Spade(Kit k)
    {
        k.Wood.Paint(Whittled.Darkened(0.1f)).Loft(new Vector3(0, 0.12f, 0), new Vector3(0, -0.36f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.024f, 0.024f), new(0.85f, 0.021f, 0.022f), new(1f, 0.02f, 0.028f),
        }, 6, 0.4f);
        k.Wood.Paint(Whittled).Loft(new Vector3(0, -0.36f, 0), new Vector3(0, -0.76f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.018f, 0.03f), new(0.15f, 0.014f, 0.062f), new(0.6f, 0.012f, 0.075f), new(1f, 0.01f, 0.07f),
        }, 6, 0.1f);
        // The stone edge, set into the blade's end and lashed through.
        k.Stone.Paint(Flint.Lightened(0.25f)).Loft(new Vector3(0, -0.75f, 0), new Vector3(0, -0.83f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.012f, 0.072f), new(0.6f, 0.009f, 0.068f), new(1f, 0.002f, 0.056f),
        }, 6, 0.1f, 0f);
        k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(0, -0.735f, 0), Vector3.Down, 0.03f, 28, 3f, 0.03f), 0.005f, 4);
        k.Plant.Paint(Cord, 0.9f).Tube(MeshKit.Circle(new Vector3(0, -0.37f, 0), Vector3.Down, 0.024f, 24, 3f, 0.025f), 0.005f, 4);
    }

    // A hide waterskin with a cord-tied neck: empty it lies slack and folded, full it
    // stands swollen and taut, the neck pulled up tight.
    static void Waterskin(Kit k, bool full)
    {
        var hide = HideSkin.Darkened(full ? 0.22f : 0.15f);
        if (full)
        {
            k.Plain.Paint(hide).Ellipsoid(new Vector3(0, 0.1f, 0), new Vector3(0.1f, 0.1f, 0.085f), 12, 8);
            k.Plain.Paint(hide, 0.95f).Ellipsoid(new Vector3(0, 0.05f, 0), new Vector3(0.105f, 0.06f, 0.09f), 12, 6);
            k.Plain.Paint(hide).Loft(new Vector3(0, 0.18f, 0), new Vector3(0.01f, 0.26f, 0), new MeshKit.Ring[]
            {
                new(0f, 0.05f, 0.045f), new(0.6f, 0.022f, 0.02f), new(1f, 0.03f, 0.026f),
            }, 8, 0.3f);
            k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(0.006f, 0.235f, 0), Vector3.Up, 0.024f, 20, 3f, 0.015f), 0.005f, 4);
        }
        else
        {
            // Lying on its side, flattened, with a fold across it and the neck flopped over.
            k.Plain.Paint(hide).Ellipsoid(new Vector3(0, 0.025f, 0), new Vector3(0.11f, 0.025f, 0.09f), 12, 6, new Basis(Vector3.Up, 0.2f));
            k.Plain.Paint(hide, 0.85f).Tube(new[] { new Vector3(-0.07f, 0.045f, -0.06f), new Vector3(0, 0.05f, 0.0f), new Vector3(0.08f, 0.044f, 0.05f) }, 0.008f, 5);
            k.Plain.Paint(hide, 0.95f).Tube(new[] { new Vector3(0.09f, 0.03f, -0.02f), new Vector3(0.14f, 0.03f, -0.03f), new Vector3(0.17f, 0.018f, -0.05f) }, 0.02f, 6, 0.016f);
            k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(0.14f, 0.03f, -0.03f), Vector3.Right, 0.022f, 20, 3f, 0.015f), 0.005f, 4);
        }
    }

    // A straight stick, its end whittled flat and hardened black in the fire.
    static void DiggingStick(Kit k)
    {
        k.Wood.Paint(Whittled.Darkened(0.12f)).Loft(new Vector3(0, 0.42f, 0), new Vector3(0, -0.5f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.032f, 0.032f), new(0.6f, 0.03f, 0.03f), new(1f, 0.027f, 0.029f),
        }, 6, 0.3f);
        k.Wood.Paint(Charred).Loft(new Vector3(0, -0.5f, 0), new Vector3(0, -0.7f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.027f, 0.029f), new(0.6f, 0.018f, 0.026f), new(1f, 0.004f, 0.018f),
        }, 6, 0.1f);
    }

    // A woven basket: stakes standing round a base, rows of weavers going in and
    // out between them, and a thick rim.
    static void Basket(Kit k)
    {
        const int stakes = 12, rows = 7;
        const float height = 0.22f, bottom = 0.14f, top = 0.19f;
        k.Plant.Paint(Weave, 0.85f).Ellipsoid(new Vector3(0, 0.012f, 0), new Vector3(bottom, 0.012f, bottom), 12, 3);
        for (int s = 0; s < stakes; s++)
        {
            float a = s * Mathf.Tau / stakes;
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            k.Plant.Paint(Weave, 0.8f).Tube(new[] { dir * bottom + Vector3.Up * 0.01f, dir * top + Vector3.Up * (height + 0.015f) }, 0.007f, 4);
        }
        for (int r = 0; r < rows; r++)
        {
            float y = 0.025f + r * (height - 0.03f) / (rows - 1);
            float radius = Mathf.Lerp(bottom, top, y / height) + 0.004f;
            var path = new Vector3[49];
            for (int i = 0; i < path.Length; i++)
            {
                float a = i * Mathf.Tau / (path.Length - 1);
                // In front of one stake, behind the next.
                float weave = Mathf.Sin(a * stakes / 2f + r * Mathf.Pi) * 0.006f;
                path[i] = new Vector3(Mathf.Cos(a) * (radius + weave), y, Mathf.Sin(a) * (radius + weave));
            }
            k.Plant.Paint(Weave, r % 2 == 0 ? 1f : 0.9f).Tube(path, 0.011f, 4, -1f, false);
        }
        k.Plant.Paint(Weave, 0.95f).Tube(MeshKit.Circle(new Vector3(0, height + 0.01f, 0), Vector3.Up, top + 0.006f, 49), 0.015f, 5, -1f, false);
    }

    // A bow with its string, and the spindle caught in a loop of the string.
    static void BowDrill(Kit k)
    {
        var bow = new Vector3[14];
        for (int i = 0; i < bow.Length; i++)
        {
            float t = i / (bow.Length - 1f);
            bow[i] = new Vector3(0, 0.06f - t * 0.74f, Mathf.Sin(t * Mathf.Pi) * 0.11f);
        }
        k.Wood.Paint(BarkLight).Tube(bow, 0.016f, 6, 0.012f);
        var a = bow[0];
        var b = bow[^1];
        var middle = (a + b) / 2f + new Vector3(0, 0, 0.05f);
        k.Plant.Paint(Cord).Tube(new[] { a, middle + new Vector3(0, 0.02f, 0), middle - new Vector3(0, 0.02f, 0), b }, 0.004f, 4);
        k.Wood.Paint(Whittled).Tube(new[] { middle + new Vector3(0.015f, 0, -0.2f), middle + new Vector3(0.015f, 0, 0.14f) }, 0.014f, 6, 0.012f);
    }

    // A small cluster of berries on a twig, with a leaf or two.
    static void Berries(Kit k)
    {
        k.Wood.Paint(Look.Bark.Lightened(0.1f)).Tube(new[] { new Vector3(-0.06f, 0.03f, -0.02f), new Vector3(0, 0.05f, 0), new Vector3(0.07f, 0.045f, 0.02f) }, 0.006f, 4, 0.004f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 2.4f;
            var at = new Vector3(Mathf.Cos(a) * 0.035f * (0.4f + H(i, 1) * 0.6f), 0.025f + H(i, 2) * 0.03f, Mathf.Sin(a) * 0.035f * (0.4f + H(i, 3) * 0.6f));
            k.Plain.Paint(BerryBush.BerryColor, 0.8f + H(i, 4) * 0.35f).Ellipsoid(at, Vector3.One * (0.017f + H(i, 5) * 0.007f), 7, 4);
        }
        k.Plant.Paint(Look.Leaf).Blade(new Vector3(0.05f, 0.045f, 0.015f), new Vector3(0.12f, 0.03f, 0.06f), Vector3.Up, 0.04f, 0.008f);
        k.Plant.Paint(Look.LeafLight).Blade(new Vector3(-0.04f, 0.035f, -0.015f), new Vector3(-0.11f, 0.02f, -0.06f), Vector3.Up, 0.035f, 0.008f);
    }

    // A small handful of hazelnuts (model-21, for Survival's autumn wild food, D-323):
    // round tan shells, paler toward the top, with a pale, flat base and a little point,
    // one still in its frilled green husk.
    static void Hazelnuts(Kit k)
    {
        var shell = new Color(0.56f, 0.42f, 0.27f);
        var spots = new[] { new Vector3(-0.025f, 0, 0.01f), new Vector3(0.022f, 0, 0.018f), new Vector3(0.0f, 0, -0.025f) };
        for (int i = 0; i < spots.Length; i++)
        {
            var at = spots[i] + new Vector3(0, 0.018f, 0);
            var tilt = new Basis(new Vector3(H(i, 93) - 0.5f, 0, H(i, 94) - 0.5f).Normalized(), 0.5f + H(i, 95) * 0.6f);
            Nut(k, at, tilt, shell.Darkened(H(i, 96) * 0.12f), i == 2);
        }
    }

    // One hazelnut: an egg of shell with a pale scar at its base and a small tip; husked
    // wraps it in a ragged cup of bracts.
    static void Nut(Kit k, Vector3 at, Basis tilt, Color shell, bool husked, float size = 1f)
    {
        var up = tilt * Vector3.Up;
        k.Plain.Paint(shell).Ellipsoid(at, new Vector3(0.017f, 0.019f, 0.017f) * size, 9, 6, tilt);
        k.Plain.Paint(new Color(0.85f, 0.76f, 0.6f)).Ellipsoid(at - up * 0.016f * size, new Vector3(0.011f, 0.004f, 0.011f) * size, 7, 2, tilt);
        k.Plain.Paint(shell.Lightened(0.18f)).Ellipsoid(at + up * 0.009f * size, new Vector3(0.0145f, 0.01f, 0.0145f) * size, 9, 4, tilt);
        k.Plain.Paint(shell.Darkened(0.2f)).Ellipsoid(at + up * 0.019f * size, new Vector3(0.004f, 0.005f, 0.004f) * size, 5, 3, tilt);
        if (!husked) return;
        // A leafy cup round the lower half, a frill of short teeth at its lip.
        var husk = new Color(0.3f, 0.4f, 0.15f);
        k.Plant.Paint(husk).Loft(at - up * 0.017f * size, at + up * 0.004f * size, new MeshKit.Ring[]
        {
            new(0f, 0.006f * size, 0.006f * size), new(0.45f, 0.018f * size, 0.018f * size), new(1f, 0.021f * size, 0.021f * size),
        }, 9, 0.3f);
        for (int j = 0; j < 7; j++)
        {
            float a = j * Mathf.Tau / 7f;
            var side = tilt * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var lip = at + (up * 0.003f + side * 0.02f) * size;
            k.Plant.Paint(husk, 0.9f + H(j, 99) * 0.2f).Blade(lip, lip + (up * (0.008f + H(j, 97) * 0.005f) + side * 0.006f) * size, side, 0.008f * size, 0.002f);
        }
    }

    // A leg chop: lean meat, a rind of fat round one side, the end of a bone.
    static void Mutton(Kit k, Color meat, Color fat, Color bone, float size)
    {
        k.Plain.Paint(meat).Ellipsoid(new Vector3(0, 0.05f, 0.01f) * size, new Vector3(0.11f, 0.05f, 0.085f) * size, 9, 5, null, 0.6f);
        k.Plain.Paint(fat).Ellipsoid(new Vector3(-0.01f, 0.045f, -0.02f) * size, new Vector3(0.115f, 0.046f, 0.075f) * size, 9, 5, null, 0.6f);
        k.Plain.Paint(bone).Tube(new[] { new Vector3(0.06f, 0.05f, 0.01f) * size, new Vector3(0.19f, 0.055f, 0.015f) * size }, 0.014f * size, 6);
        k.Plain.Paint(bone, 0.95f).Ellipsoid(new Vector3(0.195f, 0.055f, 0.015f) * size, new Vector3(0.02f, 0.022f, 0.026f) * size, 7, 4);
    }

    static readonly Color EggShell = new(0.86f, 0.72f, 0.55f);

    // A hen's egg lying on its side, the narrow end a little pointed. Shelled and
    // white when boiled; a burnt one is black with a pale crack.
    static void Egg(Kit k, Color shell, bool cracked)
    {
        k.Plain.Paint(shell).Ellipsoid(new Vector3(0, 0.022f, 0), new Vector3(0.021f, 0.021f, 0.026f), 10, 7);
        k.Plain.Paint(shell, 0.98f).Ellipsoid(new Vector3(0, 0.021f, 0.016f), new Vector3(0.016f, 0.016f, 0.017f), 8, 5);
        if (cracked)
            k.Plain.Paint(new Color(0.45f, 0.4f, 0.33f)).Tube(new[] { new Vector3(-0.012f, 0.038f, -0.012f), new Vector3(0.0f, 0.042f, -0.002f), new Vector3(-0.006f, 0.041f, 0.01f), new Vector3(0.008f, 0.037f, 0.018f) }, 0.0015f, 3);
    }

    // A long tail feather, a pale quill down its middle, lying flat.
    static void Feather(Kit k)
    {
        var root = new Vector3(0, 0.004f, -0.09f);
        k.Plain.Paint(new Color(0.93f, 0.9f, 0.84f)).Tube(new[] { root + new Vector3(0, 0, -0.02f), root, new Vector3(0.008f, 0.006f, 0.08f) }, 0.0025f, 4, 0.001f);
        for (int side = -1; side <= 1; side += 2)
            k.Plant.Paint(new Color(0.62f, 0.42f, 0.26f), side < 0 ? 1f : 0.88f).Blade(new Vector3(side * 0.002f, 0.005f, -0.08f), new Vector3(side * 0.006f + 0.007f, 0.005f, 0.085f), Vector3.Up, 0.034f, 0.002f);
    }

    // A chicken leg: a plump drumstick of meat on a short knuckled bone.
    static void Drumstick(Kit k, Color meat, Color skin)
    {
        k.Plain.Paint(skin).Ellipsoid(new Vector3(0, 0.032f, -0.02f), new Vector3(0.036f, 0.032f, 0.05f), 9, 6);
        k.Plain.Paint(meat).Loft(new Vector3(0, 0.03f, 0.02f), new Vector3(0, 0.022f, 0.06f), new MeshKit.Ring[]
        {
            new(0f, 0.026f, 0.024f), new(1f, 0.011f, 0.011f),
        }, 8, 0.2f);
        k.Plain.Paint(Bone).Tube(new[] { new Vector3(0, 0.022f, 0.055f), new Vector3(0, 0.02f, 0.09f) }, 0.007f, 6);
        k.Plain.Paint(Bone, 0.95f).Ellipsoid(new Vector3(0, 0.02f, 0.094f), new Vector3(0.011f, 0.01f, 0.009f), 7, 4);
    }

    // A small dressed rabbit lying on its side: a rounded body, the two long hind legs
    // folded forward under it, the short front legs tucked; skin and meat in two tones.
    static void Rabbit(Kit k, Color meat, Color skin)
    {
        const float z = 1.3f;
        k.Plain.Paint(skin).Ellipsoid(new Vector3(0, 0.035f, 0) * z, new Vector3(0.045f, 0.034f, 0.085f) * z, 10, 6);
        k.Plain.Paint(meat).Ellipsoid(new Vector3(0, 0.03f, 0.06f) * z, new Vector3(0.034f, 0.028f, 0.035f) * z, 8, 5);
        for (int s = -1; s <= 1; s += 2)
        {
            k.Plain.Paint(skin, 0.95f).Loft(new Vector3(s * 0.03f, 0.03f, -0.06f) * z, new Vector3(s * 0.036f, 0.016f, 0.03f) * z, new MeshKit.Ring[]
            {
                new(0f, 0.022f * z, 0.02f * z), new(0.6f, 0.014f * z, 0.013f * z), new(1f, 0.008f * z, 0.008f * z),
            }, 7, 0.2f);
            k.Plain.Paint(meat, 0.95f).Loft(new Vector3(s * 0.022f, 0.024f, 0.07f) * z, new Vector3(s * 0.026f, 0.012f, 0.11f) * z, new MeshKit.Ring[]
            {
                new(0f, 0.012f * z, 0.011f * z), new(1f, 0.006f * z, 0.006f * z),
            }, 6, 0.2f);
        }
    }

    // A rabbit's pelt, rolled loosely: soft brown fur outside, the pale belly showing at
    // one end and the white scut.
    static void RabbitFur(Kit k)
    {
        var coat = new Color(0.55f, 0.42f, 0.3f);
        k.Plant.Paint(coat).Loft(new Vector3(-0.07f, 0.035f, 0), new Vector3(0.07f, 0.035f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.028f, 0.03f), new(0.2f, 0.036f, 0.034f), new(0.8f, 0.035f, 0.034f), new(1f, 0.026f, 0.028f),
        }, 10, 0.4f);
        k.Plain.Paint(new Color(0.9f, 0.85f, 0.77f)).Ellipsoid(new Vector3(0.068f, 0.035f, 0), new Vector3(0.008f, 0.024f, 0.026f), 8, 4);
        k.Plain.Paint(new Color(0.97f, 0.96f, 0.93f)).Ellipsoid(new Vector3(-0.072f, 0.04f, 0.012f), new Vector3(0.016f, 0.016f, 0.015f), 7, 4);
    }

    // A tuft of shorn fleece.
    static void Wool(Kit k)
    {
        for (int i = 0; i < 8; i++)
        {
            float a = i * 2.4f;
            float r = i == 0 ? 0f : 0.05f + H(i, 1) * 0.03f;
            var at = new Vector3(Mathf.Cos(a) * r, 0.04f + H(i, 2) * 0.03f, Mathf.Sin(a) * r * 0.8f);
            k.Plant.Paint(Fleece, 0.9f + H(i, 3) * 0.1f).Ellipsoid(at, new Vector3(0.05f, 0.04f, 0.05f) * (0.8f + H(i, 4) * 0.5f), 7, 4);
        }
    }

    // A hide rolled up and tied: fleece outside for a fresh sheepskin, bare pale
    // skin once it has been scraped.
    static void RolledHide(Kit k, bool fleece) => RolledHide(k, fleece, Fleece);

    static void RolledHide(Kit k, bool hair, Color coat)
    {
        const float half = 0.17f, radius = 0.075f;
        var outside = hair ? k.Plant.Paint(coat) : k.Plain.Paint(HideSkin);
        outside.Loft(new Vector3(-half, radius, 0), new Vector3(half, radius, 0), new MeshKit.Ring[]
        {
            new(0f, radius * 0.95f, radius * 0.95f), new(0.15f, radius, radius), new(0.85f, radius, radius), new(1f, radius * 0.95f, radius * 0.95f),
        }, 9, 0.05f);
        // The ends show the rolled skin.
        for (int end = -1; end <= 1; end += 2)
        {
            k.Plain.Paint(HideSkin, 0.9f).Ellipsoid(new Vector3(end * half, radius, 0), new Vector3(0.008f, radius * 0.9f, radius * 0.9f), 9, 5);
            k.Plain.Paint(HideSkin, 0.75f).Ellipsoid(new Vector3(end * (half + 0.002f), radius, 0), new Vector3(0.008f, radius * 0.45f, radius * 0.45f), 7, 4);
            k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(end * half * 0.55f, radius, 0), Vector3.Right, radius + 0.004f, 20), 0.006f, 4, -1f, false);
        }
    }

    // Leather, folded into a flat stack.
    static void Leather(Kit k)
    {
        for (int i = 0; i < 3; i++)
        {
            float y = 0.012f + i * 0.022f;
            float jx = (H(i, 1) - 0.5f) * 0.02f, jz = (H(i, 2) - 0.5f) * 0.02f;
            k.Plain.Paint(LeatherBrown, 0.88f + i * 0.07f).Loft(new Vector3(-0.15f + jx, y, jz), new Vector3(0.15f + jx, y, jz), new MeshKit.Ring[]
            {
                new(0f, 0.016f, 0.16f), new(0.5f, 0.018f, 0.17f), new(1f, 0.016f, 0.16f),
            }, 4, 0.15f, Mathf.Pi / 4f);
        }
    }

    // A sleeping mat of woven rushes, rolled up and tied: the weave shows in rings.
    static void SleepingMat(Kit k)
    {
        const float half = 0.3f, radius = 0.09f;
        k.Plant.Paint(Weave).Loft(new Vector3(-half, radius, 0), new Vector3(half, radius, 0), new MeshKit.Ring[]
        {
            new(0f, radius * 0.95f, radius * 0.95f), new(0.1f, radius, radius), new(0.9f, radius, radius), new(1f, radius * 0.95f, radius * 0.95f),
        }, 9, 0.05f);
        for (int i = 1; i < 12; i++)
        {
            float x = -half + i * 2f * half / 12f;
            k.Plant.Paint(Weave, i % 2 == 0 ? 0.82f : 0.9f).Tube(MeshKit.Circle(new Vector3(x, radius, 0), Vector3.Right, radius + 0.002f, 18), 0.005f, 3, -1f, false);
        }
        for (int end = -1; end <= 1; end += 2)
        {
            k.Plant.Paint(Weave, 0.75f).Ellipsoid(new Vector3(end * half, radius, 0), new Vector3(0.006f, radius * 0.9f, radius * 0.9f), 9, 4);
            k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(end * half * 0.6f, radius, 0), Vector3.Right, radius + 0.006f, 20), 0.007f, 4, -1f, false);
        }
    }

    static readonly Color DeerCoat = new(0.55f, 0.32f, 0.2f);

    // A lean cut of venison, no rind of fat like mutton: the body, a raised top and a
    // pale sliver of sinew along one edge; raw dark red, roast browned, or burnt black.
    static void Venison(Kit k, Color meat, Color top, Color sinew)
    {
        k.Plain.Paint(meat).Ellipsoid(new Vector3(0, 0.035f, 0), new Vector3(0.12f, 0.035f, 0.08f), 8, 4, new Basis(Vector3.Up, 0.3f), 0.5f);
        k.Plain.Paint(top).Ellipsoid(new Vector3(0.03f, 0.05f, 0.01f), new Vector3(0.07f, 0.025f, 0.05f), 7, 3, new Basis(Vector3.Up, 0.5f), 0.5f);
        k.Plain.Paint(sinew).Ellipsoid(new Vector3(-0.07f, 0.04f, -0.02f), new Vector3(0.03f, 0.012f, 0.05f), 6, 3, new Basis(Vector3.Up, 0.3f), 0.5f);
    }

    // A round cake of rendered tallow, pale and waxy with a slightly sunk top, set on a
    // green leaf as it was poured to cool.
    static void Tallow(Kit k)
    {
        for (int i = 0; i < 2; i++)
        {
            float a = 0.5f + i * 2.6f;
            k.Plant.Paint(new Color(0.36f, 0.48f, 0.22f), 0.9f + i * 0.1f).Blade(new Vector3(Mathf.Cos(a) * -0.07f, 0.004f, Mathf.Sin(a) * -0.07f), new Vector3(Mathf.Cos(a) * 0.09f, 0.004f, Mathf.Sin(a) * 0.09f), Vector3.Up, 0.075f, 0.002f);
        }
        k.Plain.Paint(new Color(0.93f, 0.88f, 0.72f)).Loft(new Vector3(0, 0.008f, 0), new Vector3(0, 0.045f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.055f, 0.055f), new(0.7f, 0.058f, 0.058f), new(1f, 0.05f, 0.05f),
        }, 12, 0.4f);
        k.Plain.Paint(new Color(0.97f, 0.93f, 0.8f)).Ellipsoid(new Vector3(0, 0.043f, 0), new Vector3(0.048f, 0.006f, 0.048f), 12, 2);
    }

    // A small heap of grog: fired clay crushed to grit, with a few larger sharp crumbs
    // of potsherd on top, so it reads as broken pot rather than earth.
    static void Grog(Kit k)
    {
        var fired = new Color(0.68f, 0.38f, 0.24f);
        k.Plain.Paint(fired, 0.8f).Ellipsoid(new Vector3(0, 0.01f, 0), new Vector3(0.08f, 0.02f, 0.07f), 10, 3, null, 0.4f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 2.4f, r = 0.015f + H(i, 90) * 0.05f;
            var at = new Vector3(Mathf.Cos(a) * r, 0.022f + (0.065f - r) * 0.35f, Mathf.Sin(a) * r);
            k.Plain.Paint(fired, 0.7f + H(i, 91) * 0.55f).Ellipsoid(at, new Vector3(0.022f, 0.01f, 0.016f) * (0.8f + H(i, 92) * 0.7f), 4, 2, new Basis(Vector3.Up, a));
        }
    }

    // A cast antler: a curved beam with a few tines, held by its base.
    static void AntlerItem(Kit k)
    {
        var beam = new[] { new Vector3(0, 0.05f, 0), new Vector3(0, -0.15f, 0.04f), new Vector3(0.01f, -0.35f, 0.02f), new Vector3(0.02f, -0.52f, 0.08f) };
        k.Plain.Paint(new Color(0.66f, 0.58f, 0.46f)).Ellipsoid(new Vector3(0, 0.05f, 0), new Vector3(0.03f, 0.018f, 0.03f), 6, 3);
        k.Plain.Tube(beam, 0.02f, 5, 0.01f);
        k.Plain.Paint(new Color(0.78f, 0.72f, 0.60f)).Tube(new[] { new Vector3(0, 0.0f, 0.02f), new Vector3(0.0f, -0.05f, 0.13f) }, 0.012f, 4, 0.004f);
        k.Plain.Tube(new[] { new Vector3(0.005f, -0.2f, 0.035f), new Vector3(0.01f, -0.24f, 0.14f) }, 0.011f, 4, 0.004f);
        k.Plain.Tube(new[] { new Vector3(0.02f, -0.52f, 0.08f), new Vector3(0.025f, -0.6f, 0.04f) }, 0.009f, 4, 0.003f);
    }

    // --- Wear (D-155): what use does to each tool that wears. Level 1 is worn,
    // 2 spent. Marks go in the plain surface so the darkening above leaves them
    // alone. Fresh chip scars on flint are paler than the weathered stone.

    static readonly Color ChipScar = new(0.72f, 0.72f, 0.74f);
    static readonly Color FrayedCord = new(0.42f, 0.34f, 0.22f);
    static readonly Color Crack = new(0.12f, 0.09f, 0.07f);

    static readonly Dictionary<Item, Action<Kit, int>> WearMarks = new()
    {
        [Item.StoneAxe] = (k, w) =>
        {
            Chips(k, new Vector3(0, -0.48f, 0.14f), Vector3.Up, 0.05f, w * 3);
            LooseEnd(k, new Vector3(0.03f, -0.4f, 0f), w);
            if (w > 1) CrackLine(k, new Vector3(0.022f, -0.47f, -0.04f), new Vector3(0.024f, -0.5f, 0.08f));
        },
        [Item.StoneKnife] = (k, w) =>
        {
            Chips(k, new Vector3(0, -0.13f, 0.024f), Vector3.Up, 0.07f, w * 3);
            LooseEnd(k, new Vector3(0.022f, -0.035f, 0f), w);
        },
        [Item.Spear] = (k, w) =>
        {
            Chips(k, new Vector3(0, -1.1f, 0.02f), Vector3.Up, 0.05f, w * 2);
            LooseEnd(k, new Vector3(0.026f, -0.9f, 0f), w);
            if (w > 1) CrackLine(k, new Vector3(0.022f, -0.7f, 0f), new Vector3(0.021f, -0.4f, 0.004f));
        },
        [Item.DiggingStick] = (k, w) =>
        {
            // The hardened tip scuffs back to pale wood, then splits.
            k.Plain.Paint(new Color(0.62f, 0.5f, 0.36f)).Ellipsoid(new Vector3(0, -0.66f, 0.012f), new Vector3(0.012f, 0.04f, 0.008f) * w, 5, 3);
            if (w > 1) CrackLine(k, new Vector3(0.0f, -0.7f, 0.019f), new Vector3(0.002f, -0.5f, 0.026f));
        },
        [Item.BowDrill] = (k, w) =>
        {
            // The spindle's end chars black; the string frays.
            k.Plain.Paint(Crack).Ellipsoid(new Vector3(0.015f, -0.32f, 0.15f), new Vector3(0.016f, 0.016f, 0.03f), 6, 3);
            LooseEnd(k, new Vector3(0, -0.3f, 0.06f), w);
        },
        [(Item)16] = (k, w) =>
        {
            LooseEnd(k, new Vector3(0, 0.55f, -0.12f), w);
            if (w > 1) CrackLine(k, new Vector3(0.02f, 0.2f, -0.01f), new Vector3(0.02f, 0.38f, -0.03f));
        },
    };

    // Pale scars along an edge where flakes have come away.
    static void Chips(Kit k, Vector3 along, Vector3 axis, float spread, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var at = along + axis * (H(i, 60) - 0.5f) * 2f * spread;
            k.Plain.Paint(ChipScar, 0.92f + H(i, 61) * 0.1f).Ellipsoid(at, new Vector3(0.01f, 0.012f, 0.006f) * (0.8f + H(i, 62) * 0.6f), 5, 2);
        }
    }

    // A loose end of binding hanging free: one when worn, a frayed pair when spent.
    static void LooseEnd(Kit k, Vector3 at, int w)
    {
        for (int i = 0; i < w; i++)
        {
            var dir = new Vector3(0.6f + i * 0.3f, -0.5f, 0.3f - i * 0.5f).Normalized();
            k.Plain.Paint(FrayedCord, 0.9f + i * 0.1f).Tube(new[] { at, at + dir * 0.04f, at + dir * 0.08f + Vector3.Down * 0.02f }, 0.004f, 3, 0.002f);
        }
    }

    // A dark split in the wood or stone.
    static void CrackLine(Kit k, Vector3 a, Vector3 b) =>
        k.Plain.Paint(Crack).Tube(new[] { a, (a + b) / 2f + new Vector3(0.002f, 0, 0.003f), b }, 0.003f, 3, 0.001f);

    // A crude self bow: one stave of wood, thick at the grip and tapering to each
    // tip, bent back by a twisted cord; a wrap of cord at the grip. Held frame:
    // the grip at the origin, the stave along Y, the back of the bow toward +Z
    // (the target), the string behind it toward -Z (the archer).
    static void Bow(Kit k)
    {
        const float half = 0.62f, bend = 0.13f;
        var stave = new Vector3[13];
        for (int i = 0; i < stave.Length; i++)
        {
            float t = i / (stave.Length - 1f) * 2f - 1f;
            stave[i] = new Vector3(0, t * half, -bend * t * t);
        }
        // Thick in the middle, thin at the tips: two tubes from the grip outward.
        k.Wood.Paint(BarkLight.Lightened(0.1f)).Tube(stave[6..], 0.02f, 6, 0.009f);
        k.Wood.Paint(BarkLight.Lightened(0.1f)).Tube(stave[..7].Reverse().ToArray(), 0.02f, 6, 0.009f);
        k.Plant.Paint(Cord).Tube(new[] { stave[0], stave[^1] }, 0.003f, 3);
        k.Plant.Paint(Cord, 0.9f).Tube(MeshKit.Circle(new Vector3(0, -0.05f, 0), Vector3.Up, 0.023f, 30, 4f, 0.1f), 0.004f, 4);
        foreach (var tip in new[] { stave[0], stave[^1] })
            k.Plant.Paint(Cord, 0.85f).Tube(MeshKit.Circle(tip - Vector3.Up * Mathf.Sign(tip.Y) * 0.025f, Vector3.Up, 0.012f, 12, 2f, 0.015f), 0.003f, 3);
    }

    // A crude arrow: a straight shaft, a knapped flint point bound on with cord,
    // and three split feathers at the nock. Held frame: grip at the middle, the
    // point toward -Y.
    static void Arrow(Kit k) => ArrowParts(k, new Vector3(0, 0.36f, 0), new Vector3(0, -0.36f, 0));

    static void ArrowParts(Kit k, Vector3 nock, Vector3 point)
    {
        var dir = (point - nock).Normalized();
        k.Wood.Paint(Whittled).Tube(new[] { nock, point - dir * 0.04f }, 0.006f, 5);
        var (side, front) = MeshKit.Frame(dir);
        k.Stone.Paint(Flint).Loft(point - dir * 0.05f, point + dir * 0.03f, new MeshKit.Ring[]
        {
            new(0f, 0.005f, 0.012f), new(0.3f, 0.007f, 0.018f), new(1f, 0.001f, 0.002f),
        }, 4, 0.2f, 0f);
        k.Plant.Paint(Cord).Tube(MeshKit.Circle(point - dir * 0.06f, dir, 0.008f, 12, 3f, 0.02f), 0.002f, 3);
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.Tau / 3f;
            var outward = side * Mathf.Cos(a) + front * Mathf.Sin(a);
            var root = nock + dir * 0.02f;
            k.Plant.Paint(i == 0 ? new Color(0.30f, 0.26f, 0.22f) : new Color(0.82f, 0.78f, 0.70f))
                .Blade(root + outward * 0.006f, root + dir * 0.13f + outward * 0.006f, outward.Cross(dir), 0.028f, 0.0f);
        }
    }

    // The arrow as it flies: point toward +Z, origin at its middle, plain shaded.
    public static Mesh ArrowFlight => arrowFlight ??= BuildArrowFlight();
    static Mesh? arrowFlight;

    static Mesh BuildArrowFlight()
    {
        var kit = new Kit(PlayerModel.Style.Flat, Transform3D.Identity);
        ArrowParts(kit, new Vector3(0, 0, -0.36f), new Vector3(0, 0, 0.36f));
        EnsureMaterials();
        var mesh = new ArrayMesh();
        foreach (var (k, family) in new[] { (kit.Wood, PaintedItems.Family.Wood), (kit.Stone, PaintedItems.Family.Stone), (kit.Plant, PaintedItems.Family.Fibre), (kit.Plain, PaintedItems.Family.Wood) })
            if (!k.Empty) k.Commit(mesh, PaintedItems.Of(plain!, family, held: true));
        return mesh;
    }

    // A wool tunic folded in a square, its cord belt coiled on top.
    static void Tunic(Kit k)
    {
        k.Plain.Paint(TunicWool, 0.95f).Loft(new Vector3(-0.15f, 0.02f, 0), new Vector3(0.15f, 0.02f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.026f, 0.15f), new(0.5f, 0.03f, 0.16f), new(1f, 0.026f, 0.15f),
        }, 4, 0.2f, Mathf.Pi / 4f);
        k.Plain.Paint(TunicWool).Loft(new Vector3(-0.14f, 0.055f, 0.01f), new Vector3(0.14f, 0.055f, 0.01f), new MeshKit.Ring[]
        {
            new(0f, 0.022f, 0.13f), new(0.5f, 0.026f, 0.14f), new(1f, 0.022f, 0.13f),
        }, 4, 0.2f, Mathf.Pi / 4f);
        k.Plant.Paint(new Color(0.45f, 0.34f, 0.18f)).Tube(MeshKit.Circle(new Vector3(0.02f, 0.078f, 0.01f), Vector3.Up, 0.05f, 30, 1.8f, 0.006f), 0.006f, 4);
    }

    // A pair of leather foot wraps, each rolled and tied.
    static void FootWraps(Kit k)
    {
        for (int i = 0; i < 2; i++)
        {
            float z = i == 0 ? -0.045f : 0.045f;
            k.Plain.Paint(LeatherBrown, i == 0 ? 1f : 0.92f).Loft(new Vector3(-0.1f, 0.04f, z), new Vector3(0.1f, 0.04f, z), new MeshKit.Ring[]
            {
                new(0f, 0.036f, 0.036f), new(0.15f, 0.04f, 0.04f), new(0.85f, 0.04f, 0.04f), new(1f, 0.036f, 0.036f),
            }, 7, 0.1f);
            k.Plant.Paint(new Color(0.45f, 0.34f, 0.18f)).Tube(MeshKit.Circle(new Vector3(0, 0.04f, z), Vector3.Right, 0.043f, 16), 0.005f, 4, -1f, false);
        }
    }

    // A drop spindle: a straight shaft, a flat stone whorl near the bottom, and
    // yarn wound on above it.
    static void DropSpindle(Kit k)
    {
        k.Wood.Paint(Whittled).Tube(new[] { new Vector3(0, 0.07f, 0), new Vector3(0, -0.2f, 0) }, 0.007f, 6, 0.006f);
        k.Stone.Paint(Look.Rock.Lightened(0.1f)).Ellipsoid(new Vector3(0, -0.155f, 0), new Vector3(0.036f, 0.008f, 0.036f), 10, 3);
        k.Plain.Paint(TunicWool).Ellipsoid(new Vector3(0, -0.09f, 0), new Vector3(0.022f, 0.055f, 0.022f), 8, 6);
        // A thread trails up to a notch at the top.
        k.Plain.Paint(TunicWool, 0.9f).Tube(new[] { new Vector3(0.01f, -0.04f, 0.012f), new Vector3(0.004f, 0.02f, 0.006f), new Vector3(0, 0.066f, 0.004f) }, 0.0025f, 3);
    }

    // A ball of cream yarn, wound round and round.
    static void Yarn(Kit k)
    {
        const float r = 0.045f;
        k.Plain.Paint(TunicWool, 0.94f).Ellipsoid(new Vector3(0, r, 0), Vector3.One * r, 10, 6);
        for (int i = 0; i < 7; i++)
        {
            var axis = new Vector3(H(i, 1) - 0.5f, H(i, 2) - 0.5f, H(i, 3) - 0.5f).Normalized();
            k.Plain.Paint(TunicWool, 0.95f + H(i, 4) * 0.08f).Tube(MeshKit.Circle(new Vector3(0, r, 0), axis, r + 0.002f, 24), 0.004f, 4, -1f, false);
        }
        var loose = new Vector3(r * 0.7f, r * 0.3f, r * 0.7f);
        k.Plain.Paint(TunicWool).Tube(new[] { loose, loose + new Vector3(0.03f, -0.012f, 0.01f), loose + new Vector3(0.07f, -0.012f, -0.01f) }, 0.004f, 4);
    }

    // A folded square of coarse woven cloth, the weave showing on top.
    static void WoolCloth(Kit k)
    {
        k.Plain.Paint(TunicWool, 0.95f).Loft(new Vector3(-0.13f, 0.018f, 0), new Vector3(0.13f, 0.018f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.022f, 0.13f), new(1f, 0.022f, 0.13f),
        }, 4, 0.15f, Mathf.Pi / 4f);
        const float top = 0.0345f, half = 0.088f;
        for (int i = 0; i < 9; i++)
        {
            float t = -half + i * 2f * half / 8f;
            k.Plain.Paint(TunicWool, 0.85f).Tube(new[] { new Vector3(-half, top, t), new Vector3(half, top, t) }, 0.0022f, 3, -1f, false);
            k.Plain.Paint(TunicWool, 1.05f).Tube(new[] { new Vector3(t, top + 0.001f, -half), new Vector3(t, top + 0.001f, half) }, 0.0022f, 3, -1f, false);
        }
    }

    // Catmull-Rom points between the given ones, so a path bends smoothly.
    static Vector3[] Smooth(Vector3[] points, int steps)
    {
        var result = new List<Vector3>();
        for (int i = 0; i + 1 < points.Length; i++)
        {
            var p0 = points[Math.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(i + 2, points.Length - 1)];
            for (int s = 0; s < steps; s++)
                result.Add(p1.CubicInterpolate(p2, p0, p3, s / (float)steps));
        }
        result.Add(points[^1]);
        return result.ToArray();
    }
}
