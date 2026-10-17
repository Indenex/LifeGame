using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The sheep authored in Blender (art-rnd-1 showcase, docs/art-pipeline.md): a cloud of
// chunky wool puffs merged into one surface, a soft dark face with a pale muzzle and big
// eyes, little ears and short legs, built and baked by tools/art/sheep/ and painted by
// script. Showcase --view=art --piece=sheep puts it beside today's SheepFigure.
//
// Rig-ready: the .glb holds one mesh per part (Fleece, Tuft, Head, Body, Leg0..Leg3) in
// model space, at SheepFigure's sizes (HeadAt, Hips). Create() weights each part fully to
// SheepFigure's bones (fleece, head, body, leg0..3) on the same skeleton, so Sheep.cs's
// bone poses (walk, graze, fleece shrink when shorn) would drive it unchanged.
public static class ArtSheep
{
    public static float Spacing = 1.25f;
    public static float Height = 0.95f;
    const string Dir = "res://assets/art/sheep/";
    static ArrayMesh? mesh;
    public static int Triangles;

    public static Node3D Today()
    {
        // As Showcase's sheep view and Sheep.WoolLook.
        var wool = Look.Solid(new Color(0.74f, 0.71f, 0.63f));
        wool.SetShaderParameter("detail_scale", 1.5f);
        wool.SetShaderParameter("bump", 0.15f);
        wool.SetShaderParameter("cavity", 0.08f);
        return SheepFigure.Create(SheepFigure.Variant, wool);
    }

    public static Node3D Authored() => Create();

    // The same skeleton as SheepFigure.Create, with the authored mesh skinned to it.
    public static Skeleton3D Create()
    {
        var skeleton = new Skeleton3D();
        skeleton.AddBone("body");
        skeleton.AddBone("fleece");
        skeleton.SetBoneParent(SheepFigure.FleeceBone, SheepFigure.BodyBone);
        skeleton.AddBone("head");
        skeleton.SetBoneParent(SheepFigure.HeadBone, SheepFigure.BodyBone);
        skeleton.SetBoneRest(SheepFigure.HeadBone, new Transform3D(Basis.Identity, SheepFigure.HeadAt));
        for (int i = 0; i < 4; i++)
        {
            skeleton.AddBone($"leg{i}");
            skeleton.SetBoneParent(SheepFigure.FirstLegBone + i, SheepFigure.BodyBone);
            skeleton.SetBoneRest(SheepFigure.FirstLegBone + i, new Transform3D(Basis.Identity, SheepFigure.Hips[i]));
        }
        skeleton.ResetBonePoses();
        skeleton.AddChild(new MeshInstance3D { Mesh = Mesh, Skeleton = new NodePath("..") });
        return skeleton;
    }

    public static ArrayMesh Mesh => mesh ??= Build();

    static int BoneOf(string part) => part switch
    {
        "Fleece" => SheepFigure.FleeceBone,
        "Head" or "Tuft" => SheepFigure.HeadBone,
        "Body" => SheepFigure.BodyBone,
        _ => SheepFigure.FirstLegBone + (part[^1] - '0'),
    };

    // One surface per part, each weighted fully to its bone.
    static ArrayMesh Build()
    {
        var wool = PaintedAsset.Material(Dir + "wool.png");
        var skin = PaintedAsset.Material(Dir + "skin.png");
        var scene = GD.Load<PackedScene>(Dir + "sheep.glb").Instantiate();
        var parts = new List<MeshInstance3D>();
        Collect(scene, parts);
        var result = new ArrayMesh();
        foreach (var part in parts)
        {
            string name = part.Name.ToString();
            var arrays = part.Mesh.SurfaceGetArrays(0);
            int count = arrays[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array().Length;
            var bones = new int[count * 4];
            var weights = new float[count * 4];
            int bone = BoneOf(name);
            for (int v = 0; v < count; v++)
            {
                bones[v * 4] = bone;
                weights[v * 4] = 1f;
            }
            arrays[(int)Godot.Mesh.ArrayType.Bones] = bones;
            arrays[(int)Godot.Mesh.ArrayType.Weights] = weights;
            result.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
            result.SurfaceSetMaterial(result.GetSurfaceCount() - 1, name is "Fleece" or "Tuft" ? wool : skin);
            Triangles += arrays[(int)Godot.Mesh.ArrayType.Index].AsInt32Array().Length / 3;
        }
        scene.Free();
        return result;
    }

    static void Collect(Node node, List<MeshInstance3D> into)
    {
        if (node is MeshInstance3D m && m.Mesh != null) into.Add(m);
        foreach (var child in node.GetChildren()) Collect(child, into);
    }
}
