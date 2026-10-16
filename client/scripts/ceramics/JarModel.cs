using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The storage jar as a building (ceramics-6): pegs round the spot, then the
// kiln-fired jar itself once someone sets it down. Same mesh as the jar item,
// at PotView's scale, so a jar carried and a jar set down look alike.
public static class JarModel
{
    public static List<StructureModel.Piece> Pieces()
    {
        var list = new List<StructureModel.Piece>();
        var ring = new Vector3[8];
        for (int i = 0; i < ring.Length; i++)
        {
            float a = i * Mathf.Tau / ring.Length;
            ring[i] = new Vector3(Mathf.Sin(a) * 0.4f, 0, Mathf.Cos(a) * 0.4f);
        }
        StructureModel.Site(list, ring);
        list.Add(new StructureModel.Piece
        {
            Stage = 0,
            Mesh = Scaled(PotMesh.For(ClayForm.StorageJar, 3), PotView.Size),
            Material = ClayLook.For(ClayStage.Fired, FireKind.Kiln, 0f, 3),
            Anchor = Vector3.Zero,
        });
        return list;
    }

    static ArrayMesh Scaled(ArrayMesh source, float scale)
    {
        var arrays = source.SurfaceGetArrays(0);
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        for (int i = 0; i < verts.Length; i++) verts[i] *= scale;
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
