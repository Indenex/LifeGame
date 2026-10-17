# crafting-22: Fire-lit sound and (later) night fire reach
Lane: Crafting
Owner: Crafting thread (Sonnet, D-159)
Status: done
Branch: crafting-fire-sfx (merged as #269); crafting-fire-night (night reach)
PR: (to open)

## Goal
Two small client hooks other lanes asked for. (1) Audio (audio-3): a fire lit within 6 m of you plays the FireLit sound; fires already burning when you log in stay quiet. (2) Atmosphere (D-180, after their PR #245 merges): campfire light reaches about 10 m by day and 14 m at night, one line in Campfire._Process: `light.OmniRange = 10f + 4f * Atmosphere.Night;`.

## Progress log
- 2026-10-16: (1) done and merged as #269. (2) done on branch crafting-fire-night after #245 merged: one line in Campfire._Process; client builds.

## Open questions / blockers
- (2) needs Atmosphere's PR #245 on main first, or it won't compile.

## Next step
None. Merged to main; kept for history.
