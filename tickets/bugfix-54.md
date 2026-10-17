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
- 2026-10-17: server, client and tools build. Local CraftCheck passes (devices and roast) and the Upgrade check passes against origin/main.

## Open questions / blockers
- A device round already running when this lands has no DeviceStatus row until its next round starts; clients just don't show the running state for that one round. A roast already on a fire has no RoastQuality row, so its quality reads as raw (a one-off, small).

## Next step
Wait for PR CI, then send the PM the green sha.
