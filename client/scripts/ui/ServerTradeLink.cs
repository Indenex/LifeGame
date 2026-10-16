using System;
using System.Collections.Generic;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The trade window's link to the server (claims-7, contract in tickets/claims-7.md):
// reads my_trade and my_trade_items, which SocialLayer subscribes, calls the trade
// reducers, and turns trade_ended into a line for the HUD. Reducer errors ("Step
// closer to trade", "<Name> no longer has <item>") reach the HUD through Main's
// unhandled reducer error note.
public sealed class ServerTradeLink : ITradeLink
{
    readonly DbConnection conn;
    readonly Func<uint> myId;
    // The partner's name for each trade, kept so the end line can still name them
    // after the row is gone.
    readonly Dictionary<ulong, string> partners = new();

    public event Action<string>? Said;

    public ServerTradeLink(DbConnection conn, Func<uint> myId)
    {
        this.conn = conn;
        this.myId = myId;
        conn.Db.MyTrade.OnInsert += (_, t) => partners[t.Id] = PartnerName(t);
        conn.Db.TradeEnded.OnInsert += (_, e) => Ended(e);
    }

    Trade? Row => conn.Db.MyTrade.Iter().FirstOrDefault();
    bool IAmA(Trade t) => t.AId == myId();
    string PartnerName(Trade t) => IAmA(t) ? t.BName : t.AName;

    public TradeView? Current
    {
        get
        {
            if (Row is not { State: Social.TradeOpen } t) return null;
            bool a = IAmA(t);
            byte mySide = a ? Social.SideA : Social.SideB;
            var items = conn.Db.MyTradeItems.Iter().Where(i => i.TradeId == t.Id).ToList();
            List<TradeStack> Side(bool mine) => items
                .Where(i => (i.Side == mySide) == mine)
                .OrderBy(i => i.Id)
                .Select(i => new TradeStack((Item)i.Item, i.Quality, i.Count))
                .ToList();
            return new TradeView(t.Id, a ? t.BId : t.AId, PartnerName(t), Side(true), Side(false),
                a ? t.AAccepted : t.BAccepted, a ? t.BAccepted : t.AAccepted, t.Version);
        }
    }

    public (uint id, string name)? Incoming =>
        Row is { State: Social.TradeAsked } t && !IAmA(t) ? (t.AId, t.AName) : null;

    public (uint id, string name)? Outgoing =>
        Row is { State: Social.TradeAsked } t && IAmA(t) ? (t.BId, t.BName) : null;

    public IEnumerable<TradeStack> Carried() => conn.Db.MyInventory.Iter()
        .Where(i => i.Count > 0)
        .GroupBy(i => (i.Item, i.Quality))
        .Select(g => new TradeStack((Item)g.Key.Item, g.Key.Quality, (uint)g.Sum(i => i.Count)));

    public void Answer(bool yes) { if (Row is { } t) conn.Reducers.AnswerTrade(t.Id, yes); }
    public void Set(TradeStack s) { if (Row is { } t) conn.Reducers.SetTradeItem(t.Id, (ushort)s.Item, s.Quality, s.Count); }
    public void Accept(bool on, uint version) { if (Row is { } t) conn.Reducers.AcceptTrade(t.Id, version, on); }
    public void Cancel() { if (Row is { } t) conn.Reducers.CancelTrade(t.Id); }

    void Ended(TradeEnded e)
    {
        uint me = myId();
        if (e.AId != me && e.BId != me) return;
        bool asked = e.AId == me;
        string who = partners.Remove(e.TradeId, out var n) ? n : "them";
        string? line = e.Reason switch
        {
            "done" => $"You trade with {who}",
            "declined" => asked ? $"{who} doesn't want to trade" : null,
            "cancelled" => "The trade is off",
            "apart" => "You moved apart, so the trade is off",
            "lapsed" => asked ? $"{who} didn't answer" : null,
            _ => null,
        };
        if (line != null) Said?.Invoke(line);
    }
}
