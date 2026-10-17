using LifeGame.Shared;
using SpacetimeDB;

// Slip decoration (ceramics-14, Justin: a banded rim in a free colour). While a pot or jar you made
// is still damp, at your Potter's bench, you can paint a band or two of coloured slip round its rim.
// Look only: it never changes quality and nothing needs tending (D-148). The colour and band count
// ride on the piece's Mark number beside the maker (shared/Slip.cs), so they follow the pot through
// drying, firing, pick-up, filling and drinking like the maker's mark does. It costs one clean clay.
public static partial class Module
{
    // Puts (or replaces) slip on a damp pot or jar of yours beside your Potter's bench. The colour
    // is clamped to the mark range and snapped to the slip grid; bands is 1 or 2.
    [SpacetimeDB.Reducer]
    public static void SlipPiece(ReducerContext ctx, ulong pieceId, uint color, byte bands)
    {
        var player = RequirePlayer(ctx);
        var pos = RequirePos(ctx, player);
        var piece = ctx.Db.ClayPiece.Id.Find(pieceId) ?? throw new Exception("That clay is gone");
        RequireReach(pos, piece.X, piece.Z);
        RequireClaimAccess(ctx, piece.X, piece.Z);
        if (piece.MadeBy != player.PlayerId)
            throw new Exception("Only the one who made it can decorate it");
        var form = (ClayForm)piece.Form;
        if (!Slip.CanDecorate(form))
            throw new Exception("That doesn't take slip. Pots and jars do.");
        if (piece.Stage != (byte)ClayStage.Drying)
            throw new Exception("It is in the fire");
        if (MoistureNow(ctx, piece) < ClayRules.LeatherHard)
            throw new Exception("The clay has dried too much to take slip");
        if (StationNear(ctx, StationKind.PottersBench, pos.X, pos.Z) is null)
            throw new Exception("You need to be at your Potter's bench to decorate");
        if (bands is < 1 or > Slip.MaxBands)
            throw new Exception("A pot takes one or two bands");
        if (CountOf(ctx, Item.CleanClay) < Slip.ClayCost)
            throw new Exception("Slip needs a little clean clay");

        Take(ctx, Item.CleanClay, Slip.ClayCost);
        ctx.Db.ClayPiece.Id.Update(piece with { Mark = Slip.With(piece.Mark, color, bands) });
        News(ctx, player.PlayerId, bands == 1 ? "A band of slip round the rim." : "Two bands of slip round the rim.");
    }
}
