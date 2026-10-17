using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-31: Use at a finished trading stall opens its window (StallPanel).
public partial class SocialLayer
{
    readonly StallPanel stall = new();

    void ReadyStall() => AddChild(stall);

    void AttachStall(DbConnection c)
    {
        stall.Conn = c;
        stall.Keeps = id => c.Db.Structure.Id.Find(id) is { } s
            && c.Db.Claim.Iter().Any(cl => cl.ExpiresAt.MicrosecondsSinceUnixEpoch > NowMicros && Social.InClaim(cl.X, cl.Z, s.X, s.Z) && IsOwnerOrMember(cl));
        System.Action dirty = stall.MarkDirty;
        c.Db.StallLot.OnInsert += (_, _) => dirty(); c.Db.StallLot.OnUpdate += (_, _, _) => dirty(); c.Db.StallLot.OnDelete += (_, _) => dirty();
        c.Db.MyStallTill.OnInsert += (_, _) => dirty(); c.Db.MyStallTill.OnUpdate += (_, _, _) => dirty(); c.Db.MyStallTill.OnDelete += (_, _) => dirty();
        c.Db.MyStallSales.OnInsert += (_, _) => dirty(); c.Db.MyStallSales.OnDelete += (_, _) => dirty();
        c.Db.MyInventory.OnInsert += (_, _) => dirty(); c.Db.MyInventory.OnUpdate += (_, _, _) => dirty(); c.Db.MyInventory.OnDelete += (_, _) => dirty();

        // G at a finished stall, just inside the server's reach (as a basket opens).
        Use.Register(new UseHandler
        {
            Id = "claims.stall",
            Priority = 27,
            Find = at =>
            {
                Structure? best = null;
                float bestD = float.MaxValue;
                foreach (var s in c.Db.Structure.Iter())
                {
                    if (s.Kind != StructureKind.Stall || !s.Complete) continue;
                    if (!Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, at.X, at.Z, Structures.WorkReach - 0.2f)) continue;
                    float d = new Vector2(s.X - at.X, s.Z - at.Z).Length();
                    if (d < bestD) { best = s; bestD = d; }
                }
                if (best is not { } found || stall.IsOpen) return null;
                return new UseOffer("Look at the stall", bestD, () => stall.OpenAt(found.Id), Target: new Vector3(found.X, at.Y, found.Z));
            },
        });
    }

    // Walking away closes the stall's window.
    void TrackStall()
    {
        if (!stall.IsOpen || conn == null || stall.OpenId is not { } id) return;
        if (conn.Db.Structure.Id.Find(id) is not { } s || !Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, myPos.X, myPos.Z, Structures.WorkReach + 0.5f))
            stall.Close();
    }
}
