# atmosphere-17: Mist that settles where the land is low
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-17-valley-mist
PR: (none yet)

## Goal
Picked under D-145 after atmosphere-13 to 16; sent to the PM as options (A recommended; work proceeds on A until Justin answers). Dawn and dusk mist lies in valleys, over ponds and along the stream that Environment's reshape (env-2, #175) brings: thicker where the land is low relative to its surroundings, thinning on slopes and hilltops, burning off by mid-morning, and returning faintly at dusk on still, damp evenings. Today's Mist.cs (atmosphere-11) pools by the depth buffer under two planes near the player; this ticket makes the amount follow terrain height (World.BaseHeight) and water, so valleys fill first. Done means Render lane shots at dawn over a valley and a pond after #175, and a PR green.

## Progress log
- 2026-10-16 20:45: started from main (after #217). Options sent to the PM.

- 2026-10-16 20:55: Mist layers now sit 0.55 and 0.9 m above the local low ground (the 20th percentile of World.BaseHeight over an 84 m grid around the player, water counted at its surface, re-sampled every 0.5 s and eased over 3 s). At the water line that is the old -0.45/-0.1. Dusk mist added: up to 0.45 of dawn's, between time 0.75 and 0.95, only when the ground is damp (wetness 0.2 to 0.7). Client builds.

## Open questions / blockers
- Justin's pick (A valley mist / B leave as is), via the PM.
- Best shown after Environment's #175 (land reshape) merges with the one world wipe.

## Next step
Push; render request (dawn over a valley and a pond, dusk after rain); PR once Justin picks A, or right away if the PM says so.
