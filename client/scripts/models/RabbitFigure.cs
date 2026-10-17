using Godot;

namespace LifeGame;

// The wild rabbit (model-18, for Animals' animals-22): a soft round bun sitting
// low, about 0.2 m to the top of the head (0.3 m to the ear tips), long ears on top, big hind feet and a white
// scut, in the same soft painted look as the hen and the sheep. Bones match
// Sheep.cs (body, fleece, head, four legs) so the herd code drives it unchanged:
//   leg0 and leg1 are the front paws, leg2 and leg3 the hind legs (swing about X);
//   fleece holds the tail tuft and stays at scale 1; head pitches about X.
// Two surfaces like the sheep: 0 the fur (white, tinted by the rabbit's own
// material, so the fleece colours give brown, grey and sandy rabbits), 1 the
// eyes, nose, inner ears, belly and tail (colours in the vertices).
public static class RabbitFigure
{
    public const int BodyBone = 0, FleeceBone = 1, HeadBone = 2, FirstLegBone = 3;
    // Sitting low and hunched at rest (the PM's review of #727): the belly nearly on the
    // grass, short front paws, big round haunches with the hind feet flat along the
    // ground. Only Hop stretches it out.
    public static readonly Vector3 HeadAt = new(0, 0.15f, 0.095f);
    public static readonly Vector3[] Hips =
    {
        new(-0.03f, 0.03f, 0.09f), new(0.03f, 0.03f, 0.09f), new(-0.055f, 0.075f, -0.06f), new(0.055f, 0.075f, -0.06f),
    };
    static readonly Vector3 Center = new(0, 0.085f, -0.02f);

    static readonly Color Belly = new(0.93f, 0.89f, 0.82f);
    static readonly Color InnerEar = new(0.9f, 0.66f, 0.62f);
    static readonly Color Nose = new(0.62f, 0.38f, 0.36f);
    static readonly Color Eye = new(0.05f, 0.04f, 0.035f);
    static readonly Color Tail = new(0.97f, 0.96f, 0.93f);

    static ArrayMesh? mesh;
    static Material? plain;

