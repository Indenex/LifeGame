# audio-6: Bow sounds
Lane: Audio
Owner: Audio lane (Sonnet session_01SXcxtoyeqDmypiHxkHedU6)
Status: in_progress
Branch: claude/audio-6-bow
PR: (after CI)

## Goal
From the PM (for Animals' bow, #355 / animals-15): a soft creak while drawing, the string's twang and whoosh on release, and an arrow thunk on a hit or a miss (animal, wood, earth). Other players' shots are positional (quieter with distance, from their side). Done when each plays in the game from Animals' hook and Justin has heard a clip.

## Progress log
- 2026-10-16: five cues appended to the Cue enum: BowDraw (irregular, band-passed noise creak, never a steady tone), BowShot (damped low twang plus a falling whoosh), ArrowFlesh (dull wet thunk), ArrowWood (sharp thock, shaft hums), ArrowEarth (dull thud with grit). SoundPreview `cues:45` plays every cue in turn. Loudest 50 ms (dBFS): draw -33.1, shot -26.1, flesh -26.2, wood -26.2, earth -26.1. `--edges` and `--soak --fuzz` still clean.
- Positional play already exists: `Sfx.PlayAt(cue, worldPos)` for other players, `Sfx.Play(cue)` for your own.

## Open questions / blockers
- Hook, proposed to Animals (needs a server event): a public event table `ArrowShot {PlayerId, FromX, FromZ, ToX, ToZ, Hit, Target}` (Target: Animal, or Ground/Tree for a miss, if known; the client can guess tree vs ground from Forest.Density at ToX/ToZ) emitted once per shot, and a `Ranged` flag on `AnimalHit` so an arrow hit does not also play the melee thud. Client: on ArrowShot play BowShot at From (own shot: Sfx.Play), then after the flight time (distance / ~60 m/s) the impact at To. BowDraw only if Animals adds a draw phase (the shot is instant today).

## Next step
Wait for CI on the audio-6 PR; send the PM the green sha; wire when Animals' table is on main.
