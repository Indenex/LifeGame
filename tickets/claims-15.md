# claims-15: Outside-claim weathering pass (Justin: untouched for 48 h)
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_review
Branch: claude/claims-15-outside-weathering
PR: (none yet)

## Goal
Justin's rule (via the PM, 2026-10-16): house-type buildings are claim-only; only basic survival builds (lean-to, clay hut, fires and hearth, crafting table, basket and jar) may stand outside claims; anything outside a claim that sits untouched for 48 h gets extreme weathering, to keep the world clean and the server light. House building does the placement rule (building-13). Claims owns the outside-claim weathering pass: structures, fires, dropped items, at 48 h untouched.

## What exists (read 2026-10-16)
- `server/spacetimedb/Weathering.cs` (House building's file, D-046): hourly `WeatherSweep`. `StructureTouch` (public) and `GroundItemTouch` hold last-touched times; things inside a claim get their clock reset (Claims' `InClaimHook`); outside, a structure goes after `WeatherAfterFor(kind) + WeatherForHours` untouched, an item after `WeatherAfterHours + WeatherForHours`. Constants in `shared/Structures.cs`: WeatherAfterHours 72, WeatherForHours 24; per-kind overrides 168 h and 336 h (houses).
- Fires already burn out by fuel (`FuelUntil`), leave a `ColdHearth` that clears after `Fires.ColdSeconds` (`ClearOldHearths`, outside claims only).
- Not covered by the sweep and possibly player-made: `HideRack` (Leather.cs), `ClayPiece` (Ceramics.cs), `SimmerPot` (CookingPot.cs), `Roast` (Cooking.cs, tied to fires), terrain edits (dug holes). Kiln is a structure, so it is covered.

## Proposal (sent to the PM)
(a) [recommended] 48 h untouched starts weathering, gone 6 h later, for everything outside a claim; drop the per-kind house overrides; extend the sweep to HideRack, ClayPiece and SimmerPot. (b) gone at 48 h, no fade window. (c) 48 h only for fires and dropped items.
Split with House building: they keep Weathering.cs and Structures.cs kinds; Claims changes the constants and adds the missing tables in its own file.

## Progress log
- 2026-10-16: survey done, options and split sent. Waiting on the PM (timing) and House building (any pending change to Weathering.cs or the constants).
- 2026-10-16: the old House building session agreed to the split. As of its handoff nothing of theirs touches Weathering.cs or the constants. Watch building-10 (log cabin, branch claude/house-building-8ueqz2-cabin, no PR yet): its shape sets `WeatherAfter = 168` like the brick house; if the per-kind overrides go, the cabin's goes too. The new House building session (reach it through the PM) needs to confirm nothing has changed since 20:05.
- 2026-10-16: PM picked (a) for Justin ("starts falling apart after 2 days"). Done: WeatherAfterHours 48 and WeatherForHours 6 in shared/Structures.cs (one raisable constant each, D-197); the three per-kind overrides (mud-brick house 168, storage jar 336, log cabin 168) removed; new server/spacetimedb/WeatherOutside.cs sweeps clay pieces and free-standing hide racks (private WeatherSeen table: first seen by the hourly sweep, removed 54 h later; any use removes the row first; clock keeps resetting inside a claim); one call line added at the end of SweepWeathering in Weathering.cs. Simmer pots and roasts hang on fires and go with them, so they are not swept. Locally: server builds, upgrade check over origin/main passes, SocialTest passes; no test exercises the hourly sweep (it needs the server's own identity). Client weathering visuals read the same constants.
- 2026-10-16 23:41: PR #345 head f3bb8012: CI green (https://github.com/Indenex/LifeGame/actions/runs/38981326325). Sent the PM the sha.

## Open questions / blockers
- Timing pick (a/b/c) from Justin via the PM.
- House building: any in-flight edit to Weathering.cs / the weather constants?

## Next step
Wait for the PM to merge #345. Tell the new House building session that the cabin's WeatherAfter override is gone on main once this merges.
