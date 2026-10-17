// claims-31: a trading stall on your claim. You set things out on it, each with an asking price
// in other things; anyone who walks up can buy, whether or not you are there, and what they pay
// waits in the stall's till for you. Numbers start low (D-075). Shared by the server and the client;
// keep this file free of engine and SpacetimeDB types.
namespace LifeGame.Shared
{
    public static class Stalls
    {
        // Its StructureKind position (after House building's Bed = 25); see shared/structures/StructuresClaims.cs.
        public const int Kind = 26;
        // Lots set out at once on one stall.
        public const int Slots = 8;
        // Stacks a till holds before buyers are turned away ("The till is full").
        public const int TillStacks = 16;
        // One stall per claim.
        public const int PerClaim = 1;
        // A price is a count of one kind of thing, any quality: at most this many per item sold.
        public const uint MaxPrice = 99;
        // Lines of sales kept per stall for its owner to read.
        public const int LogKept = 20;
    }
}
