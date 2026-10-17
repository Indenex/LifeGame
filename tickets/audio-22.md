# audio-22: Basket and storage-jar sounds
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-22-stores
PR: (see below)

## Goal
Putting things into and taking them out of baskets and storage jars was silent. A basket now rustles (wicker) and a jar clinks (fired clay and lid), heard by anyone within 12 m.

## Progress log
- 2026-10-17: four data cues in `CueDefsStore.cs` (`BasketPut`, `BasketTake`, `JarPut`, `JarTake`; `CueDefs` is `partial`, as in audio-21). `StorageSounds` (client/scripts/atmosphere) watches the public `basket_item` rows: insert or a count rise is a put, a count drop or delete is a take; jar or basket is `Structures.Shape(kind).Jar`. Only rows changed by a reducer call make a sound, so rows arriving with a subscription (walking into a chunk) stay silent. Played where the store stands with `Sfx.PlayAnimal`. Preview scene `store:9`: each cue's loudest 50 ms about -28 dBFS over a -34 bed (Pickup is -25). Client builds, 0 errors.
- No lid-open/close sound: opening is a client-only UI state (no table change), so the lid sound is folded into the put/take cue. If Justin wants an open sound for others to hear, the server would need an "open" flag.

## Open questions / blockers
- Not heard in the running game.

## Next step
Send the PM the green sha once CI finishes (do not re-run or push to retrigger).
