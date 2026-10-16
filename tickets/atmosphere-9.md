# atmosphere-9: Sound off the main thread, fewer draw calls
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke (after atmosphere-7)
PR: (none yet)

## Goal
From Bug fixes' profiling (PR #55), via the PM, 2026-10-16:
1. SoundMix synthesis runs on the main thread, about half the per-frame script time (RainVoice ~20%). Run it on its own thread so frames are freed and audio doesn't stall on hitches.
2. Spawn renders ~2,500 draw calls. Merge each chunk's grass, tuft and rock multimeshes, or tune the shadow distance, so low-end PCs cope.
Keep the look and sound identical; send before/after numbers to the PM.

## Progress log
- 2026-10-16 02:20: SoundScape rewritten: a synth thread renders 256-frame blocks into a 2048-frame ring (single writer, single reader); the main thread only copies whole blocks into Godot's generator. Step, bleat and thunder are queued to the synth thread. Synthesis costs about 2.5-4% of one core (60 s of audio renders in 2.1-3.1 s in tools/SoundPreview), now all off the main thread.
- 2026-10-16 02:30: draw calls. Forward+ at spawn is ~300-460, not 2,500; ~2,150-2,500 appears only in Compatibility (GL). Most of that is berry bushes (~32 mesh instances per bush, Survival lane) and ~590 loose item MeshInstance3Ds (Crafting lane); terrain is ~430 nodes. Terrain change here: each chunk's see-through tree copies ("ghosts") sit under one node, shown only for chunks within 6 m of the camera-to-player line (the fade's reach), so the look is unchanged.
- 2026-10-16 03:10: merged main (#55) and atmosphere-7; kept the threaded SoundScape (it already did #55's whole-chunk push). Measured with #55's --frame-stats, offline, rain 0.8, main e8d1e1a vs this branch: draw calls settled ~373 -> ~345 (-28, -7.5%), objects 544 -> 516, managed allocations 45 KB/s both (a first try was 75 KB/s from a per-frame node lookup; holders are now cached). Frame time can't be compared here: the container renders in software at ~1 fps.

## Open questions / blockers
- (none)

## Next step
Once #58 merges: merge main into atmosphere-9-wip, push to claude/atmosphere-sound-hlqfke, open the PR with the numbers above. The bigger Compatibility win (berry bush and item meshes as MultiMesh) belongs to the Survival and Crafting lanes; the PM decides.
