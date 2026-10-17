using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The mud-brick house: stone footings and a hearth ring, sun-dried brick walls laid in
// courses with a timber lintel over the door, rafters with clay gable ends, and a
// thatched gable roof. +Z is the door side.
public static partial class StructureModel
{
    const float HouseWallTop = 2.0f, HouseRidge = 3.3f, HouseDoorHalf = 0.5f, HouseDoorTop = 1.78f; // 1.78 x 1.25 = 2.2 m clear (building-14)
    const float HouseEaveZ = Structures.HouseWallZ + 0.5f, HouseEaveX = Structures.HouseWallX + 0.45f;
    static ShaderMaterial? brick;
    static ShaderMaterial Brick => brick ??= Painted(Look.Solid(new Color(0.74f, 0.5f, 0.36f)), PaintedStructures.Family.Clay);

    // The doorway's two jambs and height, for hanging the door.
    public static (Vector3 a, Vector3 b, float height) HouseDoor =>
        (new Vector3(HouseDoorHalf - 0.03f, 0, Structures.HouseWallZ - 0.06f), new Vector3(-HouseDoorHalf + 0.03f, 0, Structures.HouseWallZ - 0.06f), HouseDoorTop - 0.06f);

    static List<Piece> BrickHouse(int kind)
    {
        var list = new List<Piece>();
        var shape = Structures.Shape(kind);
        // The shape's numbers are the real, scaled ones; this model is drawn at design size and scaled by the view.
        float hw = shape.HalfWidth / shape.Scale, hd = shape.HalfDepth / shape.Scale, wx = Structures.HouseWallX, wz = Structures.HouseWallZ;
        Site(list, new[] { new Vector3(-hw, 0, hd), new Vector3(hw, 0, hd), new Vector3(hw, 0, -hd), new Vector3(-hw, 0, -hd) });

        // Footings: a course of fieldstones under each wall, the door posts, and a hearth ring.
        const float t = 0.34f;
        Add(list, 0, Stone, Footing(new Vector3(-wx, 0, -wz), new Vector3(wx, 0, -wz), t, 1));
        Add(list, 0, Stone, Footing(new Vector3(wx, 0, -wz), new Vector3(wx, 0, wz), t, 2));
        Add(list, 0, Stone, Footing(new Vector3(-wx, 0, wz), new Vector3(-wx, 0, -wz), t, 3));
        Add(list, 0, Stone, Footing(new Vector3(wx, 0, wz), new Vector3(HouseDoorHalf, 0, wz), t, 4));
        Add(list, 0, Stone, Footing(new Vector3(-HouseDoorHalf, 0, wz), new Vector3(-wx, 0, wz), t, 5));
        foreach (float x in new[] { HouseDoorHalf + 0.04f, -HouseDoorHalf - 0.04f })
            Add(list, 0, Wood, Stick(new Vector3(x, -0.2f, wz), new Vector3(x, HouseDoorTop + 0.05f, wz + 0.01f), 0.07f));
        var hearth = new Vector3(0, 0, -0.55f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.Tau / 8;
            Add(list, 0, Stone, Rock(hearth + new Vector3(Mathf.Sin(a) * 0.42f, 0.03f, Mathf.Cos(a) * 0.42f),
                new Vector3(0.13f, 0.09f, 0.11f) * (0.85f + Hash(i, 21) * 0.35f), 30 + i));
        }

        // Brick walls, course by course, bottom up; the lintel goes on with the course above the door.
        const float course = 0.17f;
        int courses = Mathf.CeilToInt((HouseWallTop - 0.12f) / course);
        for (int c = 0; c < courses; c++)
        {
            float y0 = 0.12f + c * course, y1 = y0 + course;
            var st = Begin();
            float offset = c % 2 == 0 ? 0f : 0.21f;
            BrickRun(st, new Vector3(-wx - t / 2, 0, -wz), new Vector3(wx + t / 2, 0, -wz), y0, y1, t, offset, c * 7 + 1);
            BrickRun(st, new Vector3(wx, 0, -wz + t / 2), new Vector3(wx, 0, wz - t / 2), y0, y1, t, offset, c * 7 + 2);
            BrickRun(st, new Vector3(-wx, 0, wz - t / 2), new Vector3(-wx, 0, -wz + t / 2), y0, y1, t, offset, c * 7 + 3);
            if (y0 >= HouseDoorTop)
                BrickRun(st, new Vector3(wx + t / 2, 0, wz), new Vector3(-wx - t / 2, 0, wz), y0, y1, t, offset, c * 7 + 4);
            else
            {
                BrickRun(st, new Vector3(wx + t / 2, 0, wz), new Vector3(HouseDoorHalf + 0.1f, 0, wz), y0, y1, t, offset, c * 7 + 4);
                BrickRun(st, new Vector3(-HouseDoorHalf - 0.1f, 0, wz), new Vector3(-wx - t / 2, 0, wz), y0, y1, t, offset, c * 7 + 5);
            }
            Add(list, 1, Brick, st.Commit());
            if (y0 < HouseDoorTop && y1 >= HouseDoorTop)
                Add(list, 1, Wood, Stick(new Vector3(-HouseDoorHalf - 0.3f, HouseDoorTop + 0.04f, wz + 0.02f), new Vector3(HouseDoorHalf + 0.3f, HouseDoorTop + 0.05f, wz + 0.02f), 0.09f));
        }

        HouseRoof(list, t, Clay, shape.TileRoof ? Tile : Thatch);
        return list;
    }

