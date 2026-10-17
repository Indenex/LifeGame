# bugfix-54: Hidden values, family 2 (DeviceJob and Roast)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-54
PR: #819

## Goal
Part of audit item 7 (see tickets/bugfix-53.md for the audit, the rules and the stage B plan). Other players' clients no longer receive a device round's Score, InputQuality, Cap, Token or WorkerId, nor the quality of food on a fire. Crafting confirmed no client or tool reads those fields.

- `DeviceJob` becomes private (access change; server logic unchanged). New public `DeviceStatus` (StructureId, Owner, PlayerId, ProcessId, Remaining, RoundUnits, RoundStart, RoundEnd, Attended), kept in step by `SaveDeviceJob` / `DeleteDeviceJob` (Devices.cs). Client reads moved to `DeviceStatus`: BuildingSystem.cs, DeviceUse.cs, Subscriptions.Crafting.cs; CraftCheck.
- `Roast.Quality` stays a column (removing it would not migrate) but is written 0. The real value lives in the private `RoastQuality` table (Cooking.cs), read by `RoastInputQuality` when a roast is taken or the fire burns out; `DeleteRoast` removes both rows.

## Progress log
- 2026-10-17: PM asked for the live-device backfill; added HiddenBackfill.cs and tested it as described below.
- 2026-10-17: server, client and tools build. Local CraftCheck passes (devices and roast) and the Upgrade check passes against origin/main.

- 2026-10-17 15:10: head 63df553 (PM-requested backfill added; main merged). Earlier heads 3623cc0 and 50fc658 were green or superseded; CI on 63df553 had not started when this was written (the Main priority workflow may be holding PR runs: do not re-run or push to retrigger). Local: CraftCheck, Upgrade check and the backfill test passed on the pre-backfill code; the backfill itself was tested with a throwaway reducer (not committed).

- 2026-10-17 15:15: PM named 63df5531 as the code sha to merge. Commits after it on this branch are ticket-only (no code changes), so the PR head moves but the code is the same. CI result: no check runs were reported on #819 for 63df553 or the later ticket-only heads when this was written (the Main priority workflow may be holding PR runs). Earlier code heads 3623cc0 and 50fc658 had run or been superseded. Do not re-run or push to retrigger; look at the PR's check runs, and merge once the head's CI is fully green.

## Open questions / blockers
- Rows live at upgrade time: `BackfillHiddenValues` (HiddenBackfill.cs, called from PlayerConnected) gives every DeviceJob without a DeviceStatus its row, and moves the quality of any roast already on a fire into RoastQuality (zeroing the public column). It runs on each connection and skips rows already carried over. Checked with a throwaway reducer (not committed) that faked a pre-upgrade job and roast: status row appeared, quality 77 moved to the private row, public column 0, and a second run changed nothing. A walk-away device therefore shows again as soon as anyone connects after the upgrade.

## Next step
Check CI on #819 head 63df553 (github check runs for the PR); when fully green send the PM the full sha. Then family 3 (BasketItem, ClayPiece) per tickets/bugfix-53.md once the PM relays the Ceramics and Crafting field answers.
