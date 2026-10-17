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
            Item.StorageJar => Jar(ClayStage.Fired),
            Item.ClayLamp => Pot(ClayForm.Lamp, ClayStage.Fired, false),
            Item.UnfiredLamp => Pot(ClayForm.Lamp, ClayStage.Drying, false),
            Item.RoofTile => Tile(ClayStage.Fired),
            Item.UnfiredRoofTile => Tile(ClayStage.Drying),
            Item.ClayTrough => Trough(ClayStage.Fired),
            Item.UnfiredClayTrough => Trough(ClayStage.Drying),
            Item.Tallow => Look.Faceted(new SphereMesh { Radius = 0.07f, Height = 0.08f, RadialSegments = 8, Rings = 4 }, new Color(0.93f, 0.88f, 0.72f)),
            Item.PotOfBroth => Pot(ClayForm.CoilPot, ClayStage.Fired, true, Look.Solid(SimmerView.Broth)),
            Item.PotOfStew => Pot(ClayForm.CoilPot, ClayStage.Fired, true, Look.Solid(SimmerView.Stew)),
            Item.UnfiredStorageJar => Jar(ClayStage.Drying),
            Item.Brick => Baked(PotMesh.For(ClayForm.Brick, 1), ClayLook.For(ClayStage.Drying, FireKind.Pit, 0f, 1), PotView.Size),
            Item.Potsherd => Sherd(),
            // Each stage of clay has its own shape, so they tell apart at a glance in
            // the inventory: a rough clod, a smooth wedged block, a block flecked with
            // grit, and a flat pat of daub with straw in it.
            Item.Clay => Clod(),
            Item.CleanClay => Block(ClayLook.Wet.Lightened(0.1f), false),
            Item.TemperedClay => Block(ClayLook.Wet.Lightened(0.16f), true),
            Item.Daub => DaubPat(),
            Item.Grit => Pile(Look.Rock.Lightened(0.15f)),
            Item.Grog => Pile(ClayLook.PitFired.Darkened(0.1f)),
            _ => null,
        };
        Meshes[item] = mesh;
        return mesh;
    }

    static readonly Dictionary<(Item, uint), Mesh?> MarkedMeshes = new();

    // The decoration bits of an item row's Wear number: the maker (Marks) and slip (Slip), for the
    // clay items that show them; 0 for anything else. Two rows with the same value look the same.
    public static uint Decoration(Item item, uint wear)
    {
        uint d = 0;
        if (Marks.Marked(item) && Marks.Has(wear)) d |= wear & 0x1FFFFu;
        if (Slip.CanDecorate(item) && Slip.Has(wear)) d |= wear & ~0x1FFFFu & 0x3FFFFFFFu;
        return d;
    }

    // The same item with its maker's mark pressed in (ceramics-15) and/or slip round the rim
    // (ceramics-14); `decor` is Decoration(). Null when it shows neither, so the caller falls back
    // to the plain mesh.
    public static Mesh? MarkedMesh(Item item, uint decor)
    {
        if (MarkedMeshes.TryGetValue((item, decor), out var cached)) return cached;
        var glyph = decor;
        Mesh? mesh = item switch
        {
            Item.PinchPot => Pot(ClayForm.PinchPot, ClayStage.Fired, false, null, glyph),
            Item.CoilPot => Pot(ClayForm.CoilPot, ClayStage.Fired, false, null, glyph),
            Item.PinchPotOfWater => Pot(ClayForm.PinchPot, ClayStage.Fired, true, null, glyph),
            Item.CoilPotOfWater => Pot(ClayForm.CoilPot, ClayStage.Fired, true, null, glyph),
            Item.UnfiredPinchPot => Pot(ClayForm.PinchPot, ClayStage.Drying, false, null, glyph),
            Item.UnfiredCoilPot => Pot(ClayForm.CoilPot, ClayStage.Drying, false, null, glyph),
            Item.ClayLamp => Pot(ClayForm.Lamp, ClayStage.Fired, false, null, glyph),
            Item.UnfiredLamp => Pot(ClayForm.Lamp, ClayStage.Drying, false, null, glyph),
            Item.StorageJar => Jar(ClayStage.Fired, glyph),
            Item.UnfiredStorageJar => Jar(ClayStage.Drying, glyph),
            _ => null,
        };
        MarkedMeshes[(item, decor)] = mesh;
        return mesh;
    }

    static Mesh Pot(ClayForm form, ClayStage stage, bool full, Material? liquid = null, uint decor = 0)
    {
        var plain = ClayLook.For(stage, FireKind.Pit, 0f, 2);
        var mesh = Baked(PotMesh.For(form, 2), ClayLook.WithMark(plain, decor, form, PotView.Size), PotView.Size);
        if (full)
        {
            var line = PotMesh.WaterLine(form) * PotView.Size;
            var disc = new CylinderMesh { TopRadius = line.X, BottomRadius = line.X, Height = 0.002f, RadialSegments = 20 };
            var arrays = disc.GetMeshArrays();
            var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            for (int i = 0; i < verts.Length; i++) verts[i] += new Vector3(0, line.Y, 0);
            arrays[(int)Mesh.ArrayType.Vertex] = verts;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(1, liquid ?? ClayLook.Water);
        }
        return mesh;
    }

    // A trough is kiln-fired too (ceramics-13).
    static Mesh Trough(ClayStage stage) =>
        Baked(PotMesh.For(ClayForm.Trough, 2), ClayLook.For(stage, FireKind.Kiln, 0f, 2), PotView.Size);

    // Roof tiles are kiln-fired too.
    static Mesh Tile(ClayStage stage) =>
        Baked(PotMesh.For(ClayForm.Tile, 2), ClayLook.For(stage, FireKind.Kiln, 0f, 2), PotView.Size);

    // A jar on the ground is kiln-fired, so it takes the kiln's darker red.
    static Mesh Jar(ClayStage stage, uint decor = 0)
    {
        var plain = ClayLook.For(stage, FireKind.Kiln, 0f, 2);
        return Baked(PotMesh.For(ClayForm.StorageJar, 2), ClayLook.WithMark(plain, decor, ClayForm.StorageJar, PotView.Size), PotView.Size);
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

    // Dug clay: a lumpy clod with bits of stone still in it.
    static Mesh Clod() => Parts(
        (new SphereMesh { Radius = 0.12f, Height = 0.15f, RadialSegments = 5, Rings = 3 }, Look.Clay.Darkened(0.12f), Transform3D.Identity),
        (new SphereMesh { Radius = 0.07f, Height = 0.09f, RadialSegments = 5, Rings = 2 }, Look.Clay.Darkened(0.05f), At(0.08f, 0.02f, 0.04f)),
        (new SphereMesh { Radius = 0.025f, Height = 0.04f, RadialSegments = 4, Rings = 2 }, Look.Rock.Lightened(0.1f), At(-0.05f, 0.06f, 0.07f)),
        (new SphereMesh { Radius = 0.02f, Height = 0.03f, RadialSegments = 4, Rings = 2 }, Look.Rock, At(0.03f, 0.045f, -0.07f)));

    // Cleaned clay, wedged into a smooth block; tempered clay is the same block
    // flecked with pale grit.
    static Mesh Block(Color color, bool flecked)
    {
        var parts = new List<(PrimitiveMesh, Color, Transform3D)>
        {
            (new CapsuleMesh { Radius = 0.07f, Height = 0.24f, RadialSegments = 10, Rings = 3 }, color, new Transform3D(new Basis(Vector3.Forward, Mathf.Pi / 2), Vector3.Zero)),
        };
        if (flecked)
        {
            var fleck = Look.Rock.Lightened(0.45f);
            var spots = new[] { new Vector3(-0.07f, 0.055f, 0.03f), new Vector3(-0.01f, 0.068f, -0.02f), new Vector3(0.05f, 0.058f, 0.035f), new Vector3(0.08f, 0.04f, -0.04f), new Vector3(-0.04f, 0.035f, 0.06f), new Vector3(0.02f, 0.03f, 0.065f), new Vector3(-0.09f, 0.03f, -0.045f) };
            foreach (var spot in spots)
                parts.Add((new SphereMesh { Radius = 0.013f, Height = 0.02f, RadialSegments = 4, Rings = 2 }, fleck, new Transform3D(Basis.Identity, spot)));
        }
        return Parts(parts.ToArray());
    }

    // Daub: a flat pat of clay and dung with straw poking out of it.
    static Mesh DaubPat()
    {
        var straw = new Color(0.86f, 0.74f, 0.42f);
        var parts = new List<(PrimitiveMesh, Color, Transform3D)>
        {
            (new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.14f, Height = 0.06f, RadialSegments = 9 }, new Color(0.40f, 0.33f, 0.22f), Transform3D.Identity),
        };
        for (int i = 0; i < 6; i++)
        {
            float a = i * 1.9f;
            var basis = new Basis(Vector3.Up, a) * new Basis(Vector3.Right, Mathf.Pi / 2 - 0.12f);
            var at = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (0.06f + 0.015f * (i % 3)) + new Vector3(0, 0.03f, 0);
            parts.Add((new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.008f, Height = 0.17f, RadialSegments = 4 }, straw, new Transform3D(basis, at)));
        }
        return Parts(parts.ToArray());
    }

    static Transform3D At(float x, float y, float z) => new(Basis.Identity, new Vector3(x, y, z));

    // Several faceted pieces in one mesh, each in its own flat color.
    static Mesh Parts(params (PrimitiveMesh Mesh, Color Color, Transform3D At)[] parts)
    {
        var result = new ArrayMesh();
        foreach (var (source, color, at) in parts)
        {
            var st = new SurfaceTool();
            st.CreateFrom(source, 0);
            st.Deindex();
            st.GenerateNormals();
            var arrays = st.CommitToArrays();
            var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
            var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = at * verts[i];
                normals[i] = (at.Basis * normals[i]).Normalized();
            }
            arrays[(int)Mesh.ArrayType.Vertex] = verts;
            arrays[(int)Mesh.ArrayType.Normal] = normals;
            result.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            result.SurfaceSetMaterial(result.GetSurfaceCount() - 1, Look.Solid(color));
        }
        return result;
    }

    static Mesh Pile(Color color) =>
        Look.Faceted(new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.12f, Height = 0.06f, RadialSegments = 8 }, color);
}
