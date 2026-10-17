using SpacetimeDB;

// claims-12: claim fences. Fences and gates are House building's (BuildingFence.cs);
// Claims decides who may open a gate that stands on claimed land: only the owner
// and members (Q2a default, waiting on Justin). Outside a claim, gates follow the
// ordinary door rules. When a claim lapses its fences simply become ordinary
// fences and weather like anything else outside a claim (D-046).
public static partial class Module
{
    // Whether the caller may open or shut a gate at this structure's spot.
    static bool ClaimGateAllows(ReducerContext ctx, Structure gate) =>
        ClaimAt(ctx, gate.X, gate.Z) is not { } claim || HasClaimRights(ctx, claim, Me(ctx));

    // Barring a gate on a claim: its owner and members, or the town's Gatekeeper (claims-21). Neighbours who
    // only signed the roster can still open and shut it (ClaimGateAllows) but not bar it.
    static bool ClaimGateLockAllows(ReducerContext ctx, Claim claim) =>
        HasClaimRights(ctx, claim, Me(ctx)) && (IsOwnerOrMember(ctx, claim, Me(ctx)) || TownGateKeeperMay(ctx, claim));

    // Declared in BuildingStorage.cs (building-9, #202).
    static partial void GateOpenHook(ReducerContext ctx, Structure s, ref bool allowed)
    {
        if (!ClaimGateAllows(ctx, s)) allowed = false;
    }
}
