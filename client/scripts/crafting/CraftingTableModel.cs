using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The crafting table (D-098): four forked stick legs lashed to cross rails, and a
// split log laid on top as the work surface. One stage; pegs mark the site until then.
public static class CraftingTableModel
{
    public static List<StructureModel.Piece> Pieces()
    {
        var list = new List<StructureModel.Piece>();
        var wood = StructureModel.Wood;
        var pale = StructureModel.PaleWood;
        var lashing = StructureModel.Weave;
        const float hw = 0.7f, hd = 0.35f, top = 0.78f;

        foreach (var (x, z) in new[] { (-hw, -hd), (hw, -hd), (hw, hd), (-hw, hd) })
            Add(list, -1, wood, new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.025f, Height = 0.3f, RadialSegments = 5 },
                new Vector3(x * 1.1f, 0.1f, z * 1.1f), Vector3.Zero);

        // Legs, splayed a little outward.
        foreach (var (x, z) in new[] { (-hw, -hd), (hw, -hd), (hw, hd), (-hw, hd) })
            Add(list, 0, wood, new CylinderMesh { TopRadius = 0.035f, BottomRadius = 0.045f, Height = top, RadialSegments = 6 },
                new Vector3(x, top / 2 - 0.02f, z), new Vector3(z * 0.12f, 0, -x * 0.05f));
        // Cross rails, lashed at each end.
        foreach (float z in new[] { -hd, hd })
        {
            Add(list, 0, wood, new CylinderMesh { TopRadius = 0.028f, BottomRadius = 0.028f, Height = hw * 2 + 0.12f, RadialSegments = 5 },
                new Vector3(0, top - 0.1f, z), new Vector3(0, 0, Mathf.Pi / 2));
            foreach (float x in new[] { -hw, hw })
                Add(list, 0, lashing, new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.05f, Height = 0.06f, RadialSegments = 6 },
                    new Vector3(x, top - 0.1f, z), Vector3.Zero);
        }
        // The split log on top, flat face up, with its pale cut face showing.
        Add(list, 0, wood, new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.2f, Height = hw * 2 + 0.3f, RadialSegments = 10 },
            new Vector3(0, top + 0.02f, 0), new Vector3(0, 0, Mathf.Pi / 2), scale: new Vector3(1f, 1f, 1.9f), flatTop: top + 0.06f);
        Add(list, 0, pale, new BoxMesh { Size = new Vector3(hw * 2 + 0.3f, 0.02f, 0.72f) },
            new Vector3(0, top + 0.06f, 0), Vector3.Zero);
        return list;
    }

    static void Add(List<StructureModel.Piece> list, int stage, Material material, PrimitiveMesh source, Vector3 at, Vector3 rotation,
        Vector3? scale = null, float? flatTop = null)
    {
        var st = new SurfaceTool();
        st.CreateFrom(source, 0);
        st.Deindex();
        st.GenerateNormals();
        var transform = new Transform3D(Basis.FromEuler(rotation).Scaled(scale ?? Vector3.One), at);
        var arrays = st.CommitToArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = transform * verts[i];
            // A split log: everything above the cut is sliced off flat.
            if (flatTop is { } y && verts[i].Y > y) verts[i].Y = y;
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
