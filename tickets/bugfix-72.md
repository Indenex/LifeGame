# bugfix-72: Dark blob floating above a lit campfire at dusk
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-14
PR: (opening)

## Goal
Render's stills (/mnt/project-files/screenshots/bugfix-65-fire-before-after.png, left column) show a dark, soft round blob about 1 m above a lit campfire by a lean-to at dusk, before and after #844 and only from one side. The smoke-signal column (AtmosphereSmokeSignal, atmosphere-32) is the likely cause: a fire stoked past 80% of its most fuel (that fire read "about 180 min left") puts up a lit billboard column; its lowest puffs were already half opaque (ramp stop 0.06) about 2.5 m up and 1.2 m wide, lit with the firelight behind them as seen from the camera, so they drew near-black against the bright dusk fog. The column's ramp now stays clear for the first tenth of a puff's life (about 3 m), so no lit puff hangs over the flames; the column shows from above that. Not yet checked in a render: this lane has no Godot, so a Render still of the same view at the PR's sha decides it.

## Progress log
- 2026-10-17 18:30: read the still; ruled out the home smoke thread (unshaded, alpha 0.22), flames and sparks (additive), fireflies; changed the column ramp; render requested from the PM.

## Open questions / blockers
- Needs Render's after still of the same view (dusk, lean-to and stoked campfire, from the left side). If the blob is still there, the cause is not this and the next suspects are the home smoke thread's tint and the fire's shadowed OmniLight in volumetric fog.

## Next step
Wait for Render's after still and CI, then send the PM the green sha.
