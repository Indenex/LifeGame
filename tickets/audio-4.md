# audio-4: Red deer sounds
Lane: Audio
Owner: Audio lane (Sonnet session_01SXcxtoyeqDmypiHxkHedU6)
Status: in_progress
Branch: claude/audio-4-deer
PR: (after CI)

## Goal
Deer are quiet except: a short hoarse alarm bark when a deer starts to run (one per herd every few seconds, not per animal); a long deep rolling stag roar at dusk in autumn, heard from far off; soft hoof thuds when deer run. Brief from Animals (animals-14, PR #219; Justin approved, D-079). Done when a SoundPreview clip has gone to Justin via Render and the sounds play in the game from the `animal` table.

## Progress log
- 2026-10-16: brief received from both Animals sessions. `Sheep.IsStag(ulong id)` is on claude/animals-deer-5gkntn (abfd879), lands on main with #219.
- 2026-10-16: built SoundMix.Deer (DeerCall.Bark/Roar/Hoof, distance rounds and reverb-sends them), SoundMix.DeerCry, SoundScape.DeerCry, client/scripts/atmosphere/DeerVoices.cs (herd bark rule, stag roars, hoof thuds; same shape as SheepVoices), SoundPreview scene `deer` (autumn dusk). Loudest 50 ms: close bark -26.0, far bark -30.9, far roar -28.2, near roar -25.4, hooves -28.1 dBFS (standing bed -32.3). Client not built locally (no bindings here); CI checks.
- Not wired into the game yet: `AnimalSpecies.Deer` is not on main until #219 lands, and Atmosphere's sheep-only filter (#217) is not on main either.

- 2026-10-16: Animals (session_015wozcQSFB48cUjQ9p676zf) also wants hit and crit sounds: the server emits a public event table `AnimalHit {AnimalId, X, Z, Crit, Killed}` (#219), one row per hit, Crit true for the sneak-attack bonus. Added `Cue.Hit` (dull thud + slap) and `Cue.Crit` (thud + sharp crack + short bright ring), appended to the Cue enum. SoundPreview `cues:26` plays them last. Loudest 50 ms: Hit -25.3, Crit -23.5 dBFS (other cues -26 to -28). Wiring waits for #219.

## Open questions / blockers
- Wiring waits for #219 (AnimalSpecies.Deer, Sheep.IsStag) and #217 (Atmosphere sheep-only bleats).

## Next step
When #219 and #217 are on main: in client/scripts/atmosphere/Atmosphere.cs add `public readonly DeerVoices Deer = new();` with `Deer.Cry = sound.DeerCry;`, feed it from conn.Db.Animal OnInsert/OnUpdate/OnDelete for Species == AnimalSpecies.Deer with `Deer.Seen(a.Id, a.X, a.Z, a.State == AnimalState.Run, Sheep.IsStag(a.Id))` (the Animals class named Sheep, not the SheepVoices field; qualify as needed), `Deer.Gone(id)` on delete, call `Deer.Tick(dt, focus, ear.GlobalBasis.X)` each frame, and set `Deer.RoarTime` to the autumn dusk weight (Season == Autumn, smoothstep on evening TimeOfDay). Also subscribe to `AnimalHit` (`conn.Db.AnimalHit.OnInsert`): play Cue.Crit when Crit else Cue.Hit, only when the hit is within ~30 m of the player (volume: use Sfx.Play for now; a positional version is a later nicety). Then render `deer:24` and `cues:26`, request a Render clip, open the PR.
