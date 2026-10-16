# env-12: Build chunks off the main thread
Lane: Environment
Owner: Environment lane (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-chunk-thread
PR: #344

## Goal
With view distance up to Very far (radius 10, 441 chunks; env-9), chunk building costs about 9 ms of CPU per chunk on the main thread, paced at 4 ms a frame. Crossing a chunk boundary on Far queues about 17 to 31 chunk builds, so the new edge fills in over a second or more, and initial loading takes several seconds. Move the expensive part (all the noise, placement and instance math) to background tasks, so the main thread only snapshots the server data a chunk needs and turns finished instance lists into MultiMeshes with one buffer assignment each. Done means: same chunks bit for bit (same instances, same order), building runs on worker threads, walking never hitches, and the full radius fills in far faster. It also opens the way to Very far at radius 12 (with batching distant chunks, a later ticket). Justin sees the same world, filling in faster and smoother.

## Plan
1. Prewarm everything lazily created that a builder touches (PropMeshes variants, Undergrowth meshes, Terrain meshes, Look materials) on the main thread before any task runs.
2. Snapshot per chunk on the main thread: depths, the Cleared flag and clear changes for the chunk plus a margin (landmark pieces reach 8 m), and the clock.
3. The worker runs the existing BuildGround/AddVegetation math against the snapshot (thread-static "building" view behind TileHeight, IsCleared and ClearChange; its own base-height cache) and returns arrays: ground vertices and per-batch instance buffers.
4. The main thread applies finished builds within a per-frame budget, nearest first; stale builds (chunk dirtied again or out of range) are dropped.
5. Prove it: --view-bench dumps an instance hash per chunk before and after.

## Progress log
- 2026-10-16 23:15 UTC: started (ticket and plan).
- 2026-10-16 23:40 UTC: built. `environment/ChunkWorker.cs`: Prewarm (meshes, materials, ghosts made on the main thread first), Snapshot (dig records with a 10-cell margin, cleared cells and their easing for the chunk plus its corner row, the tuft flag), Compute on a worker (thread-static `building` behind TileHeight, IsCleared, Edited, Depth, ClearChange; its own base-height cache), Assemble on the main thread, PumpBuilds (up to 3 workers, nearest first; finished builds put in place within 3 ms a frame; stale or out-of-range ones dropped). Proof: `--view-hash` (every chunk out to 4 around (-4,46), ground and instance buffers) gives 3E74A7536CE734C9 on both the old code (88bffb7) and the new. Real client at radius 5 under xvfb: no errors, all 121 chunks in place.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #344 (now based on main); send the PM the green sha. Later: batch distant chunks for Very far at radius 12.