    // Rafters with gable ends over the room's side walls (stage 2), then thatch in rows
    // from the eaves up on both slopes and a bundled ridge cap (stage 3). Shared by the
    // mud-brick house (clay gables) and the log cabin (log gables).
    static void HouseRoof(List<Piece> list, float t, Material gable, Material? roof = null)
    {
        roof ??= Thatch;
        float wx = Structures.HouseWallX, wz = Structures.HouseWallZ;
        // Rafters: a ridge pole, pairs of rafters down to the eaves, gable ends.
        float slope = (HouseRidge - HouseWallTop) / wz;
        float eaveY = HouseRidge - slope * HouseEaveZ;
        Add(list, 2, Wood, Stick(new Vector3(-HouseEaveX - 0.1f, HouseRidge + 0.02f, 0), new Vector3(HouseEaveX + 0.1f, HouseRidge + 0.05f, 0), 0.07f));
        const int pairs = 9;
        for (int i = 0; i < pairs; i++)
        {
            float x = Mathf.Lerp(-HouseEaveX + 0.1f, HouseEaveX - 0.1f, i / (float)(pairs - 1));
            foreach (float side in new[] { 1f, -1f })
                Add(list, 2, Wood, Stick(new Vector3(x, eaveY - 0.02f, side * HouseEaveZ), new Vector3(x + (Hash(i, 22) - 0.5f) * 0.04f, HouseRidge + 0.1f, -side * 0.12f), 0.045f));
        }
        foreach (float side in new[] { 1f, -1f })
            Add(list, 2, gable, Gable(side * wx, wz + t / 2, HouseWallTop + 0.1f, HouseRidge - 0.04f, t * 0.8f, side));

        // Thatch in rows from the eaves up on both slopes, then a bundled ridge cap.
        const int rows = 4;
        foreach (float side in new[] { 1f, -1f })
        {
            var low = new Vector3(0, eaveY + 0.04f, side * (HouseEaveZ + 0.06f));
            var high = new Vector3(0, HouseRidge + 0.12f, 0);
            for (int r = 0; r < rows; r++)
            {
                float t0 = r / (float)rows - 0.04f, t1 = (r + 1) / (float)rows + 0.1f;
                Add(list, 3, roof, ThatchRow(low.Lerp(high, Math.Max(0, t0)), low.Lerp(high, Math.Min(1.04f, t1)), HouseEaveX * 2 + 0.2f, 0.12f + r * 0.012f, r + (side > 0 ? 0 : 10)));
            }
        }
        Add(list, 3, roof, Wrap(new Vector3(0, HouseRidge + 0.2f, 0), Vector3.Right, 0.15f, HouseEaveX * 2 + 0.25f, lumpy: true));
    }

