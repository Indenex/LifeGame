using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// How clay items look lying on the ground: real pots (full ones with water in
// them), sherds, lumps of clay at each stage, little piles of temper, bricks.
public static class ClayItemLook
{
    static readonly Dictionary<Item, Mesh?> Meshes = new();

    public static Mesh? MeshFor(Item item)
    {
        if (Meshes.TryGetValue(item, out var mesh)) return mesh;
        mesh = item switch
        {
            Item.PinchPot => Pot(ClayForm.PinchPot, ClayStage.Fired, false),
            Item.CoilPot => Pot(ClayForm.CoilPot, ClayStage.Fired, false),
            Item.PinchPotOfWater => Pot(ClayForm.PinchPot, ClayStage.Fired, true),
            Item.CoilPotOfWater => Pot(ClayForm.CoilPot, ClayStage.Fired, true),
            Item.UnfiredPinchPot => Pot(ClayForm.PinchPot, ClayStage.Drying, false),
            Item.UnfiredCoilPot => Pot(ClayForm.CoilPot, ClayStage.Drying, false),
            Item.Brick => Baked(PotMesh.For(ClayForm.Brick, 1), ClayLook.For(ClayStage.Drying, FireKind.Pit, 0f, 1), PotView.Size),
            Item.Potsherd => Sherd(),
            Item.CleanClay => Lump(ClayLook.Wet.Lightened(0.08f), 0.11f),
            Item.TemperedClay => Lump(ClayLook.Wet.Lightened(0.15f), 0.12f),
            Item.Daub => Lump(new Color(0.42f, 0.34f, 0.22f), 0.12f),
            Item.Grit => Pile(Look.Rock.Lightened(0.15f)),
            Item.Grog => Pile(ClayLook.PitFired.Darkened(0.1f)),
            _ => null,
        };
        Meshes[item] = mesh;
        return mesh;
    }

    static Mesh Pot(ClayForm form, ClayStage stage, bool full)
    {
        var mesh = Baked(PotMesh.For(form, 2), ClayLook.For(stage, FireKind.Pit, 0f, 2), PotView.Size);
        if (full)
        {
            var line = PotMesh.WaterLine(form) * PotView.Size;
            var disc = new CylinderMesh { TopRadius = line.X, BottomRadius = line.X, Height = 0.002f, RadialSegments = 20 };
            var arrays = disc.GetMeshArrays();
            var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            for (int i = 0; i < verts.Length; i++) verts[i] += new Vector3(0, line.Y, 0);
            arrays[(int)Mesh.ArrayType.Vertex] = verts;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(1, ClayLook.Water);
        }
        return mesh;
    }

    // A copy of a mesh, scaled, with its material baked in, since ground items
    // are drawn without a material override.
    static ArrayMesh Baked(ArrayMesh source, Material material, float scale)
    {
        var arrays = source.SurfaceGetArrays(0);
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        for (int i = 0; i < verts.Length; i++) verts[i] *= scale;
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, material);
        return mesh;
    }

    // A curved piece of broken pot wall.
    static Mesh Sherd()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        const int n = 5;
        var pts = new Vector3[n + 1, 2];
        for (int i = 0; i <= n; i++)
        {
            float a = -0.5f + i / (float)n;
            float jag = (i % 2 == 0 ? 0.02f : -0.015f);
            pts[i, 0] = new Vector3(Mathf.Sin(a) * 0.14f, 0.012f, Mathf.Cos(a) * 0.14f - 0.12f);
            pts[i, 1] = new Vector3(Mathf.Sin(a) * 0.13f, 0.07f + jag, Mathf.Cos(a) * 0.13f - 0.12f + 0.03f);
        }
        for (int i = 0; i < n; i++)
        {
            st.SetUV(new Vector2(0, 0.5f));
            st.AddVertex(pts[i, 0]); st.AddVertex(pts[i + 1, 0]); st.AddVertex(pts[i + 1, 1]);
            st.AddVertex(pts[i, 0]); st.AddVertex(pts[i + 1, 1]); st.AddVertex(pts[i, 1]);
        }
        st.GenerateNormals();
        var mesh = st.Commit();
        // Both faces show, so the painted material (drawn double-sided) suits a thin shard.
        mesh.SurfaceSetMaterial(0, Look.Solid(ClayLook.PitFired));
        return mesh;
    }

    static Mesh Lump(Color color, float radius) =>
        Look.Faceted(new SphereMesh { Radius = radius, Height = radius * 1.3f, RadialSegments = 8, Rings = 4 }, color);

    static Mesh Pile(Color color) =>
        Look.Faceted(new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.12f, Height = 0.06f, RadialSegments = 8 }, color);
}
