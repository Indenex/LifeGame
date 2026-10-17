# animals-24: Voice key on the AnimalDef
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-voice-5gkntn
PR: (to open)

## Goal
D-206 housekeeping, no change to what plays: `AnimalDef.Voice` ("sheep", "deer", "hen", "rabbit") names the voice set Audio plays for a species. Audio's animal dispatch today mixes `Bleats`, `Species == Deer`, `Model == "hen"` and a species number (3) in `RabbitSounds`; switching those to `AnimalDefs.Get(...).Voice` makes a new species with an existing voice a Def line. `Bleats` stays until Audio has moved over.

## Progress log
- 2026-10-17: Def field and the four values added; server and client build. Told Audio.

## Open questions / blockers
- Audio adopts the key in its own files (AtmosphereSound.cs, RabbitSounds.cs, DeerGrazing.cs, Atmosphere.cs); then `Bleats` can go.

## Next step
Wait for CI, send the green sha to the PM.
