using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-7: handing things over. G at someone in front of you opens the give list;
// G at someone offering you something takes it. Nothing moves until it is taken.
public partial class SocialLayer
{
    readonly GivePanel give = new();

    // True while the claim panel or the give list is up (Main counts these as full panels).
    public bool PanelOpen => panel.IsOpen || give.IsOpen;

    void ReadyGiving() => AddChild(give);

    void AttachGiving()
    {
        give.Conn = conn;
        conn!.Db.MyInventory.OnInsert += (_, _) => give.MarkDirty();
        conn.Db.MyInventory.OnUpdate += (_, _, _) => give.MarkDirty();
        conn.Db.MyInventory.OnDelete += (_, _) => give.MarkDirty();
        conn.Db.MyGiftOffers.OnInsert += (_, o) => OnOffer(o);

        Use.Register(new UseHandler { Id = "claims.give", Priority = 4, Find = GiveOffer });
        // Above most things: when someone holds something out to you, G takes it.
        Use.Register(new UseHandler { Id = "claims.take", Priority = 40, Find = TakeOffer });
    }

    void OnOffer(GiftOffer o)
    {
        string what = GiftWhat(o);
        if (o.ToId == myId) hud.Note($"{o.FromName} offers you {what}   ·   G to take");
        else if (o.FromId == myId) hud.Note($"You offer {NameOf(o.ToId)} {what}");
    }

    // The nearest other player within reach, to hand something to.
    UseOffer? GiveOffer(Vector3 at)
    {
        if (conn == null || myId == 0 || give.IsOpen || conn.Db.MyInventory.Count == 0) return null;
        PlayerPos? best = null;
        float bestD = Social.GiveReach;
        foreach (var p in conn.Db.PlayerPos.Iter())
        {
            if (p.PlayerId == myId) continue;
            float d = new Vector2(p.X - at.X, p.Z - at.Z).Length();
            if (d > bestD || conn.Db.Player.PlayerId.Find(p.PlayerId) is not { Online: true }) continue;
            best = p;
            bestD = d;
        }
        if (best is not { } target) return null;
        uint id = target.PlayerId;
        string name = NameOf(id);
        return new UseOffer($"Give to {name}", bestD, () => give.Open(id, name), Target: new Vector3(target.X, at.Y, target.Z));
    }

    // A fresh offer to you from someone still close by.
    UseOffer? TakeOffer(Vector3 at)
    {
        if (conn == null || myId == 0) return null;
        long now = NowMicros;
        foreach (var o in conn.Db.MyGiftOffers.Iter())
        {
            if (o.ToId != myId || now - o.OfferedAt.MicrosecondsSinceUnixEpoch > Social.GiftKeepMicros) continue;
            if (conn.Db.PlayerPos.PlayerId.Find(o.FromId) is not { } from) continue;
            float d = new Vector2(from.X - at.X, from.Z - at.Z).Length();
            if (d > Social.GiveReach + Social.GiveReachSlack * 0.5f) continue;
            ulong offerId = o.Id;
            return new UseOffer($"Take {GiftWhat(o)} from {o.FromName}", d, () => conn.Reducers.AcceptGift(offerId), Target: new Vector3(from.X, at.Y, from.Z));
        }
        return null;
    }

    void ProcessGiving()
    {
        // Walking away from the person you were choosing for closes the list.
        if (!give.IsOpen || conn == null) return;
        if (conn.Db.PlayerPos.PlayerId.Find(give.ToId) is not { } p
            || new Vector2(p.X - myPos.X, p.Z - myPos.Z).Length() > Social.GiveReach + Social.GiveReachSlack)
            give.Close();
    }

    string NameOf(uint playerId) => conn?.Db.Player.PlayerId.Find(playerId)?.Name ?? "them";

    static string GiftWhat(GiftOffer o) =>
        Items.IsKnown(o.Item) ? Social.GiftPhrase(Items.Def((Item)o.Item), o.Count) : "something";
}
