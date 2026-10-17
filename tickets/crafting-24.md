# crafting-24: Boiled eggs and fletched arrows
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-eggs (stacked on Animals' claude/animals-hen-5gkntn, #454)
PR: (open after #454 merges)

## Goal
Justin's hen answers (2026-10-17): Q1 (a) a boiled egg, roasted on a fire like meat (`cook.egg`, items BoiledEgg 212 and BurntEgg 213); Q2 (a) fletched arrows: a second arrow recipe (`carve.fletched_arrows`, 2 sticks, 3 feathers, 1 fiber for 3 arrows) whose arrows come out 10 better (ProcessDef.QualityBonus, within the crafter's cap); plain arrows (`carve.arrows`) still work. All data: ItemDefs, FoodDef, RoastDef, two ProcessDefs, and one new ProcessDef field (QualityBonus, applied in Crafting.cs).

## Progress log
- 2026-10-17: built on the hen branch; server and client build, bindings regenerate. Boiled egg food 0.2 (raw egg 0.08), burnt 0.05 (Needs.cs). Not exercised by CraftCheck (a hen is needed for eggs and feathers).

## Open questions / blockers
- Waits for #454 (Animals) to merge before opening the PR; then merge main.
- Journal: 'Things to try' lines for cook.egg and carve.fletched_arrows (sent).
- Modeling: look for Boiled egg / Burnt egg items if they need a model (sent).

## Next step
When #454 is on main: merge main into crafting-eggs, rerun build, push, open the PR, send the PM the green sha.
