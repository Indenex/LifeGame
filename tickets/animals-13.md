# animals-13: Sheep through the seasons (D-099)
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: todo
Branch: (not started; ticket kept on claude/animals-clothing-5gkntn until work begins)
PR: (none)

## Goal
Sheep live through the seasons Atmosphere adds (D-099; one in-game day per season, a 6-hour year, D-114). D-135 (Justin picked Q9 a): flocks huddle tighter and graze less in snow, and lambs are born only in spring (up to 2 per flock each spring instead of one per in-game day all year). Done when Justin sees a flock bunched in the snow in winter and new lambs in spring.

## How it works (plan)
- Atmosphere's API (branch claude/atmosphere-16-seasons, c41b1e6; reaches main after #136): `Module.SeasonNow(ctx)` returns (Year 0-1, Season Spring/Summer/Autumn/Winter, Warmth -1..1); `Module.SnowCover(ctx)` and `Snowfall(ctx)` 0-1. Admin test commands: `/season winter`, `/season-speed 0.25`.
- Flock tick (Animals.cs): wander radius and grazing share scale down with SnowCover; regroup distance tightens. FlockRegrow (Hunting.cs) only in Spring, up to 2 births per flock per spring, keeping the "never below 2" stray rule all year.

## Progress log
- 2026-10-16 23:00: checked Environment's env-2 land reshape (ee46b71, lands with the D-143 wipe): all 4 starting flocks settle on dry ground (spawn (0,-2) +0.23 m; (-36,5) -0.38; (39,-48) +0.11; (-31,-15) -0.28); in 90 s no sheep stood below -0.81 m (water -1.0, SheepDryLevel -0.85). No flock spot changes needed.
- 2026-10-16 21:40: D-135: Justin picked (a), behaviour plus spring lambing.
- 2026-10-16 21:35: API received from Atmosphere; Q9 (what seasons change for sheep) sent to the PM.

## Open questions / blockers
- Answered D-135: behaviour plus spring lambing. Waiting for the seasons API on main.

## Next step
When the seasons API is on main (after #136 and Atmosphere's seasons PR), branch from main and implement D-135.
