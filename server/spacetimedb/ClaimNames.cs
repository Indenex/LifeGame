using LifeGame.Shared;
using SpacetimeDB;

// claims-9: the owner can name their land ("Willow Hollow"). The name shows on the
// land chip and softly as you walk in. Kept beside the Claim table, not in it, so
// adding names changes no existing table.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ClaimName", Public = true)]
    public partial struct ClaimName
    {
        [PrimaryKey]
        public ulong ClaimId;
        public string Name;
    }

    // Name your own standing claim; an empty name clears it.
    [SpacetimeDB.Reducer]
    public static void NameClaim(ReducerContext ctx, string name)
    {
        MeterCall(ctx);
        var claim = RequireOwnStandingClaim(ctx);
        name = Social.CleanClaimName(name);
        bool exists = ctx.Db.ClaimName.ClaimId.Find(claim.Id) != null;
        if (name.Length == 0)
        {
            if (exists) ctx.Db.ClaimName.ClaimId.Delete(claim.Id);
            return;
        }
        var row = new ClaimName { ClaimId = claim.Id, Name = name };
        if (exists) ctx.Db.ClaimName.ClaimId.Update(row);
        else ctx.Db.ClaimName.Insert(row);
    }

    static void RemoveClaimName(ReducerContext ctx, ulong claimId)
    {
        if (ctx.Db.ClaimName.ClaimId.Find(claimId) != null) ctx.Db.ClaimName.ClaimId.Delete(claimId);
    }
}
