# atmosphere-2: Sheep sounds, canopy fade, rain and dawn polish
Lane: World atmosphere and sound
Owner: World atmosphere and sound thread
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke
PR: (none yet)

## Goal
From the PM after atmosphere-1: soft sheep bleats, with a startled bleat when the flock bolts, driven by the Animals sheep state (graze, bolt, curious). Tree canopies between the camera and the player fade out so you can see yourself in the woods. From Justin's screenshots: raindrops right in front of the camera look like thick white sticks (make them thinner and fade them up close), and dawn is too brown and hazy. Done when a screenshot shows the canopy fade working and the sheep sounds play in a build.

## Progress log
- 2026-10-15 21:50: started from main a4ce058.

## Open questions / blockers
- Sheep table field names: coordinate with Animals (cse_01XS7M1yLy2fLaFdE3HWnisM).

## Next step
Canopy dither in the foliage shader (Look.cs), with the player position as a global uniform set by Atmosphere.
