# audio-37: Cue-played counter
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-37-cuestats
PR: (see below)

## Goal
Let Render verify playback headlessly. Every cue the synth starts (and every footstep) is counted per cue id in `CueStats`. Start the client with `--audio-stats` and it logs `Audio cues (since last report): id=N, ...` after 20 s, then once a minute, and `Audio cues (run total): ...` on quit. Off by default; no effect on sound.

## Progress log
- 2026-10-17: CueStats + CueStatsLog (an [AudioSounds] class) added; client builds with 0 errors.

## Open questions / blockers
- Not heard in Godot (none available); the count is in the synth, so it runs even with no audio device.

## Next step
Wait for CI on the PR, then send the green sha to the PM.
