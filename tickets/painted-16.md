# painted-16: Rock value in the world
Lane: Modeling
Owner: Modeling lane session
Status: in_progress
Branch: claude/painted-16-rock-value
PR: (none yet; held as an option for Justin)

## Goal
In the in-game world, rocks read near-white in sun and pinkish at dusk. Since D-319 removed their relief, Painted.Rock's pale base grey (0.6) has nothing to break it up. Try a mid stone grey (0.47, base_mix 0.6) so rocks sit in the grass. Justin will notice this palette value, so it goes to him as an option via the PM.

## Progress log
- 2026-10-17: rendered the world from main at dawn, noon and dusk (renders/painted-16-world-before-*.png); changed Painted.MakeRock; build has 0 errors; after renders in progress.

## Open questions / blockers
- Justin: darker rocks (recommended) or keep them pale?
- Atmosphere (via PM): heavy haze at dawn and noon in the before renders.

## Next step
Render the world after at 0.5 and 0.74 with the scratchpad world.sh, add the before/after image here, and send the option to the PM.
