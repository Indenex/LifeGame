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

        // Log walls: alternate courses run along the side walls and the front and back,
        // each log overhanging the corner. The front stops either side of the doorway.
        float course = CabinLog * 0.85f;
        int courses = Mathf.CeilToInt((HouseWallTop - 0.28f) / course);
        for (int c = 0; c < courses; c++)
        {
            float y = 0.28f + c * course;
            float r = CabinLog * 0.5f * (0.92f + Hash(c, 71) * 0.14f);
            bool sides = c % 2 == 0;
            if (sides)
                foreach (float x in new[] { -wx, wx })
                    Add(list, 1, Wood, Stick(new Vector3(x, y, -wz - CabinOverhang), new Vector3(x, y + 0.01f, wz + CabinOverhang), r));
            else
            {
                Add(list, 1, Wood, Stick(new Vector3(-wx - CabinOverhang, y, -wz), new Vector3(wx + CabinOverhang, y + 0.01f, -wz), r));
                if (y >= HouseDoorTop)
                    Add(list, 1, Wood, Stick(new Vector3(-wx - CabinOverhang, y, wz), new Vector3(wx + CabinOverhang, y + 0.01f, wz), r));
                else
                {
                    Add(list, 1, Wood, Stick(new Vector3(-wx - CabinOverhang, y, wz), new Vector3(-HouseDoorHalf - 0.04f, y, wz), r));
                    Add(list, 1, Wood, Stick(new Vector3(HouseDoorHalf + 0.04f, y, wz), new Vector3(wx + CabinOverhang, y + 0.01f, wz), r));
                }
            }
        }
        // Door posts and a lintel log, so the course ends have something to butt against.
        foreach (float x in new[] { HouseDoorHalf + 0.04f, -HouseDoorHalf - 0.04f })
            Add(list, 1, Wood, Stick(new Vector3(x, 0.1f, wz), new Vector3(x, HouseDoorTop + 0.05f, wz + 0.01f), 0.07f));
        Add(list, 1, Wood, Stick(new Vector3(-HouseDoorHalf - 0.35f, HouseDoorTop + 0.06f, wz + 0.02f), new Vector3(HouseDoorHalf + 0.35f, HouseDoorTop + 0.07f, wz + 0.02f), 0.1f));

        HouseRoof(list, CabinLog * 1.4f, Wood);
        return list;
    }
}
