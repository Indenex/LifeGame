using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Clothes cut to the player figure (model-1, D-074): each garment is its own
// mesh on the player's skeleton, every part bound to the bone it sits on, so it
// follows every pose with no animation code. PlayerModel.Wear puts them on.
public static class Garments
{
    public const ushort WoolTunic = 200, FootWraps = 201;

    // Undyed wool, like the sheep's fleece (D-027), and tanned leather like the
    // hides drying on the racks.
    static readonly Color Wool = new(0.78f, 0.74f, 0.64f);
    static readonly Color Leather = new(0.55f, 0.37f, 0.22f);
    static readonly Color Cord = new(0.45f, 0.34f, 0.18f);

    static readonly Dictionary<(string, int), ArrayMesh?> Cache = new();
    static Material? material;

    // The mesh for an item worn in a slot, or the leaf wrap for an empty body
    // slot; null for nothing to show.
    public static ArrayMesh? For(HumanFigure.Style style, byte slot, ushort item)
    {
        var built = HumanFigure.Build(style);
        if (slot == PlayerModel.Body && item != WoolTunic) return built.LeafWrap;
        int key = slot * 100000 + item;
        if (Cache.TryGetValue((style.Key, key), out var mesh)) return mesh;
        material ??= Look.Solid(Colors.White);
        mesh = (slot, item) switch
        {
            (PlayerModel.Body, WoolTunic) => Tunic(built),
            (PlayerModel.Feet, FootWraps) => Wraps(built),
            _ => null,
        };
        Cache[(style.Key, key)] = mesh;
        return mesh;
    }

    // A sleeveless-to-short-sleeved woollen tunic to mid-thigh, belted with cord.
    static ArrayMesh Tunic(HumanFigure.Built f)
    {
        var s = f.Style;
        float b = s.Bulk, sx = s.ShoulderX;
        var k = new MeshKit(s.Flat);
        k.Paint(Wool).Bone(HumanRig.Spine);
        k.Loft(new Vector3(0, f.WaistY - 0.03f, 0), new Vector3(0, f.NeckY - 0.005f, 0), new MeshKit.Ring[]
        {
            new(0f, sx * 0.74f + 0.02f, 0.1f * b + 0.02f, 0.004f),
            new(0.5f, sx * 0.9f + 0.02f, 0.125f * b + 0.02f, 0.016f),
            new(0.78f, sx * 0.97f + 0.022f, 0.115f * b + 0.02f, 0.006f),
            new(0.95f, sx * 0.62f + 0.02f, 0.09f * b + 0.015f),
            new(1f, 0.085f * b, 0.075f * b),
        }, s.TorsoSides, 0.05f);

        // Short sleeves over the tops of the arms.
        for (int side = 0; side < 2; side++)
        {
            int u = side == 0 ? HumanRig.ShoulderL : HumanRig.ShoulderR;
            Vector3 sh = f.Joints.At[u], el = f.Joints.At[u + 1];
            var down = (el - sh).Normalized();
            k.Paint(Wool, 0.97f).Bone(u);
            k.Loft(sh + Vector3.Up * 0.03f, sh + down * 0.14f, new MeshKit.Ring[]
            {
                new(0f, 0.07f * b, 0.075f * b), new(0.3f, 0.082f * b, 0.085f * b), new(1f, 0.074f * b, 0.077f * b),
            }, s.LimbSides, 0.4f);
        }

        // The skirt hangs from the waist and flares, clear of the legs as they swing.
        k.Paint(Wool).Bone(HumanRig.Hips);
        k.Loft(new Vector3(0, f.WaistY + 0.03f, 0), new Vector3(0, f.HipY - 0.25f, 0), new MeshKit.Ring[]
        {
            new(0f, sx * 0.74f + 0.02f, 0.1f * b + 0.02f, 0.004f),
            new(0.35f, f.PelvisWidth + 0.03f, 0.112f * b + 0.03f),
            new(1f, f.PelvisWidth + 0.075f, 0.112f * b + 0.07f),
        }, s.TorsoSides, 0.02f);
        // A slightly darker hem, and a cord belt.
        k.Paint(Wool, 0.88f).Loft(new Vector3(0, f.HipY - 0.235f, 0), new Vector3(0, f.HipY - 0.255f, 0), new MeshKit.Ring[]
        {
            new(0f, f.PelvisWidth + 0.074f, 0.112f * b + 0.069f), new(1f, f.PelvisWidth + 0.077f, 0.112f * b + 0.072f),
        }, s.TorsoSides, 0.02f);
        k.Paint(Cord).Loft(new Vector3(0, f.WaistY - 0.005f, 0), new Vector3(0, f.WaistY + 0.022f, 0), new MeshKit.Ring[]
        {
            new(0f, sx * 0.74f + 0.028f, 0.1f * b + 0.028f, 0.004f), new(1f, sx * 0.74f + 0.028f, 0.1f * b + 0.028f, 0.004f),
        }, s.TorsoSides, 0.05f);

        var mesh = new ArrayMesh();
        k.Commit(mesh, material!);
        return mesh;
    }

    // Leather wrapped round each foot and up over the ankle, tied with cord.
    static ArrayMesh Wraps(HumanFigure.Built f)
    {
        var s = f.Style;
        float b = s.Bulk;
        var k = new MeshKit(s.Flat);
        for (int side = 0; side < 2; side++)
        {
            int t = side == 0 ? HumanRig.HipL : HumanRig.HipR;
            var ankle = f.Joints.At[t + 2];
            k.Paint(Leather).Bone(t + 2);
            if (s.RoundFeet)
                k.Ellipsoid(new Vector3(ankle.X, 0.047f, 0.03f), new Vector3(0.058f, 0.054f, 0.096f) * b, s.LimbSides, 7, null, 0.6f);
            else
                k.Loft(new Vector3(ankle.X, 0.035f, -0.068f), new Vector3(ankle.X, 0.032f, 0.14f), new MeshKit.Ring[]
                {
                    new(0f, 0.04f * b, 0.036f), new(0.3f, 0.049f * b, 0.045f, 0.008f), new(0.8f, 0.055f * b, 0.037f), new(1f, 0.05f * b, 0.03f, -0.004f),
                }, s.Flat ? 5 : 8, 0.4f);
            // The cuff round the ankle rides on the shin.
            k.Paint(Leather, 0.92f).Bone(t + 1);
            k.Loft(new Vector3(ankle.X, 0.04f, -0.01f), new Vector3(ankle.X, 0.17f, -0.01f), new MeshKit.Ring[]
            {
                new(0f, 0.05f * b, 0.053f * b), new(1f, 0.044f * b, 0.047f * b),
            }, s.LimbSides, 0.1f);
            foreach (float y in new[] { 0.075f, 0.14f })
                k.Paint(Cord).Tube(MeshKit.Circle(new Vector3(ankle.X, y, -0.01f), Vector3.Up, 0.05f * b, 16), 0.006f, 4, -1f, false);
        }
        var mesh = new ArrayMesh();
        k.Commit(mesh, material!);
        return mesh;
    }
}
