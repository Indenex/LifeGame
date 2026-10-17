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
The chop and dig effects fire on Animation's impact frame: anim-7's `Avatar.Impact(avatar, motion, toolPoint, thrown)` static event. The felled tree is read from the ChoppedTree table. Done: Animation's hook merged, Render shots or a clip, and the PR green and merged.

## Progress log
- 2026-10-17 03:45: `AtmosphereImpacts.cs` (new). Chips are faceted boxes; clods are faceted spheres; puffs are soft round billboards made from a radial gradient. All are one-shot CpuParticles3D that free themselves; nothing happens beyond 50 m. ChoppedTree OnUpdate fires on the Felled transition (and OnInsert from 5 s after connecting). `--impacts=seconds` cycles chop, dig and fall in front of the player for screenshots. Hooks in `Atmosphere.cs`: `ReadyImpacts`, `ConnectImpacts` and `UpdateImpacts`. The client builds with 0 errors.

- 2026-10-17 03:50: per the PM, switched to Animation's anim-7 event `Avatar.Impact(avatar, motion, toolPoint, thrown)` and dropped ToolContact. Added dig crumbs when the tool goes in, clods on a spade's throw (thrown = true), grey flakes on Knap, and sparks plus an ash puff when fuel lands (Feed). The handler `OnAvatarImpact` is ready; the subscribe line goes in once anim-7 is on main, so this PR doesn't depend on it.

## Open questions / blockers
- anim-7 (branch claude/anim-7) must reach main before I can subscribe: `Avatar.Impact += (a, m, p, thrown) => OnAvatarImpact(a, m, p, thrown);` in ConnectImpacts. The handler is ready.

## Next step
Wait for #486 CI and send the PM the green sha. Animation and Render requests are sent.
