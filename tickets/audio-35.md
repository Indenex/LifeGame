# audio-35: Animal sounds keyed on AnimalDef.Voice
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-35-voice
PR: (see below)

## Goal
Audio's animal dispatch mixed `Bleats`, the deer species enum, the model string "hen" and the species number 3. It now reads the one id Animals added to the Defs (`AnimalDef.Voice`, #751), so a new species with an existing voice is a Def line.

## Progress log
- 2026-10-17: `HearAnimal` is a switch on `Voice` ("sheep" -> sheep bleats, "deer" -> deer calls, "hen" -> hen voices); `Atmosphere`'s egg song checks `Voice == "hen"` instead of the model; `DeerGrazing` checks "deer", `RabbitSounds` checks "rabbit" (its species-number const is gone). Same animals sound the same. Client builds, 0 errors.
- `AnimalDef.Bleats` is no longer read by Audio: Animals can delete it (they asked to be told).

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes; tell Animals `Bleats` is free to delete once this merges.
