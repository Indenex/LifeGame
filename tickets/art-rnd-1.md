# art-rnd-1: Art quality R&D, better modelling methods and a turtle-shell pilot
Lane: Art quality R&D
Owner: Art R&D session (session_019Ee5efaMp7ec6dhFqQfV6R)
Status: in_progress
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

## Open questions / blockers
- (none)

## Next step
Render v2 with `tools/render/scenes/art-rnd-1-turntable.sh HEAD <name> --style=1` and review it. Then render the worn style and colour variants, commit the before/after images under renders/, and send them to the PM.
