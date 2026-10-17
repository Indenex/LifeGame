# bugfix-54: Hidden values, family 2 (DeviceJob and Roast)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa (continues claude/bugfix-54 on the new account)
PR: #819 (claude/bugfix-54) superseded by the PR from claude/bugfix-rnwmoa

## Goal
Part of audit item 7 (see tickets/bugfix-53.md for the audit, the rules and the stage B plan). Other players' clients no longer receive a device round's Score, InputQuality, Cap, Token or WorkerId, nor the quality of food on a fire. Crafting confirmed no client or tool reads those fields.

- `DeviceJob` becomes private (access change; server logic unchanged). New public `DeviceStatus` (StructureId, Owner, PlayerId, ProcessId, Remaining, RoundUnits, RoundStart, RoundEnd, Attended), kept in step by `SaveDeviceJob` / `DeleteDeviceJob` (Devices.cs). Client reads moved to `DeviceStatus`: BuildingSystem.cs, DeviceUse.cs, Subscriptions.Crafting.cs; CraftCheck.
- `Roast.Quality` stays a column (removing it would not migrate) but is written 0. The real value lives in the private `RoastQuality` table (Cooking.cs), read by `RoastInputQuality` when a roast is taken or the fire burns out; `DeleteRoast` removes both rows.

## Progress log
- 2026-10-17: server, client and tools build. Local CraftCheck passes (devices and roast) and the Upgrade check passes against origin/main.

- 2026-10-17 15:10: moved to this account. Added EnsureDeviceStatus for rounds running at upgrade time (D-326). No local toolchain here (dotnet and SpacetimeDB downloads blocked), so CI is the build check.

## Open questions / blockers
- (fixed, D-326) A device round already running when this lands got no DeviceStatus row until its next round. Now `EnsureDeviceStatus` (Devices.cs, called from PlayerConnected) fills missing rows; the upgrade drops every client, so the first one back fills them in.
- A roast already on a fire has no RoastQuality row, so its quality reads as raw (a one-off, small).

## Next step
Wait for CI on the claude/bugfix-rnwmoa PR, then send the PM the green sha and ask it to close #819.
