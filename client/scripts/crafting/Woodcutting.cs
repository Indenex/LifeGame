using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Chopping logs from trees (chop.log, server/spacetimedb/Woodcutting.cs). G next to a
// tree with an axe in the bag starts a chop. Felled trees are drawn as cleared
// cells, so they topple and later grow back with the forest's own animation.
public partial class Woodcutting : Node
{
    readonly HashSet<long> felled = new();
    DbConnection? conn;
    Terrain? terrain;

    public void Attach(DbConnection conn, Terrain terrain, CraftingUi crafting)
    {
        this.conn = conn;
        this.terrain = terrain;
        var before = terrain.Cleared;
        terrain.Cleared = (x, z) => before?.Invoke(x, z) == true || felled.Contains(World.CellKey(x, z));
        conn.Db.ChoppedTree.OnInsert += (_, t) => Changed(t);
        conn.Db.ChoppedTree.OnUpdate += (_, _, t) => Changed(t);
        conn.Db.ChoppedTree.OnDelete += (_, t) => { if (felled.Remove(t.CellKey)) terrain.Refresh(t.CellX, t.CellZ); };

        var chop = Processes.Find("chop.log")!;
        Use.Register(new UseHandler
        {
            Id = "crafting.chop",
            Priority = 10,
            Find = at =>
            {
                if (!HasAxe() || TreeNear(at) is not { } tree) return null;
                float d = new Vector2(tree.X - at.X, tree.Y - at.Z).Length();
                return new UseOffer(chop.Name, d, () => crafting.Start(chop), Target: new Vector3(tree.X, at.Y, tree.Y), Again: 0.3f);
            },
        });
    }

    void Changed(ChoppedTree t)
    {
        if (t.Felled && felled.Add(t.CellKey)) terrain?.Refresh(t.CellX, t.CellZ);
    }

    // Only an axe that is equipped and still works chops (crafting-29, Equipped.cs); otherwise
    // the tree is not offered and E goes to the loot near it.
    bool HasAxe() => Equipped.Has(conn, ItemTags.Chopping);

    // The trunk of the nearest standing tree within reach, as the server will pick it.
    Vector2? TreeNear(Vector3 at)
    {
        if (terrain == null) return null;
        int cx = Mathf.RoundToInt(at.X), cz = Mathf.RoundToInt(at.Z);
        Vector2? best = null;
        float bestD = World.Reach;
        for (int i = cx - 3; i <= cx + 3; i++)
            for (int j = cz - 3; j <= cz + 3; j++)
            {
                if (!Trees.GrowsAt(i, j) || terrain.Cleared?.Invoke(i, j) == true) continue;
                if (terrain.TileHeight(i, j) != World.Height(i, j, 0)) continue;
                var (tx, tz) = Trees.Trunk(i, j);
                float d = new Vector2(tx - at.X, tz - at.Z).Length();
                if (d < bestD) { bestD = d; best = new Vector2(tx, tz); }
            }
        return best;
    }
}
