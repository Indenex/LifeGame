using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The red deer for Animals (animals-14), in the carved style (model-2): a slim
// barrel and deep chest, a long neck carrying the head high, long legs, big ears,
// and branching antlers on stags. Same bones and layout as Animals' stand-in in
// Sheep.cs (body, fleece = coat, head, four legs; head rest and hips below), so it
// animates the same. Surface 0 is the coat (white; the deer's own coat material
// colors it), surface 1 the pale rump, belly and throat, dark nose, eyes and
// hooves, and antlers (colors in the vertices).
public static class DeerFigure
{
    public const int BodyBone = 0, CoatBone = 1, HeadBone = 2, FirstLegBone = 3;
    public static readonly Vector3 HeadAt = new(0, 1.32f, 0.62f);
    public static readonly Vector3[] Hips = { new(-0.13f, 0.82f, 0.38f), new(0.13f, 0.82f, 0.38f), new(-0.13f, 0.82f, -0.38f), new(0.13f, 0.82f, -0.38f) };

    static readonly Color Pale = new(0.78f, 0.68f, 0.54f);
    static readonly Color Dark = new(0.10f, 0.08f, 0.07f);
    static readonly Color Antler = new(0.66f, 0.58f, 0.46f);
    static readonly Color AntlerTip = new(0.86f, 0.80f, 0.70f);

    static readonly Dictionary<bool, ArrayMesh> Cache = new();
    static Material? plain;

