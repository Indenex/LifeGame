# audio-14: Tool-work sounds (chop, fell, dig)
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-14-tools
PR: (see below)

## Goal
Check the sounds for chopping, felling and digging and add what is missing, as CueDef data timed off Animation's contact moments.

## What was there (checked 2026-10-17)
- Chop: nothing. `chop.log` maps to `Work.None` in `SoundMix.WorkFor`, so a chop job was silent.
- Tree creak / fall: nothing anywhere (a felled tree just vanished from `ChoppedTree.Felled`).
- Dig: nothing; `player_act` Dig (0.9 s) had no entry in `SoundOfAct` and `Main.Dig()` plays none.
- Shovel: there is no shovel item in the game (only the digging stick and bare hands), so no shovel cue yet. A `Def` entry plus one `SoundOfAct` line adds it when the item exists.

## Progress log
- 2026-10-17: added `CueDefs.ChopThunk/TreeCreak/TreeCrash/DigStick/DigHands` and `ToolSounds` (client/scripts/atmosphere/ToolSounds.cs).
  - Chop: while a chop job runs (yours from `MyCraftJob`, others' from `craft_work`, chosen by `WorkMotions.For(processId) == Chop`) a thunk lands every 1/0.85 s, first at 0.78/0.85 s after the job starts: Animation's Chop pose reaches the strike at 0.78 of each swing. Others' are positional and quieter (`Sfx.PlayBy`).
  - Fell: when a `ChoppedTree` row becomes Felled (update false to true, or an insert after the first 5 s so the initial sync is silent): the creak now, the crash 0.95 s later, positional out to 70 m (`Sfx.PlayAnimal`).
  - Dig: `SoundOfAct` now returns `DigStick` when `player_act.Item` is a digging stick and `DigHands` for bare hands; contact at about 0.28 s, matching the stab in Animation's Dig pose.
  - Levels (loudest 50 ms, dBFS, over the morning bed): chop -27, creak -30, crash -20, dig stick -27, dig hands -30. `tools:15` SoundPreview scene plays them. Edges and fuzz soak clean; client builds.

## Open questions / blockers
- Not heard in the running game (no Godot here); the chop sync assumes Animation's work clock starts at the `MyCraftJob` insert.
- The tree vanishes the instant it is felled, so the creak and crash play over an already-empty spot. If Environment adds a falling-tree animation, tell Audio to retime the crash to its impact.

## Next step
Wait for CI, send the PM the green sha.
