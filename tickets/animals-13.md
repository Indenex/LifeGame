# animals-13: Sheep through the seasons (D-099)
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: in_review
Branch: claude/animals-seasons-5gkntn (stacked on Atmosphere's claude/atmosphere-16-seasons, PR #173)
PR: (none)

## Goal
Sheep live through the seasons Atmosphere adds (D-099; one in-game day per season, a 6-hour year, D-114). D-135 (Justin picked Q9 a): flocks huddle tighter and graze less in snow, and lambs are born only in spring (up to 2 per flock each spring instead of one per in-game day all year). Done when Justin sees a flock bunched in the snow in winter and new lambs in spring.

## How it works
- Atmosphere's API (#173): `SeasonNow(ctx)` (Year 0-1, Season, Warmth) and `SnowCover(ctx)` 0-1.
- Huddling (Animals.cs): AnimalTick reads `huddle = SnowCover` once a tick and passes it to AnimalThinkFlock/AnimalDecide. With full snow: grazing share 0.75 -> 0.30, leader wander chance x0.3 and wander distance x0.4, followers regroup from 1.5 m instead of 3.5 m (stragglers 3.5 m instead of 6.5 m) to a ring half as wide, shuffling to fresh grass x0.4. Lambs follow their mothers as before (Lambs.cs).
- Spring lambing (Hunting.cs FlockRegrow): with 2+ grown sheep, a flock below its size gets a lamb only in Spring, with a gap of 0.45 of a season between births (`LambGapMicros`, from the season clock), so at most two a spring. A flock down to 0-1 adults still gets a stray adult about once an in-game day in any season ("never below 2").
- No schema change; no client change (the flock's bunching shows through positions and states).

## Progress log
- 2026-10-16 23:00: checked Environment's env-2 land reshape (ee46b71, lands with the D-143 wipe): all 4 starting flocks settle on dry ground (spawn (0,-2) +0.23 m; (-36,5) -0.38; (39,-48) +0.11; (-31,-15) -0.28); in 90 s no sheep stood below -0.81 m (water -1.0, SheepDryLevel -0.85). No flock spot changes needed.
- 2026-10-16 21:40: D-135: Justin picked (a), behaviour plus spring lambing.
- 2026-10-16 21:35: API received from Atmosphere; Q9 (what seasons change for sheep) sent to the PM.

- 2026-10-16 23:20: started (D-145: never idle). Written and builds. Local bot test (scratch SeasonCheck, TEMP TestSnow/TestYear/TestCull reducers, not committed), spawn flock watched 120 s each: no snow: mean distance from the flock's middle 1.53 m, grazing 69%, 17.7 m walked each; full snow: 1.01 m, 23%, 15.4 m. Winter: a flock culled 5 -> 3 grew no lambs in 6 s; set to spring: +1 lamb within 6 s, no second lamb 10 s later. Upgrade check against the seasons branch passed (against main it fails on the seasons branch alone: Survival's vitals columns; Atmosphere's to merge).

## Open questions / blockers
- Waits for Atmosphere's #173 (seasons) to merge.

## Next step
Open the PR against claude/atmosphere-16-seasons (retargets to main when #173 merges); send a render request (flock bunched in snow, a spring lamb); send the PM next-animal options.
