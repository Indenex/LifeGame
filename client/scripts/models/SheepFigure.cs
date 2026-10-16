using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The sheep in each model-1 style. Bones, head and hip positions match Sheep.cs
// (body, fleece, head, four legs) so the chosen style drops straight in. Two
// surfaces: 0 the wool (white, tinted by the sheep's own material), 1 the skin,
// face and legs (colors in the vertices).
public static class SheepFigure
{
    public const int BodyBone = 0, FleeceBone = 1, HeadBone = 2, FirstLegBone = 3;
    public static readonly Vector3 HeadAt = new(0, 0.8f, 0.4f);
    public static readonly Vector3[] Hips = { new(-0.15f, 0.46f, 0.26f), new(0.15f, 0.46f, 0.26f), new(-0.15f, 0.46f, -0.26f), new(0.15f, 0.46f, -0.26f) };

    static readonly Color Face = new(0.17f, 0.14f, 0.12f);
    static readonly Color Skin = new(0.80f, 0.66f, 0.58f);
    static readonly Color Hoof = new(0.10f, 0.09f, 0.08f);
    static readonly Color Eye = new(0.05f, 0.04f, 0.035f);

    public sealed class Built
    {
        public ArrayMesh Mesh = null!;
        public int Triangles;
    }

    static readonly Dictionary<string, Built> Cache = new();
    static Material? plain;

