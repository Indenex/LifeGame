# building-17: Stone-ringed hearth
Lane: House building
Owner: House building thread (Sonnet)
Status: done
Branch: claude/house-building-8ueqz2-ring
PR: #518 (merged)

## Goal
Justin's D-286: a buildable outdoor stone-ringed hearth so builders can work through winter nights. A fire lit on it warms out to 8 m (campfire and raised hearth: 5 m). Wild-buildable like the campfire, modest stone cost (5 + 3 stones over two stages), fuel burns 1.5x as long and tops up from nearby storage like the raised hearth (D-148, D-149).

## Progress log
- 2026-10-17: `StructureKind.StoneHearth` appended after TiledHouse (re-append if another kind lands first). `StructureShape.Hearth` (a fire lit within reach goes onto it, fuel scale, top-up) and `.HeatRadius` (0 = plain fire's 5 m) are new data fields; the raised hearth sets `Hearth` too, and `HearthsNear` now checks `Hearth` rather than `Indoor` (the indoor-only placement rule still uses `Indoor`). `Fires.WarmthFrom` takes an optional radius; `WarmthAt` asks a new `FireHeatRadiusHook` (declared in Hearths.cs, implemented in BuildingHearth.cs). Numbers: 8 m, same linear falloff, asked Survival (they own warmth) and defaulting to this until they say otherwise. Scale 1.2, Clearance 0.7, Wild, Category "Fire".
- Merged Survival's survival-21 (#513): the hook is their `FireWarmthRadiusHook`; the shape's `HeatRadius = Fires.StoneRingWarmthRadius` (Survival's constant, now 9 m per their sim and the PM; Justin tunes it there). My own FireHeatRadiusHook is gone. Merged Modeling's model-9 (#516): real `StoneHearth()` model, Radius 0.55, Scale 1.0.
- Tests: BuildTest `stone-hearth` (also `both`) checks the shared numbers, builds it with two players outside any claim, lights a fire on it and checks the server's NearHeat for a player 9.3 m away (cold) and 6.8 m away (warm). Passes locally (32 s). Upgrade check passes. Server and client build.
- No new process id, so no Things to try line. Audio: reusing the fire's crackle; no new cue asked.

- 2026-10-17: merged main with the StructureDef registry (#422) and the coop (#479): the hearth def is in shared/structures/StructuresHouse.cs as `Kind = 19` (ClayTrough = 17, HenCoop = 18); a SpacetimeDB enum cannot carry explicit values, so a def's Kind equals its enum position (see building-16). The cold check in BuildTest moved to 10.5 m now the radius is 9. `both` now includes coop and stone-hearth.

## Open questions / blockers
- #513 (Survival) must merge first: it adds the hook and the constant.
- Stacked on #513 and #516 (merged into this branch). Merge those first, or this PR carries their commits.

## Next step
Nothing; merged.
