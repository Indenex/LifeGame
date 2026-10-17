# audio-31: Clap and Shrug emote sounds
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-31-clap
PR: (see below)

## Goal
Animation's Clap and Shrug emotes (#705) were silent. Clap now claps five times and Shrug makes a soft cloth rustle as the shoulders rise and drop. Point stays silent. Non-vocal.

## Progress log
- 2026-10-17: one new data cue, `EmoteHandClap` (a single clap), in `CueDefsEmotes.cs`. `EmoteSounds` gets a Clap script (hands meet at 0.25 s then every 1/3.2 s, five claps, from Animation's pose in `WorkMotion.cs`) and a Shrug script (rustles at 0.25 s and 1.05 s of its 1.4 s). Preview scene `clap:7` in `AudioScenes`.
- Levels with ambience off (silence -65 dBFS): clap about -30, shrug rustles about -37 dBFS. Client builds, 0 errors.
- If Animation retimes either pose, change the times in `EmoteSounds` to match.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
