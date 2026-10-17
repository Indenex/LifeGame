# model-20: Inventory icon pass
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/model-20-icons
PR: (see below)

## Goal
Check every item's inventory icon at its real size (ItemIcon, 96 px) and fix any that read poorly.
- Showcase view `icons` in `Showcase.Modeling.cs` lays all items out on the bag's dark backing with their names.
- Fix: roast venison (205) and burnt venison (206) had no model and drew as the same plain orange blob. `Venison` now takes colours: raw dark red, roast browned, burnt black. All three are in the Food family.
Sheet: `renders/model-20-icons.png`.

## Progress log
- 2026-10-17: added the icons view; fixed venison; build has 0 errors. The rest of the sheet reads well.
- 2026-10-17: tallow is now a pale waxy cake on a leaf (Food family), and grog a heap of crushed fired-clay crumbs. Both used to be Ceramics' plain ClayItemLook stand-ins, which ItemModels now overrides; Ceramics told via the PM.

## Open questions / blockers
- Lesser candidates, not changed: the stone flake and hand axe are both plain grey slabs. Worth a later pass.

## Next step
Wait for CI on the PR, then send the green sha to the PM.
