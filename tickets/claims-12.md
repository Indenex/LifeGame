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
  - Found: fences stop animals only. Players aren't stopped by fences on the server or the client, so a gate rule needs fences to block people first.
  - Sent the PM 4 questions for Justin (below) and proposed a file split to House building.
  - Prototype so far: `Social.SnapToClaimBorder` and `NearClaimBorder` (63 fence lengths round a 20 m claim, ends on the circle) and a SocialTest check.
  - Local build passes; all social checks pass.

## Proposed split (sent to House building, session_01RrKHUxWRH973Ndnxsad9nv)
- Claims, in `server/spacetimedb/ClaimFences.cs`: the gate rule, through a `GateOpenHook(ctx, s, ref allowed)` partial that Building declares and calls from ToggleDoor.
- Claims, in shared/Social.cs: `SnapToClaimBorder`, which Building calls from BuildingSystem.SnapFence near your own claim's border.
- Lapse: with default 4a below, fences simply stay as ordinary fences (D-046 weathering), so there is no code.
- Fences blocking people (Q1a): that's player movement in Move and client prediction, which Building or Integration owns.

## Open questions / blockers
- To PM for Justin 2026-10-16 (building on the recommended default for each):
  - Q1 do fences stop people (a yes [recommended] / b no);
  - Q2 who opens a claim gate (a members only [recommended] / b anyone unless barred);
  - Q3 border placement (a snap [recommended] / b free);
  - Q4 lapse (a fences stay and weather [recommended] / b fall with the marker).
- From House building: agreement on the split above.

## Next step
When House building agrees: add ClaimFences.cs with the gate rule behind their GateOpenHook, then test with SocialTest (an outsider can't open a claim gate; a member can). Stop there until Justin answers Q1 to Q4.
