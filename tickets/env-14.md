# env-14: Shared WaterAt for clay pipes
Lane: Environment
Owner: Environment lane (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-water-at
PR: #387

## Goal
Ceramics (ceramics-13, clay pipes for water, Justin's pick) needs one shared server+client answer to "is this cell water, and at what height?". Add `Land.WaterAt(x, z, depth, out surface, out flowing)` in shared/Land.cs: true when the land (base height minus dug depth) is below World.WaterLevel; surface is World.WaterLevel (one flat plane); flowing tells a stream (within 12 m of its line) from a pond. No behaviour change elsewhere.

## Progress log
- 2026-10-17 00:25 UTC: added. Over x, z in [-400, 400): 16,999 stream cells, 26,862 pond cells; a cross-section at z = 0 marks the stream cells S out to its banks. Server module and SpawnCheck build.

## Open questions / blockers
- Springs as fixed water sources (a FeatureDef with a Water flag, after #371) go to the PM as an option.

## Next step
Wait for CI on the PR; send the PM the green sha and tell Ceramics (session_01Sc3N1RCsurSszfX5YwGzMU).
