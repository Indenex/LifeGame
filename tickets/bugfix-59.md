# bugfix-59: AnimalCheck snare race
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa
PR: #830

## Goal
AnimalCheck sometimes failed with "timed out waiting for the caught rabbit to leave the field". The snare could catch the first rabbit while the third was still being placed, so a count taken after the spawns was already one short. `WaitForCatch` now counts before spawning. Done means AnimalCheck passes steadily.

## Progress log
- 2026-10-17: fix in 5fd43f2b; #830 green on it (run 39043716041). Main then took #819 (bugfix-54 with HiddenBackfill.cs, which covers the D-326 backfill), so on merging main #830 drops its own EnsureDeviceStatus and keeps only this test fix.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #830's merge head; send the green sha to the PM.
