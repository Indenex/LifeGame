using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The sheep (model-2): short-legged and soft, after Justin's notes on the first
// pass ("shorter, legs too long ... softer, cartoonier, fluffier"). Three fleece
// variants for him to pick from; the carved face and dark legs tie it to the
// people (style A). Bones match Sheep.cs (body, fleece, head, four legs), which
// takes HeadAt and Hips from here. Two surfaces: 0 the wool (white, tinted by the
// sheep's own material), 1 the skin, face and legs (colors in the vertices).
public static class SheepFigure
{
    public enum Fleece { Cloud, Bean, Carved }

    // The fleece the game uses until Justin picks (Cloud recommended).
    public static Fleece Variant = Fleece.Cloud;

    public const int BodyBone = 0, FleeceBone = 1, HeadBone = 2, FirstLegBone = 3;
    // Legs about 0.3 m: the fleece hangs near the grass.
    public static readonly Vector3 HeadAt = new(0, 0.64f, 0.42f);
    public static readonly Vector3[] Hips = { new(-0.15f, 0.3f, 0.25f), new(0.15f, 0.3f, 0.25f), new(-0.15f, 0.3f, -0.25f), new(0.15f, 0.3f, -0.25f) };
    static readonly Vector3 Center = new(0, 0.49f, 0);

    static readonly Color Face = new(0.17f, 0.14f, 0.12f);
    static readonly Color Skin = new(0.80f, 0.66f, 0.58f);
    static readonly Color Hoof = new(0.10f, 0.09f, 0.08f);
    static readonly Color Eye = new(0.05f, 0.04f, 0.035f);

    public sealed class Built
    {
        public ArrayMesh Mesh = null!;
        public int Triangles;
    }

    static readonly Dictionary<Fleece, Built> Cache = new();
    static Material? plain;

    static float H(int a, int b) => MeshKit.Hash(a, b + 160);

    public static Built Build(Fleece fleece, Material wool)
    {
        if (Cache.TryGetValue(fleece, out var done)) return done;
        plain ??= PaintedAnimals.Skin;
        var mesh = new ArrayMesh();
        int tris = 0;

        // Wool.
        var k = new MeshKit(fleece == Fleece.Carved);
        k.Bone(FleeceBone);
        var core = new Vector3(0.3f, 0.25f, 0.43f);
        switch (fleece)
        {
            case Fleece.Cloud:
                // A few big soft lumps run together into one body.
                k.Paint(Colors.White).Ellipsoid(Center, core * 0.95f, 14, 8);
                var lumps = new (Vector3 At, float R)[]
                {
                    (new Vector3(0, 0.1f, 0.18f), 0.22f), (new Vector3(0, 0.12f, -0.14f), 0.23f),
                    (new Vector3(0.17f, -0.01f, 0.03f), 0.2f), (new Vector3(-0.17f, -0.01f, 0.03f), 0.2f),
                    (new Vector3(0, 0.0f, -0.32f), 0.19f), (new Vector3(0, -0.03f, 0.33f), 0.18f),
                };
                for (int i = 0; i < lumps.Length; i++)
                    k.Paint(Colors.White, 0.95f + H(i, 1) * 0.05f).Ellipsoid(Center + lumps[i].At, Vector3.One * lumps[i].R, 12, 7);
                k.Bone(HeadBone).Paint(Colors.White).Ellipsoid(HeadAt + new Vector3(0, 0.1f, 0.02f), new Vector3(0.11f, 0.075f, 0.1f), 10, 6);
                break;
            case Fleece.Bean:
                // One smooth bean of wool and a small tuft on the head.
                k.Paint(Colors.White).Ellipsoid(Center + new Vector3(0, 0.02f, 0), new Vector3(0.32f, 0.27f, 0.47f), 16, 10, new Basis(Vector3.Right, -0.06f));
                k.Paint(Colors.White, 0.96f).Ellipsoid(Center + new Vector3(0, 0.05f, -0.45f), new Vector3(0.08f, 0.08f, 0.06f), 8, 5);
                k.Bone(HeadBone).Paint(Colors.White).Ellipsoid(HeadAt + new Vector3(0, 0.1f, 0.02f), new Vector3(0.09f, 0.06f, 0.08f), 8, 5);
                break;
            default:
                // Big flat-shaded lumps, faceted like the people and the rocks.
                k.Paint(Colors.White).Ellipsoid(Center, core * 0.95f, 8, 5);
                const int count = 8;
                for (int i = 0; i < count; i++)
                {
                    float y = 0.8f - (i + 0.5f) / count * 1.4f;
                    float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y)), a = i * 2.39996f;
                    var dir = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                    k.Paint(Colors.White, 0.92f + H(i, 2) * 0.08f).Ellipsoid(Center + dir * core * 0.7f, Vector3.One * (0.19f + H(i, 3) * 0.04f), 7, 4, MeshKit.Aim(dir));
                }
                k.Bone(HeadBone).Paint(Colors.White).Ellipsoid(HeadAt + new Vector3(0, 0.1f, 0.02f), new Vector3(0.11f, 0.075f, 0.1f), 6, 4);
                break;
        }
        tris += k.Triangles;
        k.Commit(mesh, wool);

        // Skin, face and legs: faceted, like the people.
        k = new MeshKit(true);
        k.Bone(BodyBone).Paint(Skin).Ellipsoid(Center + new Vector3(0, -0.02f, 0), core * 0.8f, 8, 5);
        k.Bone(HeadBone).Paint(Face);
        k.Ellipsoid(HeadAt + new Vector3(0, 0.02f, 0.12f), new Vector3(0.11f, 0.12f, 0.16f), 7, 5);
        k.Paint(Face, 0.9f).Ellipsoid(HeadAt + new Vector3(0, -0.05f, 0.24f), new Vector3(0.08f, 0.075f, 0.085f), 6, 4);
        for (int side = -1; side <= 1; side += 2)
        {
            // Ears out to the side and a little down; round, dark eyes.
            k.Paint(Face, 1.1f).Ellipsoid(HeadAt + new Vector3(side * 0.15f, 0.05f, 0.07f), new Vector3(0.09f, 0.03f, 0.045f), 6, 3, new Basis(Vector3.Back, side * 0.25f));
            k.Paint(Eye).Ellipsoid(HeadAt + new Vector3(side * 0.095f, 0.05f, 0.15f), new Vector3(0.016f, 0.022f, 0.022f), 6, 3);
        }
        for (int i = 0; i < 4; i++)
        {
            var hip = Hips[i];
            k.Bone(FirstLegBone + i).Paint(Face);
            k.Loft(hip + Vector3.Up * 0.04f, new Vector3(hip.X, 0.05f, hip.Z), new MeshKit.Ring[] { new(0f, 0.058f, 0.058f), new(1f, 0.045f, 0.047f) }, 6);
            k.Paint(Hoof).Loft(new Vector3(hip.X, 0.055f, hip.Z), new Vector3(hip.X, 0f, hip.Z + 0.01f), new MeshKit.Ring[] { new(0f, 0.047f, 0.05f), new(1f, 0.055f, 0.062f) }, 6, 0.1f);
        }
        tris += k.Triangles;
        k.Commit(mesh, plain);

        done = new Built { Mesh = mesh, Triangles = tris };
        Cache[fleece] = done;
        return done;
    }

    public static Skeleton3D Create(Fleece fleece, Material wool)
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
        skeleton.AddChild(new MeshInstance3D { Mesh = Build(fleece, wool).Mesh, Skeleton = new NodePath("..") });
        return skeleton;
    }
}
