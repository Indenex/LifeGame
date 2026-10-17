using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The wooden chest (building-22), in two stages: 0 the frame (four corner posts and the bottom boards),
// 1 the boards and the lid (a core, lapped board courses with dowel pegs, a lid with cleats and a toggle).
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

        // Stage 1: a dark core, then four courses of pale boards with the corners lapped like a log cabin's (the
        // front and back boards run long on one course, the end boards on the next), and dowel pegs through them.
        var core = Begin();
        Board(core, new Vector3(-hw + 0.03f, 0, 0), new Vector3(hw - 0.03f, 0, 0), hd - 0.03f, 0.08f, h, 11);
        core.GenerateNormals();
        Add(list, 1, Wood, core.Commit());
        var walls = Begin();
        for (int row = 0; row < 4; row++)
        {
            float y0 = 0.1f + row * 0.09f, y1 = y0 + 0.085f;
            bool longFront = row % 2 == 0;
            float fl = longFront ? hw + 0.04f : hw - 0.03f, el = longFront ? hd - 0.03f : hd + 0.04f;
            foreach (float z in new[] { -hd, hd })
                Board(walls, new Vector3(-fl, 0, z), new Vector3(fl, 0, z), 0.026f, y0, y1, row * 7 + (z > 0 ? 1 : 2));
            foreach (float x in new[] { -hw, hw })
                Board(walls, new Vector3(x, 0, -el), new Vector3(x, 0, el), 0.026f, y0, y1, row * 7 + (x > 0 ? 3 : 4));
        }
        walls.GenerateNormals();
        Add(list, 1, PaleWood, walls.Commit());
        foreach (var (px, pz) in new[] { (-hw + 0.07f, hd), (hw - 0.07f, hd), (-hw + 0.07f, -hd), (hw - 0.07f, -hd) })
            foreach (float py in new[] { 0.19f, 0.37f })
            {
                float dir = pz > 0 ? 1f : -1f;
                Add(list, 1, Wood, Stick(new Vector3(px, py, pz - dir * 0.03f), new Vector3(px, py, pz + dir * 0.075f), 0.014f));
            }

        // The lid: a broad board lid with a narrower crown board on top, cleats across both ends, and a
        // wooden toggle on a cordage loop at the front.
        var lid = Begin();
        foreach (float z in new[] { -hd * 0.5f, hd * 0.5f })
            Board(lid, new Vector3(-hw - 0.05f, 0, z), new Vector3(hw + 0.05f, 0, z), hd * 0.5f + 0.012f, h, h + 0.05f, z > 0 ? 5 : 6);
        Board(lid, new Vector3(-hw + 0.02f, 0, 0), new Vector3(hw - 0.02f, 0, 0), hd * 0.55f, h + 0.05f, h + 0.085f, 7);
        lid.GenerateNormals();
        Add(list, 1, PaleWood, lid.Commit());
        var cleats = Begin();
        foreach (float x in new[] { -hw - 0.01f, hw + 0.01f })
            Board(cleats, new Vector3(x, 0, -hd - 0.03f), new Vector3(x, 0, hd + 0.03f), 0.03f, h - 0.02f, h + 0.05f, x > 0 ? 8 : 9);
        cleats.GenerateNormals();
        Add(list, 1, Wood, cleats.Commit());
        Add(list, 1, Wood, Stick(new Vector3(0, h - 0.07f, hd + 0.045f), new Vector3(0.0f, h + 0.02f, hd + 0.045f), 0.02f));
        Add(list, 1, PaleWood, Stick(new Vector3(-0.07f, h + 0.045f, hd + 0.045f), new Vector3(0.07f, h + 0.045f, hd + 0.045f), 0.014f));
        return list;
    }
}
