using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The straw bed (building-23), in two stages: 0 the frame (four legs, side and end rails, a headboard of two
// taller posts and a rail, slats across), 1 the straw tick (rolls of straw laid across, and a bolster at the
// head). The head is at -Z. Wide enough for two to lie side by side.
public static partial class StructureModel
{
    static List<Piece> Bed()
    {
        var list = new List<Piece>();
        const float hw = 0.55f, hl = 0.9f, seat = 0.28f, headTop = 0.66f;
        Site(list, new[] { new Vector3(-0.78f, 0, 1.12f), new Vector3(0.78f, 0, 1.12f), new Vector3(0.78f, 0, -1.12f), new Vector3(-0.78f, 0, -1.12f) });

        // Legs, the head posts rising higher.
        foreach (var (x, z) in new[] { (-hw, hl), (hw, hl), (hw, -hl), (-hw, -hl) })
        {
            float top = z < 0 ? headTop : seat + 0.02f;
            float lean = (Hash((int)(x * 10 + z * 7), 51) - 0.5f) * 0.02f;
            Add(list, 0, Wood, Stick(new Vector3(x, -0.03f, z), new Vector3(x + lean, top, z), 0.045f));
        }
        // Side rails along the length, end rails across, and a headboard rail with a lower one under it.
        foreach (float x in new[] { -hw, hw })
            Add(list, 0, Wood, Stick(new Vector3(x, seat - 0.02f, -hl - 0.06f), new Vector3(x, seat - 0.02f, hl + 0.06f), 0.04f));
        Add(list, 0, Wood, Stick(new Vector3(-hw - 0.06f, seat - 0.02f, hl), new Vector3(hw + 0.06f, seat - 0.02f, hl), 0.04f));
        Add(list, 0, Wood, Stick(new Vector3(-hw - 0.06f, seat - 0.02f, -hl), new Vector3(hw + 0.06f, seat - 0.02f, -hl), 0.04f));
        Add(list, 0, Wood, Stick(new Vector3(-hw - 0.05f, headTop - 0.03f, -hl), new Vector3(hw + 0.05f, headTop - 0.02f, -hl), 0.04f));
        Add(list, 0, Wood, Stick(new Vector3(-hw, headTop - 0.2f, -hl), new Vector3(hw, headTop - 0.19f, -hl), 0.03f));
        // Slats across the rails for the tick to lie on.
        for (int i = 0; i < 9; i++)
        {
            float z = -hl + 0.12f + i * (2f * hl - 0.24f) / 8f;
            Add(list, 0, PaleWood, Stick(new Vector3(-hw - 0.03f, seat + 0.01f, z), new Vector3(hw + 0.03f, seat + 0.01f + (Hash(i, 52) - 0.5f) * 0.01f, z), 0.028f));
        }

        // The straw tick: fat rolls of straw laid across, each a little different, and a bolster at the head.
        for (int i = 0; i < 9; i++)
        {
            float z = -hl + 0.16f + i * (2f * hl - 0.32f) / 8f;
            float r = 0.095f + Hash(i, 53) * 0.015f, tilt = (Hash(i, 54) - 0.5f) * 0.03f;
            Add(list, 1, Thatch, Stick(new Vector3(-hw + 0.02f, seat + 0.1f, z), new Vector3(hw - 0.02f, seat + 0.1f + tilt, z), r));
        }
        Add(list, 1, Thatch, Stick(new Vector3(-hw + 0.1f, seat + 0.27f, -hl + 0.18f), new Vector3(hw - 0.1f, seat + 0.27f, -hl + 0.18f), 0.1f));
        return list;
    }
}
