# survival-27: Lie down on your mat
Lane: Survival (server); UX adds the button
Owner: Survival thread
Status: in_review
Branch: claude/survival-lie-down
PR: (see STATUS)

## Goal
With Animation's rest poses (#600) and the waking place (#575) on main: `LieOnRestSpot(spotId, structureId)` lets you lie down on a rest spot you may use (your laid sleeping mat), in reach, and everyone sees you asleep there (`PlayerRest` with Kind lie and SpotId the building). Just a pose, no effect (Justin's pick). The checks are the same as choosing a waking place (`RequireRestSpot`, split out of `SetRespawn`). Walking off stands you up (Animation's hook). Deleting a character clears its rest row.

## Progress log
- 2026-10-17 10:50: server done; BuildTest checks (B refused, A lies on the mat seen by B, StandUp clears it).

## Open questions / blockers
- The button itself ("Lie down" in the mat prompt, `RestSpotView.cs`) is UX's to add in their restyle (ui-37): call `conn.Reducers.LieOnRestSpot(spotId, structureId)` with the same arguments `SetRespawn` takes. I did not touch RestSpotView to avoid colliding with ui-37.

## Next step
Wait for CI, send the green sha to the PM, then tell UX the reducer is on main.
