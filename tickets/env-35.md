# env-35: Ambient life, part two (dragonflies; birds pending Atmosphere)
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_progress
Branch: claude/environment-ambient-life-2
PR: (none yet)

## Goal
More small life, client-only, under the existing Options -> Graphics -> Ambient life
setting (D-145 next item, option (b) sent to the PM):
- Dragonflies over ponds and streams on warm dry days: up to 6, each keeping to its own
  stretch of open water, hovering then darting to a new spot 1 to 3.5 m away, 0.25 to
  1.1 m above the surface. Soft jewel bodies (teal, sky blue, amber), pale see-through
  shimmering wings. `client/scripts/environment/AmbientDragonflies.cs` (partial of
  AmbientLife), hooked from AmbientLife.cs.
- A small bird flock crossing the sky by day: asked Atmosphere first whether birds are
  theirs or planned.

## Progress log
- 2026-10-17 12:45: dragonflies built; local render at (-36,-16) by the nearest pond running.

## Open questions / blockers
- Atmosphere: birds theirs or mine?

## Next step
Check the dragonfly render, open the PR, Render clip request.
