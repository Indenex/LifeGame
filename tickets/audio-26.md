# audio-26: Lamp flame sounds
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-26-lamps
PR: (see below)

## Goal
A lit clay oil lamp was silent. A lit lamp now breathes a faint flutter and ticks as the wick burns, heard only close by (9 m), and the wick catching and the flame going out each make a soft sound.

## Progress log
- 2026-10-17: stacked on audio-25 (emotes) because both add lines to `AudioHooks`; retarget to main once that merges. Four data cues in `CueDefsLamps.cs` (`LampFlutter`, `LampTick`, `LampLight`, `LampOut`). `LampSounds` (client/scripts/atmosphere) reads the public `lamp` table (`BurnsUntil` in the future = lit): every 3-8 s a flutter, every 5-14 s a tick, per lit lamp, with a puff when a reducer lights it (rows arriving with a chunk stay silent) and a gutter when the time runs out. Hooked through `AudioHooks`; preview scene `lamp:16` in `AudioScenes`.
- Levels measured with ambience off (silence -65 dBFS): flutter and tick -39, puff and gutter -36 loudest 50 ms. Client builds, 0 errors. A lamp refilled from nearby tallow never goes dark, so no out sound plays for it (D-147).

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
