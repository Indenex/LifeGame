# atmosphere-25: Valheim-grand lighting grade (prototype)
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-25-grade
PR: (none yet)

## Goal
Justin's art direction (2026-10-16, via the PM): "closer to Valheim", cutesy, "beautiful/awe inspiring with lighting". Options sent to the PM: (a) a time-of-day colour grade (recommended), (b) big god rays at low sun, (c) richer sky with glowing cloud rims. Per D-154, this stays a prototype behind a flag until Justin picks: `--grade=1` turns on (a) + (c), and the default is off. Done when Justin has picked from Render shots with and without the grade, and the pick is built as the default.

## Progress log
- 2026-10-16 23:45: prototype (client 0 errors). New client/scripts/atmosphere/AtmosphereGrade.cs, called at the end of ApplyLight. All of it is scaled by grade (0 to 1, from --grade):
  - At a low clear sun: sun light toward honey (1, 0.70, 0.40) by 35% and +15% energy. Ambient colour toward cool blue-teal (0.30, 0.42, 0.64) by 55%, with less sky contribution (0.75 down to 0.40), so the shadows read cool. Bloom intensity +0.35 and bloom 0.08 to 0.20.
  - At night: ambient toward deep blue (0.12, 0.18, 0.40) by 35%.
  - Always: saturation +0.08 and contrast 1.08 to 1.13.
  - Sky shader `grade` uniform: clouds near a low sun get warm rims from the sun colour.

## Open questions / blockers
- Justin's pick among (a), (b) and (c) (via the PM).

## Next step
Push, then ask Render for pairs with and without --grade=1 at golden hour (0.70), dawn (0.26) and night (0.92).
