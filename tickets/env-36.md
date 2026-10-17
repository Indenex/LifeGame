# env-36: Grass without stepped bands
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: done
Branch: claude/environment-grass-bands
PR: #816

## Goal
Render B saw flat colour bands in the grass on main (noon too). Atmosphere traced it to
blocky grass patches, not light or grade. Cause: each 1 m tuft took its own random height
(0.75 to 1.25), so neighbouring patches stepped up and down, and from the follow camera
the steps read as bands. Fix: height follows a smooth noise field (5 m scale, 0.8 to 1.2)
with only +-6% jitter per patch; the average height and the blade count are unchanged (D-256).

## Progress log
- 2026-10-17 14:00: built; local renders at (-4,46) and (-6,-10) running, compared with
  main (rockA.png from the same session).
- Lilac rocks (same PM message) are not this lane's: A/B shows they come from Painted.Rock's
  violet shade band; handed to Modeling with a suggested fix.

## Open questions / blockers
- (none)

## Next step
None, merged as #816 (2026-10-17 14:28).
