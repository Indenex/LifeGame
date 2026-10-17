# audio-36: Straw bed and wooden chest
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-36-bedchest
PR: (see below)

## Goal
House building's straw bed (kind 25) and wooden chest (kind 24) now sound right: lying on a straw bed crackles with straw (not reeds), and putting into or taking out of a chest has a wooden lid (creak and hollow clunk) instead of a basket's wicker rustle.

## Progress log
- 2026-10-17: three data cues in `CueDefsBedChest.cs` (`RestLieBed`, `ChestPut`, `ChestTake`). `RestSounds` picks the lie-down cue from the rest spot's structure (`PlayerRest.SpotId` is the structure id): a `Bed` model gets straw, a laid mat reeds, bare ground the plain cue. `StorageSounds` picks `ChestPut`/`ChestTake` for a `Chest` model; it also now sounds each store at most once per 0.6 s, so putting several things in at once is one lid, not a clunk each. Preview scene `bedchest` (`Scenes/BedChest.cs`).
- Levels with ambience off: bed about -30, chest about -29 dBFS loudest 50 ms. Client builds, 0 errors.
- Opening a chest or bed is client UI only (no table change), so there is no separate open or close sound; the lid is in put/take.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
