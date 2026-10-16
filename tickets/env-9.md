# env-9: Seeing further (distant land and tree silhouettes)
Lane: Environment
Owner: Environment session (Opus), session_012hVejTxLoxBpZyX3GQmNL9
Status: in_progress
Branch: claude/environment-farland (stacked on claude/environment-props / PR #284)
PR: (once opened)

## Goal
Hills, heath tops, valleys and the woods read from a distance, so players can steer by them and the world doesn't end at the last loaded chunk (about 56 m). A coarse ground mesh and simple tree silhouettes cover the land out to the fog, from the shared height and forest rules. Visual only, no wipe. Cheap: built on a background thread when the player changes chunk.

## Progress log
- 2026-10-17 01:20: sent the PM env-9 options (A seeing further, recommended; B gatherables by habitat; C small life; D worn footpaths). Started A as the default.
- 2026-10-17 01:35: `client/scripts/environment/FarLand.cs`, a Node3D that Terrain adds and updates from Focus. It builds on a Task when the player's chunk changes: a 4 m grid out to 128 m radius (World.BaseHeight; ground colour like TileColor without per-cell detail: grass, litter, heath, moss, clay, shore), with a hole over the loaded 7x7 chunks that overlaps their edge by one step, set 0.3 m low so it never shows through them. Tree silhouettes: one crown per 4 m patch with chance TreeChanceAt x 16 x 0.55, broadleaf blobs or pine cones (not pine on heath or wetland), sized by density, Look.Foliage, no shadows. The radius is capped at 128 m because Main's water plane is 260 m across (centred on the player); beyond that, distant ponds would show dry. Client builds.

## Open questions / blockers
- To see out to the fog (about 240 m), Main's water plane needs to grow to about 500 m (Integration's file). Ask once A is confirmed.

## Next step
Ask Render for distant views (from a hilltop, e.g. (-4,46), looking out in two directions) before and after, then open the PR.
