# bugfix-72: Dark blob over a lit campfire, no column on campfires, thinner campfire smoke
Lane: Bug fixes
Owner: Bug fixes thread
Status: done
Branch: claude/bugfix-rnwmoa-14
PR: #876 (merged at 778d988b)

## Goal
Render's stills (/mnt/project-files/screenshots/bugfix-65-fire-before-after.png, left column) show a dark, soft round blob about 1 m above a lit campfire by a lean-to at dusk, before and after #844 and only from one side. The smoke-signal column (AtmosphereSmokeSignal, atmosphere-32) is the likely cause: a fire stoked past 80% of its most fuel (that fire read "about 180 min left") puts up a lit billboard column; its lowest puffs were already half opaque (ramp stop 0.06) about 2.5 m up and 1.2 m wide, lit with the firelight behind them as seen from the camera, so they drew near-black against the bright dusk fog. The column's ramp now stays clear for the first tenth of a puff's life (about 3 m), so no lit puff hangs over the flames; the column shows from above that. Not yet checked in a render: this lane has no Godot, so a Render still of the same view at the PR's sha decides it.

## Added to this PR by the PM, 2026-10-17 19:25 (Justin)
- A normal campfire no longer puts up the smoke-signal column, however stoked: AtmosphereSmokeSignal.CanSignal returns false for every fire; the file otherwise works, so Survival's new fiber signal fire can hook in by making CanSignal true for its own fire kind. --smoke-signal still forces a column for stills.
- Campfire smoke (the thread from AtmosphereHomes) is a light wisp: puffs at 0.4 of a chimney's size and a third as dense (WispSize, WispDensity).
- bugfix-40 (sleeping mat vanished) marked done: Justin confirmed it no longer happens.

## Progress log
- 2026-10-17 18:30: read the still; ruled out the home smoke thread (unshaded, alpha 0.22), flames and sparks (additive), fireflies; changed the column ramp; render requested from the PM.

## Result
Render's still at 778d988b (/mnt/project-files/screenshots/bugfix-72-campfire-778d988.png, default flags) shows no dark blob and no pink haze, so the PM closed this: the blob was fixed by removing the campfire smoke column. Caveat: that still has a different camera from the earlier ones (bugfix-72-campfire-confirm.png at 0ea1ebac still showed the blob with the column only trimmed), so if a dark blob over a fire is seen again, the next suspects are the home smoke wisp's tint and the fire's shadowed OmniLight in volumetric fog.

## Open questions / blockers
- Needs Render's after still of the same view (dusk, lean-to and stoked campfire, from the left side). If the blob is still there, the cause is not this and the next suspects are the home smoke thread's tint and the fire's shadowed OmniLight in volumetric fog.

## Next step
Done. Nothing left on this ticket.
