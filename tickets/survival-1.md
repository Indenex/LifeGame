# survival-1: Hunger, thirst, cold, death and respawn
Lane: Survival needs
Owner: Survival needs thread (session cse_01EzhhyMEiXNWShq9e1gpQ4w)
Status: in_review
Branch: claude/survival-needs-g7yrhi
PR: #5

## Goal
Hunger, thirst and body temperature, with cold as the first threat for a naked player; warmth from fire (Module.WarmthAt) and later shelter. Food and drinking water. Death drops what you carry and wakes you naked at the wilderness edge; knowledge survives. Done when a player can starve or freeze, respawn, eat, drink and warm up at a fire in a build.

## Progress log
- 2026-10-15 20:38: lane started
- 2026-10-15 21:10: PR #5 opened. Server `server/spacetimedb/Survival.cs` (Vitals snapshot + MyVitals view, HeatSource placeholder campfires, ForageBush, Eat/Drink/Forage, 2 s SurvivalTick for death), tuning in `shared/Needs.cs`, client `client/scripts/survival/` (ring gauges, prompts, frost overlay, death fade, campfire and bush visuals). Tested on local SpacetimeDB 2.12: freeze and thirst deaths, item drop, respawn at rim, eat, drink, forage. Screenshot: project files screenshots/07-survival-campfire-and-bushes.png
- 2026-10-15 21:15: merged main; added `FireWarmthAt` partial hook (PM call) so Crafting can feed Module.WarmthAt in without a second heat table

## Interfaces for other lanes
- Fire: Crafting implements `static partial void FireWarmthAt(ReducerContext ctx, float x, float z, ref float warmth)` (0..1). Once it does, delete the placeholder seeding in `EnsureSurvivalWorld` and the HeatSource table.
- Shelter: House building implements `ShelterAt` in Survival.cs (0 open sky .. 1 fully sheltered; scales the cold drain).
- Items: Survival owns item ids 160-189. When crafting-core (Item ids in shared/Items.cs) and this PR meet, whichever merges second converts `ItemKind.Berries` to item 160.
- Water pots (from ceramics): PinchPotOfWater 107 / CoilPotOfWater 108 should quench thirst and give back PinchPot 105 / CoilPot 106. Add to `Nourishment`/Eat once those items are on main.

## Open questions / blockers
- Needs numbers (45 min food, 30 min water, 8 min of cold, pause while offline, 60% food/water after respawn): PM is taking them to Justin. Defaults are in shared/Needs.cs.

## Next step
Wait for CI on PR #5; when green, tell the PM (session cse_01HQQLrCgy3JhRUA9YMTqyL1) so Justin can approve the merge.
