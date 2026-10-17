# crafting-31: Walking off ends the job
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-cancel (harness branch claude/...; see PR)
PR: (see below)

## Goal
Justin's bug: "when chopping a tree down, I tried to walk away and it just kept chopping, playing the animation, sound effects, etc. until the tree was felled." Any job in progress now ends when the player leaves it, on both sides:
- Server (D-182, `server/spacetimedb/CancelJob.cs`): `CancelJob` removes the job, its timer, the work row (sounds/animation) and gives back what it took. Called when the player moves beyond reach (+0.5) of the job's target (`ApplyMoves` -> `CheckJobReach`), when a chop's axe leaves the hand (`EquipSlot`/`SyncEquip` -> `CheckJobTool`), or by the new `CancelCraft` reducer. Nothing completes remotely. One place for every craft job (chop, station, fire, roast, placed).
- Client: `CraftingUi` calls `CancelCraft` at once on moving off (0.2 m), a full panel opening, or a change of what is in hand, hides the progress bar, stops every hold-repeat (`HoldRepeat.StopAll`, new) and shows "Stopped chopping a log". `HoldRepeat.MoveAway` is now 0.3 m (was 1.5). Releasing the key already stops the repeat after the current swing.
- CraftCheck: starts a chop, walks 3 m away, the job stops with no log; putting the axe away and `CancelCraft` stop it too.
Dig, gather and shear are instant reducers with cooldowns (nothing runs in the background), so the client stop covers them.

## Progress log
- 2026-10-17: built; local CraftCheck passes with the new cases.

## Open questions / blockers
- Tell Integration (HoldRepeat/Use owner) and Animation (the swing stops when the job row goes: AvatarWork already ends on `MyCraftJob.OnDelete`).
- Inputs of a cancelled job are returned at the job's quality (close to, not exactly, the original average).

## Next step
None: merged (#614, #701, #742). Later sections below are history.