    public static Built Build(HumanFigure.Style s, Material wool)
    {
        if (Cache.TryGetValue(s.Key, out var done)) return done;
        plain ??= Look.Solid(Colors.White);
        var mesh = new ArrayMesh();
        var center = new Vector3(0, 0.66f, 0);
        int tris = 0;

        // Wool.
        var k = new MeshKit(s.Flat);
        k.Paint(Colors.White);
        if (s == HumanFigure.Carved)
        {
            var core = new Vector3(0.3f, 0.27f, 0.46f);
            k.Bone(FleeceBone).Ellipsoid(center, core * 0.95f, 8, 5);
            // A dozen chunky lumps, like a fleece carved from wood.
            const int lumps = 14;
            for (int i = 0; i < lumps; i++)
            {
                float y = 1f - (i + 0.5f) / lumps * 2f;
                if (y < -0.5f) continue;
                float r = Mathf.Sqrt(1f - y * y), a = i * 2.39996f;
                var dir = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                k.Paint(Colors.White, 0.9f + MeshKit.Hash(i, 2) * 0.1f);
                k.Ellipsoid(center + dir * core * 0.78f, Vector3.One * (0.15f + MeshKit.Hash(i, 1) * 0.06f), 6, 4, MeshKit.Aim(dir));
            }
            k.Paint(Colors.White).Bone(HeadBone).Ellipsoid(HeadAt + new Vector3(0, 0.1f, 0.03f), new Vector3(0.12f, 0.08f, 0.11f), 6, 4);
        }
        else if (s == HumanFigure.Soft)
        {
            var core = new Vector3(0.31f, 0.28f, 0.44f);
            k.Bone(FleeceBone).Ellipsoid(center, core * 0.92f, 14, 8);
            const int puffs = 26;
            for (int i = 0; i < puffs; i++)
            {
                float y = 1f - (i + 0.5f) / puffs * 2f;
                if (y < -0.55f) continue;
                float r = Mathf.Sqrt(1f - y * y), a = i * 2.39996f;
                var dir = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                k.Paint(Colors.White, 0.92f + MeshKit.Hash(i, 2) * 0.08f);
                k.Ellipsoid(center + dir * core * 0.82f, Vector3.One * (0.12f + MeshKit.Hash(i, 1) * 0.07f), 10, 6);
            }
            k.Paint(Colors.White).Bone(HeadBone);
            k.Ellipsoid(HeadAt + new Vector3(0, 0.12f, 0.05f), new Vector3(0.11f, 0.08f, 0.1f), 10, 6);
        }
        else
        {
            // A longer, lower body under many small clumps of crimped wool.
            var core = new Vector3(0.28f, 0.26f, 0.5f);
            k.Bone(FleeceBone).Ellipsoid(center + new Vector3(0, -0.01f, -0.02f), core * 0.93f, 16, 9);
            const int clumps = 64;
            for (int i = 0; i < clumps; i++)
            {
                float y = 1f - (i + 0.5f) / clumps * 2f;
                if (y < -0.6f) continue;
                float r = Mathf.Sqrt(1f - y * y), a = i * 2.39996f;
                var dir = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                k.Paint(Colors.White, 0.86f + MeshKit.Hash(i, 2) * 0.14f);
                k.Ellipsoid(center + dir * core * 0.86f, new Vector3(1f, 0.8f, 1f) * (0.075f + MeshKit.Hash(i, 1) * 0.04f), 7, 4, MeshKit.Aim(dir));
            }
            k.Paint(Colors.White).Bone(HeadBone);
            k.Ellipsoid(HeadAt + new Vector3(0, 0.1f, 0.0f), new Vector3(0.1f, 0.07f, 0.1f), 9, 5);
        }
        tris += k.Triangles;
        k.Commit(mesh, wool);

        // Skin, face and legs.
        k = new MeshKit(s.Flat);
        k.Bone(BodyBone).Paint(Skin).Ellipsoid(center + new Vector3(0, -0.03f, 0), new Vector3(0.3f, 0.27f, 0.45f) * 0.78f, s.Flat ? 8 : 12, s.Flat ? 5 : 7);
        k.Bone(HeadBone).Paint(Face);
        bool natural = s == HumanFigure.Natural;
        int seg = s.Flat ? 6 : natural ? 12 : 10;
        int rings = s.Flat ? 4 : natural ? 7 : 6;
        if (natural)
        {
            // A longer face, tapering to the muzzle, with ears that droop.
            k.Loft(HeadAt + new Vector3(0, 0.07f, -0.02f), HeadAt + new Vector3(0, -0.07f, 0.3f), new MeshKit.Ring[]
            {
                new(0f, 0.085f, 0.09f), new(0.35f, 0.1f, 0.1f), new(0.8f, 0.065f, 0.075f), new(1f, 0.05f, 0.05f),
            }, seg, 0.6f);
            for (int side = -1; side <= 1; side += 2)
                k.Paint(Face, 1.1f).Ellipsoid(HeadAt + new Vector3(side * 0.13f, 0.03f, 0.05f), new Vector3(0.09f, 0.025f, 0.04f), 8, 4, new Basis(Vector3.Back, side * 0.45f));
        }
        else
        {
            k.Ellipsoid(HeadAt + new Vector3(0, 0.02f, 0.13f), new Vector3(0.105f, 0.115f, 0.16f), seg, rings);
            k.Paint(Face, 0.9f).Ellipsoid(HeadAt + new Vector3(0, -0.05f, 0.25f), new Vector3(0.075f, 0.075f, 0.085f), seg, rings);
            for (int side = -1; side <= 1; side += 2)
                k.Paint(Face, 1.1f).Ellipsoid(HeadAt + new Vector3(side * 0.14f, 0.05f, 0.08f), new Vector3(0.085f, 0.028f, 0.042f), seg, Mathf.Max(3, rings - 2));
        }
        // Eyes, set on the sides of the face like a grazing animal's.
        float eyeSize = s == HumanFigure.Soft ? 1.4f : 1f;
        for (int side = -1; side <= 1; side += 2)
            k.Paint(Eye).Ellipsoid(HeadAt + new Vector3(side * (natural ? 0.088f : 0.095f), 0.045f, natural ? 0.12f : 0.15f), new Vector3(0.012f, 0.018f, 0.02f) * eyeSize, s.Flat ? 5 : 8, s.Flat ? 3 : 4);

        for (int i = 0; i < 4; i++)
        {
            var hip = Hips[i];
            k.Bone(FirstLegBone + i).Paint(Face);
            if (natural)
            {
                k.Loft(hip, hip + new Vector3(0, -0.24f, i < 2 ? 0.01f : -0.02f), new MeshKit.Ring[] { new(0f, 0.05f, 0.055f), new(1f, 0.032f, 0.034f) }, 8);
                k.Loft(hip + new Vector3(0, -0.24f, i < 2 ? 0.01f : -0.02f), hip + new Vector3(0, -0.42f, 0.0f), new MeshKit.Ring[] { new(0f, 0.032f, 0.034f), new(1f, 0.027f, 0.029f) }, 8);
                k.Paint(Hoof).Loft(hip + new Vector3(0, -0.42f, 0.0f), hip + new Vector3(0, -0.46f, 0.01f), new MeshKit.Ring[] { new(0f, 0.03f, 0.033f), new(1f, 0.034f, 0.04f) }, 8, 0.1f);
            }
            else
            {
                k.Loft(hip, hip + new Vector3(0, -0.42f, 0.0f), new MeshKit.Ring[] { new(0f, 0.05f, 0.05f), new(1f, 0.038f, 0.04f) }, s.Flat ? 5 : 8);
                k.Paint(s.Flat ? Hoof : Face, s.Flat ? 1f : 0.7f).Loft(hip + new Vector3(0, -0.41f, 0.0f), hip + new Vector3(0, -0.46f, 0.012f), new MeshKit.Ring[] { new(0f, 0.042f, 0.045f), new(1f, 0.05f, 0.056f) }, s.Flat ? 5 : 8, 0.1f);
            }
        }
        tris += k.Triangles;
        k.Commit(mesh, plain);

        done = new Built { Mesh = mesh, Triangles = tris };
        Cache[s.Key] = done;
        return done;
    }

    public static Skeleton3D Create(HumanFigure.Style s, Material wool)
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
        skeleton.AddChild(new MeshInstance3D { Mesh = Build(s, wool).Mesh, Skeleton = new NodePath("..") });
        return skeleton;
    }
}
