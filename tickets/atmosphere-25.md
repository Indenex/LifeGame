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
- 2026-10-17 00:05: retuned to Justin's reference images (docs/art-reference/, #368). The references are faded and warm with lifted violet blacks, so contrast now goes down (1.08 to 0.96), not up. A colour-correction curve lifts darks to (0.075, 0.065, 0.11) and eases lights to cream (0.98, 0.96, 0.89).
  - Shadows are blue-violet all day: ambient toward (0.40, 0.42, 0.66), 30% plus another 30% at low sun. Snow therefore reads warm white with blue shade.
  - Gentle bloom: HDR threshold 1.1 to 0.85, so fire glows; intensity +0.15 and bloom +0.06, swelling further at low sun.
  - The far land gets a little softer: the camera's existing far depth-of-field blur goes 0.05 to 0.08.
  - There is no lava in the game, so the emissive glow applies to fires and lit doorways.
- 2026-10-17 00:15: checked against Justin's licensed Golden Skull packs (docs/art-packs/golden-skull/, #378) by sampling dominant colours.
  - Grass: olive-yellow, lit (0.65, 0.68, 0.44) and (0.81, 0.79, 0.44), mid (0.47, 0.47, 0.33).
  - Darks are violet-tinted, (0.22, 0.20, 0.24); stone is violet-grey, (0.31, 0.27, 0.35) to (0.62, 0.62, 0.62).
  - Snow is warm white (0.96) with blue shade (0.63, 0.69, 0.83); earth is warm brown (0.27, 0.18, 0.12).
  - The violet-lifted darks and blue-violet shadows match, so they stay. The pack is muted, so the grade now lowers saturation (-0.06) instead of raising it.
  - Our grass palette (Look.GrassLow (0.30, 0.46, 0.13), GrassHigh (0.48, 0.58, 0.17)) is greener and more saturated than the pack's olive-yellow. That is a palette (D-154) call for Environment and Modeling; flagged to the PM.

## Open questions / blockers
- Render with/without pairs, then Justin's sign-off (D-154), before making --grade the default.
- (Reference images are now on main in docs/art-reference/.)

## Next step
Wait for Justin's pick (via the PM) and the Render with/without pairs (requested from session_015S96hNrEu92FrCoNZqZZjs). Then make the picked option the default and open the PR.
