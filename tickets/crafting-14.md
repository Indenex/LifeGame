# crafting-14: Full fires refuse fuel; longer fires that grow with Firecraft; cold hearths stay in claims
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-fire-cap (stacked on crafting-cold-hearth, PR #92)
PR: #122

## Goal
D-090 (amends D-054), from Justin via the PM on 2026-10-16:
- Feeding a full fire is refused with "The fire is burning as high as it will", and the stick or log is kept. Before, the server took the fuel and then clipped the burn time to the cap, so a full fire ate sticks (Bug fixes lane found it).
- A new fire burns 20 min (was 15).
- How far ahead a fire can be fuelled grows with the feeder's Firecraft rank, from 20 min at rank 0 to 40 min at Master (straight line, per the PM's note that rank effects stay linear).

- D-109 (PM, 18:24): a cold hearth lasts 1 h outside a claim; inside a standing claim it stays as long as the claim does.

## Design
- `shared/Processes.cs` Fires: `StartSeconds` 20 min, `MaxSeconds` 20 min, `MasterMaxSeconds` 40 min, `MaxSecondsFor(rank)`.
- House building's raised hearth (D-132): `BuiltHearthSpotHook` (first in FireSpot) and `FireFuelScaleHook` (LightFire start time; FeedFire fuel and cap), declared in Hearths.cs and implemented by Building.
- Fuel is refused if any of its burn time would go past the cap (D-121), so nothing is ever lost.
- `FeedFire` (Crafting.cs) checks before `Take`. Rank comes from Journal's `SkillRank(ctx, playerId, "fire.hand_drill")` (Firecraft).
- `ClearOldHearths` (Hearths.cs) skips expired hearths where `ClaimAt` finds a standing claim, so they go once the claim is gone.
- The client's fire light clamps to `MasterMaxSeconds`.
- CraftCheck: a freshly lit fire refuses a stick and a log and both are kept; the first fire takes a stick once half a stick fits.

## Progress log
- 2026-10-16 18:10: implemented on top of #92 (4d9512c). Server and client build; CraftCheck passes locally.
- 2026-10-16 18:55: D-109 added. Server builds; CraftCheck passes. The cap numbers match docs/PROGRESSION.md (20 min at rank 0, 28 at rank 40, 40 at rank 100, linear).
- 2026-10-16 19:15: CI run 38948613858 was green on 0f5fef6. Then D-121 (refuse any overflow; CraftCheck now waits up to 150 s for room for a whole stick) and D-132 hooks added. Server and client build; CraftCheck passes.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #122; when #92 merges, merge main in and send the PM the green sha.
