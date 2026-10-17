# atmosphere-33: Heavy winter ground snow
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-33-snow
PR: (none)

## Goal
Justin, on the first frost renders (atmosphere-28, Render B's stills renders/atmosphere-28-7c8b799-1..7 at full frost): "I like the snow." Frost is now lighter and autumn-only, but that heavy white look should become the basis for winter ground snow. Winter already has `snow_cover` in the painted and grass shaders (Look.cs: SNOW settles on upward faces, filling hollows first, and grass wears it on its tips). Compare it with the old full-frost look: an even, soft white over the ground, rocks, canopies and grass, with the brush texture still showing, sparkle glints, and a cool tint. Bring winter snow closer to that. Done: Render stills of a snowy morning shown to Justin, and merged.

## Progress log
- 2026-10-17 06:35: created from Justin's answer via the PM. The old full-frost shader settings are in the atmosphere-28 history: rime = smoothstep over a patch noise, mix 0.75 toward (0.80, 0.86, 0.93), glints at step(0.985) of a hash.
- 2026-10-17 07:55: built in Look.cs.
  - Painted shader: on cavity > 0.01 (land, rocks, canopies; not people, animals or made things) snow lies evenly down the sides (normal.y from -0.1), drifting in from soft-edged detail patches as snow_cover rises, up to 92%.
  - Colour: faces turned from the sky shade to a cool SNOW_SHADE (0.70, 0.78, 0.91); the brush paint still modulates it.
  - Glints: step(0.985) sparkle cells, the same as the frost.
  - Grass: white to the tips, cool blue at the roots, cover 75 to 100%.
  - The client builds with 0 errors; the shader is not compiled in CI.

## Open questions / blockers
- None yet. Start once the painted-look work (Justin's top priority) allows.

## Next step
Wait for Render B's `--snow-cover=1` stills (before on main, after on this branch) and CI; tune; send the green sha to the PM.