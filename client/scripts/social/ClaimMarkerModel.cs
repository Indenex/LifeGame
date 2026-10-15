using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The claim marker as House building draws it while it is a blueprint: cairn
// stones first, then the post and its crossbar. Once finished, ClaimMarker takes
// over drawing it so it can lean and fade as it weathers.
public static class ClaimMarkerModel
{
    public static List<StructureModel.Piece> Pieces()
    {
        var list = new List<StructureModel.Piece>();
        var stone = Look.Solid(Look.Rock);
        var wood = Look.Solid(Look.Bark.Lightened(0.08f));
        var fiber = Look.Solid(Look.LeafWarm);

        // Three pegs mark the spot until it is built.
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.Tau / 3f;
            Add(list, -1, wood, new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.025f, Height = 0.3f, RadialSegments = 5 },
                new Vector3(Mathf.Cos(a) * 0.45f, 0.1f, Mathf.Sin(a) * 0.45f), Vector3.Zero);
        }
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.Tau / 5f + 0.4f;
            float s = 0.8f + Terrain.Hash01(i, 7, 41) * 0.5f;
            Add(list, 0, stone, new SphereMesh { Radius = 0.2f * s, Height = 0.21f * s, RadialSegments = 6, Rings = 3 },
                new Vector3(Mathf.Cos(a) * 0.24f, 0.05f, Mathf.Sin(a) * 0.24f), Vector3.Zero);
        }
        Add(list, 0, wood, new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.08f, Height = 1.9f, RadialSegments = 6 }, new Vector3(0, 0.95f, 0), Vector3.Zero);
        Add(list, 0, wood, new CylinderMesh { TopRadius = 0.035f, BottomRadius = 0.035f, Height = 0.7f, RadialSegments = 5 }, new Vector3(0.12f, 1.65f, 0), new Vector3(0, 0, Mathf.Pi / 2f));
        Add(list, 0, fiber, new CylinderMesh { TopRadius = 0.085f, BottomRadius = 0.085f, Height = 0.08f, RadialSegments = 7 }, new Vector3(0, 1.65f, 0), Vector3.Zero);
        return list;
    }

    static void Add(List<StructureModel.Piece> list, int stage, Material material, PrimitiveMesh source, Vector3 at, Vector3 rotation)
    {
        var st = new SurfaceTool();
        st.CreateFrom(source, 0);
        st.Deindex();
        st.GenerateNormals();
        var transform = new Transform3D(Basis.FromEuler(rotation), at);
        var mesh = new ArrayMesh();
        var arrays = st.CommitToArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = transform * verts[i];
            normals[i] = transform.Basis * normals[i];
        }
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
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
