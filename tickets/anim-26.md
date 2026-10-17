# anim-26: station crafts show their own hand work
Lane: Animation
Owner: Animation thread (session_019roNRWQBPw4ozNLDhzseig)
Status: done
Branch: claude/anim-26
PR: #746

## Goal
Per-tool polish (option c). Every craft at a station (crafting table, potter's bench, spindle, drying rack) used one generic standing pose, with the hands moving vaguely at waist height. Now the player still stands at the station but the hands do the craft's own motion. Pots are pressed and shaped (Knead), spinning wool and weaving a basket roll fibre (Twist), cloth weaving, lacing a hide and sewing draw cord (Wrap), hides are scraped (Scrape), and fitting a spindle whorl works the stick (Carve). The back bends less than kneeling at the ground, because the work is at waist height.

How: each station `ProcessDef` sets its `Motion` (data, D-206). `WorkMotions.Standing(processId)` is true for station processes, and the client works the hand motion over a standing stance (`StandAt`) instead of the kneel. `PlayerAnimator` latches the stance with the shown motion, so switching between kneeling and standing jobs eases out and back in without snapping. A station process without a `Motion` still gets the old generic Station pose.

## Progress log
- 2026-10-17 11:45: data on 14 station Defs; standing stance for hand motions; stance latched in PlayerAnimator. The dead `hide.scrape`/`hide.stretch` special cases are removed from `WorkMotions.For`. Client and server build.
- 2026-10-17 11:45: CI green at d0c55a2d; merged (#746).

## Open questions / blockers
- (none). A render request to Render C would help confirm that the hands sit on the station top; renders don't block merges (D-142).

## Next step
Done: merged in #746.
