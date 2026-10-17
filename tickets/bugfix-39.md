# bugfix-39: SocialTest dig check fails on a dug-out database
Lane: Bug fixes
Owner: Bug fixes thread (cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_review
Branch: claude/bugfix-lyle6o
PR: (to open)

## Goal
On #44, CI failed "A can dig in their own claim" with "Too deep to dig by hand"
(run 38858229906). SocialTest dug one fixed cell next to the player, but BuildTest,
CraftCheck and ClayCheck run first on the same CI database and dig clay cells out to
`World.MaxDigDepth`, so that cell could already be at the limit. Done: SocialTest
picks the nearest cell in reach (not the player's own) whose depth is below
`MaxDigDepth`, for both the own-claim dig and the dig after the claim decays.

## Progress log
- 2026-10-17 02:40: reproduced on a fresh db with cells 34..36 x 29..31 dug to depth 12 (old code fails exactly as CI did); added `DiggableNear`.
- 2026-10-17 03:30: same dug-out setup on db soc3: "All social checks passed".
- 2026-10-17 04:00: renumbered from bugfix-20 and ported to main 50495cc. On main the old code digs (33, 30); with that cell at depth 12 on db soc7 the new code dug (34, 29) instead and "All social checks passed". A clean db passes too.

## Open questions / blockers
- (none)

## Next step
Wait for the PR's CI, then send the PM the green sha.
