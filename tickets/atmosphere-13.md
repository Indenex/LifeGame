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

## Open questions / blockers
- Sent the PM options A/B/C (A recommended). Sent Environment a split proposal (they own world stuff and placement; I own time, weather and the global shader params).

## Next step
In Atmosphere.cs's sun/fog update, add a low-sun forest term: fog density x (1 + k * woods * lowSun * fair), anisotropy toward 0.8 at low sun; then send a render request (woods at --debug-position=-44,-46, times 0.27, 0.35, 0.5, 0.72, fair and --cloud=0.9).
