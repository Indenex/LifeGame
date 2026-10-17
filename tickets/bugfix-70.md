# bugfix-70: Fence building check finds clear ground more reliably
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-12
PR: (opening)

## Goal
"Building check (other structures)" failed on #861 attempt 1 with "found clear ground either side of the
fence" and passed on re-run. WallsStopWalkers tried 5 offsets at one 0.9 m gap, and a tree or rock beside
the fence blocked all of them. It now tries 9 offsets at three gaps (0.9, 0.7, 1.1 m). Test tooling only.

## Progress log
- 2026-10-17 17:45: fix written (first pushed on the already-merged #862's branch by mistake, so moved here); no local dotnet, CI is the compile check.

## Open questions / blockers
- (none)

## Next step
Wait for this PR's "Building check (other structures)" to pass, then send the green sha to the PM.
