# bugfix-68: Wooden chest check no longer walks into a tree
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-10
PR: #862

## Goal
"Building check (wooden chest)" failed once on #839 (run 39049856783 attempt 1) and passed on re-run. In the
Storing step bot A walked to a fixed spot 0.9 m east of the chest; that run the chest stood 1.2 m from a
trunk, the spot was inside the trunk, and A pushed against it until the walker gave up. StoreInBasket now
picks two spots just past the footprint on its four sides that are clear of trunks, rocks and other
structures (the shared Obstacles rule), so the check can't go red at random on where the chest landed.

## Progress log
- 2026-10-17 16:55: read the failed log (A stuck at (18.4, -38.2), trunk at (18.9, -38.2) r0.37, chest at (17.7, -38.2)); fix written, no local dotnet here, so CI is the compile check.

## Open questions / blockers
- The wooden chest and straw-bed/other building checks pass on 7b6715cd. "Load, spawn and social checks" failed twice on SurvivalCheck's pacing step (run 39051318721: attempt 1 "honest run ... 46.5 m, expected 47.4"; attempt 2 "the bot got stuck while winded (28.6 m in 20 s)"). Not this PR's code; #863 (bugfix-69) fixes the first, the second needs the same lane retry on the winded step.

## Next step
Lane paused by Justin (usage). When resumed: extend #863 so the winded step retries on a new lane too, then merge #862 once SurvivalCheck is green.

## Update 2026-10-17 17:40
- #863 (SurvivalCheck retries) merged into main as 7dd6bed0; main merged into this branch.
- The fence building check also flaked ("found clear ground either side of the fence", on #861 attempt 1): WallsStopWalkers tried 5 offsets at one gap and a tree beside the fence blocked all of them. It now tries 9 offsets at three gaps (0.9, 0.7, 1.1 m).
