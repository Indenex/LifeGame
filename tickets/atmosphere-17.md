# atmosphere-17: Mist that settles where the land is low
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-17-valley-mist
PR: (none yet)

## Goal
Picked under D-145 after atmosphere-13 to 16; Justin picked A (valley mist at dawn and dusk following the land's low ground, ponds and streams). Dawn and dusk mist lies in valleys, over ponds and along the stream that Environment's reshape (env-2, #175) brings: thicker where the land is low relative to its surroundings, thinning on slopes and hilltops, burning off by mid-morning, and returning faintly at dusk on still, damp evenings. Today's Mist.cs (atmosphere-11) pools by the depth buffer under two planes near the player; this ticket makes the amount follow terrain height (World.BaseHeight) and water, so valleys fill first. Done means Render lane shots at dawn over a valley and a pond after #175, and a PR green.

## Progress log
- 2026-10-16 20:45: started from main (after #217). Options sent to the PM.

- 2026-10-16 20:55: Mist layers now sit 0.55 and 0.9 m above the local low ground (the 20th percentile of World.BaseHeight over an 84 m grid around the player, water counted at its surface, re-sampled every 0.5 s and eased over 3 s). At the water line that is the old -0.45/-0.1. Dusk mist added: up to 0.45 of dawn's, between time 0.75 and 0.95, only when the ground is damp (wetness 0.2 to 0.7). Client builds.

- 2026-10-16 20:40: Justin picked A. Render requested da2c2fb (Render session_015S96hNrEu92FrCoNZqZZjs). Opening the PR.

## Open questions / blockers
- Best shown after Environment's #175 (land reshape) merges with the one world wipe.

## Next step
Wait for the PR's CI; send the green sha to the PM.
