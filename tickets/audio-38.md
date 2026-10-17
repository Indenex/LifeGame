# audio-38: Rabbit snare sounds
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-38-snare
PR: (see below)

## Goal
Three gentle cues for Animals' rabbit snare (animals-34), new files only: `snare.snap` (catch: soft wooden snap and cord whip, play at the snare position), `snare.set` (rustle and cord creak when set) and `snare.collect` (shorter rustle and creak when collected). Defined as `CueDefs.SnareSnap`, `SnareSet`, `SnareCollect`; Animals plays them with `Sfx.PlayBy(def, where)`. Previewable with scene `snare`.

## Progress log
- 2026-10-17: cues written; loudest 50 ms with ambience off: set -28.8, snap -29.5, collect -29.4 dBFS; client builds, 0 errors.

## Open questions / blockers
- Not heard in Godot (none available).

## Next step
Wait for CI on the PR, send the green sha to the PM.
