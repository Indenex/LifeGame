# ceramics-17: Lamp craft time follows the crafting-16 formula
Lane: Clay and ceramics
Owner: Ceramics thread
Status: in_progress
Branch: claude/clay-ceramics-17-kfr4kr
PR: (none yet)

## Goal
The clay lamp's craft time was a hard-coded 6 s. It now uses the shared formula like every other clay process: `Base(3, 0.5f)` = 10 s at tier 3, a bit under a pinch pot (15 s) because a lamp is smaller. PM chose this (option 2a). The same PR carries the ceramics ticket status cleanup (tickets 7 to 16 marked done, 15 blocked on Crafting's Maker column). The tiled house is not added to ClayCheck (PM chose to skip; BuildTest covers its costs and stages).

## Progress log
- 2026-10-17 05:35: lamp time changed in shared/Processes.cs; tickets cleaned up.

## Open questions / blockers
- (none)

## Next step
Build, run ClayCheck --lamp, open the PR, send the PM the green sha.
