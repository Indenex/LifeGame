# animals-34: the rabbit snare
Lane: Animals
Owner: Animals thread
Status: in_progress
Branch: claude/animals-snare-5gkntn
PR: #820 (https://github.com/Indenex/LifeGame/pull/820)

## Goal
PM item (the rabbit pitch Justin's default accepted: "easy first meal, snare trap, fur"). A cord snare, crafted from cordage and two sticks, set on the ground near a warren. Over time it may catch a rabbit that comes within reach; the owner, or the owner or a member of the claim it stands in, takes the catch (meat and fur, no blade); the snare re-sets itself and falls apart after its last catch. What Justin sees: a "Rabbit snare" in the crafting list, a noose set on the ground, a rabbit lying beside it after a catch, and a "Take the rabbit" prompt.

## Decisions (options sent to the PM; building a/a/a, Justin has them)
- Q1 catch rate: (a) about one crossing in ten caught, a rabbit lingering five seconds about as often as not (`CatchPerTick` 0.05 per half second within 1 m). The PM's wording of "about 1 in 4 per pass" was a rough average of crossing and lingering.
- Q2 upkeep: (a) re-sets itself, falls apart after 6 catches (`Catches`).
- Q3 yield: (a) the species' own cuts and fur land beside the snare, quality from the snare capped by the player's Butchery skill; trains Butcher.
- Item id 218 (Animals range 190-219; one id, 219, is left). Recipe `cord.snare`: 1 cordage + 2 sticks, hand craft.

## Progress log
- 2026-10-17: server (Traps.cs, shared/Traps.cs, hooks `OnItemPlaced` in Placing.cs and `PickUpGuard` in Reducers.cs, tick call in Animals.cs), client (Snares.cs), AnimalCheck scenario. Everything builds; AnimalCheck passes end to end locally: snare set, a rabbit within reach is caught and only one, a non-member cannot take the catch or lift the snare, the owner cannot lift it while it holds a catch, a claim member takes it (meat and fur beside the snare, snare re-sets and counts the catch), it falls apart after 6 catches. Removing the pick-up guard makes the check fail.
- Waiting on assets, via the PM: Modeling's snare model (the placed noose; item icon), Audio's sounds in #817 (`snare.set`, `snare.snap`, `snare.collect`; wire with `Sfx.PlayBy`).

- 2026-10-17 14:34: PR #820 open, head 3758f1885640477487594a335f0c3e36c74002b1 (before this ticket update), CI running (see the PR's checks). Integration reviewed the two partial hooks: fine. PM merged nothing of this yet.

## Open questions / blockers
- Waiting on other lanes: Modeling's snare model in #818 (`ItemModels.SnareItem` = Item 218 for ground, hand and icon; `ItemModels.SnareSet` the placed mesh; `ItemModels.SnareSprungWith(furMaterial)` the sprung snare with the caught rabbit; `ItemModels.SnareSprung` the snare alone) and Audio's cues in #817 (`snare.set`, `snare.snap`, `snare.collect`; play with `Sfx.PlayBy(def, where)`, e.g. `CueDefs.SnareSnap`).
- Waiting on Justin: Q1 to Q3 (catch rate, upkeep, what you collect); built as a/a/a, all single constants (`TrapDef.CatchPerTick`, `.Catches`; yield in `CollectSnare`).
- The Godot client could not be run here: the caught-rabbit figure and the prompt were not seen on screen. The placed snare shows whatever the item model fallback is until Modeling's model lands.
- Skills: collecting trains Butcher; a trapping skill is a Skills-lane question.
- The snare is only checked while a player is online near enough to wake the animal tick (same as animals).

## Next step
1. Check #820's CI (it should be green; if red read the failing job's log; AnimalCheck is the likely place, run it locally: dotnet at /tmp/dn from dotnet-install.sh, CLI at /root/stdb, `tools/gen-bindings.sh`, publish, `cd tools/AnimalCheck && dotnet run -c Release`) and send the PM the full green sha.
2. After #818 (Modeling) and #817 (Audio) are on main: merge main into the branch; in `client/scripts/animals/Snares.cs` draw the caught state with `ItemModels.SnareSprungWith(...)` instead of the lying `Sheep` stand-in and make sure the placed snare shows `ItemModels.SnareSet` (check how the generic placed world item is drawn); call `Sfx.PlayBy` for set (on SnareState insert), snap (on Caught going 0 to non-zero) and collect (on Caught going back to 0 or delete). Build the client, push, tell the PM.
