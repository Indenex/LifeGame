# atmosphere-33: Heavy winter ground snow
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: todo
Branch: (not started)
PR: (none)

## Goal
Justin, on the first frost renders (atmosphere-28, Render B's stills renders/atmosphere-28-7c8b799-1..7 at full frost): "I like the snow." Frost is now lighter and autumn-only, but that heavy white look should become the basis for winter ground snow. Winter already has `snow_cover` in the painted and grass shaders (Look.cs: SNOW settles on upward faces, filling hollows first, and grass wears it on its tips). Compare it with the old full-frost look: an even, soft white over the ground, rocks, canopies and grass, with the brush texture still showing, sparkle glints, and a cool tint. Bring winter snow closer to that. Done: Render stills of a snowy morning shown to Justin, and merged.

## Progress log
- 2026-10-17 06:35: created from Justin's answer via the PM. The old full-frost shader settings are in the atmosphere-28 history: rime = smoothstep over a patch noise, mix 0.75 toward (0.80, 0.86, 0.93), glints at step(0.985) of a hash.

## Open questions / blockers
- None yet. Start once the painted-look work (Justin's top priority) allows.

## Next step
Compare today's snow_cover look with Render B's full-frost stills (ask Render B for a `--snow-cover=1` still at the same spot), then port the frost look's evenness, tint and glints into the snow code.
