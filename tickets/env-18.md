# env-18: Soften the bare-cell squares in the meadow
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-bare-cells
PR: (opening)

## Goal
Cells without a grass tuft showed as lighter squares, a visible grid in the meadow. The ground colour under grass now leans toward the tufts' muted olive (the same transform as Look's stylised grass shader), in proportion to how much grass the cell is likely to carry, so bare cells blend in and the effect fades under trees, on trails and wet ground. Ground colour only (PM: counts as ground, not grass); blade shape, height and amount are untouched.

## Progress log
- 2026-10-17: Terrain.UnderGrass applied to the ground's vertex colours (and the clear-change colours, so clearing still blends). UnderGrassMatch = 0.7. Builds; local render at 0,10 shows the squares mostly gone. Also carries env-16/env-17 tickets marked done.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, send the green sha to the PM, ask Render for a before/after; then env-19 (forest-edge undergrowth band).
