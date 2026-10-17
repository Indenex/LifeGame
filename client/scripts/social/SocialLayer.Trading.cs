using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-7: asking a player to trade (D-069). Since ui-35 that's the right-click player
// menu (Justin: "just being near them and hitting E shouldn't do anything"), not G/E.
// The trade window itself is UX/UI's (ui-9), reading my_trade, my_trade_items and
// trade_ended, which are subscribed here with the other social tables.
public partial class SocialLayer
{
    // True while the claim panel is up (Main counts it as a full panel).
    public bool PanelOpen => panel.IsOpen || town.IsOpen;

    void AttachTrading()
    {
        conn!.Db.MyTrade.OnInsert += (_, _) => RefreshTradeCue();
        conn.Db.MyTrade.OnUpdate += (_, _, _) => RefreshTradeCue();
        conn.Db.MyTrade.OnDelete += (_, _) => RefreshTradeCue();
    }

    // claims-8: when someone asks you to trade, a soft slanted "wants to trade" floats
    // over their head until you answer, like a chat bubble. Only you see it (D-080).
    Label3D? tradeCue;
    uint tradeCueFor;

    void RefreshTradeCue()
    {
        uint asker = 0;
        foreach (var t in conn!.Db.MyTrade.Iter())
            if (t.State == Social.TradeAsked && t.BId == myId) asker = t.AId;
        if (asker == tradeCueFor && (asker == 0 || tradeCue != null)) return;

        if (tradeCue is { } old)
        {
            var fade = old.CreateTween();
            fade.TweenProperty(old, "modulate:a", 0f, 0.6f).SetTrans(Tween.TransitionType.Sine);
            fade.TweenCallback(Callable.From(old.QueueFree));
            tradeCue = null;
        }
        tradeCueFor = asker;
        if (asker == 0 || avatarOf(asker) is not { } avatar) return;

        tradeCue = new Label3D
        {
            Text = "wants to trade",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 26,
            PixelSize = 0.0075f,
            Modulate = new Color(Look.Cream, 0f),
            OutlineSize = 8,
            OutlineModulate = new Color(0.08f, 0.09f, 0.07f, 0.6f),
            NoDepthTest = true,
            VerticalAlignment = VerticalAlignment.Bottom,
            Font = Slanted,
            Position = new Vector3(0, 2.35f, 0),
        };
        avatar.AddChild(tradeCue);
        tradeCue.CreateTween().TweenProperty(tradeCue, "modulate:a", 0.9f, 0.35f).SetTrans(Tween.TransitionType.Sine);
    }
}
