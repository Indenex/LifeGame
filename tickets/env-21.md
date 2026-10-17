# env-21: Water edges matched to the references (prototype)
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: done
Branch: claude/environment-water-edges
PR: #540 (merged)

## Goal
Stream and pond banks read like the art references instead of a hard pale sand strip: grass fades into sand over the last hand-widths above the water, the sand darkens to damp at the waterline, and reeds stand in clumps with open stretches between. Existing meshes; ground colour and placement only; no terrain height change.

## Progress log
- 2026-10-17: TileColor: soft sand blend (WL+0.08..0.34) and a damp band (DampShore, WL-0.02..0.12) replace the hard `top < WL+0.2` strip. Shore reeds: patch noise salt 99, 1-3 stems per clump. Builds; local renders at (-37,10) (stream 66 m west of spawn) before/after in the scratchpad.

- 2026-10-17: PM picked option 1 (after the tree fall). Shore stones gather in beds along the waterline (ShoreStones in TerrainCollision.cs, noise salt 100, shared with AddVegetation so collision stays in step). Atmosphere's new water (#517) draws foam and edge alpha from depth; the damp band is ours, foam theirs; water mean stays at Look.WaterLevel.

## Follow-up (PM, 2026-10-17)
- Justin could not see the difference in Render's before/after: the soft banks and damp band don't read at the default camera and light, and the reed and stone beds look like the old ground. Not reworked now: the painted ground (env-27) repaints the banks; its prototypes draw a bolder shoreline (wider sand bank in its own colour, darker wet band, taller reed clumps of 2-4). Carry that into whichever ground Justin picks.

## Open questions / blockers
- (none)

## Next step
Done; merged as #540. Follow-up below.