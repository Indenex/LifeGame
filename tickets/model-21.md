# model-21: Hazelnuts
Lane: Modeling
Owner: Modeling lane session
Status: done
Branch: claude/model-21-hazelnut
PR: (not yet)

## Goal
For Survival's autumn hazelnuts (D-323: about one bush in three, autumn, 3 per bush).
- `PropMeshes.HazelCluster(variant)`, 3 variants, drawn with `PropMeshes.HazelMaterial`: two or three hazelnuts in leafy green husk cups on a short stem. It uses BerryBush's cluster frame (local -Z out of the bush) so Survival can hang them like berries.
- An item model `ItemModels.Hazelnuts` for `Item.Hazelnuts` (163, Survival's #741): three nuts, one still husked.
- `BerryBush` (Survival's file, with their go-ahead): a hazel bush hangs 8 HazelClusters instead of berries, replacing #741's sphere stand-ins.
- Showcase view `hazel`.
Render: `renders/model-21-hazel.png`.

## Progress log
- 2026-10-17: built the cluster and the item function and iterated the husks over six renders; build has 0 errors.
- 2026-10-17 11:50: PM review. Clusters are now 1.5x life (about 12 cm, readable at 5 to 10 m), the nuts are paler tan with lighter caps, and 8 clusters hang per hazel bush. Merged #741, mapped Item.Hazelnuts, and wired BerryBush. The render shows real hazel bushes and a berry bush with a person, plus a close-up row.

## Open questions / blockers
- Built on Survival's claude/survival-wild-food (#741), so it merges after #741. The PR targets that branch until #741 lands.

## Next step
Wait for CI on the PR; once #741 merges, retarget the PR to main and send the green sha to the PM.
