# audio-30: Deer grazing
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-30-deer
PR: (see below)

## Goal
A grazing deer was silent (deer only barked, roared in autumn and drummed their hooves). While a deer is in the Graze state it now tears and chews grass now and then and lifts its head with a snort every so often, heard close by. Non-vocal-calls only.

## Progress log
- 2026-10-17: two data cues in `CueDefsDeer.cs` (`DeerCrunch`, `DeerSnort`). `DeerGrazing` (client/scripts/atmosphere) reads the public `animal` table itself (own `Connect`, nothing added to Atmosphere): for each deer in `AnimalState.Graze`, a crunch every 1.4-4 s (heard to 10 m) and a snort every 25-65 s (22 m). When they graze is up to Animals' daily rhythm (dawn and dusk): this follows the animal's state, so it needs no time-of-day logic. Hooked through `AudioHooks`; preview scene `deer:10` in `AudioScenes`.
- Levels with ambience off: crunch about -34, snort about -32 dBFS loudest 50 ms. Client builds, 0 errors.

## Open questions / blockers
- Not heard in the running game. If Animals' rhythm adds a distinct state for the dawn/dusk graze, key off that instead.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
