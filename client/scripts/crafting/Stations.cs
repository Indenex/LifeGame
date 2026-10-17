using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Crafting stations on the client (D-098, D-112), matching server/spacetimedb/Stations.cs:
// which finished station the player stands at, and what a station process can draw
// on beyond the bag (storage near the station and the claim's stash). The server
// has the last word on claim rights.
public static class Stations
{
    // The nearest finished station of this kind within working reach of a point.
    public static Structure? NearestOf(DbConnection conn, StationKind kind, Vector3 at)
    {
        Structure? best = null;
        float bestD = float.MaxValue;
        foreach (var s in conn.Db.Structure.Iter())
        {
            if (!s.Complete || Structures.Shape((int)s.Kind).Station != kind) continue;
            float d = new Vector2(s.X - at.X, s.Z - at.Z).Length();
            if (d - Radius(s) > Structures.WorkReach || d >= bestD) continue;
            best = s;
            bestD = d;
        }
        return best;
    }

    // The player's own position as the server last saw it, or null before it is known.
    public static Vector3? PlayerAt(DbConnection conn) =>
        Me.Key(conn) is { } id && conn.Db.Player.Identity.Find(id) is { } p && conn.Db.PlayerPos.PlayerId.Find(p.PlayerId) is { } pos
            ? new Vector3(pos.X, 0, pos.Z) : null;

    // The station of this kind the player stands at, if any.
    public static Structure? AtPlayer(DbConnection conn, StationKind kind) =>
        PlayerAt(conn) is { } at ? NearestOf(conn, kind, at) : null;

    // How many of an item sit in storage near a station and in its claim's stash.
    public static uint StoredNear(DbConnection conn, Structure station, Item item)
    {
        uint n = 0;
        foreach (var store in conn.Db.Structure.Iter())
        {
            if (!store.Complete || !Structures.IsStore((int)store.Kind)) continue;
            if (new Vector2(store.X - station.X, store.Z - station.Z).Length() > StationInfo.StorageReach) continue;
            foreach (var row in conn.Db.BasketItem.Iter())
                if (row.StructureId == store.Id && row.Item == (ushort)item) n += row.Count;
        }
        var claim = conn.Db.Claim.Iter().FirstOrDefault(c => Social.InClaim(c.X, c.Z, station.X, station.Z));
        if (claim != null)
            foreach (var row in conn.Db.MyClaimStash.Iter())
                if (row.ClaimId == claim.Id && row.Item == (ushort)item) n += row.Count;
        return n;
    }

    static float Radius(Structure s)
    {
        var shape = Structures.Shape((int)s.Kind);
        return shape.Round ? shape.Radius : Mathf.Max(shape.HalfWidth, shape.HalfDepth);
    }
}
