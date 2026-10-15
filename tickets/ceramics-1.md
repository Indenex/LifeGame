# ceramics-1: Clay to fired pot
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session cse_017sscCMDfHcyeZNqGW22GBE)
Status: in_review
Branch: claude/clay-ceramics-kfr4kr (from crafting-core, with crafting-core e165799 merged in)
PR: #11 (base crafting-core; retarget to main after #4)

## Goal
Dig clay at riverbanks, sift and temper, shape pinch and coil pots (short shaping minigame), dry (rain slows it; firing a damp pot cracks it), pit-fire, then an updraft kiln. Also bricks and daub for House building. Done when a player digs clay and ends with a fired pot that holds water, with screenshots of each stage. This is the MVP finish line.

Milestone 1 (this ticket's first PR): dig, clean, temper, shape, dry, pit-fire, fill with water; bricks and daub. Milestone 2: updraft kiln.

## Design (decided by Justin: D-013, D-014, D-015)
- Chain: dig Clay -> `clay.clean` -> Clean clay; `clay.grit` (stone + hammer) or `clay.grog` (potsherd + hammer) -> temper; `clay.temper_grit` (2 tempered) or `clay.temper_grog` (3 tempered) -> `clay.pinch_pot` (Shape minigame) or `clay.coil_pot` (Coil minigame) -> wet pot set down in the world.
- Drying: 8 min in the open, about twice as long in rain, faster beside a fire (shared/ClayRules.cs). Stored as moisture + time + rate in `ClayPiece`, rechecked every 15 s.
- V (tend clay; KEYMAP.md interim until the G Use key) on a piece within reach: beside a burning fire it goes into the fire; otherwise a dry piece is picked up (unfired pot or brick). Firing a damp pot asks first, then cracks it into potsherds. Near a fire with unfired pots in your bag: V sets them into it (up to 4). At the water's edge: V fills every empty fired pot.
- Pit firing 4 min, needs that much fuel in the fire. Crack chance falls with pot quality. Result drops on the ground as a pot or 2 potsherds; a news line tells the maker.
- Items 100 to 112 (range 100 to 129 reserved for this lane). Process ids `clay.*`, plus `clay.pit_fire` and `clay.fill` sent to the skills hooks.
- Hooks added: `Outcome.Placed` + `static partial void OnPlaced(ctx, job, def)` in Crafting.cs; `static partial void RainAt(ctx, x, z, ref rain)` in Ceramics.cs for the weather lane.

## Progress log
- 2026-10-15: lane started (ticket created by the PM from the lane brief)
- 2026-10-15: pot meshes, clay shader (wet, dry, fired, cracked), --ceramics-demo render. Screenshot: screenshots/clay-01-pot-stages.png in project files.
- 2026-10-15: rebased onto crafting-core (+ main). Server: Ceramics.cs, items, clay processes, OnPlaced/RainAt hooks; module builds, bindings regenerated.

- 2026-10-15: client done: Shape/Coil minigames (client/scripts/ceramics/CraftingUi.Clay.cs via a MinigameFor partial hook in CraftingUi.cs), CeramicsView (pieces, labels, T key, news), pot meshes for ground items (hook in ItemLook.MeshFor). Pots drawn at 2x life size.
- 2026-10-15: tools/ClayCheck passed twice on a local server: dug clay -> clean -> grit temper -> pinch pots by a fire -> damp pot cracked into sherds -> other pot dried in ~4 min by the fire -> pit-fired 4 min -> came out whole -> filled with water (quality 70, "fine") -> sherds crushed into grog -> 3 tempered clay.
- 2026-10-15 21:48: opened PR #11; merged crafting-core e165799 (main with House building), pushed ab0c72e. Tend clay key X -> V (PM, X is eat under D-025).
- 2026-10-15 22:00: merged crafting-core 87b303e (main with weather); RainAt implemented in server/spacetimedb/CeramicsWeather.cs from the region Weather row. CI on 2635db4 was green (run 38813062890).
- Screenshots need the Vulkan Forward+ renderer to match Justin's view: apt install mesa-vulkan-drivers, then run Godot without --rendering-driver under xvfb-run (about 2 min per shot on llvmpipe).

## Open questions / blockers
- Sent to PM: Survival lane can drink from pots of water (items 107, 108).
- After Claims PR #8 is on main: call Module.RequireClaimAccess(ctx, x, z) in TendClay, FirePots and OnPlaced.
- After Crafting's crafting-2 Use-key registry is on main (D-025): move V (tend clay) onto the G Use handler.
- V is Survival's eat on this branch until Survival #12 (eat moves to X) is merged; the PM assigned V to tend clay in KEYMAP.md.

## Merge plan (D-032)
PM approved #11. Merge it myself as a merge commit once crafting #4, skills #7 and claims #8 are on main. Merge main in first, regenerate bindings, wait for green CI, then send the PM the merge commit.

## Next step
PR #11 is green on f5033be (CI run 38814135770) and was sent to the PM to merge. Next: Claims access checks once #8 is on main, then milestone 2 (updraft kiln).
