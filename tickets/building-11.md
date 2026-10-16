# building-11: See-through structures
Lane: House building
Owner: House building thread (Sonnet)
Status: in_review
Branch: claude/house-building-8ueqz2-seethrough (stacked on building-10, #271)
PR: (not yet)

## Goal
D-168 (Justin): any time the player is rendered underneath something, the see-through effect happens. When the player is inside a finished hut, house, cabin or lean-to footprint (client `BuildingSystem.ShelterAt`), or the camera-to-head line crosses a structure's box, that StructureView's roof and camera-side wall pieces swap to `Look.GhostOf` ghosts, with a smooth fade like the trees in #223. Client only.

## Progress log
- 2026-10-16: ticket opened from the PM's message. Read the code: `StructureView` holds `pieces` (Stage, Mesh, Material, Anchor) with one MeshInstance3D per piece and no roof/wall tag yet; `Look.GhostOf` only knows tree materials (`Look.cs` ~110); `Terrain.ShowGhosts` is the tree pattern to copy.

- 2026-10-16 21:45: built (client only; client project compiles, shader not run: no Godot here). Look.HidesPlayer is on main. Look.cs: the painted shader gets `uniform float fade_override = -1.0` (when >= 0 it replaces the tree test) and `Look.StructureGhost(source)` makes a ghost copy. StructureView.UpdateSeeThrough(hides, dt) eases a 0..1 fade (about 0.33 s), swaps each finished piece's mesh for a ghost mesh (same shader, alpha 1 - fade*(0.35..1), fully clear in a soft circle round the player), and back. BuildingSystem.UpdateSeeThrough tests each finished shelter (RainCover > 0: lean-to, hut, house, cabin) within 25 m with Look.HidesPlayer on its footprint box (3 m tall) and the camera.
- Default chosen: the whole finished building ghosts, not only roof and camera-side walls; the clear circle round the player stands in for the "camera-side wall" part and avoids tagging every model piece. Say if Justin wants only roof and near walls (needs a roof/wall tag per Piece).
- 2026-10-16: Render requested cf1f7ad (Render session_015S96hNrEu92FrCoNZqZZjs).
- 2026-10-16 (Justin via PM): "like Project Zomboid". Reworked from the whole-building ghost: inside a building the roof and upper parts clear away entirely; walls between camera and player fade, far walls stay solid; outside behind a building only the walls in the way fade (clear circle round the player) and the roof stays; other buildings solid; ~0.3 s eases. Built: `StructureModel.Piece.Role` (Fixed = stage 0, Roof = thatch or anything above 2.15 m, Wall = the rest), painted shader ghost gets `cut_mode` (1 wall, 2 roof), `cut_origin`, `cam_dir` (a wall fragment fades by how much it faces the camera from the building's centre, so round hut courses cut away correctly without splitting meshes), `StructureView.UpdateSeeThrough(inside, hides, camera, dt)` with separate roof and wall fades, `BuildingSystem.UpdateSeeThrough` passes inside (Structures.Contains) and hides (Look.HidesPlayer). Client compiles; shader not run (no Godot here). An Opus helper reviews the shader and tuning read-only.

## Open questions / blockers
- Not seen running: needs a Godot run or a render (shader change is small but unverified at runtime).

## Next step
Open/refresh the PR (base: claude/house-building-8ueqz2 until #271 merges), wait for CI, request a render (walk into a hut, house and cabin; camera behind a wall), send the PM the green sha.
