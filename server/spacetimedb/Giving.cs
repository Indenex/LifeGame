using LifeGame.Shared;
using SpacetimeDB;

// claims-7: handing things to each other. The giver offers; nothing moves until the
// receiver takes it. An offer lapses after a minute, or when they walk apart.
public static partial class Module
{
    // Server-only; each player reads the offers to or from them through MyGiftOffers.
    [SpacetimeDB.Table(Accessor = "GiftOffer")]
    public partial struct GiftOffer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity From;
        [SpacetimeDB.Index.BTree]
        public Identity To;
        public uint FromId;
        public uint ToId;
        public string FromName;
        public ushort Item;
        public byte Quality;
        public uint Count;
        [SpacetimeDB.Index.BTree]
        public Timestamp OfferedAt;
    }

    [SpacetimeDB.View(Accessor = "MyGiftOffers", Public = true)]
    public static List<GiftOffer> MyGiftOffers(ViewContext ctx)
    {
        var result = ctx.Db.GiftOffer.To.Filter(ctx.Sender).ToList();
        result.AddRange(ctx.Db.GiftOffer.From.Filter(ctx.Sender));
        return result;
    }

    // Offer some of what you carry to the player in front of you. A new offer
    // replaces your old one, so you can change your mind.
    [SpacetimeDB.Reducer]
    public static void OfferGift(ReducerContext ctx, uint toPlayerId, ushort item, byte quality, uint count)
    {
        var giver = RequirePlayer(ctx);
        if (toPlayerId == giver.PlayerId) throw new Exception("You already have it");
        if (count == 0) throw new Exception("Nothing to give");
        var receiver = ctx.Db.Player.PlayerId.Find(toPlayerId) is { Online: true } p ? p : throw new Exception("They are not here");
        RequireGiveReach(ctx, giver.PlayerId, toPlayerId);
        var stack = FindStack(ctx, giver.Identity, (Item)item, quality) ?? throw new Exception("You are not carrying that");
        if (stack.Count < count) throw new Exception("You don't have that many");

        foreach (var old in ctx.Db.GiftOffer.From.Filter(giver.Identity).ToList())
            ctx.Db.GiftOffer.Id.Delete(old.Id);
        ctx.Db.GiftOffer.Insert(new GiftOffer
        {
            Id = 0,
            From = giver.Identity,
            To = receiver.Identity,
            FromId = giver.PlayerId,
            ToId = receiver.PlayerId,
            FromName = giver.Name,
            Item = item,
            Quality = quality,
            Count = count,
            OfferedAt = ctx.Timestamp,
        });
    }

    // Take what was offered. If the giver has since used some, you get what is left.
    [SpacetimeDB.Reducer]
    public static void AcceptGift(ReducerContext ctx, ulong offerId)
    {
        var receiver = RequirePlayer(ctx);
        var offer = ctx.Db.GiftOffer.Id.Find(offerId) is { } o && o.To == receiver.Identity ? o : throw new Exception("That offer is gone");
        ctx.Db.GiftOffer.Id.Delete(offer.Id);
        if (ctx.Timestamp.MicrosecondsSinceUnixEpoch - offer.OfferedAt.MicrosecondsSinceUnixEpoch > Social.GiftKeepMicros)
            throw new Exception("That offer has lapsed");
        RequireGiveReach(ctx, offer.FromId, receiver.PlayerId);
        var stack = FindStack(ctx, offer.From, (Item)offer.Item, offer.Quality) ?? throw new Exception("They no longer have it");
        uint count = Math.Min(offer.Count, stack.Count);
        RemoveFromStack(ctx, stack, count);
        AddToInventory(ctx, receiver.Identity, (Item)offer.Item, count, offer.Quality);

        var at = RequirePos(ctx, receiver);
        string what = Items.IsKnown(offer.Item) ? Social.GiftPhrase(Items.Def((Item)offer.Item), count) : "something";
        SayNearby(ctx, at.X, at.Z, $"{offer.FromName} hands {receiver.Name} {what}");
    }

    // Turn an offer down, or (as the giver) take it back.
    [SpacetimeDB.Reducer]
    public static void DeclineGift(ReducerContext ctx, ulong offerId)
    {
        if (ctx.Db.GiftOffer.Id.Find(offerId) is { } offer && (offer.To == ctx.Sender || offer.From == ctx.Sender))
            ctx.Db.GiftOffer.Id.Delete(offer.Id);
    }

    static void RequireGiveReach(ReducerContext ctx, uint a, uint b)
    {
        var pa = ctx.Db.PlayerPos.PlayerId.Find(a) ?? throw new Exception("No position");
        var pb = ctx.Db.PlayerPos.PlayerId.Find(b) ?? throw new Exception("They are not here");
        float dx = pa.X - pb.X, dz = pa.Z - pb.Z, reach = Social.GiveReach + Social.GiveReachSlack;
        if (dx * dx + dz * dz > reach * reach) throw new Exception("Step closer to hand it over");
    }

    // Run with the chat prune: lapsed offers are cleared.
    static void PruneGifts(ReducerContext ctx)
    {
        var cutoff = new Timestamp(ctx.Timestamp.MicrosecondsSinceUnixEpoch - Social.GiftKeepMicros);
        foreach (var offer in ctx.Db.GiftOffer.OfferedAt.Filter((new Timestamp(0), cutoff)).ToList())
            ctx.Db.GiftOffer.Id.Delete(offer.Id);
    }
}
