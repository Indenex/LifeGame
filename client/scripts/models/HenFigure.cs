using Godot;

namespace LifeGame;

// The hen (model-7, for Animals' animals-19): a plump little bird about 0.42 m
// tall, soft and round like the sheep. Bones match Sheep.cs (body, fleece, head,
// four "legs") so the herd code drives it unchanged:
//   leg0 and leg1 are the hen's two legs (swing about X to walk);
//   leg2 and leg3 are its left and right wings (swing about Z, outward, to flap;
//     the walk's small swing about X reads as a waddle of the wings);
//   head pitches down about X to peck; fleece holds the tail and stays at scale 1.
// Two surfaces like the sheep: 0 the feathers (white, tinted by the hen's own
// material, so the sheep's fleece colours give white, grey and brown hens), 1 the
// comb, wattle, beak, eyes and legs (colours in the vertices).
public static class HenFigure
{
    public const int BodyBone = 0, FleeceBone = 1, HeadBone = 2, FirstLegBone = 3;
    public static readonly Vector3 HeadAt = new(0, 0.33f, 0.1f);
    public static readonly Vector3[] Hips =
    {
        new(-0.045f, 0.15f, 0.0f), new(0.045f, 0.15f, 0.0f), new(-0.1f, 0.25f, 0.04f), new(0.1f, 0.25f, 0.04f),
    };
    static readonly Vector3 Center = new(0, 0.23f, -0.02f);

    static readonly Color Comb = new(0.72f, 0.2f, 0.16f);
    static readonly Color Beak = new(0.86f, 0.66f, 0.3f);
    static readonly Color Shank = new(0.84f, 0.62f, 0.3f);
    static readonly Color Eye = new(0.05f, 0.04f, 0.035f);

    static ArrayMesh? mesh;
    static Material? plain;

    static float H(int a, int b) => MeshKit.Hash(a, b + 240);

    public static ArrayMesh Build(Material feathers)
    {
        if (mesh != null) return mesh;
        plain ??= Look.Solid(Colors.White);
        mesh = new ArrayMesh();

        // Feathers: a round body leaning back, a breast, a neck up to the head, the
        // tail fanned up behind, and folded wings with a few long primaries.
        var k = new MeshKit(false);
        k.Bone(BodyBone).Paint(Colors.White).Ellipsoid(Center, new Vector3(0.12f, 0.11f, 0.15f), 14, 9, new Basis(Vector3.Right, 0.15f));
        k.Paint(Colors.White, 1.02f).Ellipsoid(Center + new Vector3(0, 0.0f, 0.09f), new Vector3(0.1f, 0.1f, 0.08f), 12, 7);
        k.Paint(Colors.White).Loft(Center + new Vector3(0, 0.04f, 0.08f), HeadAt + new Vector3(0, -0.01f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.075f, 0.07f), new(0.6f, 0.05f, 0.05f), new(1f, 0.045f, 0.045f),
        }, 10, 0.3f);
        k.Bone(FleeceBone);
        // A short tuft cocked up behind, its feathers close together.
        k.Paint(Colors.White, 0.95f).Ellipsoid(Center + new Vector3(0, 0.07f, -0.13f), new Vector3(0.05f, 0.08f, 0.045f), 9, 6, new Basis(Vector3.Right, -0.5f));
        for (int i = 0; i < 4; i++)
        {
            float a = (i - 1.5f) * 0.12f;
            var root = Center + new Vector3(a * 0.12f, 0.06f, -0.13f);
            var tip = root + new Vector3(a * 0.1f, 0.1f + H(i, 1) * 0.02f, -0.06f - H(i, 2) * 0.015f);
            k.Paint(Colors.White, 0.86f + H(i, 3) * 0.08f).Blade(root, tip, new Vector3(0, 0.4f, -1f), 0.045f, 0.015f);
        }
        k.Bone(HeadBone).Paint(Colors.White).Ellipsoid(HeadAt + new Vector3(0, 0.03f, 0.01f), new Vector3(0.055f, 0.06f, 0.06f), 10, 7);
        for (int side = 0; side < 2; side++)
        {
            float s = side == 0 ? -1f : 1f;
            var shoulder = Hips[2 + side];
            k.Bone(FirstLegBone + 2 + side).Paint(Colors.White, 0.9f);
            k.Ellipsoid(shoulder + new Vector3(s * 0.025f, -0.03f, -0.07f), new Vector3(0.035f, 0.075f, 0.11f), 10, 6, new Basis(Vector3.Right, 0.35f));
            for (int i = 0; i < 3; i++)
            {
                var root = shoulder + new Vector3(s * 0.03f, -0.05f - i * 0.012f, -0.1f);
                k.Paint(Colors.White, 0.82f).Blade(root, root + new Vector3(s * 0.01f, -0.02f + i * 0.01f, -0.09f + i * 0.015f), new Vector3(s, 0, 0), 0.04f, 0.004f);
            }
        }
        k.Commit(mesh, feathers);

