# audio-32: Wild rabbit sounds
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-32-rabbit
PR: (see below)

## Goal
For Animals' wild rabbit (D-323, animals-22): a foot-thump alarm when a rabbit bolts, a small squeak when one is hurt, and quiet rustling while one grazes close by. No bleating.

## Progress log
- 2026-10-17: three data cues in `CueDefsRabbit.cs` (`RabbitThump`, `RabbitSqueak`, `RabbitRustle`). `RabbitSounds` (client/scripts/atmosphere) keys on the species (`AnimalSpecies.Rabbit` = 3, `const RabbitSpecies`), not the model (it is "sheep" until Modeling's figure lands), from the public `animal` table and the `AnimalHit` event, in its own subscription: State going to Run is the thump (heard 30 m, reducer-driven updates only), a hit on a known rabbit is the squeak (25 m), and a rabbit in Graze rustles every 1.5-5 s (7 m). `Bleats` is false for rabbits so the existing sheep voices never fire. Hooked through `AudioHooks`; preview scene `rabbit:8` in `AudioScenes`.
- Levels with ambience off: thump about -29, squeak about -30, rustle about -36 dBFS. Client builds, 0 errors.
- Independent of the animals-22 branch (it compares the species number), so it merges in either order; check that Rabbit is still 3 if the enum changes.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
