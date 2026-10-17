# audio-25: Emote sounds
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-25
PR: (see below)

## Goal
Waving, cheering, dancing and bowing were silent. Each now makes the sounds of the gesture, heard by everyone nearby (22 m): no voices, only cloth, hands and feet.

## Progress log
- 2026-10-17: three data cues in `CueDefsEmotes.cs` (`EmoteSwish`, `EmoteClap`, `EmoteTap`). `EmoteSounds` (client/scripts/atmosphere) watches `PlayerAct` rows (reducer-driven only) and schedules cues at the times of Animation's poses in `client/scripts/animation/WorkMotion.cs`: wave = swish as the arm goes up (0.2 s) and down (1.65 s); cheer = a clap at the top of each hop (0.28 s, 1.08 s) and a foot tap on each landing; dance = a tap every 0.4 s, alternating loud and soft; bow = a rustle at the start and at the rise. Hooked through `AudioHooks`; preview scene `emote:15` in `AudioScenes`.
- Levels measured with the ambience off (`--no-music --ambience=0`, silence -65 dBFS) so the cue stands alone: loudest 50 ms clap -31, taps -33/-34, swishes -34 to -38 dBFS. Client builds, 0 errors.
- If Animation retimes a pose, the times in `EmoteSounds` (arrays at the top) need the same change. A new emote is a new array plus a case in `Connect`.

## Open questions / blockers
- Not heard in the running game. Voiced emotes (a "hey" or whoop) are not built: a voice style Justin would pick, asked via the PM.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
