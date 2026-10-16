# claims-12: Claim fences
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules lane (handed to a new Sonnet session 2026-10-16, D-159; previous session session_013ECn821EbTpbj4h7d3PepH)
Status: in_review
Branch: claude/claims-12-fences (stacked on claude/claims-11-stash-log; merge after #196)
PR: #300

## Goal
Justin picked claim fences for M3 (PM, 2026-10-16). Members can ring their claim with House building's fences (Fence and Gate StructureKinds, #153); the gate keeps outsiders out. Claims owns the claim side:
- who may open a gate in a claim;
- snapping fences onto the soft border;
- what happens to fences when a claim lapses.

## Progress log
- 2026-10-16 19:59: Justin picked Q2a. Only the owner and members can open, bar or unbar a claim gate. Ask House building to make CanLock members-only for gates on claimed land (whoever hung the gate keeps that right only outside a claim). Q3 and Q4 are still open, so keep the defaults: snap, and fences stay as ordinary fences.
- 2026-10-16 19:57: House building put both hook declarations in PR #202 (head 5793230, CI running): `GateOpenHook` in BuildingStorage.cs (called from ToggleDoor for gates) and `SnapFenceHook(Vector3 target, ref Vector3 snapped, ref float yaw, ref bool done)` in BuildingSystem.cs (called first in SnapFence). Their note: `yaw` is the ghost's heading. Check that it matches the Structures.ToWorld yaw that SnapToClaimBorder returns before wiring. Build against the hooks once #202 merges.
- 2026-10-16 19:56: handoff.
  - Done so far on this branch:
    - `Social.SnapToClaimBorder` and `NearClaimBorder`, with a SocialTest check;
    - `server/spacetimedb/ClaimFences.cs`, which holds `ClaimGateAllows` (members only on claimed land). The GateOpenHook implementation is written there but commented out, until House building's declaration exists.
  - Waiting on House building's building-9 PR sha with the two hook declarations.
  - Waiting on Justin (via the PM) for Q2 to Q4.
  - The lane's other work: PR #191 (claims-10) and PR #196 (claims-11, merge after #191). See their tickets.
- 2026-10-16 20:20: the lived-in marker (the earlier claims-12 default) is dropped; its code is parked on `claude/claims-12-lived-marker` at 5294de1.
  - Correction from House building: the client already stops players at finished fences and shut gates (BuildingSystem.Collide). The server doesn't enforce player walls at all, and that's out of scope. So Q1 is dropped.
  - Sent the PM 4 questions for Justin (below) and proposed a file split to House building.
  - Prototype so far: `Social.SnapToClaimBorder` and `NearClaimBorder` (63 fence lengths round a 20 m claim, ends on the circle) and a SocialTest check.
  - Local build passes; all social checks pass.

- 2026-10-16: merged main. Wrote GateOpenHook (ClaimFences.cs) and SnapFenceHook (client/scripts/social/BuildingSystem.ClaimFences.cs, plus SocialLayer.ClaimBorderNear and one Main.cs line). Yaw checked: SnapToClaimBorder's tangent (-sin a, cos a) equals the ghost's fence axis (cos yaw, -sin yaw), so conventions match. A throwaway merge of #202's head (2ca220c) was conflict-free, but this sandbox has no dotnet, so NOT built or tested. Does not compile on its own until #202 merges (hooks undeclared).
- 2026-10-16 22:24: PR #300 head c159f13: CI green (Client Linux, Client Windows, Server module and load test) https://github.com/Indenex/LifeGame/actions/runs/38973731025. Sent the PM the sha.

## Agreed split (House building, session_01RrKHUxWRH973Ndnxsad9nv, 2026-10-16)
- Claims, in `server/spacetimedb/ClaimFences.cs`: implements `static partial void GateOpenHook(ReducerContext ctx, Structure s, ref bool allowed)`. Building declares it in BuildingStorage.cs and calls it in ToggleDoor for gates. If allowed is false, they throw "Only the claim's members can open this gate". Default: barring a claim gate is members-only too (CanLock is still theirs; raise it with them if Q2a holds).
- Claims, in `client/scripts/social/BuildingSystem.ClaimFences.cs` (a `public partial class BuildingSystem`): implements `partial void SnapFenceHook(Vector3 target, ref Vector3 snapped, ref float yaw, ref bool done)`, using Social.SnapToClaimBorder. Building calls it first in SnapFence.
- Building ships both declarations in their building-9 PR and sends the sha. A partial implementation won't compile until the declarations exist.
- Lapse: with default 4a below, fences simply stay as ordinary fences (D-046 weathering), so there is no code.

## Open questions / blockers
- To PM for Justin 2026-10-16 (building on the recommended default for each):
  - Q1 dropped (fences already stop people on the client).
  - Q2 answered: a, members only (open, bar and unbar).
  - Q3 answered: snap to the border.
  - Q4 answered: fences stay as ordinary fences, gates open to anyone, everything weathers if untouched (D-046). ClaimAt only counts standing claims, so no code is needed.
- From House building: the building-9 sha with the two hook declarations.

## Next step
Wait for the PM to merge #300. Then pick the next Claims item (D-145). Old plan, done:
When House building's PR #202 merges:
1. Merge main into this branch.
2. Build server, client, and run SocialTest (code for both hooks is already written).
3. Ask House building to make barring a claim gate members-only in CanLock.
4. Add SocialTest checks: an outsider can't open a claim gate; a member can.
5. Build, test, push.
Then ask House building to make barring a claim gate members-only in their CanLock (Justin picked Q2a).
