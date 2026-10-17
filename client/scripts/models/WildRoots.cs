using Godot;

namespace LifeGame;

// Wild roots (model-35, for Survival's survival-36 root patches): wild carrot in open
// meadow, about 1.2x life so it reads from a few metres. A low rosette of feathery leaves,
// one to three stalks standing out of it, each crowned with a flat white umbel of tiny
// florets with the odd dark one at its heart, as wild carrot has. Colours are in the
// vertices; draw with WildRoots.Material (it sways a little in the wind).
//
// - Patch(variant): the ripe plant, ready to dig.
// - Dug(variant): the same spot dug: a mound of turned earth by a shallow hole, a broken
//   stalk and a few torn leaves lying about. Same variant, same spot.
// - Pile: two or three pale tapered roots with their leaf stubs (ItemModels: ground, hand, icon).
// Origins are the ground under the middle; y = 0 is the soil.
public static class WildRoots
{
    public const int Variants = 4;

    static readonly Color Leaf = new(0.38f, 0.52f, 0.2f);
    static readonly Color LeafDark = new(0.27f, 0.4f, 0.15f);
    static readonly Color Stalk = new(0.45f, 0.55f, 0.27f);
    static readonly Color Floret = new(0.96f, 0.95f, 0.88f);
    static readonly Color Heart = new(0.36f, 0.16f, 0.3f);
    static readonly Color Root = new(0.9f, 0.84f, 0.67f);
    static readonly Color Earth = Look.Earth.Lightened(0.06f);

    static readonly System.Collections.Generic.Dictionary<(int, bool), Mesh> Built = new();
    static Mesh? pile;
    static Material? material;

    public static Material Material => material ??= Swaying();

    public static Mesh Patch(int variant) => Get(variant, true);
    public static Mesh Dug(int variant) => Get(variant, false);
    public static Mesh Pile => pile ??= BuildPile();

    static Material Swaying()
    {
        var m = (ShaderMaterial)((ShaderMaterial)Painted.Of(Look.Solid(Colors.White))).Duplicate();
        // The stalks nod in the wind from the ground up; the rosette barely moves.
        m.SetShaderParameter("sway", 0.05f);
        m.SetShaderParameter("sway_height", 0.55f);
        return m;
    }

    static float H(int v, int salt) => MeshKit.Hash(v * 41 + salt, 577);

    static Mesh Get(int variant, bool ripe)
    {
        int v = ((variant % Variants) + Variants) % Variants;
        lock (Built)
        {
            if (!Built.TryGetValue((v, ripe), out var mesh)) Built[(v, ripe)] = mesh = BuildPatch(v, ripe);
            return mesh;
        }
    }

    // One feathery leaf from root along dir (flat, length in metres): a midrib with leaflets
    // in pairs down both sides, smaller toward the tip, the whole leaf arching up then out.
    static void FeatherLeaf(MeshKit k, Vector3 root, Vector3 dir, float length, float lift, Color color, float shade)
    {
        dir = new Vector3(dir.X, 0, dir.Z).Normalized();
        var side = Vector3.Up.Cross(dir).Normalized();
        Vector3 At(float t) => root + dir * (length * t) + Vector3.Up * (lift * Mathf.Sin(t * Mathf.Pi * 0.8f));
        k.Paint(color.Darkened(0.1f), shade).Tube(new[] { At(0f), At(0.35f), At(0.7f), At(1f) }, 0.0035f, 4, 0.0015f, false);
        const int Pairs = 7;
        for (int p = 0; p < Pairs; p++)
        {
            float t = 0.16f + p * 0.8f / Pairs;
            float size = length * 0.3f * (1f - t * 0.5f);
            var at = At(t);
            for (int s = -1; s <= 1; s += 2)
            {
                var tip = at + (side * s * 0.85f + dir * 0.5f).Normalized() * size + Vector3.Up * size * 0.15f;
                k.Paint(color, shade * (0.92f + 0.08f * s)).Blade(at, tip, Vector3.Up, size * 0.55f, size * 0.15f);
            }
        }
        k.Paint(color, shade).Blade(At(0.92f), At(1f) + dir * length * 0.12f, Vector3.Up, length * 0.08f, 0f);
    }

