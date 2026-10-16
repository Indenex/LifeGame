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
- 2026-10-16 22:05 UTC: Justin picked A (PM): blend the fog with Atmosphere, keep frame cost small on slow PCs, with a setting. Added `FarLand.Level` (Settings graphics/distant_land: Off, Near = 96 m on an 8 m grid with 1.25x crowns, Far = 128 m on 4 m; default Far) and a "Distant land: Far/Near/Off" button under a Graphics heading on the Options page (UX/UI's OptionsPage.cs, small hook). Off hides the node and skips building. Asked Atmosphere to fade Look.Ground/Look.Foliage into the fog over the last quarter of a global `far_land_radius` (option A), so the disc's rim never shows on clear days. Client builds.
- 2026-10-16 22:10 UTC: Atmosphere did A in their PR #289 (claude/atmosphere-farland-fog): the painted shaders fade albedo to the fog tint over the outer quarter of the global `far_land_radius`. FarLand sets it (128/96/0) whenever the level changes, but only once the global exists, so it is harmless before #289 merges. The PM approved asking Integration (session_015g3pLB5eJ4qPrXAwJUmkYT) for a 500 m water plane in Main.cs and camera Far 300 in CameraRig.cs; asked.

## Open questions / blockers
- Waiting on Integration's water-plane and camera-far hooks; then raise Shape(Far) from 128 to 240 m.
- To see out to the fog (about 240 m), Main's water plane needs to grow to about 500 m (Integration's file). Ask once A is confirmed.

## Next step
When Integration's hooks land, raise the Far reach to 240 m; once #284 merges, merge main into claude/environment-farland and open the env-9 PR (Render's hilltop shots were requested from session_015S96hNrEu92FrCoNZqZZjs).
