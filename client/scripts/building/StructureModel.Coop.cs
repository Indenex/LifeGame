using System.Collections.Generic;
using Godot;

namespace LifeGame;

// A hen coop (building-16; model by Modeling, model-7): a little woven-wattle hen house raised
// on four short legs off the damp ground, a framed pop-hole in the front (+Z) with a stick ramp
// down to the grass, a roost bar inside, and a steep thatched roof with deep eaves. Hens walk to
// the ramp foot (Entry, local (0, 1.5)) and roost on the bar (Roost, local (0, -0.1)); both are
// Shapes entries, so the model only has to agree with them. Footprint 1.8 x 1.4 m.
// Stages: 0 frame (legs, sills, floor, roost bar, stakes, pop-hole, ramp), 1 wattle walls,
// 2 thatch roof (ridge, gables, thatch).
public static partial class StructureModel
{
    static List<Piece> Coop()
    {
        var list = new List<Piece>();
        const float hw = 0.9f, hd = 0.7f;
        const float w = 0.84f, d = 0.64f, floorY = 0.3f, eaveY = 0.95f, ridgeY = 1.38f;
        const float holeW = 0.15f, holeTop = floorY + 0.32f;
        const float entryZ = 1.5f, roostZ = -0.1f;
        Site(list, new[] { new Vector3(-hw - 0.1f, 0, hd + 0.9f), new Vector3(hw + 0.1f, 0, hd + 0.9f), new Vector3(hw + 0.1f, 0, -hd - 0.1f), new Vector3(-hw - 0.1f, 0, -hd - 0.1f) });

        // Frame: legs up to the eaves, sill logs, a floor of split poles, the roost bar.
        foreach (var (x, z) in new[] { (-w, d), (w, d), (w, -d), (-w, -d) })
        {
            float lean = (Hash((int)(x * 10 + z * 7), 41) - 0.5f) * 0.03f;
            Add(list, 0, Wood, Stick(new Vector3(x, -0.15f, z), new Vector3(x + lean, eaveY + 0.04f, z), 0.042f));
        }
        foreach (float z in new[] { -d, d })
            Add(list, 0, Wood, Stick(new Vector3(-w - 0.07f, floorY - 0.04f, z), new Vector3(w + 0.07f, floorY - 0.03f, z), 0.05f));
        for (int i = 0; i < 10; i++)
        {
            float x = -w + 0.07f + i * (2f * w - 0.14f) / 9f;
            Add(list, 0, PaleWood, Stick(new Vector3(x, floorY + 0.01f, -d - 0.02f), new Vector3(x + 0.01f, floorY + 0.01f, d + 0.02f), 0.03f));
        }
        // The roost bar across the inside, on two short forked props.
        foreach (float x in new[] { -0.62f, 0.62f })
            Add(list, 0, Wood, Stick(new Vector3(x, floorY, roostZ), new Vector3(x, floorY + 0.3f, roostZ), 0.022f, fork: true));
        Add(list, 0, Wood, Stick(new Vector3(-0.7f, floorY + 0.29f, roostZ), new Vector3(0.7f, floorY + 0.3f, roostZ), 0.026f));

        // Upright stakes for the wattle, all round; the front leaves the pop-hole open.
        int Stakes(float len) => Mathf.Max(3, Mathf.RoundToInt(len / 0.2f));
        void StakeRow(Vector3 from, Vector3 to, bool front)
        {
            int n = Stakes(from.DistanceTo(to));
            for (int i = 1; i < n; i++)
            {
                var at = from.Lerp(to, i / (float)n);
                if (front && Mathf.Abs(at.X) < holeW + 0.02f) continue;
                Add(list, 0, Wood, Stick(at + new Vector3(0, floorY - 0.02f, 0), at + new Vector3(0, eaveY + 0.02f, 0), 0.018f));
            }
        }
        StakeRow(new Vector3(-w, 0, d), new Vector3(w, 0, d), true);
        StakeRow(new Vector3(w, 0, -d), new Vector3(-w, 0, -d), false);
        StakeRow(new Vector3(-w, 0, -d), new Vector3(-w, 0, d), false);
        StakeRow(new Vector3(w, 0, d), new Vector3(w, 0, -d), false);

        // The pop-hole: two posts, a lintel and a sill; a ramp of rungs down to the Entry point.
        foreach (float x in new[] { -holeW - 0.02f, holeW + 0.02f })
            Add(list, 0, Wood, Stick(new Vector3(x, floorY, d + 0.02f), new Vector3(x, holeTop + 0.03f, d + 0.02f), 0.026f));
        Add(list, 0, Wood, Stick(new Vector3(-holeW - 0.08f, holeTop, d + 0.03f), new Vector3(holeW + 0.08f, holeTop + 0.01f, d + 0.03f), 0.028f));
        var rampTop = new Vector3(0, floorY + 0.02f, d + 0.03f);
        var rampFoot = new Vector3(0, 0.01f, entryZ - 0.05f);
        foreach (float x in new[] { -0.1f, 0.1f })
            Add(list, 0, PaleWood, Stick(rampTop + new Vector3(x, 0, 0), rampFoot + new Vector3(x, 0, 0), 0.02f));
        for (int i = 1; i < 8; i++)
        {
            var at = rampTop.Lerp(rampFoot, i / 8f);
            Add(list, 0, Wood, Stick(at + new Vector3(-0.12f, 0.015f, 0), at + new Vector3(0.12f, 0.015f, 0), 0.012f));
        }

        // Wattle walls: rods woven in and out between the stakes, a row at a time.
        void Wall(Vector3 from, Vector3 to, Vector3 outward, bool front, int seed)
        {
            int n = Stakes(from.DistanceTo(to));
            const int rows = 14;
            for (int r = 0; r < rows; r++)
            {
                float y = floorY + 0.05f + r * (eaveY - floorY - 0.08f) / (rows - 1);
                for (int i = 0; i < n; i++)
                {
                    var a = from.Lerp(to, i / (float)n);
                    var b = from.Lerp(to, (i + 1) / (float)n);
                    if (front && y < holeTop && Mathf.Abs((a.X + b.X) * 0.5f) < holeW + 0.06f) continue;
                    float weave = ((i + r) % 2 == 0 ? 1f : -1f) * 0.016f;
                    bool pale = Hash(r * 13 + i, seed) > 0.9f;
                    Add(list, 1, pale ? PaleWood : Wood, Stick(a + new Vector3(0, y, 0) + outward * weave, b + new Vector3(0, y + (Hash(r, i + seed) - 0.5f) * 0.012f, 0) - outward * weave, 0.022f));
                }
            }
        }
        Wall(new Vector3(-w, 0, d), new Vector3(w, 0, d), Vector3.Back, true, 1);
        Wall(new Vector3(w, 0, -d), new Vector3(-w, 0, -d), Vector3.Forward, false, 2);
        Wall(new Vector3(-w, 0, -d), new Vector3(-w, 0, d), Vector3.Left, false, 3);
        Wall(new Vector3(w, 0, d), new Vector3(w, 0, -d), Vector3.Right, false, 4);
        foreach (float y in new[] { floorY + 0.12f, floorY + 0.4f })
            Add(list, 1, Fiber, Wrap(new Vector3(-w, y, d), Vector3.Up, 0.06f, 0.08f));

        // Roof: a ridge pole on crossed gable sticks at each end, the gables filled with rods,
        // then deep thatch sloping front and back, its front eave over the pop-hole.
        foreach (float x in new[] { -w, w })
        {
            Add(list, 2, Wood, Stick(new Vector3(x, eaveY - 0.04f, -d - 0.05f), new Vector3(x, ridgeY + 0.12f, 0.08f), 0.03f));
            Add(list, 2, Wood, Stick(new Vector3(x, eaveY - 0.04f, d + 0.05f), new Vector3(x, ridgeY + 0.12f, -0.08f), 0.03f));
            for (int r = 0; r < 12; r++)
            {
                float y = eaveY + 0.03f + r * 0.04f;
                float half = d * (1f - (y - eaveY) / (ridgeY - eaveY)) - 0.02f;
                if (half < 0.04f) break;
                Add(list, 2, Wood, Stick(new Vector3(x, y, -half), new Vector3(x, y + 0.008f, half), 0.02f));
            }
        }
        Add(list, 2, Wood, Stick(new Vector3(-w - 0.1f, ridgeY, 0), new Vector3(w + 0.1f, ridgeY, 0), 0.035f));
        Add(list, 2, Thatch, ThatchRow(new Vector3(0, eaveY - 0.08f, -d - 0.18f), new Vector3(0, ridgeY + 0.06f, 0), 2f * w + 0.32f, 0.11f, 21));
        Add(list, 2, Thatch, ThatchRow(new Vector3(0, eaveY - 0.08f, d + 0.18f), new Vector3(0, ridgeY + 0.06f, 0), 2f * w + 0.32f, 0.11f, 22));
        Add(list, 2, Fiber, Wrap(new Vector3(0, ridgeY + 0.08f, 0), Vector3.Right, 0.06f, 2f * w + 0.2f, lumpy: true));
        return list;
    }
}
