# env-16: Paler, warmer world rocks
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: done
Branch: claude/environment-pale-rocks
PR: #455 (merged)

## Goal
Boulders, river stones and outcrop stones in the world read paler and warmer, closer to the art references (pale warm grey instead of mid neutral grey). Colour only: no shape, size or density change. A new `Look.WorldRock` colour is used by Terrain's boulders and Environment's LandmarkProps stones; `Look.Rock` (tools, stations, items, claim markers of other lanes) is unchanged.

## Progress log
- 2026-10-17: WorldRock (0.64, 0.61, 0.55) vs Rock (0.46, 0.47, 0.46). Builds; two local renders at 0,10. PM picked this next (then white flowers, env-17).

## Open questions / blockers
- (none)

## Next step
Done; merged.
