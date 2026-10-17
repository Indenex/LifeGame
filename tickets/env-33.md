# env-33: Fallen giant, a new world feature (D-166)
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_progress
Branch: claude/environment-fallen-giant (merged #718); follow-up claude/environment-giant-log
PR: (none yet)

## Goal
D-166: repeated features should vary strongly. Only three kinds existed (giant oak,
outcrop, ford), each already seeded per instance, so the gap was variety of kinds.
First new kind (the recommended option sent to the PM): a huge old trunk lying across a
glade near a wood. Modeling's FallenLog is scaled up 9–13 m long and 1–1.5 m thick,
straight and splintered or broken with its root plate. It follows the slope. Each one
has its own rot (fresh bark to a moss-green hulk), ferns along its sides (more when
rotten), and the odd stone. One per 56 m square at most; it blocks walkers along its
whole length (client collision, like an outcrop's stones).

Files: `shared/Features.cs` (FeatureId.FallenGiant = 4, appended), `shared/Landmarks.cs`
(FallenGiantFits), `client/scripts/environment/LandmarkProps.cs` (FallenGiant builder).

## Progress log
- 2026-10-17 09:45: built; first renders landed in woodland (crowns over the trunk).
- 2026-10-17 09:55: Fits tightened to open glades (density < 0.2 at the cell, < 0.3 six metres out all round); 30 in the 9x9 squares, nearest (-151,-42).

- 2026-10-17 10:40: no standing tree within 7 m (7d4ad4ed); 22 in the 9x9 squares, nearest
  (-73,-129), (149,-63). Render at (149,-63) (--camera-zoom=6, positive = farther) shows it
  lying in an open glade, about 13 m with a long shadow. Draws see-through when the player
  stands on it (the usual tree ghost).
- PM: fallen giant first is a PM default (D-323).

- 2026-10-17 11:08: #718 merged.
- 2026-10-17 11:20: follow-up on claude/environment-giant-log: Modeling's FallenGiantLog (#730,
  6 variants with root plates, own colours) swapped in, tint PropMeshes.GiantTint darkened by
  rot; preloaded in ChunkWorker. Blocking circles unchanged (bugfix-49 folds them into shared).

## Open questions / blockers
- (none)

## Next step
Render (149,-54) zoom 4 to check the new log, open the follow-up PR, send the Render request.
