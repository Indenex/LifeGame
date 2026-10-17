# building-18: Shift keeps placing
Lane: House building
Owner: House building thread (Sonnet)
Status: in_review
Branch: claude/house-building-8ueqz2-shift
PR: (see STATUS)

## Goal
UX's request for the town milestone: holding Shift when you set a footprint down keeps the ghost up for another of the same kind (fences, walls, houses in a row). The placing prompt says "Shift keeps placing".

## Progress log
- 2026-10-17: `BuildingSystem.Place()` calls `MakeGhost()` instead of `StopPlacing()` when Shift is held; prompt text updated. Client builds; not tried in Godot.

## Open questions / blockers
- (none)

## Next step
Wait for CI and send the PM the green sha.
