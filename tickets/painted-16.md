# painted-16: Rock value in the world
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/painted-16-rock-value
PR: (none yet; held as an option for Justin)

## Goal
In the in-game world, rocks read near-white in sun and pinkish at dusk. Since D-319 removed their relief, Painted.Rock's pale base grey (0.6) has nothing to break it up. Try a mid stone grey (0.47, base_mix 0.6) so rocks sit in the grass. Justin will notice this palette value, so it goes to him as an option via the PM.

## Progress log
- 2026-10-17: rendered the world from main at dawn, noon and dusk (renders/painted-16-world-before-*.png); changed Painted.MakeRock; build has 0 errors; after renders in progress.
- 2026-10-17 10:00: after renders done: renders/painted-16-rocks-before-after.png (left before 0.6, right after 0.47; noon top, dusk bottom). It's a modest gain: the noon rocks sit greyer in the grass. The dusk pink is the sunset light, not the rock value. Sent to the PM as two options.
- 2026-10-17 10:40: merged main (#690 haze fix); re-shot the before (main) and after (this branch) at noon and dusk into renders/painted-16-rocks-before-after.png. Held for Justin per the PM (D-154).

## Open questions / blockers
- Justin: darker rocks (recommended) or keep them pale?
- Atmosphere (via PM): heavy haze at dawn and noon in the before renders.

## Next step
Wait for the PM's pick (0.47 or keep 0.6); if 0.47, open a PR to main.
