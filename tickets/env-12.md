# env-12: Build chunks off the main thread
Lane: Environment
Owner: Environment lane (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_progress
Branch: claude/environment-chunk-thread (stacked on claude/environment-view, PR #337)
PR: (not yet)

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

## Open questions / blockers
- (none)

## Next step
Step 1 and 2: prewarm and snapshot, then move AddVegetation's math behind the snapshot.
