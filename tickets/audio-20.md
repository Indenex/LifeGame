# audio-20: Kiln roar
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-20-kiln
PR: (see below)

## Goal
A kiln firing its load (ceramics) was silent. A firing kiln now roars like a hot fire in a brick dome.

## Progress log
- 2026-10-17: the campfire voice is a continuous voice, so the kiln feeds it rather than a CueDef: in `Atmosphere.FireSound`, each firing kiln (the public `kiln` table, State == Firing and `FireFinish` ahead, kept in `kilnFires` by `HearKilns` in AtmosphereSound.cs) counts as a fire at full stoke (`FireStoke` = 1, the deep rumble and extra pops of audio-15), at 0.8 of a campfire's strength and heard out to 14 m instead of 18 (the dome muffles it). It starts and stops with the kiln's State, so nothing to time. Edges and fuzz soak clean; client builds. Same levels as the well-stoked fire: about +2.6 dB rms over a normal campfire.
- Left: roast sizzle, basket and jar lids.

## Open questions / blockers
- Not heard in the running game. If the kiln should sound different from a stoked campfire (more rumble, fewer pops, with a bellows-like breathing), tell Audio and I add a `FireMuffle` parameter to the voice.

## Next step
Send the PM the green sha.
