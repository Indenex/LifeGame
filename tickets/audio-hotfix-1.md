# audio-hotfix-1: All sound went silent mid-session
Lane: Audio
Owner: Audio lane (Sonnet session_01SXcxtoyeqDmypiHxkHedU6)
Status: in_review
Branch: claude/audio-hotfix-1
PR: (see PM message)

## Goal
In Justin's live game all sound (music and every effect) stopped while he walked around and never came back. Find the cause, fix it, and make sure no single bad sound can ever silence the game again.

## Root cause
`SoundMix.Pan()` took the square root of `0.5 * (1 - pan)` with no clamp. `StreamVoice.Render` (audio-2a, #216) pans its two halves at `pan - 0.3` and `pan + 0.3`. Whenever a stream lies mostly to one side of the listener (`StreamPan` of 0.75 or more in size, which happens all the time walking along a bank), one of those went past +/-1, the square root of a negative number gave NaN, and the NaN went into the shared wall filter and reverb. A NaN in a filter or reverb state never leaves, so every later sample was NaN: silence for the rest of the session. Found by sweeping every public input at its limits against the old code (StreamPan = 0.75, 0.875, 1, -1 all produce NaN; FirePan and WaterPan above 1.25 and 1 respectively do too).

## What changed
- `Pan()` clamps and treats NaN as centre. All inputs (weather, pans, levels, year) are sanitised at the top of `Render`; event entry points (Step, Land, Play, Bleat, DeerCry, Thunder, OtherPlace) clamp their gains and pans.
- `SoundMix.Render` never throws and never returns a non-finite sample: it checks after each stage (ambience, effects, music, reverb), and on a fault it reports once (`SoundMix.Fault`), silences that block and rebuilds every voice and filter (`Build()`), so sound comes back on its own.
- `SoundScape`: each event lambda and the Render call are in try/catch (logged with `GD.PushError`, at most 5 times); watchdogs in `_Process` restart a dead synth thread and a stopped `AudioStreamPlayer`.
- SoundPreview test tools: `--edges` (every setting at its limits, must report no faults), `--soak=H [--seed=N] [--fuzz] [--inject]` (H game hours of random weather, seasons and every event type; `--fuzz` feeds NaN/infinite/out-of-range inputs; `--inject` forces bad blocks and exceptions to prove recovery).

## Progress log
- 2026-10-16: root cause found with the edge sweep; fix, guards, watchdogs and tests written. `--edges` ok; `--soak=1 --fuzz --inject` ok (every injected fault recovered, output always finite, max |sample| 0.98); `--soak=3` plain (random legal inputs, no fuzz) see PM message for the result. The Godot client was not built locally (no bindings): CI is the client build.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the audio-hotfix-1 PR, send the PM the green sha.
