# art-rnd-1: Art quality R&D, better modelling methods and a turtle-shell pilot
Lane: Art quality R&D
Owner: Art R&D session (session_019Ee5efaMp7ec6dhFqQfV6R)
Status: blocked (waiting on Justin's tree-palette and rollout answers)
Branch: claude/art-rnd-1
PR: none for this branch (no CI run). The shell landed via Modeling's #623 (claude/model-14-authored-shell).

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

- 2026-10-17: Modeling landed the shell as PR #623 (claude/model-14-authored-shell, from 94479019); #594 closed. PM: Modeling lands the shell (option a). Shell colours re-picked to the D-313 palette (54381e65): caramel (0.54,0.42,0.28), honey (0.6,0.5,0.29), sage-olive (0.41,0.43,0.27); image renders/art-rnd-1-shell-colours.png.
- 2026-10-17: Justin asked for more Blender showcase pieces. Added PaintedAsset.cs and Showcase --view=art --piece=<p> (class LifeGame.Art<P>: Today(), Authored(), Spacing, Height). Three Opus helpers on worktree branches: trees (birch + pine), heads (Bramble, Pebble, faces with 4 expressions), sheep (+ axe). Each lands as tools/art/<piece>/, client/assets/art/<piece>/, client/scripts/models/art/Art<Piece>.cs and renders/art-rnd-1-<piece>.png; the coordinator merges them into claude/art-rnd-1 and sends links one at a time.

- 2026-10-17: merged sheep + stone axe (1ccca5b0): renders/art-rnd-1-sheep.png, renders/art-rnd-1-axe.png; sent to the PM.

- 2026-10-17: Modeling set up Blender (tools/art/get-blender.sh on #623) and reproduced the shell rebuild pixel-identical; #623 carries the no-grain shell.

- 2026-10-17: merged birch + pine: renders/art-rnd-1-birch.png, -pine.png (+ -close); sent to the PM.

- 2026-10-17: PM: tree palette (fresh vs olive) waits for Justin (world palette, D-154). Opus helper started on 3 seeded variants per tree (D-166) + a rounded pine tip.

- 2026-10-17: merged heads (3b3eb158): ChibiFace.cs (face atlas, expression 0 open, 1 closed, 2 happy, 3 sleepy via ChibiFace.SetExpression), ChibiFigure.AuthoredHeads (off by default); renders/art-rnd-1-bramble.png, -pebble-head.png, -faces.png.

- 2026-10-17: merged branch 282da54a builds and renders (faces check). PM: hair pass + Pebble beak before morning, then one sheet per piece and a list for the PM to present together with the rollout question.

- 2026-10-17: merged hair pass + Pebble beak; softened the beak line and nostrils (31d91f3e); re-rendered pebble-head and faces (f2592d55). Known: a small dark bake mark at Pebble's right temple.

- 2026-10-17: merged tree variants (2dd8bc7d); one sheet per piece; showcase index in docs/art-pipeline.md; list sent to the PM.

- 2026-10-17: merged rough-edge fixes (Pebble temple dip from the brow ridge, sheep wool -8% and blended puff seams, birch Spacing 4.7, Bramble side locks tucked in); sheets pebble-head, faces, bramble, sheep, birch changed; the PM told.

## Open questions / blockers
- WAITING ON JUSTIN (sent to the PM 2026-10-17, not defaulted): A. Tree palette (D-154, world palette): (a) fresh greens as shown, recommended, (b) partway to today's olive, (c) mixed, fresh birches and deeper pines.
- WAITING ON JUSTIN: B. Rollout of the Blender pipeline: (a) piece by piece, recommended: heads and faces first (already behind ChibiFigure.AuthoredHeads), then trees (need a far LOD and a wind term; Modeling), then the sheep (Animals) and the axe; (b) only new content; (c) not yet.
- Session state: final showcase sheets are at 59ed1e95 (renders/art-rnd-1-*.png); the index, numbers and rough edges are in docs/art-pipeline.md "Showcase". No helper sessions or worktrees are left running.

## Next step
Ask the PM for Justin's answers to A and B. If B is (a), write tickets/art-rnd-2.md for the heads rollout: Modeling switches on ChibiFigure.AuthoredHeads and authors the cropped haircut via tools/art/heads; Animation drives blinks with ChibiFace.SetExpression.
