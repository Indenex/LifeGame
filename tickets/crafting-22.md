# crafting-22: Fire-lit sound and (later) night fire reach
Lane: Crafting
Owner: Crafting thread (Sonnet, D-159)
Status: in_review
Branch: crafting-fire-sfx
PR: (to open)

## Goal
Two small client hooks other lanes asked for. (1) Audio (audio-3): a fire lit within 6 m of you plays the FireLit sound; fires already burning when you log in stay quiet. (2) Atmosphere (D-180, after their PR #245 merges): campfire light reaches about 10 m by day and 14 m at night, one line in Campfire._Process: `light.OmniRange = 10f + 4f * Atmosphere.Night;`.

## Progress log
- 2026-10-16: (1) done in client/scripts/crafting/FireView.cs; client builds. (2) not started: waits for #245 (Atmosphere.Night).

## Open questions / blockers
- (2) needs Atmosphere's PR #245 on main first, or it won't compile.

## Next step
Open the PR for (1); when #245 is on main, add the OmniRange line from a fresh branch off main.
