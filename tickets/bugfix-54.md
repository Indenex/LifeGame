# bugfix-54: Hidden values, family 2 (DeviceJob and Roast)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-54
PR: (see PM)

## Goal
Part of audit item 7 (see tickets/bugfix-53.md for the audit, the rules and the stage B plan). Other players' clients no longer receive a device round's Score, InputQuality, Cap, Token or WorkerId, nor the quality of food on a fire. Crafting confirmed no client or tool reads those fields.

- `DeviceJob` becomes private (access change; server logic unchanged). New public `DeviceStatus` (StructureId, Owner, PlayerId, ProcessId, Remaining, RoundUnits, RoundStart, RoundEnd, Attended), kept in step by `SaveDeviceJob` / `DeleteDeviceJob` (Devices.cs). Client reads moved to `DeviceStatus`: BuildingSystem.cs, DeviceUse.cs, Subscriptions.Crafting.cs; CraftCheck.
- `Roast.Quality` stays a column (removing it would not migrate) but is written 0. The real value lives in the private `RoastQuality` table (Cooking.cs), read by `RoastInputQuality` when a roast is taken or the fire burns out; `DeleteRoast` removes both rows.

## Progress log
- 2026-10-17: PM asked for the live-device backfill; added HiddenBackfill.cs and tested it as described below.
- 2026-10-17: server, client and tools build. Local CraftCheck passes (devices and roast) and the Upgrade check passes against origin/main.

## Open questions / blockers
- Rows live at upgrade time: `BackfillHiddenValues` (HiddenBackfill.cs, called from PlayerConnected) gives every DeviceJob without a DeviceStatus its row, and moves the quality of any roast already on a fire into RoastQuality (zeroing the public column). It runs on each connection and skips rows already carried over. Checked with a throwaway reducer (not committed) that faked a pre-upgrade job and roast: status row appeared, quality 77 moved to the private row, public column 0, and a second run changed nothing. A walk-away device therefore shows again as soon as anyone connects after the upgrade.

## Next step
Wait for PR CI, then send the PM the green sha.
