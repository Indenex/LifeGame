# audio-28: Roosting hens murmur
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-28-roost
PR: (see below)

## Goal
A hen roosting at night in a coop murmurs sleepily (audio-13's `HenRoost` cue), heard by state and distance even though a hen in a finished coop is not drawn.

## Progress log
- 2026-10-17: Animals' #483 put `AnimalState.Roost` on main (after Run). `HearAnimal` now maps it to `HenVoices.Mood.Roost`. `HenVoices` takes each hen's position and mood from the animal row, not from anything drawn, so a hen out of sight inside a coop still murmurs: only once it is getting dark (`NightTime` over 0.1) and louder the darker it gets, within 10 m. Client builds, 0 errors. No new cue.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
