# model-21: Hazelnuts
Lane: Modeling
Owner: Modeling lane session
Status: in_progress
Branch: claude/model-21-hazelnut
PR: (not yet)

## Goal
For Survival's autumn hazelnuts (D-323: about one bush in three, autumn, 3 per bush).
- `PropMeshes.HazelCluster(variant)`, 3 variants, drawn with `PropMeshes.HazelMaterial`: two or three hazelnuts in leafy green husk cups on a short stem. It uses BerryBush's cluster frame (local -Z out of the bush) so Survival can hang them like berries.
- An item model `ItemModels.Hazelnuts`: three nuts, one still husked. It's built but not yet mapped, because the Item id isn't on main yet.
- Showcase view `hazel`.
Render: `renders/model-21-hazel.png`.

## Progress log
- 2026-10-17: built the cluster and the item function and iterated the husks over six renders; build has 0 errors.

## Open questions / blockers
- Need the hazelnut Item id and its branch from Survival or the PM (asked).

## Next step
Map the item id in ItemModels' table and PaintedItems.FamilyOf (Food), add it to the Showcase pending list, render the icon, and open the PR.
