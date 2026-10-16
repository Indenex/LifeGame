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
- 2026-10-16 23:55: Justin picked all three (a, b, c), via the PM. Added (b) god rays to the prototype (client 0 errors). At a low clear sun: sun volumetric energy +1.8 x rays, volumetric fog density x(1 + 1.2 x rays), anisotropy up to 0.85, everywhere rather than only in woods. Quality level `Atmosphere.GodRays`: 0 off, 1 normal, 2 high (x1.25, fog length 112 m instead of 64). `--godrays=0|1|2` overrides it, and UX is asked for an Options line. Still behind --grade until Justin sees the renders.
- Justin's original reference screenshots are not in this container or the repo. I asked the PM to have them re-shared (ideally into docs/reference/) and am tuning to D-002's wording meanwhile.
- 2026-10-17 00:00: UX (session_01V6qgbNbvTwniqW946KACLr) asked me to carry the Options line. client/scripts/ui/OptionsPage.cs gets a Graphics heading and a "Light rays: Off / Normal / High" button that cycles Atmosphere.GodRays and saves it as graphics/light_rays. ReadyGrade loads it (default 1), and --godrays still overrides it. UX may restyle later.

## Open questions / blockers
- Render with/without pairs, then Justin's sign-off (D-154), before making --grade the default.
- Justin's reference screenshots (re-share requested).

## Next step
Wait for Justin's pick (via the PM) and the Render with/without pairs (requested from session_015S96hNrEu92FrCoNZqZZjs). Then make the picked option the default and open the PR.
