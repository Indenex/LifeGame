using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The wooden chest (building-22), in two stages: 0 the frame (four corner posts and the bottom boards),
// 1 the boards and the lid (side boards, a lid of two boards with battens across).
public static partial class StructureModel
{
    static List<Piece> Chest()
    {
        var list = new List<Piece>();
        const float hw = 0.42f, hd = 0.27f, h = 0.46f;
        Site(list, new[] { new Vector3(-0.55f, 0, 0.42f), new Vector3(0.55f, 0, 0.42f), new Vector3(0.55f, 0, -0.42f), new Vector3(-0.55f, 0, -0.42f) });

        // A board from a to b along the ground, half-thickness w, from y0 to y1.
        void Board(SurfaceTool st, Vector3 a, Vector3 b, float w, float y0, float y1, int seed)
        {
            var side = (b - a).Normalized().Cross(Vector3.Up).Normalized();
            float k = 0.9f + Hash(seed, 61) * 0.2f;
            OrientedBox(st, a, b, side, w, y0, y1, new Color(k, k * 0.96f, k * 0.9f));
        }

        // Frame: corner posts and a floor of boards across the width.
        foreach (var (x, z) in new[] { (-hw, hd), (hw, hd), (hw, -hd), (-hw, -hd) })
            Add(list, 0, Wood, Stick(new Vector3(x, -0.02f, z), new Vector3(x + (Hash((int)(x * 10 + z * 7), 43) - 0.5f) * 0.01f, h, z), 0.04f));
        var floor = Begin();
        for (int i = 0; i < 6; i++)
        {
            float x = -hw + 0.06f + i * (2f * hw - 0.12f) / 5f;
            Board(floor, new Vector3(x, 0, -hd), new Vector3(x, 0, hd), 0.06f, 0.05f, 0.09f, i);
        }
        floor.GenerateNormals();
        Add(list, 0, PaleWood, floor.Commit());

        // Side boards: three to a side, just outside the posts.
        var walls = Begin();
        for (int row = 0; row < 3; row++)
        {
            float y0 = 0.1f + row * 0.12f, y1 = y0 + 0.11f;
            foreach (float z in new[] { -hd, hd })
                Board(walls, new Vector3(-hw - 0.02f, 0, z), new Vector3(hw + 0.02f, 0, z), 0.025f, y0, y1, row * 7 + (z > 0 ? 1 : 2));
            foreach (float x in new[] { -hw, hw })
                Board(walls, new Vector3(x, 0, -hd), new Vector3(x, 0, hd), 0.025f, y0 + 0.01f, y1 + 0.01f, row * 7 + (x > 0 ? 3 : 4));
        }
        walls.GenerateNormals();
        Add(list, 1, PaleWood, walls.Commit());

        // The lid: two boards laid along the length with a little overhang, and two battens across.
        var lid = Begin();
        foreach (float z in new[] { -hd * 0.5f, hd * 0.5f })
            Board(lid, new Vector3(-hw - 0.05f, 0, z), new Vector3(hw + 0.05f, 0, z), hd * 0.5f + 0.015f, h, h + 0.06f, z > 0 ? 5 : 6);
        lid.GenerateNormals();
        Add(list, 1, PaleWood, lid.Commit());
        var battens = Begin();
        foreach (float x in new[] { -hw + 0.08f, hw - 0.08f })
            Board(battens, new Vector3(x, 0, -hd - 0.03f), new Vector3(x, 0, hd + 0.03f), 0.03f, h + 0.06f, h + 0.09f, x > 0 ? 8 : 9);
        battens.GenerateNormals();
        Add(list, 1, Wood, battens.Commit());
        return list;
    }
}
