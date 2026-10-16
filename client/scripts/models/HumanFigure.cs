using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The player figure, built in code in one of a few styles so Justin can pick a
// look (model-1). Each style is one skinned mesh on HumanRig's skeleton: one
// draw call per player. A fresh player wears only a wrap of leaves.
public static class HumanFigure
{
    public sealed class Style
    {
        public string Key = "", Name = "";
        public bool Flat;
        // Body height in head heights: 5 reads friendly, 6 sturdy, 7+ lifelike.
        public float Heads = 6f;
        public float LegFraction = 0.46f;
        public float ShoulderX = 0.19f, HipX = 0.095f;
        // Multiplies limb thickness.
        public float Bulk = 1f;
        public int LimbSides = 8, TorsoSides = 10, HeadSegments = 10, HeadRings = 7;
        public int Leaves;
        public bool Skirt, Brow, Ears, Thumbs, RoundFeet, RoundHands;
        public float EyeSize = 1f;
        public int HairLocks;
        public float LeafWidth = 0.1f;
    }

    // Chunky low-poly, flat-shaded facets: reads at a distance, fits the painted
    // low-poly world, cheapest to draw.
    public static readonly Style Carved = new()
    {
        Key = "A", Name = "Carved", Flat = true, Heads = 6.2f, LegFraction = 0.46f, ShoulderX = 0.205f, HipX = 0.1f, Bulk = 1.15f,
        LimbSides = 6, TorsoSides = 8, HeadSegments = 8, HeadRings = 6, Leaves = 9, LeafWidth = 0.12f, Brow = true, HairLocks = 7,
    };

    // Smooth and rounded, a bigger head and shorter legs: soft and friendly.
    public static readonly Style Soft = new()
    {
        Key = "B", Name = "Soft", Flat = false, Heads = 5.0f, LegFraction = 0.42f, ShoulderX = 0.17f, HipX = 0.095f, Bulk = 1.05f,
        LimbSides = 10, TorsoSides = 14, HeadSegments = 16, HeadRings = 10, Skirt = true, EyeSize = 1.5f, RoundFeet = true, RoundHands = true,
    };

    // Lifelike proportions and more detail (brow, ears, thumbs): closest to Valheim.
    public static readonly Style Natural = new()
    {
        Key = "C", Name = "Natural", Flat = false, Heads = 7.3f, LegFraction = 0.48f, ShoulderX = 0.19f, HipX = 0.092f, Bulk = 0.95f,
        LimbSides = 12, TorsoSides = 16, HeadSegments = 18, HeadRings = 12, Leaves = 14, LeafWidth = 0.08f, Brow = true, Ears = true, Thumbs = true, HairLocks = 4,
    };

    public static readonly Style[] All = { Carved, Soft, Natural };

    public sealed class Built
    {
        public Style Style = null!;
        public ArrayMesh Mesh = null!;
        // The leaf wrap a fresh player wears; clothes on the body replace it.
        public ArrayMesh LeafWrap = null!;
        public HumanRig.Joints Joints = null!;
        // Body measurements clothes are cut to.
        public float HeadHeight, HipY, WaistY, ShoulderY, NeckY, PelvisWidth;
        public int Triangles;
        // How far below the wrist joint the middle of the fist is.
        public float GripDrop;
    }

    static readonly Dictionary<string, Built> Cache = new();
    static Material? material;

    static readonly Color Eye = new(0.10f, 0.08f, 0.07f);
    static readonly Color Cord = new(0.45f, 0.34f, 0.18f);