    // A flat-topped umbel at top, facing up: rays fan out from the stalk's tip to a broad
    // dome of floret clusters, the middle one dark.
    static void Umbel(MeshKit k, Vector3 top, float radius, int v, int salt)
    {
        int rays = 12 + (int)(H(v, salt) * 5f);
        for (int r = 0; r < rays; r++)
        {
            float a = r * Mathf.Tau / rays + H(v, salt + r) * 0.3f;
            // Every other ray ends short, so the florets fill the dome in two rings.
            float reach = radius * (r % 2 == 0 ? 0.8f + H(v, salt + 20 + r) * 0.2f : 0.45f + H(v, salt + 20 + r) * 0.15f);
            var end = top + new Vector3(Mathf.Cos(a) * reach, radius * 0.38f, Mathf.Sin(a) * reach);
            k.Paint(Stalk.Lightened(0.1f)).Tube(new[] { top, top.Lerp(end, 0.5f) + Vector3.Up * radius * 0.08f, end }, 0.0018f, 3, 0.0012f, false);
            // A little cluster of florets at each ray's end.
            k.Paint(Floret, 0.9f + H(v, salt + 40 + r) * 0.1f).Ellipsoid(end + Vector3.Up * 0.006f, new Vector3(radius * 0.24f, radius * 0.1f, radius * 0.24f), 7, 3, null, 0.4f);
        }
        k.Paint(Floret).Ellipsoid(top + Vector3.Up * radius * 0.42f, new Vector3(radius * 0.3f, radius * 0.12f, radius * 0.3f), 8, 3, null, 0.4f);
        k.Paint(Heart).Ellipsoid(top + Vector3.Up * radius * 0.52f, new Vector3(radius * 0.08f, radius * 0.05f, radius * 0.08f), 6, 2);
    }

    static ArrayMesh BuildPatch(int v, bool ripe)
    {
        var k = new MeshKit(false) { Skinned = false };
        float spin = H(v, 1) * Mathf.Tau;
        if (ripe)
        {
            int leaves = 7 + (int)(H(v, 2) * 4f);
            for (int i = 0; i < leaves; i++)
            {
                float a = spin + i * Mathf.Tau / leaves + H(v, 10 + i) * 0.35f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float length = 0.17f + H(v, 20 + i) * 0.08f;
                FeatherLeaf(k, dir * 0.012f + Vector3.Up * 0.005f, dir, length, 0.05f + H(v, 30 + i) * 0.04f, i % 3 == 0 ? LeafDark : Leaf, 0.9f + H(v, 40 + i) * 0.2f);
            }
            int stalks = 1 + (int)(H(v, 3) * 2.99f);
            for (int s = 0; s < stalks; s++)
            {
                float a = spin + s * 2.3f;
                float tall = (0.42f + H(v, 50 + s) * 0.12f) * (s == 0 ? 1f : 0.78f);
                var lean = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (s == 0 ? 0.03f : 0.09f + H(v, 55 + s) * 0.05f);
                var foot = lean * 0.2f;
                var top = foot + lean + Vector3.Up * tall;
                var mid = foot.Lerp(top, 0.5f) + lean * 0.3f;
                k.Paint(Stalk, 0.95f).Tube(new[] { foot, mid, top }, 0.0075f, 5, 0.0045f, false);
                // A small leaf part-way up, as wild carrot has.
                FeatherLeaf(k, foot.Lerp(top, 0.35f), new Vector3(Mathf.Cos(a + 2f), 0, Mathf.Sin(a + 2f)), 0.07f, 0.02f, Leaf, 1f);
                Umbel(k, top, (0.07f + H(v, 60 + s) * 0.02f) * (s == 0 ? 1f : 0.8f), v, 100 + s * 50);
            }
        }
        else
        {
            // The hole the root came out of, and its earth heaped to one side.
            var heapAt = new Vector3(Mathf.Cos(spin), 0, Mathf.Sin(spin)) * 0.11f;
            k.Paint(Earth.Darkened(0.35f)).Ellipsoid(new Vector3(0, -0.012f, 0), new Vector3(0.075f, 0.016f, 0.065f), 10, 3, null, 1f);
            // A low, lumpy heap: a broad spread and two smaller humps on it, crumbs round the edge.
            k.Paint(Earth, 0.95f).Ellipsoid(heapAt, new Vector3(0.12f, 0.02f, 0.09f), 12, 4, new Basis(Vector3.Up, -spin), 0.2f);
            for (int c = 0; c < 2; c++)
            {
                var hump = heapAt + new Vector3(Mathf.Cos(spin + c * 2.5f), 0, Mathf.Sin(spin + c * 2.5f)) * 0.035f;
                k.Paint(Earth.Lightened(0.05f), 0.9f + H(v, 70 + c) * 0.15f).Ellipsoid(hump + Vector3.Up * 0.012f, new Vector3(0.05f, 0.018f, 0.042f), 9, 3, new Basis(Vector3.Up, spin + c), 0.3f);
            }
            for (int c = 0; c < 7; c++)
            {
                float a = spin + c * 0.9f + H(v, 74 + c) * 0.5f;
                var at = heapAt + new Vector3(Mathf.Cos(a) * 0.12f, 0, Mathf.Sin(a) * 0.095f) * (0.9f + H(v, 84 + c) * 0.35f);
                k.Paint(Earth.Darkened(0.05f), 0.9f + H(v, 78 + c) * 0.2f).Ellipsoid(at + Vector3.Up * 0.004f, Vector3.One * (0.006f + H(v, 82 + c) * 0.006f), 5, 2);
            }
            // A few torn leaves and the broken stalk with its umbel, lying where they fell.
            for (int i = 0; i < 3; i++)
            {
                float a = spin + 2.4f + i * 1.3f + H(v, 90 + i) * 0.4f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                FeatherLeaf(k, dir * 0.1f + Vector3.Up * 0.004f, dir, 0.1f + H(v, 94 + i) * 0.04f, 0.008f, i == 1 ? LeafDark : Leaf, 0.8f);
            }
            float b = spin + Mathf.Pi;
            var lay = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
            var from = lay * 0.06f + Vector3.Up * 0.006f;
            var to = from + lay * 0.34f + Vector3.Up * 0.02f;
            k.Paint(Stalk, 0.85f).Tube(new[] { from, from.Lerp(to, 0.5f) + Vector3.Up * 0.012f, to }, 0.005f, 5, 0.0035f, false);
            // Its umbel lying tipped on the grass, a little wilted.
            var head = to + lay * 0.012f + Vector3.Up * 0.012f;
            for (int f = 0; f < 6; f++)
            {
                float a = f * Mathf.Tau / 6f;
                var at = head + new Vector3(0, Mathf.Sin(a) * 0.022f, 0) + lay.Cross(Vector3.Up) * Mathf.Cos(a) * 0.03f + lay * 0.012f;
                k.Paint(Floret.Darkened(0.08f), 0.9f).Ellipsoid(at, new Vector3(0.012f, 0.012f, 0.012f), 6, 2);
            }
        }
        var mesh = new ArrayMesh();
        k.Commit(mesh, Material);
        return mesh;
    }

