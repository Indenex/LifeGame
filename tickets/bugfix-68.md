# bugfix-68: Wooden chest check no longer walks into a tree
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-10
PR: (opening)

## Goal
"Building check (wooden chest)" failed once on #839 (run 39049856783 attempt 1) and passed on re-run. In the
Storing step bot A walked to a fixed spot 0.9 m east of the chest; that run the chest stood 1.2 m from a
trunk, the spot was inside the trunk, and A pushed against it until the walker gave up. StoreInBasket now
picks two spots just past the footprint on its four sides that are clear of trunks, rocks and other
structures (the shared Obstacles rule), so the check can't go red at random on where the chest landed.

## Progress log
- 2026-10-17 16:55: read the failed log (A stuck at (18.4, -38.2), trunk at (18.9, -38.2) r0.37, chest at (17.7, -38.2)); fix written, no local dotnet here, so CI is the compile check.

## Open questions / blockers
- (none)

## Next step
Wait for the PR's "Building check (wooden chest)" and "Building check (other structures)" (the basket uses the same step) to pass, then send the green sha to the PM.
