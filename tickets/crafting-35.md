# crafting-35: D-182 audit of the crafting reducers
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-audit
PR: (see below)

## Goal
Check every reducer the Crafting lane owns for what the client can send that the server should decide (CLAUDE.md, D-182). Audited 2026-10-17 on main (after #285): StartCraft, StartFireAt, FinishCraft, FeedFire, BurnOutFires, CancelCraft, EquipSlot, UseHeld, SetToolWearStep, RepairTool, PlaceItem, ShearSheep, StrikeAnimal, ButcherCarcass, TakeOffFire, LoadDevice, WorkDevice, FinishDeviceRound, CollectDevice, RegrowTrees.

## Sound (checked in code)
- Scheduled reducers (FinishCraft, BurnOutFires, FinishDeviceRound, RegrowTrees) call `RequireServer`.
- Reach is checked on the server by position it holds (RequireReach / WorkReach / the new job leash): FeedFire, TakeOffFire, PlaceItem, ShearSheep, ButcherCarcass, StrikeAnimal, LoadDevice/WorkDevice/CollectDevice, StartFireAt (spot reach plus ground rules via the shared `FireGround`).
- Ownership: TakeOffFire (roast owner), devices (claim rights), RepairTool and PlaceItem (the exact stack must be in the bag; `Me(ctx)` everywhere, guarded by `tools/check-ctx-sender.sh`).
- Tools must be equipped for chop, dig, shear, butcher, strike and bow (Equip.cs); unknown item or process ids are refused; counts are capped (`Devices.BatchCap`); floats are checked finite (StartFireAt, PlaceItem, Move).
- A running job ends when the player walks off, drops the axe or cancels (CancelJob.cs).

## Gaps (not fixed here: each needs a design call)
1. **Minigame score is client-reported.** `StartCraft(processId, score)`, `StartFireAt(..., score, ...)` and `WorkDevice(structureId, score)` take the score of a hands-on minigame played on the client; the server only clamps it to 0..1. A modified client can always send 1.0: best quality the skill cap allows, and never a failed strike. The skill cap (`QualityCapFor`) still bounds the result, so it is a quality edge, not an item dupe. Options (recommended first): (a) keep as is until outside players join, then add a server-side plausibility rule: the score is only accepted if the minigame's minimum play time has passed since the player opened it (server records the "opened" moment via a small reducer) and scores of 1.0 repeated N times in a row are capped; (b) move the check to a server-run minigame (largest change); (c) accept it for a cosy game and document it.
2. **An attended device round can be worked and then abandoned.** `WorkDevice` shortens the round (Attended) but nothing checks the player stays; they can send a score and walk away. Impact is only the speed-up (AttendedTime vs UnattendedTime) and the score quality. Fix when wanted: in `CheckJobReach`, if the player has an attended `DeviceJob` and leaves the device's reach, revert it to the unattended schedule (new token, recompute `RoundEnd`).
3. **No gate on which processes a player may start.** Any known process id can be started if the inputs, tool and station are present; "discovering" a recipe is a journal/hint matter, not enforced. Likely intended (D-081: fun first); noting it so it is a decision, not an accident.

## Next step
PM: pick an option for gap 1 (and whether gap 2 should be fixed now); then a follow-up ticket for the chosen fix.
