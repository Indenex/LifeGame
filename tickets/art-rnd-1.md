# art-rnd-1: Art quality R&D, better modelling methods and a turtle-shell pilot
Lane: Art quality R&D
Owner: Art R&D session (session_019Ee5efaMp7ec6dhFqQfV6R)
Status: in_review
Branch: claude/art-rnd-1
PR: (none yet)

## Goal
Justin found Pebble's turtle shell (#594, renders/model-11-pebble-shell-*.png on claude/model-11-pebble-shell) awful and asked for research into better modelling methods. Done means:
1. `docs/art-pipeline.md`: methods ranked by quality gain against effort for a cloud-only Claude lane (Blender headless, sculpt-to-low-poly bakes, hand-painted texture pipelines, normal/parallax, Golden Skull packs, reference-driven checks), and a recommended pipeline. Summary sent to the PM.
2. Pilot: Pebble's shell redone with that pipeline (domed carapace, vertebral/costal/marginal scutes, plastron) in the painted style, fitting the chibi rig (ChibiFigure.cs, 1.35 m, 16 bones). Before/after turntable images under `renders/`, links sent to the PM.

## Progress log
- 2026-10-17: started. Old shell = one ellipsoid with ellipsoid "plates" stuck on top and a ring of ellipsoid marginals; no continuous surface, no painted texture. No Blender/Godot in the container: installing Blender 4.2 LTS to ~/tools/blender and Godot via tools/render/setup.sh.
- 2026-10-17: Blender 4.2.3 runs headless (Cycles CPU ok; Eevee ok under xvfb with libegl1). Golden Skull packs are 2D PNGs only. Draft report in docs/art-pipeline.md. Showcase --view=turntable added (bdfa9163).

- 2026-10-17: report pushed (d3fc29ae) and summary sent to the PM. Before turntables of #594 rendered (Sonnet helper; tools/render/scenes/art-rnd-1-turntable.sh).
- 2026-10-17: pilot pipeline works end to end: tools/art/pebble_shell/build.sh (Blender: model, Cycles bake of AO and normals, glb; paint.py: painted albedo) -> client/assets/models/pebble_shell/; ChibiShell.cs puts it on the Spine bone (ChibiShell.Enabled; Showcase --built-shell for the old one). v1 render e48b64f7: big step up, but flat from the side and too orange. v2: higher dome, muted palette.

- 2026-10-17: v3 done: rounder lit centres, cooler browns; three shell colours and worn style checked (Showcase --variants). Before/after sheets in renders/art-rnd-1-shell-turntable.png and renders/art-rnd-1-shell-close.png (before = #594 at 17825e4e with the same Turntable camera, applied in a scratch worktree only). Pilot write-up in docs/art-pipeline.md. Sent to the PM.

- 2026-10-17: D-313 (cutesy, relaxing, hint of realism) from the PM: v4 49d7efa2 makes the shell cute: 3 vertebral + 2 costal a side, 16 chunky marginals, wide soft grooves, pastel caramel palette, cocoa seams, one faint ring. Sheets: renders/art-rnd-1-shell-cute-turntable.png, renders/art-rnd-1-shell-cute-close.png. Report updated with D-313 rules.

## Open questions / blockers
- Modeling owns client/scripts/models and client/assets/models: does Modeling take ChibiShell + tools/art/pebble_shell as is, or fold the method into #594? (PM to decide with Modeling.)
- Justin: is this the direction (painted texture on authored meshes) to roll out to other hero pieces?

## Next step
Wait for the PM's answer. Meanwhile (D-145), try stamped brush strokes sampled from the Golden Skull tiles in paint.py, plus slight asymmetry, and compare with the v3 turntable.
