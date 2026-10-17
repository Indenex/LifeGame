# audio-33: Pounding grit and grog
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-33-pound
PR: (see below)

## Goal
Animation's Pound pose (#732: a stone held in both hands strikes the pile 1.2 times a second, for crushing grit and grog) now has a dull stone-on-stone thud on each blow, for everyone.

## Progress log
- 2026-10-17: one data cue, `PoundThud` (`CueDefsPound.cs`): a dull knock with a gritty crumble. `ToolSounds.SoundOf` maps `WorkMotion.Pound` to it, so it plays on each `Avatar.Impact` with that motion (yours at full volume, others' from where the stone lands via `Sfx.PlayBy`). Preview scene `pound:8` in `AudioScenes`.
- Level with ambience off (silence -65 dBFS): about -28 dBFS loudest 50 ms. Client builds, 0 errors. The clay work voice (`clay.grit`, `clay.grog`) keeps running underneath.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