    public static ArrayMesh Build(bool stag, Material coat)
    {
        if (Cache.TryGetValue(stag, out var done)) return done;
        plain ??= Look.Solid(Colors.White);
        var mesh = new ArrayMesh();

        // Coat.
        var k = new MeshKit(true);
        k.Bone(CoatBone).Paint(Colors.White);
        // Body: rounded rump, slim barrel, deep chest.
        k.Loft(new Vector3(0, 1.02f, -0.56f), new Vector3(0, 1.04f, 0.46f), new MeshKit.Ring[]
        {
            new(0f, 0.12f, 0.15f), new(0.12f, 0.19f, 0.22f), new(0.45f, 0.18f, 0.22f, -0.01f), new(0.78f, 0.19f, 0.27f, -0.05f), new(1f, 0.12f, 0.17f, -0.02f),
        }, 8, 0.4f);
        // Neck, rising forward to the head.
        k.Loft(new Vector3(0, 1.1f, 0.36f), HeadAt + new Vector3(0, -0.03f, -0.05f), new MeshKit.Ring[]
        {
            new(0f, 0.1f, 0.13f), new(0.5f, 0.075f, 0.1f), new(1f, 0.062f, 0.08f),
        }, 7, 0.3f);
        k.Loft(new Vector3(0, 1.08f, -0.55f), new Vector3(0, 1.0f, -0.66f), new MeshKit.Ring[] { new(0f, 0.04f, 0.035f), new(1f, 0.02f, 0.02f) }, 5, 0.5f);
        // Head: a long skull tapering to the muzzle, big ears set back.
        k.Bone(HeadBone);
        k.Loft(HeadAt + new Vector3(0, 0.04f, -0.07f), HeadAt + new Vector3(0, -0.06f, 0.29f), new MeshKit.Ring[]
        {
            new(0f, 0.06f, 0.07f), new(0.25f, 0.075f, 0.09f), new(0.7f, 0.055f, 0.065f), new(1f, 0.04f, 0.045f),
        }, 7, 0.4f);
        for (int side = -1; side <= 1; side += 2)
            k.Ellipsoid(HeadAt + new Vector3(side * 0.1f, 0.12f, -0.03f), new Vector3(0.035f, 0.085f, 0.018f), 6, 3,
                new Basis(Vector3.Up, side * 0.5f) * new Basis(Vector3.Back, -side * 0.6f));
        // Legs: muscled above the knee, slender below.
        for (int i = 0; i < 4; i++)
        {
            var hip = Hips[i];
            bool hind = hip.Z < 0f;
            var knee = new Vector3(hip.X, 0.45f, hip.Z + (hind ? -0.05f : 0.02f));
            k.Bone(FirstLegBone + i);
            k.Loft(hip + Vector3.Up * 0.08f, knee, new MeshKit.Ring[] { new(0f, 0.07f, hind ? 0.1f : 0.08f), new(1f, 0.035f, 0.042f) }, 6, 0.3f);
            k.Loft(knee, new Vector3(hip.X, 0.06f, hip.Z + 0.01f), new MeshKit.Ring[] { new(0f, 0.034f, 0.04f), new(1f, 0.024f, 0.027f) }, 5, 0.2f);
        }
        k.Commit(mesh, coat);

        // Pale patches, dark points, antlers.
        k = new MeshKit(true);
        k.Bone(CoatBone).Paint(Pale);
        k.Ellipsoid(new Vector3(0, 1.03f, -0.57f), new Vector3(0.12f, 0.14f, 0.035f), 7, 4);
        k.Ellipsoid(new Vector3(0, 0.84f, -0.04f), new Vector3(0.12f, 0.05f, 0.34f), 8, 3);
        k.Ellipsoid(new Vector3(0, 1.15f, 0.46f), new Vector3(0.055f, 0.09f, 0.045f), 6, 3, new Basis(Vector3.Right, -0.75f));
        k.Bone(HeadBone).Paint(Dark);
        k.Ellipsoid(HeadAt + new Vector3(0, -0.055f, 0.3f), new Vector3(0.035f, 0.03f, 0.02f), 6, 3);
        for (int side = -1; side <= 1; side += 2)
            k.Ellipsoid(HeadAt + new Vector3(side * 0.068f, 0.035f, 0.05f), new Vector3(0.012f, 0.016f, 0.018f), 5, 3);
        for (int i = 0; i < 4; i++)
        {
            var hip = Hips[i];
            k.Bone(FirstLegBone + i).Paint(Dark).Loft(new Vector3(hip.X, 0.07f, hip.Z + 0.01f), new Vector3(hip.X, 0f, hip.Z + 0.025f), new MeshKit.Ring[]
            {
                new(0f, 0.025f, 0.028f), new(1f, 0.03f, 0.038f),
            }, 5, 0.1f);
        }
        if (stag)
        {
            k.Bone(HeadBone);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 P(float x, float y, float z) => HeadAt + new Vector3(side * x, y, z);
                // The beam sweeps up and back and out, then forward at the crown.
                var beam = new[] { P(0.05f, 0.1f, -0.03f), P(0.12f, 0.22f, -0.08f), P(0.2f, 0.38f, -0.12f), P(0.25f, 0.55f, -0.08f), P(0.26f, 0.68f, 0.0f) };
                k.Paint(Antler).Tube(beam, 0.022f, 5, 0.011f);
                // Brow tine forward over the face, a bez tine above it, and a crown.
                k.Tube(new[] { P(0.1f, 0.19f, -0.07f), P(0.14f, 0.24f, 0.05f), P(0.15f, 0.25f, 0.15f) }, 0.014f, 4, 0.005f);
                k.Tube(new[] { P(0.17f, 0.32f, -0.11f), P(0.21f, 0.37f, 0.0f), P(0.22f, 0.39f, 0.08f) }, 0.012f, 4, 0.005f);
                k.Paint(AntlerTip).Tube(new[] { P(0.25f, 0.55f, -0.08f), P(0.32f, 0.66f, -0.14f) }, 0.011f, 4, 0.004f);
                k.Tube(new[] { P(0.26f, 0.68f, 0.0f), P(0.24f, 0.76f, 0.04f) }, 0.01f, 4, 0.004f);
                k.Tube(new[] { P(0.26f, 0.68f, 0.0f), P(0.33f, 0.74f, -0.02f) }, 0.01f, 4, 0.004f);
            }
        }
        k.Commit(mesh, plain);
        Cache[stag] = mesh;
        return mesh;
    }

    public static Skeleton3D Create(bool stag, Material coat)
    {
        var skeleton = new Skeleton3D();
        skeleton.AddBone("body");
        skeleton.AddBone("fleece");
        skeleton.SetBoneParent(CoatBone, BodyBone);
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
        skeleton.AddChild(new MeshInstance3D { Mesh = Build(stag, coat), Skeleton = new NodePath("..") });
        if (stag) skeleton.Scale = Vector3.One * 1.1f;
        return skeleton;
    }
}
