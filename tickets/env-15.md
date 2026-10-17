# env-15: Light stylise of the old grass
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-grass-stylise
PR: (opening)

## Goal
Justin: "for the grass, just go back to the old one, and try to stylize it a bit to match references." Main's blade grass keeps its shape, height, density and wind; only the colour of the near grass tufts (`Look.GrassTufts`) changes toward the art references: muted olive and warm yellow-green, a blue-violet shade at the roots, warm lit tips, in soft tone bands. No other ground changes. The painted-ground rework (env-13, `claude/environment-painted-ground`) is parked and not to be merged.

## Progress log
- 2026-10-17: `stylised` uniform on the grass shader, set to 1 on GrassTufts only. Tuned over four renders (first pass read pale and grey; now warm olive). Builds; renders with no shader errors.

## Open questions / blockers
- Flowers or clover: not added, to keep this to colour only. Can follow if Justin wants them.

## Next step
Wait for CI on the PR, send the green sha to the PM, and ask Render for a before/after (main vs this branch, same spot and camera).
