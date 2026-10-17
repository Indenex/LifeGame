using System.Collections.Generic;
using Godot;

namespace LifeGame;

// A hen coop (building-16): a little shed on legs with woven walls, a thatched roof sloping back, a
// pop-hole in the front (+Z) and a ramp down to the ground. Hens walk to the ramp foot (Entry) and
// roost on the bar inside (Roost); both are Shapes entries, so the model only has to agree with them.
public static partial class StructureModel
{
    static List<Piece> Coop()
    {
        var list = new List<Piece>();
        const float hw = 0.9f, hd = 0.7f, floorY = 0.3f, frontTop = 1.25f, rearTop = 0.98f;
        Site(list, new[] { new Vector3(-hw - 0.1f, 0, hd + 0.9f), new Vector3(hw + 0.1f, 0, hd + 0.9f), new Vector3(hw + 0.1f, 0, -hd - 0.1f), new Vector3(-hw - 0.1f, 0, -hd - 0.1f) });

        // Frame: four legs that run up to the roof line, a floor of sticks, the roost bar and a ramp.
        foreach (var (x, z, top) in new[] { (-hw, hd, frontTop), (hw, hd, frontTop), (-hw, -hd, rearTop), (hw, -hd, rearTop) })
        {
            float lean = (Hash((int)(x * 10 + z * 7), 41) - 0.5f) * 0.04f;
            Add(list, 0, Wood, Stick(new Vector3(x, -0.2f, z), new Vector3(x + lean, top, z), 0.05f));
        }
        for (int i = 0; i < 7; i++)
        {
            float z = -hd + 0.1f + i * (2f * hd - 0.2f) / 6f, j = (Hash(i, 43) - 0.5f) * 0.03f;
            Add(list, 0, Wood, Stick(new Vector3(-hw - 0.05f, floorY + j, z), new Vector3(hw + 0.05f, floorY - j, z), 0.03f));
        }
        Add(list, 0, Wood, Stick(new Vector3(-hw, floorY - 0.04f, hd), new Vector3(hw, floorY - 0.04f, hd), 0.045f));
        Add(list, 0, Wood, Stick(new Vector3(-hw, floorY - 0.04f, -hd), new Vector3(hw, floorY - 0.04f, -hd), 0.045f));
        Add(list, 0, Wood, Stick(new Vector3(-0.7f, floorY + 0.28f, -0.1f), new Vector3(0.7f, floorY + 0.3f, -0.1f), 0.028f));
        Add(list, 0, Wood, Stick(new Vector3(0.0f, 0.02f, hd + 0.75f), new Vector3(0.02f, floorY + 0.02f, hd + 0.02f), 0.04f));

        // Walls: woven rows of rods between the legs, with the pop-hole left open in the front.
        for (int r = 0; r < 6; r++)
        {
            float y = floorY + 0.12f + r * 0.13f, w = (r % 2 == 0 ? 1 : -1) * 0.015f;
            float rear = y > rearTop - 0.04f ? rearTop - 0.04f : y;
            Add(list, 1, Weave, Stick(new Vector3(-hw, rear, -hd + w), new Vector3(hw, rear + 0.01f, -hd - w), 0.026f));
            foreach (float x in new[] { -hw, hw })
                Add(list, 1, Weave, Stick(new Vector3(x + w, y, -hd), new Vector3(x - w, y + 0.01f, hd), 0.026f));
            bool beside = y < floorY + 0.58f;
            if (beside)
            {
                Add(list, 1, Weave, Stick(new Vector3(-hw, y, hd + w), new Vector3(-0.2f, y + 0.01f, hd - w), 0.026f));
                Add(list, 1, Weave, Stick(new Vector3(0.2f, y, hd + w), new Vector3(hw, y + 0.01f, hd - w), 0.026f));
            }
            else
                Add(list, 1, Weave, Stick(new Vector3(-hw, y, hd + w), new Vector3(hw, y + 0.01f, hd - w), 0.026f));
        }
        foreach (float y in new[] { floorY + 0.12f, floorY + 0.3f })
            Add(list, 1, Fiber, Wrap(new Vector3(-hw, y, hd), Vector3.Up, 0.065f, 0.08f));

        // Roof: thatch laid from the rear eave up to the front, in overlapping rows.
        var low = new Vector3(0, rearTop - 0.06f, -hd - 0.14f);
        var high = new Vector3(0, frontTop + 0.06f, hd + 0.16f);
        const int rows = 4;
        for (int r = 0; r < rows; r++)
        {
            float t0 = r / (float)rows - 0.04f, t1 = (r + 1) / (float)rows + 0.12f;
            Add(list, 2, Thatch, ThatchRow(low.Lerp(high, Mathf.Max(0, t0)), low.Lerp(high, Mathf.Min(1.04f, t1)), 2.35f, 0.1f + r * 0.01f, r + 20));
        }
        return list;
    }
}
