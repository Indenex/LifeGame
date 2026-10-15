using System;
using Godot;

namespace LifeGame;

// A bow drill lying on the ground: a bent bow, its cord, and the spindle
// caught in a loop of the cord.
public static class BowDrillMesh
{
    const float BowLength = 0.75f, Bend = 0.55f;

    public static Mesh Build()
    {
        var mesh = new ArrayMesh();

        // The bow: a thin stick bent into an arc of radius Bend, laid flat.
        var bow = Shape(new CylinderMesh { TopRadius = 0.016f, BottomRadius = 0.022f, Height = BowLength, RadialSegments = 5, Rings = 8 },
            v =>
            {
                float theta = v.Y / Bend, r = Bend - v.X;
                return new Vector3(r * MathF.Sin(theta), v.Z + 0.022f, Bend - r * MathF.Cos(theta));
            });
        Add(mesh, bow, Look.Bark.Lightened(0.2f));

        // The cord runs straight between the bow's tips.
        float tip = BowLength / 2f / Bend;
        float chordZ = Bend - Bend * MathF.Cos(tip), half = Bend * MathF.Sin(tip);
        var cord = Shape(new CylinderMesh { TopRadius = 0.005f, BottomRadius = 0.005f, Height = half * 2f, RadialSegments = 4 },
            v => new Vector3(v.Y, v.Z + 0.022f, chordZ + v.X));
        Add(mesh, cord, Look.LeafWarm.Darkened(0.25f));

        // The spindle crosses the cord near its middle.
        var spindle = Shape(new CylinderMesh { TopRadius = 0.013f, BottomRadius = 0.016f, Height = 0.32f, RadialSegments = 5 },
            v => new Vector3(0.04f + v.X, v.Z + 0.03f, chordZ - 0.06f - v.Y));
        Add(mesh, spindle, Look.Bark.Lightened(0.35f));
        return mesh;
    }

    // move must keep handedness (a rotation or cyclic axis swap) so the faces stay outward.
    static Godot.Collections.Array Shape(PrimitiveMesh source, Func<Vector3, Vector3> move)
    {
        var st = new SurfaceTool();
        st.CreateFrom(source, 0);
        st.Deindex();
        var arrays = st.CommitToArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        for (int i = 0; i < verts.Length; i++) verts[i] = move(verts[i]);

        // Rebuild with flat normals for the faceted look.
        var rebuilt = new SurfaceTool();
        rebuilt.Begin(Mesh.PrimitiveType.Triangles);
        foreach (var v in verts) rebuilt.AddVertex(v);
        rebuilt.GenerateNormals();
        return rebuilt.CommitToArrays();
    }

    static void Add(ArrayMesh mesh, Godot.Collections.Array arrays, Color color)
    {
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, Look.Solid(color));
    }
}