        // Comb, wattle, beak, eyes and legs.
        k = new MeshKit(false);
        k.Bone(HeadBone);
        for (int i = 0; i < 3; i++)
            k.Paint(Comb, 1f - i * 0.04f).Ellipsoid(HeadAt + new Vector3(0, 0.09f - i * 0.004f, 0.035f - i * 0.025f), new Vector3(0.011f, 0.026f - i * 0.003f, 0.016f), 7, 4);
        k.Paint(Comb, 0.95f).Ellipsoid(HeadAt + new Vector3(0, -0.012f, 0.055f), new Vector3(0.012f, 0.022f, 0.012f), 6, 4);
        k.Paint(Beak).Loft(HeadAt + new Vector3(0, 0.02f, 0.05f), HeadAt + new Vector3(0, 0.012f, 0.1f), new MeshKit.Ring[]
        {
            new(0f, 0.022f, 0.018f), new(1f, 0.002f, 0.002f),
        }, 6, 0.1f);
        for (int s = -1; s <= 1; s += 2)
            k.Paint(Eye).Ellipsoid(HeadAt + new Vector3(s * 0.045f, 0.04f, 0.035f), new Vector3(0.008f, 0.011f, 0.011f), 6, 3);
        for (int side = 0; side < 2; side++)
        {
            var hip = Hips[side];
            var foot = new Vector3(hip.X, 0.012f, hip.Z + 0.01f);
            k.Bone(FirstLegBone + side).Paint(Shank).Tube(new[] { hip + Vector3.Down * 0.02f, foot }, 0.011f, 5, 0.009f);
            // Three toes forward and one back.
            foreach (var (dx, dz) in new[] { (-0.025f, 0.045f), (0f, 0.055f), (0.025f, 0.045f), (0f, -0.03f) })
                k.Paint(Shank, 0.92f).Tube(new[] { foot, new Vector3(foot.X + dx, 0.006f, foot.Z + dz) }, 0.006f, 4, 0.003f);
        }
        k.Commit(mesh, plain);
        return mesh;
    }

    public static Skeleton3D Create(Material feathers)
    {
        var skeleton = new Skeleton3D();
        skeleton.AddBone("body");
        skeleton.AddBone("fleece");
        skeleton.SetBoneParent(FleeceBone, BodyBone);
        skeleton.AddBone("head");
        skeleton.SetBoneParent(HeadBone, BodyBone);
        skeleton.SetBoneRest(HeadBone, new Transform3D(Basis.Identity, HeadAt));
        for (int i = 0; i < 4; i++)
        {
            skeleton.AddBone($"leg{i}");
            skeleton.SetBoneParent(FirstLegBone + i, BodyBone);
            skeleton.SetBoneRest(FirstLegBone + i, new Transform3D(Basis.Identity, Hips[i]));
        }
        skeleton.ResetBonePoses();
        var body = new MeshInstance3D { Mesh = Build(feathers), Skeleton = new NodePath("..") };
        // The mesh is shared; each hen's feather colour rides on its own instance.
        body.SetSurfaceOverrideMaterial(0, feathers);
        skeleton.AddChild(body);
        return skeleton;
    }
}
