# claims-29: What you dropped is yours to pick back up
Lane: Claims
Owner: Claims thread (session_01T6RYk3zyUHiSncD3ZXrFFm)
Status: in_review
Branch: claude/claims-nxkyjn
PR: #839 (with claims-27 and claims-28)

## Goal
Justin picked "Yes, always" (2026-10-17, via the PM) on the dropper prototype (/mnt/project-files/handoff/claims-dropper-proto/): you can always pick back up what you dropped by hand, set down, or lost by dying, on any land, inside someone else's claim included (and under a town ban).

## Progress log
- 2026-10-17: ported onto current main. `Droppers.cs`: private `ItemDropper` table (item id to player id; clients never see it, D-182), recorded from `OnPlayerDroppedItem` (Weathering.cs: Drop and PlaceItem) and from Survival's death drop (one-line hook `OnDeathDropped` in `Die`); `PickUp` skips the claim check for the dropper and forgets the row; the claim decay tick clears rows whose item is gone. SocialTest: B drops a thing inside A's claim and picks it back up.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #839 and send the green sha to the PM.
