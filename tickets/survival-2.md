# survival-2: Water pots, item ids, crafted fires, weather chill
Lane: Survival needs
Owner: Survival needs thread (session cse_01EzhhyMEiXNWShq9e1gpQ4w)
Status: in_review
Branch: claude/survival-needs-g7yrhi (restarted from main after PR #5 merged)
PR: #12 (draft; lands after #4 crafting-core and #11 ceramics)

## Goal
Plug survival into the other lanes as they land on main, keeping Justin's gentle rates (90 min food, 60 min water, 16 min cold):
- Drinking from ceramics water pots: PinchPotOfWater 107 / CoilPotOfWater 108 quench thirst and give back PinchPot 105 / CoilPot 106 (needs clay-ceramics on main).
- Berries move from `ItemKind.Berries` to item id 160 in `shared/Items.cs` (Survival owns 160-189) once crafting-core is on main.
- Crafted fires: once Crafting implements `FireWarmthAt` (via Module.WarmthAt), delete the placeholder campfires (HeatSource table, seeding in `EnsureSurvivalWorld`, client Campfire for placeholders) — or keep Campfire visuals if Crafting reuses them.
- Weather: once Atmosphere PR #9 merges, fold night chill (AirTemperature(ctx), °C) and rain (CurrentWeather(ctx).Rain / Wetness) into the warmth rate, with rain reduced by ShelterAtHook's rain cover. The 2 s SurvivalTick already re-reads surroundings, so time-of-day changes apply without player movement.

## Progress log
- 2026-10-15 21:26: ticket created from the PM's go-ahead. None of the dependencies are on main yet (crafting-core, clay-ceramics, atmosphere #9 all open).

- 2026-10-15 21:36: PM said start now: merged origin/crafting-core and origin/claude/clay-ceramics-kfr4kr into this branch. Berries is Item 160. Placeholder campfires and HeatSource removed; warmth = Module.WarmthAt * 1/30 per s (called directly, the FireWarmthAt hook is gone). DrinkFrom(107/108) gives back 105/106 at the same quality and quenches 0.35 / 0.7. Weather: AirTemperatureAt partial hook (new, for Atmosphere) and Ceramics' RainAt partial, combined in Survival.Chill() with Justin's numbers.
- 2026-10-15 21:45: tested on local SpacetimeDB: pot of water 0.30 -> 0.65 water and empty pot returned; hand-drill fire gives near_heat and +0.024/s warmth. Screenshot: project files screenshots/08-survival-pot-of-water-by-crafted-fire.png
- 2026-10-15 21:48: key clash with crafting (C = craft, G = feed fire): eat moved to X (matches the open key proposal); G forages only when a ripe bush is in reach and otherwise falls through to feeding the fire.
- 2026-10-15 21:58: merged latest crafting-core (e165799, which carries main with House building). Kept Crafting's FireWarmthAt implementation and call the hook again (no cross-lane deletion needed); took Crafting's Eat (CountOf/Take); eat stays on X per D-025 (crafting-core had V). Weather: per PM, AirTemperatureAt hook gets replaced by a direct call to WorldClock.AirTemperature/CurrentWeather once #9 lands.

## Open questions / blockers
- After Atmosphere #9 merges: replace the AirTemperatureAt hook in Survival.cs with WorldClock's AirTemperature(ctx) (Ceramics provides RainAt). Until then it is always a mild dry 15 °C.
- Keys: Justin's card on a single Use key is open; current default G forage/feed, X eat, R drink.
- Removing the HeatSource table may need `--delete-data` when publishing over a survival-1 world (the crafting and ceramics merges may need it anyway).

## Next step
When #4 or #11 merges: merge origin/main in, regenerate client/module_bindings, build, push; when #9 merges, check Atmosphere implements AirTemperatureAt/RainAt.
