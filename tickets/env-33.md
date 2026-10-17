# env-33: Fallen giant, a new world feature (D-166)
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_progress
Branch: claude/environment-fallen-giant
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
- 2026-10-17 09:45: built; 37 instances in the 9x9 squares round spawn, nearest (-28,13),
  (31,-20). Local renders running.

## Open questions / blockers
- Which feature kind comes first went to the PM as options (fallen giant recommended).

## Next step
Check renders at (-24,16) and (35,-17), tune, open the PR, request a Render still pair.