    public static Built Build(Style s)
    {
        if (Cache.TryGetValue(s.Key, out var done)) return done;
        material ??= Look.Solid(Colors.White);

        const float H = 1.75f;
        float hh = H / s.Heads;
        float chin = H - hh;
        float neckY = chin - hh * 0.22f;
        float shoulderY = neckY - hh * 0.18f;
        float hipY = s.LegFraction * H;
        const float ankleY = 0.075f;
        float thigh = (hipY - 0.02f - ankleY) * 0.5f;
        float wristY = hipY - 0.04f - (s.Heads - 5f) * 0.035f;
        float arm = shoulderY - wristY;
        float waistY = MeshKit.Lerp(hipY, shoulderY, 0.35f);
        var j = HumanRig.Place(hipY, waistY, neckY, chin + 0.03f, s.ShoulderX, shoulderY, s.HipX, arm * 0.53f, arm * 0.47f, thigh, 0f, ankleY);

        var k = new MeshKit(s.Flat);
        float b = s.Bulk;
        float pelvisW = s.HipX + 0.065f;

        // Torso in two parts, so the chest can bend over the hips.
        k.Bone(HumanRig.Hips).Paint(Look.Skin);
        k.Loft(new Vector3(0, hipY - 0.1f, 0), new Vector3(0, waistY + 0.03f, 0), new MeshKit.Ring[]
        {
            new(0f, pelvisW * 0.8f, 0.09f * b),
            new(0.3f, pelvisW, 0.112f * b),
            new(1f, s.ShoulderX * 0.7f, 0.1f * b, 0.004f),
        }, s.TorsoSides, 0.4f);
        k.Bone(HumanRig.Spine).Paint(Look.Skin);
        k.Loft(new Vector3(0, waistY - 0.02f, 0), new Vector3(0, neckY + 0.01f, 0), new MeshKit.Ring[]
        {
            new(0f, s.ShoulderX * 0.7f, 0.1f * b, 0.004f),
            new(0.5f, s.ShoulderX * 0.9f, 0.125f * b, 0.016f),
            new(0.78f, s.ShoulderX * 0.97f, 0.115f * b, 0.006f),
            new(0.93f, s.ShoulderX * 0.6f, 0.085f * b),
            new(1f, 0.06f * b, 0.055f * b),
        }, s.TorsoSides, 0.3f);

        // Neck and head.
        k.Bone(HumanRig.Neck).Paint(Look.Skin);
        k.Loft(new Vector3(0, neckY - 0.03f, -0.005f), new Vector3(0, chin + 0.04f, 0.0f), new MeshKit.Ring[]
        {
            new(0f, 0.05f * b, 0.05f * b),
            new(1f, 0.045f * b, 0.048f * b),
        }, s.LimbSides, 0.2f);
        k.Bone(HumanRig.Head);
        if (s.Flat)
        {
            // Carved: a block of a head in a few clean planes, narrow at the chin,
            // broad at the cheekbones, flat on top.
            k.Loft(new Vector3(0, chin, hh * 0.06f), new Vector3(0, H, -hh * 0.02f), new MeshKit.Ring[]
            {
                new(0f, hh * 0.17f, hh * 0.2f, hh * 0.06f),
                new(0.22f, hh * 0.3f, hh * 0.38f, hh * 0.03f),
                new(0.55f, hh * 0.39f, hh * 0.45f),
                new(0.85f, hh * 0.37f, hh * 0.43f),
                new(1f, hh * 0.25f, hh * 0.3f),
            }, s.HeadSegments, 0.3f, 0f);
        }
        else
        {
            k.Ellipsoid(new Vector3(0, chin + hh * 0.52f, 0), new Vector3(hh * 0.40f, hh * 0.52f, hh * 0.46f), s.HeadSegments, s.HeadRings);
            // Jaw and cheeks, a little forward of the skull.
            k.Paint(Look.Skin, 0.97f).Ellipsoid(new Vector3(0, chin + hh * 0.24f, hh * 0.05f), new Vector3(hh * 0.31f, hh * 0.25f, hh * 0.36f), s.HeadSegments, s.HeadRings);
        }
        k.Paint(Look.Skin, 0.95f).Ellipsoid(new Vector3(0, chin + hh * 0.42f, hh * 0.45f), new Vector3(hh * 0.065f, hh * 0.1f, hh * 0.08f), s.Flat ? 4 : 8, s.Flat ? 3 : 5);
        if (s.Brow)
            k.Paint(Look.Skin, 0.93f).Ellipsoid(new Vector3(0, chin + hh * 0.64f, hh * 0.35f), new Vector3(hh * 0.3f, hh * 0.065f, hh * 0.1f), s.Flat ? 6 : 12, s.Flat ? 3 : 5);
        if (s.Ears)
            for (int side = -1; side <= 1; side += 2)
                k.Paint(Look.Skin, 0.92f).Ellipsoid(new Vector3(side * hh * 0.4f, chin + hh * 0.5f, -hh * 0.02f), new Vector3(hh * 0.05f, hh * 0.12f, hh * 0.08f), 8, 5);
        for (int side = -1; side <= 1; side += 2)
            k.Paint(Eye).Ellipsoid(new Vector3(side * hh * 0.16f, chin + hh * 0.55f, hh * (s.Flat ? 0.37f : 0.405f)), new Vector3(hh * 0.055f, hh * 0.065f, hh * 0.03f) * s.EyeSize, s.Flat ? 5 : 8, s.Flat ? 3 : 5);

        // Hair: a cap set high and back so the face shows, a lock at the nape, and
        // in some styles chunky locks round the crown.
        k.Paint(Look.Hair).Ellipsoid(new Vector3(0, chin + hh * 0.78f, -hh * 0.06f), new Vector3(hh * 0.45f, hh * 0.36f, hh * 0.5f), s.HeadSegments, s.HeadRings);
        k.Paint(Look.Hair, 0.92f).Ellipsoid(new Vector3(0, chin + hh * 0.5f, -hh * 0.3f), new Vector3(hh * 0.36f, hh * 0.32f, hh * 0.2f), s.HeadSegments, s.HeadRings);
        for (int i = 0; i < s.HairLocks; i++)
        {
            float a = Mathf.DegToRad(-105f + 210f * i / Mathf.Max(1, s.HairLocks - 1)) + Mathf.Pi;
            var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            var at = new Vector3(0, chin + hh * (0.66f + MeshKit.Hash(i, 3) * 0.1f), -hh * 0.06f) + dir * hh * 0.4f;
            var tilt = MeshKit.Aim((dir + Vector3.Down * 0.8f).Normalized());
            k.Paint(Look.Hair, 0.9f + MeshKit.Hash(i, 4) * 0.12f).Ellipsoid(at, new Vector3(hh * 0.12f, hh * 0.11f, hh * 0.2f), s.Flat ? 5 : 8, s.Flat ? 3 : 5, tilt);
        }

        // Arms.
        for (int side = 0; side < 2; side++)
        {
            int u = side == 0 ? HumanRig.ShoulderL : HumanRig.ShoulderR;
            Vector3 sh = j.At[u], el = j.At[u + 1], wr = j.At[u + 2];
            k.Bone(u).Paint(Look.Skin);
            k.Ellipsoid(sh + new Vector3(-Mathf.Sign(sh.X) * 0.02f, -0.035f, 0), new Vector3(0.062f, 0.07f, 0.064f) * b, s.LimbSides, s.Flat ? 4 : 6);
            k.Loft(sh, el, new MeshKit.Ring[] { new(0f, 0.058f * b, 0.062f * b), new(0.5f, 0.053f * b, 0.056f * b), new(1f, 0.043f * b, 0.046f * b) }, s.LimbSides);
            k.Bone(u + 1);
            k.Loft(el, wr, new MeshKit.Ring[] { new(0f, 0.043f * b, 0.045f * b), new(0.3f, 0.046f * b, 0.05f * b), new(1f, 0.031f * b, 0.029f * b) }, s.LimbSides);
            k.Bone(u + 2);
            var down = (wr - el).Normalized();
            float hand = hh * 0.6f;
            if (s.RoundHands)
                k.Ellipsoid(wr + down * hand * 0.42f, new Vector3(0.033f, 0.052f, 0.045f) * b, s.LimbSides, 6);
            else
            {
                k.Loft(wr - down * 0.01f, wr + down * hand, new MeshKit.Ring[]
                {
                    new(0f, 0.02f * b, 0.032f * b), new(0.45f, 0.022f * b, 0.044f * b, 0.004f), new(0.85f, 0.018f * b, 0.038f * b), new(1f, 0.012f * b, 0.022f * b),
                }, s.Flat ? 4 : 8, 0.6f);
                if (s.Thumbs)
                {
                    var root = wr + down * hand * 0.22f + new Vector3(0, 0, 0.03f);
                    k.Loft(root, root + (down + Vector3.Back * 0.9f).Normalized() * hand * 0.45f,
                        new MeshKit.Ring[] { new(0f, 0.014f, 0.016f), new(1f, 0.01f, 0.011f) }, 6, 0.7f);
                }
            }
        }

        // Legs and feet.
        for (int side = 0; side < 2; side++)
        {
            int t = side == 0 ? HumanRig.HipL : HumanRig.HipR;
            Vector3 hip = j.At[t], knee = j.At[t + 1], ankle = j.At[t + 2];
            k.Bone(t).Paint(Look.Skin);
            k.Loft(hip + Vector3.Up * 0.05f, knee, new MeshKit.Ring[] { new(0f, 0.085f * b, 0.092f * b), new(0.5f, 0.075f * b, 0.08f * b, 0.005f), new(1f, 0.054f * b, 0.057f * b) }, s.LimbSides);
            k.Bone(t + 1);
            k.Ellipsoid(knee, new Vector3(0.052f, 0.058f, 0.055f) * b, s.LimbSides, s.Flat ? 4 : 6);
            k.Loft(knee, ankle, new MeshKit.Ring[] { new(0f, 0.053f * b, 0.055f * b), new(0.3f, 0.058f * b, 0.064f * b, -0.012f), new(1f, 0.036f * b, 0.038f * b) }, s.LimbSides);
            k.Bone(t + 2);
            if (s.RoundFeet)
                k.Ellipsoid(new Vector3(ankle.X, 0.045f, 0.035f), new Vector3(0.05f, 0.048f, 0.1f) * b, s.LimbSides, 7, null, 0.6f);
            else
            {
                k.Ellipsoid(ankle, new Vector3(0.04f, 0.045f, 0.045f) * b, s.LimbSides, s.Flat ? 4 : 6);
                k.Loft(new Vector3(ankle.X, 0.035f, -0.06f), new Vector3(ankle.X, 0.03f, 0.16f), new MeshKit.Ring[]
                {
                    new(0f, 0.034f * b, 0.03f), new(0.3f, 0.042f * b, 0.038f, 0.008f), new(0.75f, 0.048f * b, 0.03f), new(1f, 0.04f * b, 0.018f, -0.008f),
                }, s.Flat ? 5 : 8, 0.5f);
            }
        }

        var mesh = new ArrayMesh();
        k.Commit(mesh, material);
        int tris = k.Triangles;

        // A wrap of leaves: a band round the hips with leaves hanging from it, or
        // (soft style) one smooth skirt. Its own mesh, so clothes can replace it.
        k = new MeshKit(s.Flat);
        k.Bone(HumanRig.Hips);
        float bandW = pelvisW + 0.012f, bandD = 0.122f * b;
        if (s.Skirt)
        {
            k.Paint(Look.Leaf);
            k.Loft(new Vector3(0, hipY + 0.05f, 0), new Vector3(0, hipY - 0.2f, 0), new MeshKit.Ring[]
            {
                new(0f, bandW, bandD), new(0.4f, bandW + 0.03f, bandD + 0.03f), new(1f, bandW + 0.07f, bandD + 0.07f),
            }, s.TorsoSides, 0.05f);
        }
        else
        {
            k.Paint(s.Leaves > 10 ? Cord : Look.Leaf, 0.9f);
            k.Loft(new Vector3(0, hipY - 0.01f, 0), new Vector3(0, hipY + 0.05f, 0), new MeshKit.Ring[] { new(0f, bandW, bandD), new(1f, bandW - 0.004f, bandD - 0.004f) }, s.TorsoSides, 0.05f);
            for (int i = 0; i < s.Leaves; i++)
            {
                float a = Mathf.Tau * (i + MeshKit.Hash(i, 1) * 0.3f) / s.Leaves;
                var outward = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var root = new Vector3(Mathf.Sin(a) * bandW, hipY + 0.03f, Mathf.Cos(a) * bandD);
                float len = 0.2f + MeshKit.Hash(i, 2) * 0.07f;
                var tip = root + outward * 0.06f + Vector3.Down * len;
                k.Paint(i % 2 == 0 ? Look.Leaf : Look.LeafLight, 0.85f + MeshKit.Hash(i, 5) * 0.2f);
                k.Blade(root, tip, outward, s.LeafWidth, 0.015f);
            }
        }

        var wrap = new ArrayMesh();
        k.Commit(wrap, material);
        done = new Built
        {
            Style = s, Mesh = mesh, LeafWrap = wrap, Joints = j, Triangles = tris + k.Triangles, GripDrop = hh * 0.6f * 0.45f,
            HeadHeight = hh, HipY = hipY, WaistY = waistY, ShoulderY = shoulderY, NeckY = neckY, PelvisWidth = pelvisW,
        };
        Cache[s.Key] = done;
        return done;
    }

    // A ready figure: a skeleton with the mesh bound to it. Pose it through the
    // skeleton's bone poses (HumanRig indices).
    public static Skeleton3D Create(Style s)
    {
        var built = Build(s);
        var skeleton = HumanRig.Skeleton(built.Joints);
        skeleton.AddChild(new MeshInstance3D { Mesh = built.Mesh, Skeleton = new NodePath("..") });
        skeleton.AddChild(new MeshInstance3D { Name = PlayerModel.SlotNode(PlayerModel.Body), Mesh = built.LeafWrap, Skeleton = new NodePath("..") });
        return skeleton;
    }
}
