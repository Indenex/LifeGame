# atmosphere-13: Light through the forest
Lane: World atmosphere and sound (visual side; audio moved to the Audio lane, D-079)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-13-forest-light (stacked on claude/atmosphere-sound-hlqfke / #66)
PR: (after #66)

## Goal
Option A of three sent to the PM 2026-10-16 (recommended; the default until the PM or Justin picks). Mornings and evenings in the woods get visible sun shafts slanting between the trunks and dappled light on the ground, strongest at low sun on fair days, gone under overcast or rain. Built on the volumetric fog Main.cs already enables (Forward+): fog density and anisotropy rise at low sun in wooded places (Atmosphere.Woodedness), with the sun's volumetric energy following. Compatibility has no volumetric fog, so it gets a cheap fallback (a few soft additive light-shaft cards toward the sun near the player) or nothing, decided by cost. Paired with Environment (D-078), whose tree density shapes the shafts. Done means Render lane shots of the same woods spot at dawn, mid-morning, noon and evening, fair and overcast, plus a PR green after #66.

## Progress log
- 2026-10-16: ticket opened. Renderer is Forward+ (project.godot sets no rendering_method); Main.cs enables volumetric fog (density 0.007, anisotropy 0.6, length 64); Atmosphere.cs drives VolumetricFogDensity from mood.MistDensity and storm, and sun.LightVolumetricFogEnergy from sun height.

- 2026-10-16: pushed dc26f57. Atmosphere.ApplyLight: shafts = woods (Woodedness eased over 3 s) x lowSun (smoothstep 0.02-0.12 sun height, gone by 0.3-0.55) x fair ((1-overcast)^2 x (1 - 2*rain)); volumetric fog density x(1 + 1.6 shafts), anisotropy 0.6 + 0.25 shafts, sun fog energy + 1.4 shafts. Client builds; not seen yet (no Godot in this container). Render requested dc26f57 (before 47a151d / after, woods -44,-46 at 0.27, 0.35, 0.5, 0.72 fair; 0.3 overcast; meadow 0.3). Compatibility fallback not done: wait for the shots and Justin first.

## Open questions / blockers
- Sent the PM options A/B/C (A recommended). Sent Environment a split proposal (they own world stuff and placement; I own time, weather and the global shader params).

## Next step
Wait for the Render lane's dc26f57 shots; tune the shaft numbers from them; then decide the Compatibility fallback.
