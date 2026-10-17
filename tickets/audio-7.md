# audio-7: Sounds for the quick body actions, on a data-driven cue engine
Lane: Audio
Owner: Audio lane (Sonnet session_01SXcxtoyeqDmypiHxkHedU6)
Status: in_review
Branch: claude/audio-7-acts
PR: (see PM message)

## Goal
Animation's anim-4 (#382) adds the public event table `player_act {PlayerId, Motion, Item}` for drink, eat, pluck and pick-up. Give each a sound timed to its motion, heard by everyone nearby, and make them the first entries on a data-driven cue engine (docs/CONTENT.md Sound cue row, approved by the PM: convert gradually, one cue per PR).

## Progress log
- 2026-10-17: `client/scripts/atmosphere/audio/CueDefs.cs`: `CueDef` (id, length, reverb send, layers) and `CueLayer` (noise band or gliding tone, start, decay, gain, cutoffs). One generic voice in SoundMix.Cues plays any CueDef (`SoundMix.Play(CueDef)`, `SoundScape.Play`, `Sfx.Play(CueDef)`, `Sfx.PlayBy(def, where)`: yours at full volume, others positional). Four entries timed to Animation's motions: act.drink (scoop splash 0.35 to 0.75, gulp near 1.4, second gulp), act.eat (chews 0.6 to 1.1, swallow), act.pluck (rustle and snap 0.35 to 0.55), act.pickup (scuff at 0.22). Atmosphere subscribes to PlayerAct and plays the matching def at that player's PlayerPos (a motion-to-def switch in AtmosphereSound.cs). Loudest 50 ms (dBFS): drink -27.9, others about -29 to -30 (soft on purpose). Edges and fuzz soak still clean; client builds, 0 errors.
- Survival's key-press `Sfx.Play(Cue.Drink)` (water) and `Cue.Eat` calls are removed (the act sound replaces them, synced and heard by others); the pot drink keeps `Cue.Drink` because the server notes no act for it. Old Cue.Drink/Eat/Pickup stay in the enum for now (Pickup still plays when an item enters the bag, a separate sound).

## Open questions / blockers
- (none)

## Next step
Wait for CI; then convert the next simple cues (Place, Hit, Jump) to CueDefs, one per PR, within 1 dB (measure with `cues:45`).

## audio-9: Cue.Place converted
- 2026-10-17: `Cue.Place` now plays `CueDefs.Place` (a knock tone and a low scuff); `CueDefs.ForCue(Cue)` maps converted enum cues to their entries and `SoundMix.Play(Cue)` uses it, so callers (building) are unchanged and the enum stays append-only. Old Place code removed. Loudest 50 ms -27.6 dBFS vs -27.5 before (within 1 dB). Edges and fuzz soak clean; client builds. Branch claude/audio-9-place. Next: Hit, Crit, Jump, then the rest, one per PR.

## audio-10: Cue.Hit, Cue.Crit and Cue.Jump converted
- 2026-10-17: Place (audio-9) is merged. Hit, Crit and Jump are now `CueDefs.Hit/Crit/Jump` through `CueDefs.ForCue`; the three old code blocks are removed. Done in one PR because each is a small two- or three-layer cue; levels measured one by one (loudest 50 ms, dBFS, now vs before): Hit -25.4 vs -25.3, Crit -24.1 vs -23.5, Jump -31.0 vs -30.4. Edges and fuzz soak clean; client builds. Branch claude/audio-10-hit. Remaining enum cues to convert: Pickup, Eat, Drink, FireLit, SkillUp, Learn (marimba layers need a note layer type), BowDraw, BowShot, ArrowFlesh, ArrowWood, ArrowEarth.
- 2026-10-17 (audio-11): the rest of the enum is converted: Pickup, Eat, Drink, FireLit, SkillUp, Learn, BowDraw, BowShot, ArrowFlesh, ArrowWood, ArrowEarth are `CueDefs` entries (Eat and Drink cue versions are `EatBites` / `DrinkGulps`, since `Eat`/`Drink` are the act versions). The marimba chime is built from tone layers (`Bar`: fundamental, 3.9x partial, two echoes). All old code blocks, `Marimba`, `Burst` and `Trigger(Cue)` are gone; every Cue enum value now plays through `CueDefs.ForCue`. Loudest 50 ms, now vs before (dBFS): Pickup -26.5/-26.5, Eat -28.4/-28.5, Drink -27.5/-27.2, SkillUp -30.1/-30.5, Learn -37.4/-37.5, FireLit -26.9/-26.8, BowDraw -34.1/-33.5, BowShot -26.7/-26.2, ArrowFlesh -26.1/-26.3, ArrowWood -26.9/-26.6, ArrowEarth -26.6/-26.2. Known small differences: BowShot's 3% sharp glide on the twang and BowDraw's wandering centre and stutter are simplified to static layers. Edges, fuzz and inject soaks clean; client builds. Branch claude/audio-11-cues. Next: a PR, green sha to the PM; then test the music player when Justin's first .ogg arrives.