    public static ArrayMesh Build(Material fur)
    {
        if (mesh != null) return mesh;
        plain ??= PaintedAnimals.Skin;
        mesh = new ArrayMesh();

        // Fur: a round body sitting low at the back, a chest, the head, two long ears
        // laid a little back, small front paws and big hind haunches and feet.
        var k = new MeshKit(false);
        k.Bone(BodyBone).Paint(Colors.White).Ellipsoid(Center, new Vector3(0.085f, 0.075f, 0.115f), 14, 9, new Basis(Vector3.Right, 0.15f));
        k.Paint(Colors.White, 1.02f).Ellipsoid(Center + new Vector3(0, -0.01f, 0.065f), new Vector3(0.068f, 0.072f, 0.062f), 12, 7);
        k.Bone(HeadBone).Paint(Colors.White).Ellipsoid(HeadAt, new Vector3(0.058f, 0.055f, 0.062f), 12, 8);
        // Cheeks, full and round, so the face reads soft from the front.
        for (int s = -1; s <= 1; s += 2)
            k.Paint(Colors.White, 1.03f).Ellipsoid(HeadAt + new Vector3(s * 0.028f, -0.018f, 0.03f), new Vector3(0.03f, 0.028f, 0.03f), 8, 5);
        for (int s = -1; s <= 1; s += 2)
        {
            var root = HeadAt + new Vector3(s * 0.022f, 0.045f, -0.01f);
            var tip = root + new Vector3(s * 0.018f, 0.11f, -0.035f);
            k.Paint(Colors.White, 0.97f).Loft(root, tip, new MeshKit.Ring[]
            {
                new(0f, 0.014f, 0.012f), new(0.45f, 0.024f, 0.012f), new(0.85f, 0.018f, 0.01f), new(1f, 0.004f, 0.004f),
            }, 8, 0.2f);
        }
        for (int side = 0; side < 2; side++)
        {
            var hip = Hips[side];
            k.Bone(FirstLegBone + side).Paint(Colors.White, 0.98f);
            // Only the paw tips peek out from under the chest.
            k.Loft(hip, new Vector3(hip.X, 0.01f, hip.Z + 0.02f), new MeshKit.Ring[] { new(0f, 0.018f, 0.018f), new(1f, 0.014f, 0.014f) }, 8, 0.3f);
            k.Ellipsoid(new Vector3(hip.X, 0.009f, hip.Z + 0.03f), new Vector3(0.017f, 0.01f, 0.024f), 8, 4);
        }
        for (int side = 2; side < 4; side++)
        {
            var hip = Hips[side];
            float s = side == 2 ? -1f : 1f;
            k.Bone(FirstLegBone + side).Paint(Colors.White, 0.97f);
            // The big round haunch folded against the body, its bottom on the grass, and the
            // long hind foot tucked forward flat along the ground beneath it.
            k.Ellipsoid(hip + new Vector3(s * 0.012f, -0.015f, 0.005f), new Vector3(0.042f, 0.056f, 0.068f), 10, 6, new Basis(Vector3.Right, 0.3f));
            k.Paint(Colors.White, 0.95f).Ellipsoid(new Vector3(hip.X + s * 0.01f, 0.011f, hip.Z + 0.045f), new Vector3(0.02f, 0.011f, 0.06f), 8, 4);
        }
        k.Commit(mesh, fur);

        // The pale belly and chin, the scut, the eyes, nose and the pink inside the ears.
        k = new MeshKit(false);
        k.Bone(BodyBone).Paint(Belly).Ellipsoid(Center + new Vector3(0, -0.035f, 0.075f), new Vector3(0.05f, 0.045f, 0.05f), 10, 5);
        k.Bone(FleeceBone).Paint(Tail).Ellipsoid(Center + new Vector3(0, 0.025f, -0.115f), new Vector3(0.03f, 0.03f, 0.026f), 9, 6);
        k.Bone(HeadBone);
        k.Paint(Belly).Ellipsoid(HeadAt + new Vector3(0, -0.035f, 0.035f), new Vector3(0.03f, 0.018f, 0.025f), 8, 4);
        k.Paint(Nose).Ellipsoid(HeadAt + new Vector3(0, -0.005f, 0.06f), new Vector3(0.009f, 0.007f, 0.006f), 6, 3);
        for (int s = -1; s <= 1; s += 2)
        {
            k.Paint(Eye).Ellipsoid(HeadAt + new Vector3(s * 0.04f, 0.012f, 0.034f), new Vector3(0.008f, 0.011f, 0.01f), 6, 3);
            var root = HeadAt + new Vector3(s * 0.022f, 0.05f, -0.004f);
            var tip = root + new Vector3(s * 0.016f, 0.095f, -0.03f);
            k.Paint(InnerEar).Loft(root + new Vector3(0, 0, 0.008f), tip + new Vector3(0, 0, 0.007f), new MeshKit.Ring[]
            {
                new(0f, 0.007f, 0.004f), new(0.45f, 0.014f, 0.005f), new(1f, 0.003f, 0.002f),
            }, 6, 0.2f);
        }
        k.Commit(mesh, plain);
        return mesh;
    }

    public static Skeleton3D Create(Material fur)
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
        var body = new MeshInstance3D { Mesh = Build(fur), Skeleton = new NodePath("..") };
        // The mesh is shared; each rabbit's fur colour rides on its own instance.
        body.SetSurfaceOverrideMaterial(0, fur);
        skeleton.AddChild(body);
        return skeleton;
    }

    // A rabbit hops even when it is only pottering about: one hop per gait cycle, the
    // body lifting nose-up off the hind feet, which kick back, the front paws reaching
    // forward to land nose-down, then all four gathered under again. Sheep.cs can use this in
    // place of the walk for Model == "rabbit"; amount 0 is standing, 1 a full bound.
    // Leg angles are about X as Sheep.cs sets them (positive swings a leg back, toward
    // -Z; the rabbit faces +Z); pitch positive tips the nose down; lift in metres at scale 1.
    public static (float Lift, float Pitch, float Front, float Hind) Hop(float gait, float amount)
    {
        float phase = Mathf.PosMod(gait / Mathf.Tau, 1f);
        // Airborne for the first 60% of the cycle, gathered on the ground for the rest.
        float air = phase < 0.6f ? Mathf.Sin(Mathf.Pi * phase / 0.6f) : 0f;
        float lift = air * 0.08f * amount;
        // Nose up as it pushes off, nose down as it lands.
        float pitch = -Mathf.Sin(Mathf.Tau * phase / 0.6f) * 0.2f * amount * (phase < 0.6f ? 1f : 0f);
        float front = (-0.9f * air + 0.2f * (1f - air)) * amount;
        float hind = (1.0f * air - 0.15f * (1f - air)) * amount;
        return (lift, pitch, front, hind);
    }
}
