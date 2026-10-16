# atmosphere-9: Sound off the main thread, fewer draw calls
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: todo
Branch: claude/atmosphere-sound-hlqfke (after atmosphere-7)
PR: (none yet)

## Goal
From Bug fixes' profiling (PR #55), via the PM, 2026-10-16:
1. SoundMix synthesis runs on the main thread, about half the per-frame script time (RainVoice ~20%). Run it on its own thread so frames are freed and audio doesn't stall on hitches.
2. Spawn renders ~2,500 draw calls. Merge each chunk's grass, tuft and rock multimeshes, or tune the shadow distance, so low-end PCs cope.
Keep the look and sound identical; send before/after numbers to the PM.

## Progress log
- (not started)

## Open questions / blockers
- (none)

## Next step
After atmosphere-7's PR is open: measure frame script time and draw calls at spawn (before), then move SoundScape's Render loop to a worker thread feeding a ring buffer.
