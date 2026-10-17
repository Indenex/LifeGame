# model-23: Icon pass 2 (stone flake, hand axe, daub)
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/model-23-icons2
PR: (see below)

## Goal
The weaker icons from model-20, now distinct at 96 px:
- Stone flake: cooler flint, a bulb of percussion and two ripple scars.
- Hand axe: knapping scars over both faces.
- Daub: was Ceramics' ClayItemLook DaubPat. Now a lumpy pat of straw-tempered mud with stalks poking out and a palm print in the top; ItemModels overrides the stand-in.
Comparison: `renders/model-23-icons-before-after.png` (top before, bottom after).

## Progress log
- 2026-10-17: done; build has 0 errors; rendered with the Showcase icons view.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the green sha to the PM.
