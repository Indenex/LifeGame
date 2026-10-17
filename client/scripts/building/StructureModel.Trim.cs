using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Hewn trim in the village kit's chunky style (building-26, Justin picked "trim and
// textures"): squared door frames with a heavy lintel and a sill board, and squared
// corner posts, in the kit's darker wood grain.
public static partial class StructureModel
{
    static ShaderMaterial? trimWood;
    internal static ShaderMaterial TrimWood => trimWood ??= Kit(Painted(Tinted(Look.BarkSurface, Look.Bark.Darkened(0.05f)), PaintedStructures.Family.Wood), PaintedStructures.Surface.WoodDark, 0.25f);

    // A squared post standing at (x, z) from y0 to y1, turned by yaw.
    static void SquarePostInto(SurfaceTool st, float x, float z, float half, float y0, float y1, float yaw, Color shade)
    {
        var along = new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw)) * half;
        var side = new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));
        var at = new Vector3(x, 0, z);
        OrientedBox(st, at - along, at + along, side, half, y0, y1, shade);
    }

    // A doorway's frame: two squared jambs at ±half across x (in a frame turned by yaw round the
    // structure's centre, standing at distance out from it), a heavy lintel reaching past them,
    // and a sill board on the ground between.
    static void DoorFrame(List<Piece> list, int stage, float half, float top, float out_, float yaw = 0f)
    {
        var st = Begin();
        const float jamb = 0.085f;
        Vector3 At(float lx, float lz) => new(lx * Mathf.Cos(yaw) + lz * Mathf.Sin(yaw), 0, -lx * Mathf.Sin(yaw) + lz * Mathf.Cos(yaw));
        foreach (float lx in new[] { -half - jamb, half + jamb })
        {
            var p = At(lx, out_);
            SquarePostInto(st, p.X, p.Z, jamb, -0.05f, top, yaw, Shade((int)(lx * 100) + 5));
        }
        var a = At(-half - 0.32f, out_ + 0.02f);
        var b = At(half + 0.32f, out_ + 0.02f);
        var across = new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw));
        OrientedBox(st, a, b, across, 0.11f, top - 0.02f, top + 0.2f, Shade(41));
        var c = At(-half - jamb, out_);
        var d = At(half + jamb, out_);
        OrientedBox(st, c, d, across, 0.14f, -0.02f, 0.05f, Shade(43).Lightened(0.06f));
        st.GenerateNormals();
        Add(list, stage, TrimWood, st.Commit());
    }

    // Squared corner posts at (±x, ±z), from the ground to top.
    static void CornerPosts(List<Piece> list, int stage, float x, float z, float half, float top)
    {
        var st = Begin();
        int i = 0;
        foreach (float sx in new[] { -x, x })
            foreach (float sz in new[] { -z, z })
                SquarePostInto(st, sx, sz, half, -0.05f, top + Hash(i, 61) * 0.04f, 0f, Shade(50 + i++));
        st.GenerateNormals();
        Add(list, stage, TrimWood, st.Commit());
    }
}
