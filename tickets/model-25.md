# model-25: Icon pass 3 (clay-working items)
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/model-25-clay-items
PR: (see below)

## Goal
The clay-working items that drew as plain stand-ins (Ceramics' ClayItemLook) get proper models, overridden from ItemModels:
- Stone grit: a heap of crushed grey stone crumbs. This shares `Crumbs` with grog, which now uses it too.
- Clean clay: a smooth, wedged loaf with a cut end and a thumb press.
- Tempered clay: the same loaf, flecked with pale grit on its skin and cut face, so the two tell apart.
- Raw clay (the clod) already read well and is unchanged.
Comparison: `renders/model-25-clay-items-before-after.png` (top before, bottom after).

## Progress log
- 2026-10-17: done; build has 0 errors; rendered with the Showcase icons view.

## Open questions / blockers
- (none) Ceramics told via the PM: their ClayItemLook entries for these items are now overridden.

## Next step
Wait for CI on the PR, then send the green sha to the PM.
