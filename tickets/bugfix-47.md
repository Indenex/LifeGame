# bugfix-47: unstuck avoids trunks and boulders
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-47
PR: (see PR list)

## Goal
`unstuck` (bugfix-44) checked structure footprints, water and claims but not trees and boulders, because the server did not enforce them yet. #660 (bugfix-45) enforces them, so `unstuck` now treats a spot inside a trunk or boulder (grown by the clearance) as not free, both for the player's current spot and for destinations. Uses `Obstacles.PushOut` with the server's `ServerGround`.

## Progress log
- 2026-10-17: implemented in `Unstuck.cs`; SpawnCheck calls unstuck beside a boulder and checks the destination is clear (`Obstacles.PushOut` unchanged). 4 of 4 local runs pass; server builds; ctx.Sender check passes.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, send the green sha to the PM.
