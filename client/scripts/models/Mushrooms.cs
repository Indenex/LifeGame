using Godot;

namespace LifeGame;

// Woodland mushrooms (model-34, for Survival's survival-35 mushroom patches): a brown cap
// mushroom like a penny bun, about 1.3x life so it reads from a few metres. A thick pale
// stem swelling a little at the foot, a domed warm-brown cap with a paler rolled rim and a
// cream ring of gills under it. Colours are in the vertices; draw with Mushrooms.Material.
//
// - Patch(variant): a ripe cluster in a tree's shade, four to seven of mixed sizes, some
//   leaning, the biggest near the middle, with a scrap of moss and a fallen leaf.
// - Picked(variant): the same spot after picking: two tiny buttons just breaking the soil
//   and the cut stubs of the stems that were taken. Same variant, same spot.
// - Pile: three picked mushrooms lying together (ItemModels' MushroomItem: ground, hand, icon).
// Origins are the ground under the middle; y = 0 is the soil.
public static class Mushrooms
{
    public const int Variants = 4;

    static readonly Color Stem = new(0.88f, 0.83f, 0.72f);
    static readonly Color Gill = new(0.86f, 0.76f, 0.56f);
    static readonly Color Moss = new(0.5f, 0.66f, 0.34f);
    static readonly Color Leaf = new(0.6f, 0.44f, 0.27f);
    // Caps from a warm chestnut to a paler hazel; each patch keeps to two of them.
    static readonly Color[] Caps = { new(0.5f, 0.31f, 0.17f), new(0.58f, 0.38f, 0.2f), new(0.45f, 0.29f, 0.18f), new(0.64f, 0.46f, 0.27f) };

    static readonly System.Collections.Generic.Dictionary<(int, bool), Mesh> Built = new();
    static Mesh? pile;
    static Material? material;

    public static Material Material => material ??= Painted.Of(Look.Solid(Colors.White));

    public static Mesh Patch(int variant) => Get(variant, true);
    public static Mesh Picked(int variant) => Get(variant, false);
    public static Mesh Pile => pile ??= BuildPile();

    static float H(int v, int salt) => MeshKit.Hash(v * 37 + salt, 563);

    static Mesh Get(int variant, bool ripe)
    {
        int v = ((variant % Variants) + Variants) % Variants;
        lock (Built)
        {
            if (!Built.TryGetValue((v, ripe), out var mesh)) Built[(v, ripe)] = mesh = BuildPatch(v, ripe);
            return mesh;
        }
    }

    // One mushroom standing from foot along up, size 1 = a 9 cm tall, 6 cm wide cap.
    // open runs 0 (a closed button, cap round and low) to 1 (cap opened out flat-domed).
    public static void One(MeshKit k, Vector3 foot, Vector3 up, float size, Color cap, float open = 1f)
    {
        up = up.Normalized();
        float tall = 0.068f * size * Mathf.Lerp(0.5f, 1f, open);
        float stem = 0.016f * size * Mathf.Lerp(1.3f, 1f, open);
        float r = 0.042f * size * Mathf.Lerp(0.7f, 1f, open);
        var top = foot + up * tall;
        // Local Y along the stem, so the cap's flattened underside faces down it.
        var upright = new Basis(new Quaternion(Vector3.Up, up));
        // A stout stem, swollen toward the foot like a penny bun's.
        k.Paint(Stem, 0.95f).Loft(foot - up * 0.006f, top, new MeshKit.Ring[]
        {
            new(0f, stem * 1.3f, stem * 1.3f), new(0.3f, stem * 1.45f, stem * 1.45f), new(0.7f, stem * 1.1f, stem * 1.1f), new(1f, stem * 0.9f, stem * 0.9f),
        }, 10, 0.2f);
        // The gills: a cream disc under the cap, a touch inside its rim.
        k.Paint(Gill).Ellipsoid(top - up * r * 0.1f, new Vector3(r * 0.92f, r * 0.14f, r * 0.92f), 14, 3, upright);
        // The cap: a broad soft dome from its rolled, paler rim up to a round crown.
        float capTall = r * Mathf.Lerp(0.9f, 0.62f, open);
        k.Paint(cap.Lightened(0.2f)).Loft(top - up * r * 0.18f, top, new MeshKit.Ring[]
        {
            new(0f, r * 0.9f, r * 0.9f), new(1f, r * 1.03f, r * 1.03f),
        }, 14, 0f);
        // A half-ellipsoid: round at the crown, its underside flattened into the rim.
        k.Paint(cap).Ellipsoid(top - up * r * 0.04f, new Vector3(r * 1.03f, capTall, r * 1.03f), 14, 7, upright, 0.12f);
    }

