# env-21: Water edges matched to the references (prototype)
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_progress
Branch: claude/environment-water-edges
PR: (none yet; prototype, waiting on PM's pick among options 1-3)

## Goal
Stream and pond banks read like the art references instead of a hard pale sand strip: grass fades into sand over the last hand-widths above the water, the sand darkens to damp at the waterline, and reeds stand in clumps with open stretches between. Existing meshes; ground colour and placement only; no terrain height change.

## Progress log
- 2026-10-17: TileColor: soft sand blend (WL+0.08..0.34) and a damp band (DampShore, WL-0.02..0.12) replace the hard `top < WL+0.2` strip. Shore reeds: patch noise salt 99, 1-3 stems per clump. Builds; local renders at (-37,10) (stream 66 m west of spawn) before/after in the scratchpad.

## Open questions / blockers
- PM to pick next lane item (1 water edges, 2 meadow ground cover, 3 radius-12 batching). Recommended 1.

## Next step
If PM picks 1: add pebble clusters along the waterline, render, open the PR and ask Render for a before/after at --debug-position=-37,10 --camera-zoom=-3.
