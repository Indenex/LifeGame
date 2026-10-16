# building-11: See-through structures
Lane: House building
Owner: House building thread (Sonnet)
Status: todo
Branch: (new, off origin/main after #202 merges)
PR: (not yet)

## Goal
D-168 (Justin): any time the player is rendered underneath something, the see-through effect happens. When the player is inside a finished hut, house, cabin or lean-to footprint (client `BuildingSystem.ShelterAt`), or the camera-to-head line crosses a structure's box, that StructureView's roof and camera-side wall pieces swap to `Look.GhostOf` ghosts, with a smooth fade like the trees in #223. Client only.

## Progress log
- 2026-10-16: ticket opened from the PM's message. Read the code: `StructureView` holds `pieces` (Stage, Mesh, Material, Anchor) with one MeshInstance3D per piece and no roof/wall tag yet; `Look.GhostOf` only knows tree materials (`Look.cs` ~110); `Terrain.ShowGhosts` is the tree pattern to copy.

## Open questions / blockers
- Waiting on Environment's segment-crosses-box helper in Look (sha from Environment).
- `Piece` needs a tag (roof / wall / other) per shape so only roof and camera-side walls fade; the house, hut, lean-to and cabin models each need tagging.
- No dotnet in this container: client changes can't be built locally, so CI is the only check.

## Next step
Wait for Environment's helper sha, then: tag pieces in StructureModel (roof vs wall), give Look.GhostOf a path for structure materials, and add `StructureView.SetSeeThrough(float fade)` driven from BuildingSystem each frame.
