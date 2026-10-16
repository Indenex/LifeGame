# survival-11: Breath in cold air
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-next
PR: (none yet)

## Goal
D-144: in cold air every player's breath shows as a soft white puff every few seconds (no camera shake). Denser and longer-lasting the colder it is; none above about 6 C. Remote players too.

## Design
- Animation (session_019roNRWQBPw4ozNLDhzseig, anim-2) draws the puffs: a small faint puff from each nearby player's mouth every ~3.5 s, pooled particles; Atmosphere reviews the look (unshaded blue-white, alpha ~0.35, no emission; fade in heavy snow or at night if too bright).
- Air temperature on the client comes from Atmosphere: `Atmosphere.AirCelsius` (#173), the server's AirTemperature published in SeasonState.
- Survival supplies the curve in shared/Needs.cs: `Needs.AirCold(celsius)` (0 at 15 C .. 1 at 0 C; the cold drain is 1 + this) and `Needs.Breath(airCold)` (0 below about 6 C .. 1 at 0 C). Animation reads `Needs.Breath(Needs.AirCold(Atmosphere.AirCelsius))`, so breath and cold always agree. No server field.

## Progress log
- 2026-10-16: assigned (D-144). Proposal sent to Atmosphere.

- 2026-10-16: first built a server Vitals.AirCold + SurvivalClient.AirColdness; dropped it before merge once Atmosphere exposed Atmosphere.AirCelsius (single source, no extra server writes). Kept Needs.AirCold / Needs.Breath. Local: module and client build.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #185, send the green sha to the PM; Animation builds the puff once #173 and #185 are on main.
