# crafting-17: Hook for hearth fires that feed themselves (D-149)
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-hearth-topup
PR: #194

## Goal
D-148/D-149: a fire on a raised hearth feeds itself from storage nearby, so it never needs tending (open campfires still do). House building (building-9) does the feeding. Crafting adds `FireTopUpHook(ctx, fire, ref fuelUntil)`, declared in Hearths.cs and called at the top of BurnOutFires for every fire, before the burned-out check. A changed FuelUntil is written back. With no implementation it does nothing.

## Progress log
- 2026-10-16 19:55: hook added on main; server builds.

## Open questions / blockers
- (none)

## Next step
None. Merged to main; kept for history.
