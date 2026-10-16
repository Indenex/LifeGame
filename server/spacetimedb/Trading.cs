using LifeGame.Shared;
using SpacetimeDB;

// claims-7: trading between two players (D-069). One asks, the other answers; both
// put in things they carry and both accept; then the swap happens in one go.
// Any change to either side clears both accepts. Walking apart ends it.
// The contract the trade window (ui-9) builds on is in tickets/claims-7.md.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "Trade")]
    public partial struct Trade
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity A;
        [SpacetimeDB.Index.BTree]
        public Identity B;
        public uint AId;
        public uint BId;
        public string AName;
        public string BName;
        public byte State;
        public bool AAccepted;
        public bool BAccepted;
        public uint Version;
        public Timestamp StartedAt;
    }

    [SpacetimeDB.Table(Accessor = "TradeItem")]
    public partial struct TradeItem
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public ulong TradeId;
        public byte Side;
        public ushort Item;
        public byte Quality;
        public uint Count;
    }

    // Tells both traders how a trade ended: "done", "declined", "cancelled", "apart" or "lapsed".
    [SpacetimeDB.Table(Accessor = "TradeEnded", Public = true, Event = true)]
    public partial struct TradeEnded
    {
        public ulong TradeId;
        public uint AId;
        public uint BId;
        public string Reason;
    }

    [SpacetimeDB.Table(Accessor = "TradeWatchTimer", Scheduled = nameof(WatchTrades), ScheduledAt = nameof(ScheduledAt))]
    public partial struct TradeWatchTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    [SpacetimeDB.View(Accessor = "MyTrade", Public = true)]
    public static List<Trade> MyTrade(ViewContext ctx)
    {
        var result = ctx.Db.Trade.A.Filter(ctx.Sender).ToList();
        result.AddRange(ctx.Db.Trade.B.Filter(ctx.Sender));
        return result;
    }

    [SpacetimeDB.View(Accessor = "MyTradeItems", Public = true)]
    public static List<TradeItem> MyTradeItems(ViewContext ctx)
    {
        var result = new List<TradeItem>();
        foreach (var trade in ctx.Db.Trade.A.Filter(ctx.Sender)) result.AddRange(ctx.Db.TradeItem.TradeId.Filter(trade.Id));
        foreach (var trade in ctx.Db.Trade.B.Filter(ctx.Sender)) result.AddRange(ctx.Db.TradeItem.TradeId.Filter(trade.Id));
        return result;
    }

    [SpacetimeDB.Reducer]
    public static void AskTrade(ReducerContext ctx, uint playerId)
    {
        var me = RequirePlayer(ctx);
        if (playerId == me.PlayerId) throw new Exception("You can't trade with yourself");
        var them = ctx.Db.Player.PlayerId.Find(playerId) is { Online: true } p ? p : throw new Exception("They are not here");
        if (TraderDistance(ctx, me.PlayerId, them.PlayerId) > Social.TradeReach + Social.TradeReachSlack)
            throw new Exception("Step closer to trade");
        if (TradeOf(ctx, me.Identity) != null) throw new Exception("You are already trading");
        if (TradeOf(ctx, them.Identity) != null) throw new Exception("They are busy trading");

        ctx.Db.Trade.Insert(new Trade
        {
            Id = 0,
            A = me.Identity,
            B = them.Identity,
            AId = me.PlayerId,
            BId = them.PlayerId,
            AName = me.Name,
            BName = them.Name,
            State = Social.TradeAsked,
            AAccepted = false,
            BAccepted = false,
            Version = 0,
            StartedAt = ctx.Timestamp,
        });
        if (ctx.Db.TradeWatchTimer.Count == 0)
            ctx.Db.TradeWatchTimer.Insert(new TradeWatchTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromSeconds(1)) });
    }

    [SpacetimeDB.Reducer]
    public static void AnswerTrade(ReducerContext ctx, ulong tradeId, bool accept)
    {
        if (ctx.Db.Trade.Id.Find(tradeId) is not { } trade || trade.B != ctx.Sender || trade.State != Social.TradeAsked)
            throw new Exception("That trade is gone");
        if (accept) ctx.Db.Trade.Id.Update(trade with { State = Social.TradeOpen, StartedAt = ctx.Timestamp });
        else EndTrade(ctx, trade, "declined");
    }

    // Set how many of one stack you put in (0 takes it back out).
    [SpacetimeDB.Reducer]
    public static void SetTradeItem(ReducerContext ctx, ulong tradeId, ushort item, byte quality, uint count)
    {
        var trade = RequireOpenTrade(ctx, tradeId);
        byte side = trade.A == ctx.Sender ? Social.SideA : Social.SideB;
        uint held = CarriedCount(ctx, ctx.Sender, (Item)item, quality);
        if (count > held) throw new Exception("You are not carrying that many");

        var mine = ctx.Db.TradeItem.TradeId.Filter(trade.Id).Where(i => i.Side == side).ToList();
        var row = mine.FirstOrDefault(i => i.Item == item && i.Quality == quality);
        bool exists = mine.Any(i => i.Item == item && i.Quality == quality);
        if (count == 0)
        {
            if (!exists) return;
            ctx.Db.TradeItem.Id.Delete(row.Id);
        }
        else if (exists)
        {
            if (row.Count == count) return;
            ctx.Db.TradeItem.Id.Update(row with { Count = count });
        }
        else
        {
            if (mine.Count >= Social.TradeMaxStacks) throw new Exception("That's as much as a trade can hold");
            ctx.Db.TradeItem.Insert(new TradeItem { Id = 0, TradeId = trade.Id, Side = side, Item = item, Quality = quality, Count = count });
        }
        ctx.Db.Trade.Id.Update(trade with { AAccepted = false, BAccepted = false, Version = trade.Version + 1 });
    }

    // Accept the offer as you saw it (version); once both have, the swap happens.
    // accept false takes back only your own Accept and leaves the version alone.
    [SpacetimeDB.Reducer]
    public static void AcceptTrade(ReducerContext ctx, ulong tradeId, uint version, bool accept)
    {
        var trade = RequireOpenTrade(ctx, tradeId);
        if (!accept)
        {
            ctx.Db.Trade.Id.Update(trade.A == ctx.Sender ? trade with { AAccepted = false } : trade with { BAccepted = false });
            return;
        }
        if (version != trade.Version) throw new Exception("The offer changed; look again");
        var items = ctx.Db.TradeItem.TradeId.Filter(trade.Id).ToList();
        if (items.Count == 0) throw new Exception("Nothing to trade");
        trade = trade.A == ctx.Sender ? trade with { AAccepted = true } : trade with { BAccepted = true };
        if (!(trade.AAccepted && trade.BAccepted))
        {
            ctx.Db.Trade.Id.Update(trade);
            return;
        }

        // Both accepted: check everything is still there (else this accept fails and
        // nothing changes), then swap in one go.
        foreach (var it in items)
        {
            var owner = it.Side == Social.SideA ? trade.A : trade.B;
            if (CarriedCount(ctx, owner, (Item)it.Item, it.Quality) < it.Count)
                throw new Exception($"{(it.Side == Social.SideA ? trade.AName : trade.BName)} no longer has {ItemWords(it.Item, it.Count)}");
        }
        foreach (var it in items)
        {
            var (from, to) = it.Side == Social.SideA ? (trade.A, trade.B) : (trade.B, trade.A);
            MoveCarried(ctx, from, to, (Item)it.Item, it.Quality, it.Count);
        }

        // D-080: nothing is said to bystanders; the two traders get TradeEnded "done".
        EndTrade(ctx, trade, "done");
    }

    [SpacetimeDB.Reducer]
    public static void CancelTrade(ReducerContext ctx, ulong tradeId)
    {
        if (ctx.Db.Trade.Id.Find(tradeId) is { } trade && (trade.A == ctx.Sender || trade.B == ctx.Sender))
            EndTrade(ctx, trade, "cancelled");
    }

    // Every second while trades exist: end those whose players drifted apart, left,
    // or (respawning far away) died, and asks nobody answered.
    [SpacetimeDB.Reducer]
    public static void WatchTrades(ReducerContext ctx, TradeWatchTimer timer)
    {
        foreach (var trade in ctx.Db.Trade.Iter().ToList())
        {
            bool online = ctx.Db.Player.PlayerId.Find(trade.AId) is { Online: true } && ctx.Db.Player.PlayerId.Find(trade.BId) is { Online: true };
            if (!online || TraderDistance(ctx, trade.AId, trade.BId) > Social.TradeBreakDistance)
                EndTrade(ctx, trade, "apart");
            else if (trade.State == Social.TradeAsked && ctx.Timestamp.MicrosecondsSinceUnixEpoch - trade.StartedAt.MicrosecondsSinceUnixEpoch > Social.TradeAskMicros)
                EndTrade(ctx, trade, "lapsed");
        }
        if (ctx.Db.Trade.Count == 0)
            ctx.Db.TradeWatchTimer.ScheduledId.Delete(timer.ScheduledId);
    }

    static Trade? TradeOf(ReducerContext ctx, Identity who)
    {
        foreach (var t in ctx.Db.Trade.A.Filter(who)) return t;
        foreach (var t in ctx.Db.Trade.B.Filter(who)) return t;
        return null;
    }

    static Trade RequireOpenTrade(ReducerContext ctx, ulong tradeId) =>
        ctx.Db.Trade.Id.Find(tradeId) is { } t && (t.A == ctx.Sender || t.B == ctx.Sender) && t.State == Social.TradeOpen
            ? t : throw new Exception("That trade is gone");

    static void EndTrade(ReducerContext ctx, Trade trade, string reason)
    {
        foreach (var it in ctx.Db.TradeItem.TradeId.Filter(trade.Id).ToList()) ctx.Db.TradeItem.Id.Delete(it.Id);
        ctx.Db.Trade.Id.Delete(trade.Id);
        ctx.Db.TradeEnded.Insert(new TradeEnded { TradeId = trade.Id, AId = trade.AId, BId = trade.BId, Reason = reason });
    }

    static float TraderDistance(ReducerContext ctx, uint a, uint b)
    {
        if (ctx.Db.PlayerPos.PlayerId.Find(a) is not { } pa || ctx.Db.PlayerPos.PlayerId.Find(b) is not { } pb) return float.MaxValue;
        float dx = pa.X - pb.X, dz = pa.Z - pb.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    static string ItemWords(ushort item, uint count) =>
        Items.IsKnown(item) ? Social.GiftPhrase(Items.Def((Item)item), count) : "something";

}
