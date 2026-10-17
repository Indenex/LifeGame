# model-9: Stone-ringed hearth model
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: done
Branch: claude/model-9-stone-hearth
PR: (opening)

## Goal
The model for House building's stone-ringed hearth (D-286): an outdoor fire ring about 0.5 m round, sturdier than the campfire, built in two stages (ring stones, then a kerb stacked two high with clay chinking). Model id "StoneHearth" in StructureModel; House building lands the StructureKind and shape.

## Progress log
- 2026-10-17 06:30: `StructureModel.StoneHearth()` (client/scripts/building/StructureModel.StoneHearth.cs): stage 0 an ash bed (filled shallow dish) with a few small stones and a first course of 11 big fieldstones turned along the circle; stage 1 dark clay mud chinking and a second course set over the joints, leaning a touch inward. Dispatch `"StoneHearth" => StoneHearth()` added. Showcase `stonehearth` (both stages beside the player).

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR; send the PM the green sha; tell House building it is on main.
