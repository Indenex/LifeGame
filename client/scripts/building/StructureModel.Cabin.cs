using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The log cabin (building-10, D-151): the same room as the mud-brick house, built of
// logs. A sill of logs on fieldstones, walls of logs laid course on course with their
// ends crossing at the corners and a split-log lintel over the door, then the
// house's rafters and thatch with log gable ends. +Z is the door side.
public static partial class StructureModel
{
    const float CabinLog = 0.2f, CabinOverhang = 0.22f;

    // A log's radius: a touch over half a course, so stacked logs overlap a little and leave no gap.
    static float LogRadius(int seed) => CabinLog * 0.5f * (0.97f + Hash(seed, 71) * 0.08f);

    static List<Piece> LogCabin()
    {
        var list = new List<Piece>();
        float hw = Structures.HouseWallX + 0.2f, hd = Structures.HouseWallZ + 0.2f, wx = Structures.HouseWallX, wz = Structures.HouseWallZ;
        Site(list, new[] { new Vector3(-hw, 0, hd), new Vector3(hw, 0, hd), new Vector3(hw, 0, -hd), new Vector3(-hw, 0, -hd) });

        // Sill: a stone under each corner and two sill logs along the long walls.
        int seed = 0;
        foreach (float x in new[] { -wx, wx })
            foreach (float z in new[] { -wz, wz })
                Add(list, 0, Stone, Rock(new Vector3(x, 0.03f, z), new Vector3(0.3f, 0.14f, 0.3f), 70 + seed++));
        Add(list, 0, Wood, Stick(new Vector3(-wx - CabinOverhang, 0.17f, -wz), new Vector3(wx + CabinOverhang, 0.18f, -wz), CabinLog * 0.55f));
        // The front sill log stops either side of the doorway, so nothing lies across the way in (building-14).
        Add(list, 0, Wood, Stick(new Vector3(-wx - CabinOverhang, 0.17f, wz), new Vector3(-HouseDoorHalf - 0.04f, 0.17f, wz), CabinLog * 0.55f));
        Add(list, 0, Wood, Stick(new Vector3(HouseDoorHalf + 0.04f, 0.17f, wz), new Vector3(wx + CabinOverhang, 0.18f, wz), CabinLog * 0.55f));

        // Log walls stacked tight (bugfix-61): each wall gets a log every course, the side walls
        // half a log higher than the front and back, so the ends cross at the corners and no
        // daylight shows between courses. The front and back start on the sill logs; the side
        // walls on a low log each between the corner stones. The front stops either side of the doorway.
        const float course = CabinLog * 0.9f;
        foreach (float x in new[] { -wx, wx })
            Add(list, 0, Wood, Stick(new Vector3(x, 0.08f, -wz + 0.1f), new Vector3(x, 0.08f, wz - 0.1f), CabinLog * 0.5f));
        for (int c = 0; 0.17f + c * course <= HouseWallTop; c++)
        {
            float ySide = 0.17f + course * 0.5f + c * course, yEnd = 0.17f + course * (c + 1);
            if (ySide <= HouseWallTop)
                foreach (float x in new[] { -wx, wx })
                    Add(list, 1, Wood, Stick(new Vector3(x, ySide, -wz - CabinOverhang), new Vector3(x, ySide + 0.01f, wz + CabinOverhang), LogRadius(2 * c)));
            if (yEnd > HouseWallTop) continue;
            float r = LogRadius(2 * c + 1);
            Add(list, 1, Wood, Stick(new Vector3(-wx - CabinOverhang, yEnd, -wz), new Vector3(wx + CabinOverhang, yEnd + 0.01f, -wz), r));
            // A log whose underside clears the door head runs across; lower ones stop at the posts.
            if (yEnd - r >= HouseDoorTop)
                Add(list, 1, Wood, Stick(new Vector3(-wx - CabinOverhang, yEnd, wz), new Vector3(wx + CabinOverhang, yEnd + 0.01f, wz), r));
            else
            {
                Add(list, 1, Wood, Stick(new Vector3(-wx - CabinOverhang, yEnd, wz), new Vector3(-HouseDoorHalf - 0.04f, yEnd, wz), r));
                Add(list, 1, Wood, Stick(new Vector3(HouseDoorHalf + 0.04f, yEnd, wz), new Vector3(wx + CabinOverhang, yEnd + 0.01f, wz), r));
            }
        }

        // A floor of split boards laid across the room (bugfix-63), low enough that a fire or hearth set on it still sits right.
        var floor = Begin();
        int boards = Mathf.CeilToInt((2f * wz - 0.2f) / 0.26f);
        for (int i = 0; i < boards; i++)
        {
            float z = -wz + 0.1f + (i + 0.5f) * (2f * wz - 0.2f) / boards;
            float k = 0.88f + Hash(i, 83) * 0.16f;
            OrientedBox(floor, new Vector3(-wx + 0.1f, 0, z), new Vector3(wx - 0.1f, 0, z), Vector3.Back, 0.125f, 0.01f, 0.05f + Hash(i, 84) * 0.012f, new Color(k, k * 0.96f, k * 0.9f));
        }
        floor.GenerateNormals();
        Add(list, 0, PaleWood, floor.Commit());

        // Door posts and a lintel log, so the course ends have something to butt against.
        foreach (float x in new[] { HouseDoorHalf + 0.04f, -HouseDoorHalf - 0.04f })
            Add(list, 1, Wood, Stick(new Vector3(x, 0.1f, wz), new Vector3(x, HouseDoorTop + 0.05f, wz + 0.01f), 0.07f));
        Add(list, 1, Wood, Stick(new Vector3(-HouseDoorHalf - 0.35f, HouseDoorTop + 0.06f, wz + 0.02f), new Vector3(HouseDoorHalf + 0.35f, HouseDoorTop + 0.07f, wz + 0.02f), 0.1f));

        HouseRoof(list, CabinLog * 1.4f, Wood);
        return list;
    }
}
