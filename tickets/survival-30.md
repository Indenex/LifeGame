# survival-30: Hazelnuts, an autumn wild food
Lane: Survival
Owner: Survival thread
Status: in_review
Branch: claude/survival-wild-food
PR: (see STATUS)

## Goal
Wild food variety in its smallest form (a PM default while Justin sleeps, D-323; Justin to revisit): about one forage bush in three (`Needs.IsHazelBush`, bush id) is a hazel. In autumn it gives 3 hazelnuts a pick instead of berries (Item.Hazelnuts = 163, 0.08 food each, a little under a berry; regrows like berries, so nothing new to tend, D-148). Out of autumn the server refuses ("not ripe until autumn") and the client shows the bush unripe and offers nothing. Food is one `FoodDef`; the yield rule is in `Forage` (Survival.cs); the prompt says "Gather hazelnuts" on a hazel bush.

## Progress log
- 2026-10-17 12:00: built; SurvivalCheck gained the hazelnut and one-in-three checks.

## Open questions / blockers
- The hazelnut item uses the berries model as a stand-in; Modeling to make a hazelnut (asked via the PM).
- Mushrooms and roots (a larger set that needs plant models and placement) are not started.

## Next step
Wait for CI, send the green sha to the PM; ask Modeling for the hazelnut model.
