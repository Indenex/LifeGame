# model-14: Pebble's authored shell (Blender pipeline)
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm); built by Art R&D (art-rnd-1)
Status: in_review
Branch: claude/model-14-authored-shell
PR: (opening)

## Goal
Replace Pebble's code-built shell, which Justin rejected in #594 ("awful"), with Art R&D's authored shell. It is modelled and baked in headless Blender with a script-painted texture, in D-313's cute, rounded style. The PM asked Modeling to take it into its lane with small hooks.

## Progress log
- 2026-10-17 08:00: Brought in from claude/art-rnd-1 (head 94479019), onto main.
  - Assets: `client/assets/models/pebble_shell/` (glb, painted scute and worn textures, normal map).
  - `ChibiShell.cs` puts the shell on the Spine bone. `PaintedAsset.cs` is the loader and ChibiLook-style material for authored assets.
  - Tools: `tools/art/pebble_shell/`, the Blender build plus geometry and paint scripts.
  - Hooks: ChibiFigure skips the built shell and belly plate when `ChibiShell.Enabled`; PlayerModel.SetLook calls `ChibiShell.Sync`. Showcase gets `turntable` and `art` views.
  - Builds clean on main. The turntable render looks right (renders/model-14-shell-turntable.png).
- Left on R&D's branch: its research report (docs/art-pipeline.md), its ticket, and Python caches.

## Open questions / blockers
- (none; wider Blender rollout waits on Justin per the PM)

## Next step
Open the PR and close #594; send a Render request for the shell in the world.
