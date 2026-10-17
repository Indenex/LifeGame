# atmosphere-30: Tool impacts (chips, clods, a felled tree's dust)
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-30-impacts
PR: #486

## Goal
The PM's add-on after frost, from Justin's "interact with the world more". Tool work gets painted, stylised impact feedback, client-only:
- pale wood chips and a little bark fly from the trunk on each axe hit;
- dirt clods and a small dust puff kick up on each dig;
- a felled tree comes down in a ring of dust and a last shower of leaves, coloured by the season (none when the tree is already bare).
The chop and dig effects fire at Animation's contact moment, through `Atmosphere.ToolContact`, a one-line call from Animation. The felled tree is read from the ChoppedTree table. Done: Animation's hook merged, Render shots or a clip, and the PR green and merged.

## Progress log
- 2026-10-17 03:45: `AtmosphereImpacts.cs` (new). Chips are faceted boxes; clods are faceted spheres; puffs are soft round billboards made from a radial gradient. All are one-shot CpuParticles3D that free themselves; nothing happens beyond 50 m. ChoppedTree OnUpdate fires on the Felled transition (and OnInsert from 5 s after connecting). `--impacts=seconds` cycles chop, dig and fall in front of the player for screenshots. Hooks in `Atmosphere.cs`: `ReadyImpacts`, `ConnectImpacts` and `UpdateImpacts`. The client builds with 0 errors.

## Open questions / blockers
- Animation (session_019roNRWQBPw4ozNLDhzseig) needs to call `Atmosphere.ToolContact?.Invoke(motion, toolHeadWorldPos, facing)` when a Chop swing lands (u = 0.78 in WorkMotion.Chop) and at the bottom of a Dig stab (about t = 0.45 s), for both you and other players.

## Next step
Wait for #486 CI and send the PM the green sha. Animation and Render requests are sent.