    static ArrayMesh BuildPatch(int v, bool ripe)
    {
        var k = new MeshKit(false) { Skinned = false };
        var capA = Caps[v % Caps.Length];
        var capB = Caps[(v + 1 + (int)(H(v, 1) * 2f)) % Caps.Length];
        // A soft cushion of moss at one side and two fallen leaves; no ground of its own, so the
        // patch sits on whatever the terrain is.
        var mossAt = new Vector3(-0.08f + H(v, 4) * 0.05f, -0.004f, 0.05f);
        k.Paint(Moss, 0.9f + H(v, 3) * 0.2f).Ellipsoid(mossAt, new Vector3(0.05f, 0.016f, 0.038f), 10, 4, new Basis(Vector3.Up, H(v, 2) * 3f), 0.2f);
        for (int i = 0; i < 2; i++)
        {
            float la = H(v, 5 + i) * Mathf.Tau;
            var leafAt = new Vector3(Mathf.Cos(la), 0, Mathf.Sin(la)) * (0.1f + H(v, 8 + i) * 0.05f) + Vector3.Up * 0.004f;
            var tip = leafAt + new Vector3(Mathf.Cos(la + 1.8f), 0.006f, Mathf.Sin(la + 1.8f)) * 0.06f;
            k.Paint(i == 0 ? Leaf : Leaf.Darkened(0.25f), 0.85f + H(v, 6 + i) * 0.3f).Blade(leafAt, tip, Vector3.Up, 0.032f, 0.006f);
        }

        int count = 4 + (int)(H(v, 7) * 4f);
        for (int i = 0; i < count; i++)
        {
            // Bunched toward the middle: the biggest first, the rest smaller round it.
            float a = i * 2.4f + H(v, 10 + i);
            float rad = i == 0 ? 0.01f : 0.05f + H(v, 20 + i) * 0.09f;
            var foot = new Vector3(Mathf.Cos(a) * rad, 0, Mathf.Sin(a) * rad);
            float size = i == 0 ? 1.2f : 0.55f + H(v, 30 + i) * 0.55f;
            if (ripe)
            {
                // The outer ones lean out from the middle, as they push for the light.
                var lean = i == 0 ? Vector3.Zero : new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (0.1f + H(v, 40 + i) * 0.3f);
                One(k, foot, Vector3.Up + lean, size, H(v, 50 + i) < 0.6f ? capA : capB, i == count - 1 ? 0.35f : 0.8f + H(v, 60 + i) * 0.2f);
            }
            else
            {
                // The cut stub of a picked stem, its pale cut face up.
                float stub = 0.013f * size * 1.2f;
                k.Paint(Stem).Loft(foot - Vector3.Up * 0.004f, foot + Vector3.Up * 0.012f, new MeshKit.Ring[] { new(0f, stub * 1.2f, stub * 1.2f), new(1f, stub, stub) }, 8, 0f);
                k.Paint(Stem.Lightened(0.15f)).Ellipsoid(foot + Vector3.Up * 0.012f, new Vector3(stub, 0.002f, stub), 8, 2);
            }
        }
        // Two buttons just breaking the soil, ripe or picked, so a picked patch still reads as one.
        for (int i = 0; i < 2; i++)
        {
            float a = H(v, 70 + i) * Mathf.Tau;
            var foot = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (0.07f + H(v, 72 + i) * 0.05f);
            One(k, foot - Vector3.Up * 0.012f, Vector3.Up, 0.4f, capA, 0f);
        }
        var mesh = new ArrayMesh();
        k.Commit(mesh, Material);
        return mesh;
    }

    // Three picked mushrooms lying together, stems crossed, one on its side showing its gills.
    static ArrayMesh BuildPile()
    {
        var k = new MeshKit(false) { Skinned = false };
        PileInto(k);
        var mesh = new ArrayMesh();
        k.Commit(mesh, Material);
        return mesh;
    }

    public static void PileInto(MeshKit k)
    {
        One(k, new Vector3(-0.045f, 0.026f, 0.02f), new Vector3(1f, 0.05f, -0.25f), 1.05f, Caps[0]);
        One(k, new Vector3(0.05f, 0.022f, -0.03f), new Vector3(-0.85f, 0.12f, -0.55f), 0.9f, Caps[1]);
        One(k, new Vector3(0.0f, 0.016f, 0.05f), new Vector3(0.35f, 0.08f, 1f), 0.7f, Caps[0], 0.6f);
    }
}
