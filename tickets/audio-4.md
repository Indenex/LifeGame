# audio-4: Red deer sounds
Lane: Audio
Owner: Audio lane (Sonnet session_01SXcxtoyeqDmypiHxkHedU6)
Status: in_review
Branch: claude/audio-4-deer (sounds, merged #242); claude/audio-4-wiring (this PR)
PR: (after CI)

## Goal
Deer are quiet except: a short hoarse alarm bark when a deer starts to run (one per herd every few seconds, not per animal); a long deep rolling stag roar at dusk in autumn, heard from far off; soft hoof thuds when deer run. Brief from Animals (animals-14, PR #219; Justin approved, D-079). Done when a SoundPreview clip has gone to Justin via Render and the sounds play in the game from the `animal` table.

## Progress log
- 2026-10-16: brief received from both Animals sessions. `Sheep.IsStag(ulong id)` is on claude/animals-deer-5gkntn (abfd879), lands on main with #219.
- 2026-10-16: built SoundMix.Deer (DeerCall.Bark/Roar/Hoof, distance rounds and reverb-sends them), SoundMix.DeerCry, SoundScape.DeerCry, client/scripts/atmosphere/DeerVoices.cs (herd bark rule, stag roars, hoof thuds; same shape as SheepVoices), SoundPreview scene `deer` (autumn dusk). Loudest 50 ms: close bark -26.0, far bark -30.9, far roar -28.2, near roar -25.4, hooves -28.1 dBFS (standing bed -32.3). Client not built locally (no bindings here); CI checks.
- Not wired into the game yet: `AnimalSpecies.Deer` is not on main until #219 lands, and Atmosphere's sheep-only filter (#217) is not on main either.

- 2026-10-16: Animals (session_015wozcQSFB48cUjQ9p676zf) also wants hit and crit sounds: the server emits a public event table `AnimalHit {AnimalId, X, Z, Crit, Killed}` (#219), one row per hit, Crit true for the sneak-attack bonus. Added `Cue.Hit` (dull thud + slap) and `Cue.Crit` (thud + sharp crack + short bright ring), appended to the Cue enum. SoundPreview `cues:26` plays them last. Loudest 50 ms: Hit -25.3, Crit -23.5 dBFS (other cues -26 to -28). Wiring waits for #219.

- 2026-10-16: Bug fixes (bugfix-25, tickets/bugfix-25.md on claude/bugfix-25) asked for jump sounds. Added `Cue.Jump` (push-off scuff + breath), `Sfx.Land(float strength, Ground ground)` (both feet, heavier and lower with strength; SoundMix.Land) and public `Atmosphere.GroundAt(Vector3)` (the ground-type logic from Footsteps, moved). SoundPreview `jump:12`: push -30.2, hop landing -31.1, big landing -27.5, sand landing -29.1, normal step -33.2 dBFS.

- 2026-10-16: Justin picked "others see your jump" (via Bug fixes): added positional `Sfx.PlayAt(Cue, worldPos)` and `Sfx.LandAt(strength, ground, worldPos)` (Atmosphere reports the listener each frame via Sfx.Listen; others sit at 0.4 gain, silent beyond 30 m, panned to their side). SoundMix.Play/Land gained gain and pan.

- 2026-10-16: #219 (deer) merged (f940fa3). Wired: Atmosphere reports deer to DeerVoices (Seen/Gone/Tick; Deer.Cry = sound.DeerCry; RoarTime = autumn x dusk), AnimalHit.OnInsert plays Cue.Crit or Cue.Hit via new Sfx.PlayHit (your own blows full volume, others' positional). The client builds locally now (installed the SpacetimeDB CLI to ~/stdb and ran tools/gen-bindings.sh): 0 errors.

## Open questions / blockers
- Wiring waits for #219 (AnimalSpecies.Deer, Sheep.IsStag) and #217 (Atmosphere sheep-only bleats).

## Next step
Wait for CI on the audio-4-wiring PR; send the PM the green sha. Later: a bowstring cue for #355 (bow shots are otherwise quiet).
