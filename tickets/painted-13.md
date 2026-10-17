# painted-13: Painted ghosts follow env-25
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/painted-13-ghosts (stacked on painted-12, #642)
PR: (opening)

## Goal
Environment's env-25 (#587) changed how Look's ghosts fade: `depth_prepass_alpha`, so overlapping faded surfaces don't stack, and `STRUCTURE_GHOST` for buildings' own fade rules. Painted props, structures and trees fade the same way.

## Progress log
- 2026-10-17 08:20: Painted props' ghost uses `render_mode cull_back, depth_prepass_alpha`, as Look's tree and prop ghosts do.
  - Painted structures' ghost defines `STRUCTURE_GHOST`, with `cull_disabled, depth_prepass_alpha`, exactly as Look.StructureGhostShader.
  - PaintedTrees' ghost uses `depth_prepass_alpha` in place of `depth_draw_always`. It stays `cull_disabled`, because painted boughs are single-sided cards.
  - Built clean. The Showcase `structures --cutaway` render shows the painted roofs fading correctly.

## Open questions / blockers
- (none)

## Next step
Merge after #642; send the green sha to the PM.
