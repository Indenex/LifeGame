using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The Animals stations (D-098), drawn by StructureModel like any structure. One
// stage each; pegs mark the site until it is finished.
public static class StationModels
{
    static readonly Color Fleece = new(0.86f, 0.82f, 0.72f);
    static readonly Color StoneGrey = new(0.52f, 0.50f, 0.47f);
    static readonly Color Yarn = new(0.80f, 0.76f, 0.66f);

    // Spindle station: a distaff, a forked staff standing in a ring of stones with a
    // hank of fleece bound to its top, a log stool beside it, and a small weaving
    // frame of four lashed sticks leaning against it, half woven.
    public static List<StructureModel.Piece> Spindle()
    {
        var list = new List<StructureModel.Piece>();
        var wood = StructureModel.Wood;
        var pale = StructureModel.PaleWood;
        var lashing = StructureModel.Weave;
        var stone = Look.Solid(StoneGrey);
        var fleece = Look.Solid(Fleece);
        var yarn = Look.Solid(Yarn);

        Pegs(list, wood, 0.6f, 0.4f);
        // Base stones round the foot of the staff.
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5f;
            Add(list, stone, new SphereMesh { Radius = 0.09f, Height = 0.12f, RadialSegments = 7, Rings = 3 },
                new Vector3(-0.3f + Mathf.Cos(a) * 0.13f, 0.04f, Mathf.Sin(a) * 0.13f), new Vector3(0, a, 0));
        }
        // The staff, a fork at the top, and the fleece bound in it.
        Add(list, wood, new CylinderMesh { TopRadius = 0.018f, BottomRadius = 0.03f, Height = 1.3f, RadialSegments = 6 },
            new Vector3(-0.3f, 0.65f, 0), new Vector3(0, 0, 0.04f));
        foreach (float s in new[] { -1f, 1f })
            Add(list, wood, new CylinderMesh { TopRadius = 0.01f, BottomRadius = 0.016f, Height = 0.22f, RadialSegments = 5 },
                new Vector3(-0.33f + s * 0.04f, 1.36f, 0), new Vector3(0, 0, s * 0.35f));
        Add(list, fleece, new SphereMesh { Radius = 0.15f, Height = 0.42f, RadialSegments = 9, Rings = 5 },
            new Vector3(-0.34f, 1.32f, 0), new Vector3(0.1f, 0, 0.08f));
        Add(list, lashing, new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.04f, Height = 0.04f, RadialSegments = 6 },
            new Vector3(-0.33f, 1.2f, 0), Vector3.Zero);
        // A thread drawn down from the fleece to a drop spindle hanging at rest.
        Add(list, yarn, new CylinderMesh { TopRadius = 0.003f, BottomRadius = 0.003f, Height = 0.55f, RadialSegments = 3 },
            new Vector3(-0.2f, 0.98f, 0.04f), new Vector3(0, 0, -0.25f));
        Add(list, pale, new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.008f, Height = 0.26f, RadialSegments = 4 },
            new Vector3(-0.12f, 0.62f, 0.05f), Vector3.Zero);
        Add(list, stone, new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.04f, Height = 0.02f, RadialSegments = 10 },
            new Vector3(-0.12f, 0.53f, 0.05f), Vector3.Zero);
        Add(list, yarn, new SphereMesh { Radius = 0.025f, Height = 0.09f, RadialSegments = 6, Rings = 3 },
            new Vector3(-0.12f, 0.6f, 0.05f), Vector3.Zero);
        // The stool: a short log on end.
        Add(list, wood, new CylinderMesh { TopRadius = 0.16f, BottomRadius = 0.17f, Height = 0.38f, RadialSegments = 10 },
            new Vector3(0.25f, 0.19f, 0.22f), Vector3.Zero);
        Add(list, pale, new CylinderMesh { TopRadius = 0.15f, BottomRadius = 0.15f, Height = 0.01f, RadialSegments = 10 },
            new Vector3(0.25f, 0.385f, 0.22f), Vector3.Zero);
        // The weaving frame leaning on the far side, with some cloth already on it.
        var frame = new Vector3(0.3f, 0.42f, -0.28f);
        var lean = new Vector3(-0.3f, 0, 0);
        foreach (float x in new[] { -0.28f, 0.28f })
            Add(list, wood, new CylinderMesh { TopRadius = 0.014f, BottomRadius = 0.014f, Height = 0.86f, RadialSegments = 5 },
                frame + new Vector3(x, 0, 0), lean);
        foreach (float y in new[] { -0.36f, 0.36f })
            Add(list, wood, new CylinderMesh { TopRadius = 0.013f, BottomRadius = 0.013f, Height = 0.66f, RadialSegments = 5 },
                frame + new Vector3(0, y * Mathf.Cos(0.3f), y * Mathf.Sin(0.3f)), new Vector3(-0.3f, 0, Mathf.Pi / 2));
        Add(list, yarn, new BoxMesh { Size = new Vector3(0.52f, 0.34f, 0.012f) },
            frame + new Vector3(0, -0.15f * Mathf.Cos(0.3f), -0.15f * Mathf.Sin(0.3f)), lean);
        return list;
    }

    // Leather drying rack: two forked posts and a long crossbar, guyed with cord.
    // Hides hang on the bar (HideRacks draws them, up to StationHides); the bar
    // height matches the small A-frame racks so a hide looks the same on either.
    public static List<StructureModel.Piece> DryingRack()
    {
        var list = new List<StructureModel.Piece>();
        var wood = StructureModel.Wood;
        var lashing = StructureModel.Weave;
        const float half = 1.15f, bar = 0.99f;

        Pegs(list, wood, half, 0.35f);
        foreach (float x in new[] { -half, half })
        {
            Add(list, wood, new CylinderMesh { TopRadius = 0.03f, BottomRadius = 0.045f, Height = bar + 0.1f, RadialSegments = 6 },
                new Vector3(x, (bar + 0.1f) / 2, 0), Vector3.Zero);
            foreach (float s in new[] { -1f, 1f })
                Add(list, wood, new CylinderMesh { TopRadius = 0.014f, BottomRadius = 0.02f, Height = 0.16f, RadialSegments = 5 },
                    new Vector3(x, bar + 0.1f, s * 0.035f), new Vector3(s * 0.45f, 0, 0));
            // A guy stick braced against each post.
            Add(list, wood, new CylinderMesh { TopRadius = 0.014f, BottomRadius = 0.02f, Height = 1.05f, RadialSegments = 5 },
                new Vector3(x, 0.47f, -0.22f), new Vector3(-0.44f, 0, 0));
            Add(list, lashing, new CylinderMesh { TopRadius = 0.045f, BottomRadius = 0.045f, Height = 0.05f, RadialSegments = 6 },
                new Vector3(x, bar - 0.04f, 0), Vector3.Zero);
        }
        Add(list, wood, new CylinderMesh { TopRadius = 0.022f, BottomRadius = 0.022f, Height = half * 2 + 0.2f, RadialSegments = 6 },
            new Vector3(0, bar, 0), new Vector3(0, 0, Mathf.Pi / 2));
        return list;
    }

    // Site pegs at the corners, shown until the station is finished.
    static void Pegs(List<StructureModel.Piece> list, Material wood, float hw, float hd)
    {
        foreach (var (x, z) in new[] { (-hw, -hd), (hw, -hd), (hw, hd), (-hw, hd) })
            Add(list, wood, new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.025f, Height = 0.3f, RadialSegments = 5 },
                new Vector3(x, 0.1f, z), Vector3.Zero, stage: -1);
    }

    static void Add(List<StructureModel.Piece> list, Material material, PrimitiveMesh source, Vector3 at, Vector3 rotation, int stage = 0)
    {
        var st = new SurfaceTool();
        st.CreateFrom(source, 0);
        st.Deindex();
        st.GenerateNormals();
        var transform = new Transform3D(Basis.FromEuler(rotation), at);
        var arrays = st.CommitToArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = transform * verts[i];
            normals[i] = (transform.Basis * normals[i]).Normalized();
        }
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, material);
        var aabb = mesh.GetAabb();
        list.Add(new StructureModel.Piece
        {
            Stage = stage,
            Mesh = mesh,
            Material = material,
            Anchor = new Vector3(aabb.GetCenter().X, aabb.Position.Y, aabb.GetCenter().Z),
        });
    }
}
