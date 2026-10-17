# atmosphere-39: Clear, warm golden hour (no pink veil)
Lane: Atmosphere
Owner: World atmosphere lane (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-39-dusk-clear
PR: (opening)

## Goal
Render A's ground shots at time 0.72 (renders/ground31b-botw-4b355f5-5/6/7.png on claude/render-1-013qe8) were a murky red-brown under a heavy pink haze. The grass read dark, the sheep and player were hard to pick out, and pale stones glowed pink: gloomy dusk, not golden hour. Aim (PM): warm, clear and readable at 0.65 to 0.75.

Cause:
- **Fog depth:** the follow camera looks down from 20 to 30 m away, so even the player sits that deep in the air. At the 0.010/m combined cap, about 20 to 25% of the view near the player was fog.
- **Colour:** the cosy grade then coloured that fog rose (0.96, 0.74, 0.70 at 45%) and lifted the shade toward rose-lavender.

Fix:
- `AtmosphereFogCap.cs`: the combined fog cap now depends on the camera-to-player distance, so at least 90% of the light reaches the camera from the player (about 0.004/m at 25 m, never below 0.003). The far land still fades.
- `AtmosphereGrade.cs`: the golden-hour fog is a light warm peach (0.96, 0.84, 0.74 at 30%), and the low-sun shade is less pink.

## Progress log
- 2026-10-17 11:25: built; the client compiles with 0 errors.

## Open questions / blockers
- Needs Render A or B before/after (main vs this branch) at 0.65, 0.72 and 0.75 in the ground31b view.

## Next step
Open the PR and request the before/after; send the PM the green sha.
