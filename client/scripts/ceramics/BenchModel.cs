using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The potter's bench (ceramics-8, D-112/D-127): a split log laid flat-side up across
// two field stones, at kneeling height, with a dusting of dried clay on the work
// face. One stage; pegs mark the site until it is built. +Z is the potter's side.
public static class BenchModel
{
    public static List<StructureModel.Piece> Pieces()
    {
        var list = new List<StructureModel.Piece>();
        const float hw = 0.5f, top = 0.42f;
        StructureModel.Site(list, new[]
        {
            new Vector3(-hw - 0.1f, 0, -0.3f), new Vector3(hw + 0.1f, 0, -0.3f),
            new Vector3(hw + 0.1f, 0, 0.3f), new Vector3(-hw - 0.1f, 0, 0.3f),
        });
        // Two rough stones, a little uneven, as the bench's feet.
        foreach (var (x, tilt) in new[] { (-hw + 0.08f, 0.12f), (hw - 0.08f, -0.08f) })
            Add(list, StructureModel.Stone, new SphereMesh { Radius = 0.2f, Height = 0.4f, RadialSegments = 7, Rings = 4 },
                new Vector3(x, 0.14f, 0), new Vector3(tilt, x, 0), new Vector3(1.0f, 0.75f, 0.85f));
        // The split log, flat face up.
        Add(list, StructureModel.Wood, new CylinderMesh { TopRadius = 0.16f, BottomRadius = 0.16f, Height = hw * 2 + 0.3f, RadialSegments = 10 },
            new Vector3(0, top - 0.1f, 0), new Vector3(0, 0, Mathf.Pi / 2), new Vector3(1f, 1f, 1.5f), flatTop: top);
        Add(list, StructureModel.PaleWood, new BoxMesh { Size = new Vector3(hw * 2 + 0.3f, 0.015f, 0.42f) },
            new Vector3(0, top, 0), Vector3.Zero);
        // Pale dried clay worked into the middle of the top, where pots are shaped.
        Add(list, StructureModel.Clay, new CylinderMesh { TopRadius = 0.17f, BottomRadius = 0.19f, Height = 0.012f, RadialSegments = 12 },
            new Vector3(0.05f, top + 0.012f, 0.02f), Vector3.Zero);
        return list;
    }

    static void Add(List<StructureModel.Piece> list, Material material, PrimitiveMesh source, Vector3 at, Vector3 rotation,
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
            Stage = 0,
            Mesh = mesh,
            Material = material,
            Anchor = new Vector3(aabb.GetCenter().X, aabb.Position.Y, aabb.GetCenter().Z),
        });
    }
}
