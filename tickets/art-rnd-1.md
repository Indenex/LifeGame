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

## Open questions / blockers
- (none)

## Next step
Build the shell in Blender: tools/art/pebble_shell.py (model, bake, paint, export glb), then load it in ChibiFigure and render the turntable.
