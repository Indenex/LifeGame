// Lets check bots walk like players (bugfix-34): the server refuses steps into tree trunks and
// boulders, so a bot that walks straight lines wedges on them. A bot slides along the edge with
// the same shared rule (Obstacles) the game client uses. Linked into each tool with
// <Compile Include="../common/*.cs" LinkBase="Common" />.
using LifeGame.Shared;
using SpacetimeDB.Types;


public sealed class BotGround : ICellGround
{
    readonly DbConnection conn;

    public BotGround(DbConnection conn) => this.conn = conn;

    public float TileHeight(int x, int z) =>
        World.Height(x, z, conn.Db.TerrainEdit.CellKey.Find(World.CellKey(x, z)) is { } edit ? edit.Depth : (short)0);

    public bool Dug(int x, int z) => conn.Db.TerrainEdit.CellKey.Find(World.CellKey(x, z)) != null;

    public bool Cleared(int x, int z)
    {
        if (conn.Db.ChoppedTree.CellKey.Find(World.CellKey(x, z)) is { Felled: true }) return true;
        foreach (var s in conn.Db.Structure.Iter())
            if (Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, x, z, 0.4f)) return true;
        return false;
    }

    // Where a bot ends up after trying to step to (x, z): pushed out of any trunk or boulder, as a
    // player is. The body is the game client's (0.3), wider than the server's, so the server never refuses it.
    public (float X, float Z) Slide(float x, float z) => Obstacles.PushOut(this, Obstacles.WalkerRadius, x, z, 0f);
}
