# survival-2: Water pots, item ids, crafted fires, weather chill
Lane: Survival needs
Owner: Survival needs thread (session cse_01EzhhyMEiXNWShq9e1gpQ4w)
Status: blocked
Branch: claude/survival-needs-g7yrhi (restarted from main after PR #5 merged)
PR: (none yet)

## Goal
Plug survival into the other lanes as they land on main, keeping Justin's gentle rates (90 min food, 60 min water, 16 min cold):
- Drinking from ceramics water pots: PinchPotOfWater 107 / CoilPotOfWater 108 quench thirst and give back PinchPot 105 / CoilPot 106 (needs clay-ceramics on main).
- Berries move from `ItemKind.Berries` to item id 160 in `shared/Items.cs` (Survival owns 160-189) once crafting-core is on main.
- Crafted fires: once Crafting implements `FireWarmthAt` (via Module.WarmthAt), delete the placeholder campfires (HeatSource table, seeding in `EnsureSurvivalWorld`, client Campfire for placeholders) — or keep Campfire visuals if Crafting reuses them.
- Weather: once Atmosphere PR #9 merges, fold night chill (AirTemperature(ctx), °C) and rain (CurrentWeather(ctx).Rain / Wetness) into the warmth rate, with rain reduced by ShelterAtHook's rain cover. The 2 s SurvivalTick already re-reads surroundings, so time-of-day changes apply without player movement.

## Progress log
- 2026-10-15 21:26: ticket created from the PM's go-ahead. None of the dependencies are on main yet (crafting-core, clay-ceramics, atmosphere #9 all open).

## Open questions / blockers
- Blocked on other lanes merging to main: crafting-core (Items.cs, WarmthAt), claude/clay-ceramics-kfr4kr (water pots), Atmosphere PR #9 (weather). Design question for later, to the PM: how cold should night feel at the gentle rates? Default: cold drain scales from 1x at 15 °C to 2x at 0 °C, rain adds +50% unless covered.

## Next step
`git fetch origin main` and check whether crafting-core, clay-ceramics or Atmosphere #9 has merged; start with whichever landed first.