    // One root from crown along dir: a pale taper with fine ridges of rings, a few hair
    // roots and the cut stubs of its leaves at the crown.
    public static void One(MeshKit k, Vector3 crown, Vector3 dir, float size)
    {
        dir = dir.Normalized();
        float length = 0.15f * size, r = 0.016f * size;
        var tip = crown + dir * length;
        k.Paint(Root, 0.95f).Loft(crown, tip, new MeshKit.Ring[]
        {
            new(0f, r * 0.85f, r * 0.85f), new(0.12f, r, r), new(0.45f, r * 0.72f, r * 0.72f), new(0.8f, r * 0.32f, r * 0.32f), new(1f, r * 0.06f, r * 0.06f),
        }, 9, 0.4f);
        var side = dir.Cross(Vector3.Up).LengthSquared() > 0.01f ? dir.Cross(Vector3.Up).Normalized() : Vector3.Right;
        for (int h = 0; h < 2; h++)
        {
            var at = crown + dir * length * (0.4f + h * 0.25f);
            k.Paint(Root.Darkened(0.12f)).Tube(new[] { at, at + (side * (h == 0 ? 1 : -1) + dir) * 0.018f * size }, 0.0012f * size, 3, 0.0005f, false);
        }
        for (int s = 0; s < 3; s++)
        {
            var stubDir = (-dir + side * (s - 1) * 0.5f + Vector3.Up * 0.3f).Normalized();
            k.Paint(Stalk, 0.9f).Tube(new[] { crown, crown + stubDir * 0.035f * size }, 0.0025f * size, 4, 0.0018f * size, true);
        }
    }

    static ArrayMesh BuildPile()
    {
        var k = new MeshKit(false) { Skinned = false };
        PileInto(k);
        var mesh = new ArrayMesh();
        k.Commit(mesh, Material);
        return mesh;
    }

    // Three roots lying together, crossed.
    public static void PileInto(MeshKit k)
    {
        One(k, new Vector3(-0.07f, 0.016f, 0.01f), new Vector3(1f, -0.04f, 0.2f), 1.05f);
        One(k, new Vector3(0.06f, 0.014f, -0.04f), new Vector3(-0.8f, -0.03f, 0.55f), 0.9f);
        One(k, new Vector3(0.0f, 0.03f, 0.05f), new Vector3(0.4f, -0.1f, -1f), 0.75f);
    }
}