    // A low course of fieldstones from a to b.
    static Mesh Footing(Vector3 a, Vector3 b, float width, int seed)
    {
        var st = Begin();
        var dir = (b - a).Normalized();
        var side = dir.Cross(Vector3.Up).Normalized();
        float length = a.DistanceTo(b);
        int n = Math.Max(1, Mathf.RoundToInt(length / 0.42f));
        for (int i = 0; i < n; i++)
        {
            float s0 = i * length / n + 0.02f, s1 = (i + 1) * length / n - 0.02f;
            float h = 0.1f + Hash(i, seed + 40) * 0.05f, w = width * (0.48f + Hash(i, seed + 41) * 0.1f);
            var shade = Shade(i * 11 + seed).Darkened(Hash(i, seed + 42) * 0.15f);
            OrientedBox(st, a + dir * s0, a + dir * s1, side, w, -0.08f, h, shade);
        }
        st.GenerateNormals();
        return st.Commit();
    }

    // One course of bricks along a wall from a to b, at heights y0..y1. offset staggers the joints.
    static void BrickRun(SurfaceTool st, Vector3 a, Vector3 b, float y0, float y1, float width, float offset, int seed)
    {
        const float length = 0.42f, joint = 0.018f;
        var dir = (b - a).Normalized();
        var side = dir.Cross(Vector3.Up).Normalized();
        float total = a.DistanceTo(b);
        // Mortar behind the bricks, a little inset so the joints read as shadowed lines.
        var mortar = new Color(0.86f, 0.8f, 0.7f);
        OrientedBox(st, a, b, side, width / 2 - 0.02f, y0, y1, mortar);
        int i = 0;
        for (float s = -offset; s < total; s += length, i++)
        {
            float s0 = Math.Max(0, s) + joint / 2, s1 = Math.Min(total, s + length) - joint / 2;
            if (s1 - s0 < 0.05f) continue;
            float k = 0.82f + Hash(seed * 31 + i, 17) * 0.3f;
            var shade = new Color(k, k * (0.92f + Hash(seed * 31 + i, 18) * 0.1f), k * 0.9f);
            OrientedBox(st, a + dir * s0, a + dir * s1, side, width / 2, y0 + joint / 2, y1 - joint / 2, shade);
        }
    }

    // A box from a to b along the ground, half-width w to each side, from y0 to y1.
    static void OrientedBox(SurfaceTool st, Vector3 a, Vector3 b, Vector3 side, float w, float y0, float y1, Color shade)
    {
        var dir = (b - a).Normalized();
        Vector3 P(Vector3 at, float s, float y) => new(at.X + side.X * s, y, at.Z + side.Z * s);
        // Each face listed bottom-left, top-left, top-right, bottom-right as seen from outside.
        Quad(st, P(a, w, y0), P(a, w, y1), P(b, w, y1), P(b, w, y0), side, shade);
        Quad(st, P(b, -w, y0), P(b, -w, y1), P(a, -w, y1), P(a, -w, y0), -side, shade.Darkened(0.06f));
        Quad(st, P(b, w, y0), P(b, w, y1), P(b, -w, y1), P(b, -w, y0), dir, shade.Darkened(0.03f));
        Quad(st, P(a, -w, y0), P(a, -w, y1), P(a, w, y1), P(a, w, y0), -dir, shade.Darkened(0.03f));
        Quad(st, P(a, -w, y1), P(b, -w, y1), P(b, w, y1), P(a, w, y1), Vector3.Up, shade.Lightened(0.04f));
    }

    // A clay-daubed gable end over a side wall at x, from the wall top up to the ridge.
    static Mesh Gable(float x, float halfDepth, float bottom, float top, float thickness, float side)
    {
        var st = Begin();
        foreach (float face in new[] { 1f, -1f })
        {
            float fx = x + side * face * thickness / 2;
            var n = new Vector3(side * face, 0, 0);
            // Outward from the room on +X is seen with +Z on its left.
            float zl = side * face > 0 ? halfDepth : -halfDepth;
            var shade = Colors.White.Darkened(face > 0 ? 0f : 0.12f);
            Vert(st, new Vector3(fx, bottom, zl), n, shade);
            Vert(st, new Vector3(fx, top, 0), n, shade);
            Vert(st, new Vector3(fx, bottom, -zl), n, shade);
        }
        return st.Commit();
    }
}
