# audio-16: Door sounds
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-16
PR: (see below)

## Goal
Doors were silent. A hung door now creaks open, clacks shut, and a barred door drops its pole with a thunk, as CueDef data heard where the building stands.

## Progress log
- 2026-10-17: `CueDefs.DoorOpen/DoorShut/DoorBar/DoorUnbar` plus `StructureSounds` (client/scripts/atmosphere/StructureSounds.cs), which reads the `structure_door` OnUpdate old/new rows: Locked changed -> bar/unbar, else Closed changed -> shut/open. Played at the structure (`Sfx.PlayAnimal`, full beside it, fading out to 25 m), for anyone's door, yours included. Timing follows `StructureView.SetDoor`'s 0.6 s swing: open creaks through the swing and knocks at the stop; shut's latch clack lands at 0.38 s. Levels (loudest 50 ms over the bed, dBFS): open -33, shut -28, bar -28, unbar -35. `doors:9` SoundPreview scene. Edges and fuzz soak clean; client builds.
- Not covered yet: hanging a new door (the structure view's settle pop plays the generic Place cue only if Building plays it), baskets/jars opening, kiln firing, pots simmering, roast sizzle. Proposed as the next candidates to the PM.

## Open questions / blockers
- Not heard in the running game.

## Next step
Wait for CI, send the PM the green sha.
