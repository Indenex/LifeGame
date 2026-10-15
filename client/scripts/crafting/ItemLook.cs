using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// How each item looks lying on the ground, and what it is called in the HUD.
public static class ItemLook
{
    static readonly Dictionary<Item, Mesh> Meshes = new();

    public static Mesh MeshFor(Item item)
    {
        if (Meshes.TryGetValue(item, out var mesh)) return mesh;
        mesh = ClayItemLook.MeshFor(item) ?? SurvivalItemLook.MeshFor(item) ?? item switch
        {
            Item.Stick => Lying(new CylinderMesh { TopRadius = 0.03f, BottomRadius = 0.04f, Height = 0.9f, RadialSegments = 5 }, Look.Bark.Lightened(0.15f)),
            Item.Stone => Look.Faceted(new SphereMesh { Radius = 0.14f, Height = 0.18f, RadialSegments = 6, Rings = 3 }, Look.Rock),
            Item.Fiber => Look.Faceted(new CylinderMesh { TopRadius = 0.09f, BottomRadius = 0.07f, Height = 0.12f, RadialSegments = 7 }, Look.LeafWarm),
            Item.Tinder => Look.Faceted(new SphereMesh { Radius = 0.1f, Height = 0.1f, RadialSegments = 7, Rings = 3 }, Look.LeafWarm.Lightened(0.2f)),
            Item.Flake => Look.Faceted(new PrismMesh { Size = new Vector3(0.12f, 0.03f, 0.08f) }, Look.Rock.Darkened(0.2f)),
            Item.HandAxe => Look.Faceted(new PrismMesh { Size = new Vector3(0.14f, 0.06f, 0.2f) }, Look.Rock.Darkened(0.15f)),
            Item.Cordage => Look.Faceted(new TorusMesh { InnerRadius = 0.05f, OuterRadius = 0.09f, Rings = 8, RingSegments = 5 }, Look.LeafWarm.Darkened(0.2f)),
            Item.StoneKnife => Lying(new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.025f, Height = 0.3f, RadialSegments = 5 }, Look.Rock.Darkened(0.1f)),
            Item.DiggingStick => Lying(new CylinderMesh { TopRadius = 0.005f, BottomRadius = 0.035f, Height = 1.1f, RadialSegments = 5 }, Look.Bark.Lightened(0.35f)),
            Item.Basket => Look.Faceted(new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.15f, Height = 0.22f, RadialSegments = 9 }, Look.LeafWarm.Darkened(0.1f)),
            Item.BowDrill => BowDrillMesh.Build(),
            Item.RawMutton => Look.Faceted(new SphereMesh { Radius = 0.1f, Height = 0.12f, RadialSegments = 7, Rings = 4 }, new Color(0.78f, 0.36f, 0.34f)),
            Item.CookedMutton => Look.Faceted(new SphereMesh { Radius = 0.1f, Height = 0.12f, RadialSegments = 7, Rings = 4 }, new Color(0.50f, 0.27f, 0.12f)),
            Item.BurntMutton => Look.Faceted(new SphereMesh { Radius = 0.09f, Height = 0.11f, RadialSegments = 7, Rings = 4 }, new Color(0.09f, 0.07f, 0.06f)),
            Item.Berries => Look.Faceted(new SphereMesh { Radius = 0.07f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, new Color(0.55f, 0.08f, 0.16f)),
            Item.Spear or Item.StoneAxe => Lying(new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.03f, Height = item == Item.Spear ? 1.6f : 0.7f, RadialSegments = 5 }, Look.Bark.Lightened(0.25f)),
            _ => Look.Faceted(new SphereMesh { Radius = 0.12f, Height = 0.16f, RadialSegments = 7, Rings = 4 }, Look.Clay),
        };
        Meshes[item] = mesh;
        return mesh;
    }

    // "Fine stone flake", or just "Stick" for raw materials.
    public static string Label(Item item, byte quality, uint count)
    {
        if (!Items.All.TryGetValue(item, out var def)) return $"Item {(ushort)item}";
        string name = def.Count(count);
        if (!def.HasQuality) return name;
        string grade = Processes.Grade(quality);
        return char.ToUpperInvariant(grade[0]) + grade[1..] + " " + name.ToLowerInvariant();
    }

    // A long primitive laid on its side, like a stick dropped on the ground.
    static Mesh Lying(PrimitiveMesh source, Color color)
    {
        var st = new SurfaceTool();
        st.CreateFrom(source, 0);
        st.Deindex();
        st.GenerateNormals();
        var arrays = st.CommitToArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
        var tilt = new Basis(Vector3.Forward, Mathf.Pi / 2f);
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = tilt * verts[i];
            normals[i] = tilt * normals[i];
        }
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, Look.Solid(color));
        return mesh;
    }
}
