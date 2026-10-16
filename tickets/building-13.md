# building-13: Claim-only buildings (Justin)
Lane: House building
Owner: House building thread (Sonnet)
Status: in_progress
Branch: claude/house-building-8ueqz2-wild
PR: (not yet)

## Goal
Justin: "House-type buildings HAVE to be built inside of a claim ONLY. Temporary shelters or the small hut can be built outside, and basic crafting tables, but not much else. Essentially, basic survival, and anything outside of a claim has extreme weathering applied to manage server performance and world 'cleanliness'. The idea is players can cooperate and build cities and stuff together."
His picks: allowed outside a claim: lean-to, clay hut, campfire and hearth, crafting table, storage basket and jar. Everything else is claim-only: brick house, log cabin, tiled house, kiln, stations, devices, repair table, fences and gates. Anything outside a claim untouched for 48 h starts falling apart, quickly.

## Progress log
- 2026-10-16: built as data. `StructureShape.Wild` (shared/Structures.cs) is true for lean-to, clay hut, claim marker (it makes the claim, so it has to stand outside one), storage basket, storage jar, crafting table and raised hearth (which only goes inside a hut or house anyway). `Structures.IsWild(kind)`.
  - Server: `PlaceStructure` calls new `CheckClaimOnly`: a non-wild kind needs a claim the builder owns or belongs to ("A brick house has to stand inside your claim"). `CheckStationSite` skips wild stations (the table), and `StationNear` lets a wild station be used on open ground (in a claim, only members, as before). Campfires are Crafting's fires, not structures: unchanged. Fires burn out and cold hearths clear on their own timers, so only structures and dropped items needed the 48 h rule.
  - Weathering (Weathering.cs, D-046) already existed: outside a claim, untouched things weather and go. Numbers now: `WeatherAfterHours` 72 -> 48 (Justin), `WeatherForHours` 24 -> 6 ("falls apart quickly", my pick; ask PM to confirm), and the per-shape overrides (house, cabin 168 h, jar 336 h) are gone, so everything outside a claim weathers the same. Dropped items use the same clock (gone after 54 h). Inside a claim nothing weathers.
  - Client: `PlaceProblem` shows "A <kind> has to stand inside your claim" when the ghost would be refused; `BuildingSystem.NeedsClaimHere(kind)` for the build menu to grey tiles with "Needs a claim".
  - Tests: BuildTest raises a claim (claim marker, B made a member for gathering and building, taken out again for the "B is a stranger" door and gate checks) before building fence, gate, brick house and log cabin, and checks a brick house is refused outside a claim. CraftCheck now expects a spindle station (not the crafting table) to be refused outside a claim. SocialTest unchanged (lean-to only). ClayCheck (Ceramics') places a kiln outside a claim and needs a claim raised first; told Ceramics.
  - Local: server and client build; BuildTest default (344 s), CraftCheck and SocialTest pass; log-cabin run below.

## Open questions / blockers
- PM: "quickly" is WeatherForHours = 6 (options: 6 h [built, recommended], 12 h, 24 h as before). Existing houses outside claims on Justin's world weather at 48 h too (he is wiping the world for the release anyway).
- Claims (ruins-on-lapse code): lapsed claims drop the claim; buildings left standing then weather by this clock. Nothing for Claims to change unless they want different numbers.

## Next step
Wait for CI on the building-13 PR and send the PM the green sha; then building-12 step 2 (ModelId) and the Tiled house for Ceramics.
