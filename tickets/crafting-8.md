# crafting-8: Batch items lying on the ground
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-batch-items
PR: #62

## Goal
Items on the ground (sticks, stones, fiber, dropped tools) draw as one MultiMesh per item type per 32 m patch instead of one MeshInstance3D each, cutting draw calls in the Compatibility (OpenGL) renderer. Nothing looks different and picking up still works.

## Progress log
- 2026-10-16 03:00: `client/scripts/crafting/GroundItems.cs` added; Main.AddItem/RemoveItem now call it. Rotation and height are the same as before (yaw from Terrain.Hash01, +0.04 m). Unused slots collapse to a zero-scale transform inside their patch so they draw nothing and keep the bounds tight. Removal swaps the last item into the freed slot.
- 2026-10-16 03:20: measured on a fresh `perfitems` database (768 items), standing at spawn, `--frame-stats` (Xvfb, software GL, so FPS means nothing):
  - Compatibility (`--rendering-method gl_compatibility --rendering-driver opengl3`): draw calls about 2,520 before, 2,210 after; objects 2,730 to 2,340; nodes 5,118 to 4,579.
  - Forward+ (Vulkan): draw calls about 500 either way, since Forward+ already merges identical meshes; objects 2,500 to 2,300.
- Before and after screenshots from the same spot show the same items in the same places.
- 2026-10-16 03:55: removal check. A temporary (uncommitted) verifier compared every MultiMesh slot with the tracked items every few seconds while a script deleted half the world_item rows and the server respawned more. Every sample matched (live items = client rows, all slots correct, 0 bad). Picking up goes through conn.Db.WorldItem, not the nodes, so it is unchanged.

## Open questions / blockers
- (none)

## Next step
None. Merged to main; kept for history.
