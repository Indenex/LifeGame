# claims-12: Claim fences
Lane: Claims, chat and PvP rules
Owner: Claims, chat and PvP rules thread (session session_013ECn821EbTpbj4h7d3PepH)
Status: in_progress
Branch: claude/claims-12-fences (stacked on claude/claims-11-stash-log; merge after #196)
PR: (not yet)

## Goal
Justin picked claim fences for M3 (PM, 2026-10-16). Members can ring their claim with House building's fences (Fence and Gate StructureKinds, #153); the gate keeps outsiders out. Claims owns the claim side:
- who may open a gate in a claim;
- snapping fences onto the soft border;
- what happens to fences when a claim lapses.

## Progress log
- 2026-10-16 20:20: the lived-in marker (the earlier claims-12 default) is dropped; its code is parked on `claude/claims-12-lived-marker` at 5294de1.
  - Correction from House building: the client already stops players at finished fences and shut gates (BuildingSystem.Collide). The server doesn't enforce player walls at all, and that's out of scope. So Q1 is dropped.
  - Sent the PM 4 questions for Justin (below) and proposed a file split to House building.
  - Prototype so far: `Social.SnapToClaimBorder` and `NearClaimBorder` (63 fence lengths round a 20 m claim, ends on the circle) and a SocialTest check.
  - Local build passes; all social checks pass.

## Agreed split (House building, session_01RrKHUxWRH973Ndnxsad9nv, 2026-10-16)
- Claims, in `server/spacetimedb/ClaimFences.cs`: implements `static partial void GateOpenHook(ReducerContext ctx, Structure s, ref bool allowed)`. Building declares it in BuildingStorage.cs and calls it in ToggleDoor for gates. If allowed is false, they throw "Only the claim's members can open this gate". Default: barring a claim gate is members-only too (CanLock is still theirs; raise it with them if Q2a holds).
- Claims, in `client/scripts/social/BuildingSystem.ClaimFences.cs` (a `public partial class BuildingSystem`): implements `partial void SnapFenceHook(Vector3 target, ref Vector3 snapped, ref float yaw, ref bool done)`, using Social.SnapToClaimBorder. Building calls it first in SnapFence.
- Building ships both declarations in their building-9 PR and sends the sha. A partial implementation won't compile until the declarations exist.
- Lapse: with default 4a below, fences simply stay as ordinary fences (D-046 weathering), so there is no code.

## Open questions / blockers
- To PM for Justin 2026-10-16 (building on the recommended default for each):
  - Q1 dropped (fences already stop people on the client).
  - Q2 who opens a claim gate (a members only [recommended] / b anyone unless barred);
  - Q3 border placement (a snap [recommended] / b free);
  - Q4 lapse (a fences stay and weather [recommended] / b fall with the marker).
- From House building: the building-9 sha with the two hook declarations.

## Next step
When House building sends the building-9 sha: merge it locally, write ClaimFences.cs (GateOpenHook) and BuildingSystem.ClaimFences.cs (SnapFenceHook), and add SocialTest checks (an outsider can't open a claim gate; a member can).
