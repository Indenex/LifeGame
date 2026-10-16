// Where boulders lie, shared so the server can keep fires and other placed things
// off them. The client draws the same boulders (client/scripts/Terrain.cs): a cell
// that doesn't grow a tree has a small extra chance of a boulder instead.
using System;

namespace LifeGame.Shared
{
    public static class Rocks
    {
        // Extra chance on top of the forest roll, matching Terrain.cs.
        const float Chance = 0.006f;

        // Whether a boulder lies on this cell before anyone digs or builds there.
        public static bool LiesAt(int x, int z)
        {
            if (World.Height(x, z, 0) < World.WaterLevel + 0.1f) return false;
            float roll = Trees.Hash01(x, z, 11), forest = Trees.Forest(x, z);
            return roll < forest + Chance && (Trees.NearSpawn(x, z) || roll >= forest);
        }

        // The boulder's middle: jittered within its cell like a tree trunk.
        public static (float X, float Z) Center(int x, int z) => Trees.Trunk(x, z);

        // How far the boulder reaches from its middle, in meters: the mesh's main
        // lump and side lobe span about 0.73 of its scale.
        public static float Radius(int x, int z) => 0.73f * 0.55f * (1.3f + Trees.Hash01(x, z, 15) * 0.6f);
    }
}
