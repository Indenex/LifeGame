# audio-13: Hen sounds
Lane: Audio
Owner: Audio thread
Status: in_progress
Branch: claude/audio-13-hen
PR: (see below)

## Goal
Sounds for Animals' hen (Justin's pick): soft clucks (idle and while pecking), a startled squawk with a flap, and a sleepy roosting murmur at dusk. CueDef data (`CueDefs.HenCluck/HenClucks/HenPeck/HenSquawk/HenRoost`) plus a scheduler (`HenVoices`) that decides when each hen makes a sound.

## Progress log
- 2026-10-17: stacked on audio-12 (#427). Cues and `HenVoices` done and level-checked in the `hens:13` SoundPreview scene (loudest 50 ms over a -37 to -43 dBFS bed: peck -32/-38, cluck -32, clucks -33, squawk -26, roost -36). `Sfx.PlayAnimal(def, where, hearing, loudness)` plays any animal cue where it is. `Atmosphere.Hens` is ticked each frame with NightTime from the time of day and `Hens.Gone` is wired. `HearAnimal` calls `Hens.Seen` for any animal whose `AnimalDefs` Model is "hen" (Graze=Peck, Walk, Run=squawk, else Idle), so it starts working when Animals' hen (#454) merges. Roost is not mapped yet: Animals plans `AnimalState.Roost` appended after Run; when it lands map it to `HenVoices.Mood.Roost`. Laying cluck (Animal.Eggs going up, seen in the Animal OnUpdate old/new rows) comes after #454 merges.
- Hook for Animals once `AnimalSpecies.Hen` lands: in `Atmosphere.HearAnimal` (AtmosphereSound.cs), add `else if (a.Species == AnimalSpecies.Hen) Hens.Seen(a.Id, a.X, a.Z, mood)` with mood = Run if State==Run, Peck if Graze, Walk if Walk, Idle if Idle, Roost when the hen is in its coop at night (whatever signal Animals uses). Audio will add this line when the species merges.

## Open questions / blockers
- Needs `AnimalSpecies.Hen` on main and how a roosting hen is represented (a state, or inside the coop).
- Not heard in the running game (no Godot here).

## Next step
When the hen species lands on main, add the `HearAnimal` line above, build, and send the PM the green sha.
