# atmosphere-30: Tool impacts (chips, clods, a felled tree's dust)
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-30-impacts (merged); follow-up claude/atmosphere-30b-impact-hook (#505, merged); next claude/atmosphere-30c-tree-landed
PR: #486 (merged); follow-up opens when #496 lands

## Goal
The PM's add-on after frost, from Justin's "interact with the world more". Tool work gets painted, stylised impact feedback, client-only:
- pale wood chips and a little bark fly from the trunk on each axe hit;
- dirt clods and a small dust puff kick up on each dig;
- a felled tree comes down in a ring of dust and a last shower of leaves, coloured by the season (none when the tree is already bare).
The chop and dig effects fire on Animation's impact frame: anim-7's `Avatar.Impact(avatar, motion, toolPoint, thrown)` static event. The felled tree is read from the ChoppedTree table. Done: Animation's hook merged, Render shots or a clip, and the PR green and merged.

## Progress log
- 2026-10-17 03:45: `AtmosphereImpacts.cs` (new). Chips are faceted boxes; clods are faceted spheres; puffs are soft round billboards made from a radial gradient. All are one-shot CpuParticles3D that free themselves; nothing happens beyond 50 m. ChoppedTree OnUpdate fires on the Felled transition (and OnInsert from 5 s after connecting). `--impacts=seconds` cycles chop, dig and fall in front of the player for screenshots. Hooks in `Atmosphere.cs`: `ReadyImpacts`, `ConnectImpacts` and `UpdateImpacts`. The client builds with 0 errors.

- 2026-10-17 03:50: per the PM, switched to Animation's anim-7 event `Avatar.Impact(avatar, motion, toolPoint, thrown)` and dropped ToolContact. Added dig crumbs when the tool goes in, clods on a spade's throw (thrown = true), grey flakes on Knap, and sparks plus an ash puff when fuel lands (Feed). The handler `OnAvatarImpact` is ready; the subscribe line goes in once anim-7 is on main, so this PR doesn't depend on it.

- 2026-10-17 03:55: following Animation's notes, figures face +Z, the handler takes the fifth `ushort item` argument, and Feed sparks rise from the nearest fire within 2.5 m of the hand (the Fire table), not from the hand.

- 2026-10-17 03:47: #486 merged (green sha 6ec922f8, CI run 38999737058). The felled-tree dust and leaves are live. Chop, dig, knap and feed wait on the subscribe line after anim-7 (#496).

- 2026-10-17 03:55: #496 is on main. Subscribed `Avatar.Impact += OnAvatarImpact` once in ConnectImpacts; the handler ignores the event if the atmosphere or avatar has been freed. The client builds with 0 errors.

- 2026-10-17 04:01: #505 merged (green sha 5961f9fa, CI run 39000342708). Chips, crumbs, clods, flakes and sparks are live on Avatar.Impact.
- Next: Environment's env-22 (branch claude/environment-tree-fall, PR coming) adds `TreeFall.Landed(Vector3 crownImpact, Vector3 direction)`, firing 1.6 s after Felled when the crown hits, for every fresh felling. Move the dust and leaves onto it (a ring at crownImpact, a smaller one at foot + direction × 2 m, leaves bursting round the crown) and drop the ChoppedTree trigger.

- 2026-10-17 06:05: env-22 (#537) merged. The felled-tree dust and leaves now fire on TreeFall.Landed(crownImpact, direction): a dust ring at the crown, a thinner one mid-trunk, and leaves bursting up from the crown and fluttering down. The ChoppedTree trigger is dropped, so nothing fires 1.6 s early or twice. The client builds with 0 errors.

## Open questions / blockers
- anim-7 (branch claude/anim-7) must reach main before I can subscribe: `Avatar.Impact += (a, m, p, thrown, item) => OnAvatarImpact(a, m, p, thrown, item);` (anim-7 is PR #496) in ConnectImpacts. The handler is ready.

## Next step
Open the PR from claude/atmosphere-30c-tree-landed, wait for CI, send the PM the green sha.
