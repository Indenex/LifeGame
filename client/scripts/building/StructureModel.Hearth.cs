using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A raised hearth (building-9): a ring of fieldstones laid on the floor, then a low
// clay surround smoothed over and around them, with a shallow bowl where the fire sits.
public static partial class StructureModel
{
    static List<Piece> Hearth()
    {
        var list = new List<Piece>();
        float r = Structures.HearthRadius;
        var corners = new Vector3[8];
        for (int i = 0; i < corners.Length; i++)
            corners[i] = Around(r + 0.05f, i * Mathf.Tau / corners.Length, 0);
        Site(list, corners);

        // Stone ring: eight stones set edge to edge around the fire bed.
        const int stones = 8;
        for (int i = 0; i < stones; i++)
        {
            float a = i * Mathf.Tau / stones + (Hash(i, 51) - 0.5f) * 0.15f;
            var size = new Vector3(0.2f + Hash(i, 52) * 0.06f, 0.13f + Hash(i, 53) * 0.04f, 0.16f);
            Add(list, 0, Stone, Rock(Around(r - 0.06f, a, 0.06f), size, 60 + i));
        }

        // Clay surround: a low wall around the stones, open nowhere, and a clay floor in the bowl.
        Add(list, 1, Clay, Wall(r, 0.16f, 0f, 0.22f, 0f, Mathf.Tau, top: true));
        Add(list, 1, Clay, Cone(r - 0.06f, 0.05f, 0.05f, 0.03f, 16, 9));
        return list;
    }
}
