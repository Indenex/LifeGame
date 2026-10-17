using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The stone-ringed hearth (D-286; model by Modeling, model-9): an outdoor fire ring, bigger and
// more solid than the campfire, about 0.5 m round. Stage 0 lays a ring of big fieldstones, each
// turned along the circle, round a bed of ash and earth; stage 1 builds the kerb up, a second
// course of stones set over the joints of the first, with clay chinking pressed into the gaps.
// The fire itself is drawn by the fire code in the middle.
public static partial class StructureModel
{
    internal static List<Piece> StoneHearth()
    {
        var list = new List<Piece>();
        const float r = 0.5f;
        var corners = new Vector3[10];
        for (int i = 0; i < corners.Length; i++)
            corners[i] = Around(r + 0.08f, i * Mathf.Tau / corners.Length, 0);
        Site(list, corners);

        // Stage 0: the bed (ash in the middle, trodden earth round it) and the first course.
        Add(list, 0, Ash, Bed(r - 0.05f, 0.04f, 0.015f, 31));
        const int stones = 11;
        for (int i = 0; i < stones; i++)
        {
            float a = (i + (Hash(i, 71) - 0.5f) * 0.18f) * Mathf.Tau / stones;
            var size = new Vector3(0.15f + Hash(i, 72) * 0.04f, 0.1f + Hash(i, 73) * 0.035f, 0.11f + Hash(i, 74) * 0.03f);
            Add(list, 0, Stone, TurnedRock(Around(r - 0.06f, a, size.Y * 0.8f), size, a, 0.08f * (Hash(i, 75) - 0.5f), 80 + i));
        }
        // A few small stones knocked into the bed.
        for (int i = 0; i < 5; i++)
        {
            float a = Hash(i, 76) * Mathf.Tau, d = 0.12f + Hash(i, 77) * 0.2f;
            Add(list, 0, Stone, TurnedRock(Around(d, a, 0.03f), new Vector3(0.035f, 0.025f, 0.03f), a, 0f, 95 + i));
        }

        // Stage 1: clay chinking, then the second course over the joints.
        Add(list, 1, Mud, Wall(r - 0.07f, 0.1f, 0.06f, 0.22f, 0f, Mathf.Tau, top: false));
        for (int i = 0; i < stones; i++)
        {
            float a = (i + 0.5f + (Hash(i, 81) - 0.5f) * 0.2f) * Mathf.Tau / stones;
            var size = new Vector3(0.13f + Hash(i, 82) * 0.04f, 0.085f + Hash(i, 83) * 0.03f, 0.1f + Hash(i, 84) * 0.025f);
            // Leaning a touch inward, as a dry-stacked course settles.
            Add(list, 1, Stone, TurnedRock(Around(r - 0.08f, a, 0.21f + size.Y * 0.7f), size, a, -0.12f, 110 + i));
        }
        return list;
    }

    // Grey ash over the fire bed, and dark clay mud pressed between the stones.
    static ShaderMaterial Ash => ash ??= Painted(Tinted(Look.Ground, new Color(0.36f, 0.33f, 0.3f)), PaintedStructures.Family.Clay);
    static ShaderMaterial Mud => mud ??= Painted(Tinted(Look.Ground, Look.Clay.Darkened(0.55f)), PaintedStructures.Family.Clay);
    static ShaderMaterial? ash, mud;

    // A filled, shallow dish for the fire bed: its rim at rimY, sinking to midY in the middle,
    // the rim a little uneven.
    static Mesh Bed(float radius, float rimY, float midY, int seed)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        const int segs = 18;
        var mid = new Vector3(0, midY, 0);
        for (int i = 0; i < segs; i++)
        {
            float a0 = i * Mathf.Tau / segs, a1 = (i + 1) * Mathf.Tau / segs;
            var p0 = Around(radius * (0.94f + Hash(i, seed) * 0.08f), a0, rimY);
            var p1 = Around(radius * (0.94f + Hash((i + 1) % segs, seed) * 0.08f), a1, rimY);
            foreach (var p in new[] { mid, p1, p0 })
            {
                st.SetNormal(Vector3.Up);
                st.SetColor(Colors.White);
                st.AddVertex(p);
            }
        }
        return st.Commit();
    }

    // A rock turned to lie along the ring at angle a (its long side along the circle), tipped
    // inward by lean radians.
    static Mesh TurnedRock(Vector3 center, Vector3 size, float a, float lean, int seed)
    {
        var rock = Rock(Vector3.Zero, size, seed);
        var basis = new Basis(Vector3.Up, a) * new Basis(Vector3.Right, lean);
        var xf = new Transform3D(basis, center);
        var arrays = rock.SurfaceGetArrays(0);
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        for (int i = 0; i < verts.Length; i++) verts[i] = xf * verts[i];
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        if (arrays[(int)Mesh.ArrayType.Normal].VariantType != Variant.Type.Nil)
        {
            var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
            for (int i = 0; i < normals.Length; i++) normals[i] = basis * normals[i];
            arrays[(int)Mesh.ArrayType.Normal] = normals;
        }
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
