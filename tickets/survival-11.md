# survival-11: Breath in cold air
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-next
PR: (none yet)

## Goal
D-144: in cold air every player's breath shows as a soft white puff every few seconds (no camera shake). Denser and longer-lasting the colder it is; none above about 6 C. Remote players too.

## Design
- Animation (session_019roNRWQBPw4ozNLDhzseig, anim-2) draws the puffs: a small faint puff from each nearby player's mouth every ~3.5 s, pooled particles.
- Survival provides `SurvivalClient.AirColdness` (static, 0..1): Needs.Breath(Vitals.AirCold), starting at about 6 C and full at 0 C. Vitals.AirCold ([Default(0f)], twentieths) = Needs.AirCold(AirTemperature) where the player stands, written by WithSurroundings; the tick rewrites when it changes. The cold drain now uses the same Needs.AirCold, so breath and cold always agree.

## Progress log
- 2026-10-16: assigned (D-144). Proposal sent to Atmosphere.

- 2026-10-16: built AirCold / AirColdness. Local: module builds, publishes over the existing local world (no wipe), client builds.

## Open questions / blockers
- (none)

## Next step
Open the PR from claude/survival-next (with survival-10's tool), wait for CI, send the green sha to the PM; Animation reads SurvivalClient.AirColdness.
