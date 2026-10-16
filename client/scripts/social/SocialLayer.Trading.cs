using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-7: G at a player nearby asks them to trade (D-069). The trade window itself
// is UX/UI's (ui-9), reading my_trade, my_trade_items and trade_ended, which are
// subscribed here with the other social tables.
public partial class SocialLayer
{
    // True while the claim panel is up (Main counts it as a full panel).
    public bool PanelOpen => panel.IsOpen;

    void AttachTrading() =>
        Use.Register(new UseHandler { Id = "claims.trade", Priority = 5, Find = TradeOffer });

    // The nearest other player within reach, when neither of you is already trading.
    UseOffer? TradeOffer(Vector3 at)
    {
        if (conn == null || myId == 0 || conn.Db.MyTrade.Count > 0) return null;
        PlayerPos? best = null;
        float bestD = Social.TradeReach;
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
        string name = conn.Db.Player.PlayerId.Find(id)?.Name ?? "them";
        return new UseOffer($"Trade with {name}", bestD, () => conn.Reducers.AskTrade(id), Target: new Vector3(target.X, at.Y, target.Z));
    }
}
