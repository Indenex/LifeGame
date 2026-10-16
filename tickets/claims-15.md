# claims-15: Outside-claim weathering pass (Justin: untouched for 48 h)
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_progress (design: waiting on the PM's timing pick)
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

## Open questions / blockers
- Timing pick (a/b/c) from Justin via the PM.
- House building: any in-flight edit to Weathering.cs / the weather constants?

## Next step
When the PM answers: implement the pick (constants plus a new server/spacetimedb/WeatherOutside.cs for the untracked tables), build, run SocialTest and the upgrade check locally (see claims-13 ticket for the toolchain recipe), open the PR.
