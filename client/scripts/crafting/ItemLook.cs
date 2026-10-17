using System;
using System.Linq;
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
        // The painted look (Modeling's painted-6) for the meshes made elsewhere too.
        mesh = ItemModels.GroundMesh(item) ?? PaintedItems.Paint(OldMeshFor(item), PaintedItems.FamilyOf(item));
        Meshes[item] = mesh;
        return mesh;
    }

    // The simple shapes from before the Modeling lane's models (model-1); the
    // fallback for items without one yet, and the "before" in comparisons.
    public static Mesh OldMeshFor(Item item)
    {
        return ClayItemLook.MeshFor(item) ?? SurvivalItemLook.MeshFor(item) ?? item switch
        {
            // Raw materials are a little oversized and light so a new player spots them in the grass.
            Item.Stick => Lying(new CylinderMesh { TopRadius = 0.04f, BottomRadius = 0.05f, Height = 1.0f, RadialSegments = 5 }, new Color(0.58f, 0.45f, 0.32f)),
            Item.Stone => Look.Faceted(new SphereMesh { Radius = 0.14f, Height = 0.18f, RadialSegments = 6, Rings = 3 }, Look.Rock),
            Item.Fiber => Look.Faceted(new CylinderMesh { TopRadius = 0.15f, BottomRadius = 0.1f, Height = 0.2f, RadialSegments = 7 }, Look.LeafWarm.Lightened(0.15f)),
            Item.Tinder => Look.Faceted(new SphereMesh { Radius = 0.1f, Height = 0.1f, RadialSegments = 7, Rings = 3 }, Look.LeafWarm.Lightened(0.2f)),
            Item.Flake => Look.Faceted(new PrismMesh { Size = new Vector3(0.12f, 0.03f, 0.08f) }, Look.Rock.Darkened(0.2f)),
            Item.HandAxe => Look.Faceted(new PrismMesh { Size = new Vector3(0.14f, 0.06f, 0.2f) }, Look.Rock.Darkened(0.15f)),
            Item.Cordage => Look.Faceted(new TorusMesh { InnerRadius = 0.05f, OuterRadius = 0.09f, Rings = 8, RingSegments = 5 }, Look.LeafWarm.Darkened(0.2f)),
            Item.StoneKnife => Lying(new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.025f, Height = 0.3f, RadialSegments = 5 }, Look.Rock.Darkened(0.1f)),
            Item.DiggingStick => Lying(new CylinderMesh { TopRadius = 0.005f, BottomRadius = 0.035f, Height = 1.1f, RadialSegments = 5 }, Look.Bark.Lightened(0.35f)),
            Item.Basket => Look.Faceted(new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.15f, Height = 0.22f, RadialSegments = 9 }, Look.LeafWarm.Darkened(0.1f)),
            Item.BowDrill => BowDrillMesh.Build(),
            Item.Log => Lying(new CylinderMesh { TopRadius = 0.08f, BottomRadius = 0.09f, Height = 0.8f, RadialSegments = 7 }, Look.Bark.Lightened(0.1f)),
            Item.RawMutton => Look.Faceted(new SphereMesh { Radius = 0.1f, Height = 0.12f, RadialSegments = 7, Rings = 4 }, new Color(0.78f, 0.36f, 0.34f)),
            Item.CookedMutton => Look.Faceted(new SphereMesh { Radius = 0.1f, Height = 0.12f, RadialSegments = 7, Rings = 4 }, new Color(0.50f, 0.27f, 0.12f)),
            Item.BurntMutton => Look.Faceted(new SphereMesh { Radius = 0.09f, Height = 0.11f, RadialSegments = 7, Rings = 4 }, new Color(0.09f, 0.07f, 0.06f)),
            Item.Berries => Look.Faceted(new SphereMesh { Radius = 0.07f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, new Color(0.55f, 0.08f, 0.16f)),
            Item.Spear or Item.StoneAxe => Lying(new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.03f, Height = item == Item.Spear ? 1.6f : 0.7f, RadialSegments = 5 }, Look.Bark.Lightened(0.25f)),
            _ => Look.Faceted(new SphereMesh { Radius = 0.12f, Height = 0.16f, RadialSegments = 7, Rings = 4 }, Look.Clay),
        };
    }

    // "Fine stone flake", or just "Stick" for raw materials. A tool that is wearing out
    // says so: "Rough stone axe (worn)".
    public static string Label(Item item, byte quality, uint count, string condition = "")
    {
        if (!Items.All.TryGetValue(item, out var def)) return $"Item {(ushort)item}";
        string name = def.Count(count);
        if (!def.HasQuality) return name;
        string grade = Processes.Grade(quality);
        string label = char.ToUpperInvariant(grade[0]) + grade[1..] + " " + name.ToLowerInvariant();
        return condition == "" ? label : $"{label} ({condition})";
    }

    // "worn", "nearly spent" or "spent" for a tool that is wearing out, else "" (D-155).
    public static string Condition(ushort item, byte quality, uint wear)
    {
        if (!Items.IsKnown(item)) return "";
        if (Durability.Wears((Item)item)) return Durability.Condition(quality, wear);
        // A pot with a maker's mark says who made it (ceramics-15), and slip says so too (ceramics-14).
        // Main fills in the name lookup.
        string made = Marks.Marked((Item)item) && Marks.Has(wear) ? $"made by {MakerName?.Invoke(Marks.Maker(wear)) ?? "someone"}" : "";
        string slip = Slip.On((Item)item, wear) ? (Slip.Bands(wear) == 2 ? "two bands of slip" : "a band of slip") : "";
        return made != "" && slip != "" ? $"{made}, {slip}" : made + slip;
    }

    // Looks up a player's name from the low 16 bits of their id (a maker's mark carries that much).
    public static Func<uint, string?>? MakerName;

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
        // Rest it on the ground instead of half buried in the grass.
        float bottom = verts.Min(v => v.Y);
        for (int i = 0; i < verts.Length; i++) verts[i].Y -= bottom;
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, Look.Solid(color));
        return mesh;
    }
}
