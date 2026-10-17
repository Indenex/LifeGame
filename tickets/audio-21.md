# audio-21: Roast sizzle
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-21-roast
PR: (see below)

## Goal
Meat roasting over a fire was silent. It now sizzles softly with spits and drips of fat, heard close by (9 m).

## Progress log
- 2026-10-17: three data cues in `CueDefsRoast.cs` (`RoastSizzle`, `RoastSpit`, `RoastDrip`; `CueDefs` is now `partial`). `RoastSounds` (client/scripts/atmosphere) reads the public `roast` table (put-on time `PutAt`) and, per piece, schedules cues by `Roasting` state from shared: browning is busy sizzle, ready settles with the odd drip, starting to char (last 10 s before `BurnSeconds`) and burnt spit and crackle more. Played with `Sfx.PlayAnimal` (falloff by distance). Preview scene `roast:18` (browning, ready, charring). First gains were ~12x too quiet: CueLayer gains run 0.03-0.4 for handling sounds (Pickup 0.39), not the 0.00x of the bubbles; rescaled so the sizzle sits at about -32 dBFS and spit/drip at about -30 over the bed (measured as the loudest 50 ms of each cue in a scene, the bed being about -34). Client builds, 0 errors.
- NOTE for #631 (pot bubbles, audio-19): its cue gains use the same too-quiet scale and probably need the same fix; will follow up once its CI settles.
- Left: none (basket and jar sounds are audio-22).

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
