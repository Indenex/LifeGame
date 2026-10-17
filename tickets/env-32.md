# env-32: Grass detail setting
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-grass-detail
PR: (opening)

## Goal
Options -> Graphics -> "Grass: Low / Normal" (Settings graphics/grass_detail, default
Normal). Normal draws every blade exactly as now (D-256). Low draws about half of each
tuft's blades for slower PCs. Changing it redraws every tuft at once, with no reload,
because the tuft mesh is rebuilt in place. This is groundwork for the botw/ghibli ground
pick (env-31): whichever look wins, its grass density hangs off this setting (a High
step can come with it).

## Progress log
- 2026-10-17 09:30: built; Normal builds the identical mesh (no blade skipped).

## Open questions / blockers
- (none)

## Next step
Wait for PR CI; send the green sha to the PM.
