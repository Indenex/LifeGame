using LifeGame.Shared;
using SpacetimeDB;

// A sundial (building-19, Justin): a finished one standing in a claim shows the time of day to everyone
// who belongs to that claim, and to the rest of its town. The server decides who (D-182); the client's
// time-of-day arc reads this one-row view, and shows nothing without it.
public static partial class Module
{
    // One row per claim that covers the player. The player has the dial while this view is not empty.
    [SpacetimeDB.Type]
    public partial struct SundialAccess
    {
        public ulong ClaimId;
    }

    [SpacetimeDB.View(Accessor = "MySundial", Public = true)]
    public static List<SundialAccess> MySundial(ViewContext ctx)
    {
        var covered = new List<SundialAccess>();
        void Consider(Claim own)
        {
            if (covered.Any(c => c.ClaimId == own.Id)) return;
            if (ClaimHasSundial(ctx, own)) { covered.Add(new SundialAccess { ClaimId = own.Id }); return; }
            // Its town: any signer's claim with a sundial covers the whole roster.
            if (ctx.Db.TownSigner.ClaimId.Find(own.Id) is not { } signed) return;
            foreach (var signer in ctx.Db.TownSigner.ByTown.Filter(signed.TownId))
                if (ctx.Db.Claim.Id.Find(signer.ClaimId) is { } other && ClaimHasSundial(ctx, other))
                {
                    covered.Add(new SundialAccess { ClaimId = own.Id });
                    return;
                }
        }
        foreach (var claim in ctx.Db.Claim.Owner.Filter(ctx.Sender)) Consider(claim);
        foreach (var membership in ctx.Db.ClaimMember.Member.Filter(ctx.Sender))
            if (ctx.Db.Claim.Id.Find(membership.ClaimId) is { } claim) Consider(claim);
        return covered;
    }

    // A finished sundial standing inside this claim's radius.
    static bool ClaimHasSundial(ViewContext ctx, Claim claim)
    {
        int cx = World.ChunkOf(claim.X), cz = World.ChunkOf(claim.Z), r = ClaimChunkReach;
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
                foreach (var s in ctx.Db.Structure.ByChunk.Filter((cx + dx, cz + dz)))
                    if (s.Complete && Structures.Shape((int)s.Kind).Sundial && Social.InClaim(claim.X, claim.Z, s.X, s.Z))
                        return true;
        return false;
    }
}
