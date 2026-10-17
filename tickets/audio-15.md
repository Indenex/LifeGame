# audio-15: A roaring crackle for a well-stoked fire
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-15-fire
PR: (see below)

## Goal
Matching Atmosphere's big smoke plumes for a well-stoked fire (above 75%): the campfire gets a deeper, slightly louder roar and more pops.

## Progress log
- 2026-10-17: the campfire is a continuous voice (SoundMix.FireVoice), not a one-shot cue, so this is a parameter rather than a CueDef: `SoundScape.FireStoke` / `SoundMix.FireStoke` (0 to 1). `Atmosphere.FireSound` sets it from the nearest strongest fire: fuel left / `Fires.MaxSeconds` (a freshly lit or well-fed fire is 1, below about 0.7 nothing changes). The voice swells in slowly (about 0.4 Hz smoothing) from 0.7 to 0.95: the breathing roar gets +35%, a deep 120 Hz rumble breathes under it, and crackles and pops come about 1.9x and 3x as often. Measured (`firestoked:30` vs `fire:30` SoundPreview scenes, standing rms): -28.9 vs -26.3 dBFS (+2.6 dB, "slightly louder"), peak -6.8 vs -5.8. Edges and fuzz soak clean; client builds.
- Hook for Atmosphere: nothing to do; it reads the same fuel as FireView. If Atmosphere uses a different "stoked" definition for the plumes (e.g. a Firecraft-scaled cap), tell Audio and I align `FireSound`.

- 2026-10-17 (follow-up): Crafting's `Fire.FuelCapSeconds` (#531) is on main, so stoke is now fuel left over each fire's own cap, ramping in over 70 to 80% of it (the same ramp as Atmosphere's smoke signal), via `Atmosphere.Stoke` and a `fireCaps` map kept in AtmosphereSound.cs. Fires from before the column (cap 0) fall back to the Master cap. The voice's own smoothing ramp is removed (it now follows `FireStoke` directly, with the same slow swell).

## Open questions / blockers
- Not heard in the running game.

## Next step
Wait for CI, send the PM the green sha.
