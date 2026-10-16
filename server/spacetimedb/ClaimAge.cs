using LifeGame.Shared;
using SpacetimeDB;

// claims-12: when each claim was raised, so its marker can look lived-in as the
// place is kept (a cord wrap after a day, more stones after three, carvings and a
// charm after a week). Kept beside the Claim table so no existing table changes.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "ClaimAge", Public = true)]
    public partial struct ClaimAge
    {
        [PrimaryKey]
        public ulong ClaimId;
        public Timestamp RaisedAt;
    }

    // Called when a claim is raised, and on tending for claims raised before ages existed.
    static void NoteClaimRaised(ReducerContext ctx, ulong claimId)
    {
        if (ctx.Db.ClaimAge.ClaimId.Find(claimId) == null)
            ctx.Db.ClaimAge.Insert(new ClaimAge { ClaimId = claimId, RaisedAt = ctx.Timestamp });
    }

    static void RemoveClaimAge(ReducerContext ctx, ulong claimId)
    {
        if (ctx.Db.ClaimAge.ClaimId.Find(claimId) != null) ctx.Db.ClaimAge.ClaimId.Delete(claimId);
    }
}
