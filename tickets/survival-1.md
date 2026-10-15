# survival-1: Hunger, thirst, cold, death and respawn
Lane: Survival needs
Owner: Survival needs thread (session cse_01EzhhyMEiXNWShq9e1gpQ4w)
Status: done
Branch: claude/survival-needs-g7yrhi
PR: #5

## Goal
Hunger, thirst and body temperature, with cold as the first threat for a naked player; warmth from fire (Module.WarmthAt) and later shelter. Food and drinking water. Death drops what you carry and wakes you naked at the wilderness edge; knowledge survives. Done when a player can starve or freeze, respawn, eat, drink and warm up at a fire in a build.

## Progress log
- 2026-10-15 20:38: lane started
- 2026-10-15 21:10: PR #5 opened. Server `server/spacetimedb/Survival.cs` (Vitals snapshot + MyVitals view, HeatSource placeholder campfires, ForageBush, Eat/Drink/Forage, 2 s SurvivalTick for death), tuning in `shared/Needs.cs`, client `client/scripts/survival/` (ring gauges, prompts, frost overlay, death fade, campfire and bush visuals). Tested on local SpacetimeDB 2.12: freeze and thirst deaths, item drop, respawn at rim, eat, drink, forage. Screenshot: project files screenshots/07-survival-campfire-and-bushes.png
- 2026-10-15 21:15: merged main; added `FireWarmthAt` partial hook (PM call) so Crafting can feed Module.WarmthAt in without a second heat table
- 2026-10-15 21:16: renamed the shelter hook to ShelterAtHook to avoid clashing with House building's ShelterAt (PM call)
- 2026-10-15 21:17: CI green on e3e9b0b, run https://github.com/Indenex/LifeGame/actions/runs/38808400967 (server + load test, Linux/Windows/macOS clients)
- 2026-10-15 21:20: Justin chose gentler needs: 90 min food, 60 min water, 16 min cold (3x in water); pause offline and 60% after respawn kept. Applied in shared/Needs.cs
- 2026-10-15 21:21: Justin approved the merge (via PM); merging PR #5 into main once CI is green on this commit. No data wipe needed: a plain `spacetime publish` migrates an existing world.

## Interfaces for other lanes
- Fire: Crafting implements `static partial void FireWarmthAt(ReducerContext ctx, float x, float z, ref float warmth)` (0..1). Once it does, delete the placeholder seeding in `EnsureSurvivalWorld` and the HeatSource table.
- Shelter: House building implements `static partial void ShelterAtHook(ReducerContext ctx, float x, float z, ref float rainCover, ref float warmth)` by calling its own ShelterAt; warmth scales the cold drain down, rain cover is unused until weather exists.
- Items: Survival owns item ids 160-189. When crafting-core (Item ids in shared/Items.cs) and this PR meet, whichever merges second converts `ItemKind.Berries` to item 160.
- Water pots (from ceramics): PinchPotOfWater 107 / CoilPotOfWater 108 should quench thirst and give back PinchPot 105 / CoilPot 106. Add to `Nourishment`/Eat once those items are on main.

## Open questions / blockers
- (none)

## Next step
Next milestone (survival-2, ask the PM): drink from ceramics water pots (items 107/108 back to 105/106), convert Berries to item 160 when crafting-core lands, and swap placeholder campfires for crafted fires via FireWarmthAt.
