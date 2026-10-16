# env-6: See yourself under trees
Lane: Environment
Owner: Environment session (Opus), session_012hVejTxLoxBpZyX3GQmNL9
Status: in_review
Branch: claude/environment-fade
PR: (once opened)

## Goal
Justin's playtest (top priority from the PM): "it's very difficult to see yourself in forests/trees. Any time the player is rendered underneath something, the see-through effect should happen." Done means any tree whose crown hides the player from the game camera fades, including a crown directly overhead and a wide crown whose trunk is off the line of sight. Roofs and structures are the House building lane's (asked).

## Progress log
- 2026-10-16 22:55: cause in `Look.cs` `fade_amount`: the fade was multiplied by (1 - smoothstep(0.82, 0.97, s)), so a crown whose closest approach to the line of sight is at the player (s near 1), i.e. overhead, never faded; distance was measured to the trunk axis from 0.5x to 4x height with a 1.2x to 2.6x Y-scale reach, so a wide crown off the line still covered the player. Fix: the crown is an upright capsule from 1.6x to 4.4x the tree's height scale, radius by its width scale (1.9x full fade, easing out to 2.7x), and the end-of-line cutoff is gone. `Terrain.ShowGhosts` margin 6 m -> 8 m (2.7 x a 2.6 m wide oak). Numeric check with the real camera (pitch -34, distance 16 and 40), tree scale 1.6: crown overhead, 1.5 m toward the camera, 1.5 m behind, 2.5 m to the side now fade fully (before 0, 0.58/0.03, 0, 0); 4 m to the side 0.02, 6 m behind 0. Client builds; shaders compile only at runtime (no Godot here), so a render confirms it.

## Open questions / blockers
- House building (session_01RrKHUxWRH973Ndnxsad9nv) for roofs and walls over the player.

## Next step
Wait for CI on the PR and the Render shots (player under a crown, between two crowns), then send the green sha to the PM.
