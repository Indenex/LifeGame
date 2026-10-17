# audio-21: Roast sizzle
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-21-roast
PR: (see below)

## Goal
Meat roasting over a fire was silent. It now sizzles softly with spits and drips of fat, heard close by (9 m).

## Progress log
- 2026-10-17: three data cues in `CueDefsRoast.cs` (`RoastSizzle`, `RoastSpit`, `RoastDrip`; `CueDefs` is now `partial`). `RoastSounds` (client/scripts/atmosphere) reads the public `roast` table (put-on time `PutAt`) and, per piece, schedules cues by `Roasting` state from shared: browning is busy sizzle, ready settles with the odd drip, starting to char (last 10 s before `BurnSeconds`) and burnt spit and crackle more. Played with `Sfx.PlayAnimal` (falloff by distance). Preview scene `roast:18` (browning, ready, charring): loudest 50 ms -29.3 dBFS, median -35.5 (pot sim was -30.7 / -33.3). Client builds, 0 errors.
- Left: basket and storage-jar lids.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
