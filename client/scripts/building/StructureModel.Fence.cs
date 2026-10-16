using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A stick fence (building-8): two posts driven in near the ends, then two rails
// lashed across them. A gate has the same posts with lashings for hinges; its
// woven hurdle is hung by StructureView like a hut door (GateDoor).
public static partial class StructureModel
{
    const float FencePostX = 0.94f, FenceTop = 1.05f, GateHeight = 0.98f;

    // The gate's hurdle: hinged on the left post, closing onto the right one.
    public static (Vector3 a, Vector3 b, float height) GateDoor =>
        (new Vector3(-FencePostX + 0.06f, 0.05f, 0), new Vector3(FencePostX - 0.06f, 0.05f, 0), GateHeight);

    static List<Piece> Fence(bool gate)
    {
        var list = new List<Piece>();
        float l = Structures.FenceHalfLength;
        Site(list, new[] { new Vector3(-l, 0, 0.12f), new Vector3(l, 0, 0.12f), new Vector3(l, 0, -0.12f), new Vector3(-l, 0, -0.12f) });

        // Posts, a little crooked, sunk into the ground.
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -FencePostX : FencePostX, lean = (Hash(i, 31) - 0.5f) * 0.06f;
            Add(list, 0, Wood, Stick(new Vector3(x, -0.3f, 0), new Vector3(x + lean, FenceTop + (gate ? 0.12f : 0f), lean * 0.5f), 0.055f));
        }

        if (gate)
        {
            // Fiber hinges on the left post and a loop on the right one to hold it shut.
            foreach (float y in new[] { 0.25f, 0.85f })
                Add(list, 1, Fiber, Wrap(new Vector3(-FencePostX, y, 0), Vector3.Up, 0.075f, 0.1f));
            Add(list, 1, Fiber, Wrap(new Vector3(FencePostX, 0.55f, 0), Vector3.Up, 0.075f, 0.1f));
            return list;
        }

        // Two rails, overhanging the posts a little so runs overlap where they meet.
        foreach (float y in new[] { 0.42f, 0.86f })
        {
            float sag = (Hash((int)(y * 10), 37) - 0.5f) * 0.04f;
            Add(list, 1, Wood, Stick(new Vector3(-l - 0.06f, y + sag, 0.06f), new Vector3(l + 0.06f, y - sag, 0.06f), 0.04f));
            foreach (float x in new[] { -FencePostX, FencePostX })
                Add(list, 1, Fiber, Wrap(new Vector3(x, y, 0.03f), Vector3.Up, 0.07f, 0.08f));
        }
        return list;
    }
}
