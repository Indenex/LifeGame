# render-c-1: clips and video for every lane
Lane: Render C (D-284)
Owner: Render C session_01KA7oA899HvQKhMWZhZYnuU
Status: in_progress
Branch: claude/render-c-clips (tooling and scenes; clips land in renders/ on this branch; raw links https://github.com/Indenex/LifeGame/raw/claude/render-c-clips/renders/<file>)
PR: none

## Goal
Short gameplay clips (5-15 s, 12 fps movie, 960x540) requested by lanes or the PM (session_01CvoSQGRe3vDvDXWQu2qruU). Every clip is sent to the PM only (plus the requester when it asks), as mp4 + small gif, with an honest one or two lines of what is seen.

## How to run (this container; Render A's notes are tools/render/WORKERS.md on claude/render-1-013qe8)
- `tools/render/setup.sh`, `apt-get update && apt-get install -y xdotool imagemagick ffmpeg`.
- Queue runner (not in the repo): a loop over a jobs file of `name tools/render/scene.sh <ticket> <sha|ref> <scene>`; env RENDER_WORK=/tmp/render RENDER_OUT=$PWD/renders RENDER_SIZE=960x540 RENDER_SCENE_TIMEOUT=7200. One render at a time. ~3-6 s per frame; a first build of a new sha is ~10 min.
- Lessons: default scene timeout (1800 s) killed the first run; the project viewport is 1600x900 so scene.sh now overrides it; ffmpeg needed -nostdin in the mark loop; the puppet's `give` needs the 8-column inventory_item and `walkto bush` needs forage_bush subscribed (both fixed here). `--debug-position` pins the player every frame (no walking); to place the player use `sql UPDATE player_pos SET x=..` (the client snaps to the server if >3 m off). The camera sits at (sin a, cos a)*dist from the player with a = 45 + render-cam yaw; w walks (-x,-z), s (+x,+z). Ponds on main are <= 0.4 m deep (no swimming); the pond near (30,76) starts at z about 78 (x 26-42).
- Check frames before sending (ffmpeg tile into a PNG and look).

## Queue (priority order; PM re-orders)
1. water before/after #517 (claude/atmosphere-31-water, latest head; before = main a6c49acb), then rain, no-reflection, stream take; autumn still (Atmosphere)
2. hold-repeat PR #495 (0f4ecc5d): bush, dig, tree (first run was unusable: no bush, no axe)
3. painted trees #544 (claude/model-10-painted-trees latest head): walk from spawn, day 0.55 and dusk 0.76, note pops at ~40 m
4. tree fall #537 on main: chop a pine until it falls, lie and sink, stump
5. chibi clip PR #466 (d9f8571c) + Animation anim-10 pose check (claude/model-6-chibi 163ac301)
6. smoke signal #524 (claude/atmosphere-32-smoke-signal, c1fa9240): near, far, night
7. dig with stick, wading (main), anim-9 idle clips (PR #508, warm and cold)
8. crafting-29 equip clips (PR #582 branch crafting-equip fbb3fea8): nothing equipped / axe equipped / axe in bag only
9. painted-6 items (PR #584, 4a90fc8b): swing stone axe and spade at noon, look for shimmering paint, hotbar icons
10. anim-11 (PR #586 claude/anim-11): equip/stow draw clips
11. painted-2 bushes walk-past clip (PR #561 10899ddd) if the PM wants it
12. anim-12 sit/lie clip (PR #600 claude/anim-12 a030b0ce): Rest(1) sit, Rest(2) lie, StandUp, ~3 s apart (or H key); chibi (Bramble/Pebble) and adult, side and 3/4 views; look at ground clipping of the lying shoulder/arm and the nameplate
13. anim-13 cutesy clips (PR #613 claude/anim-13 e2a67c28, chibi): idle fidgets ~30 s; sway by a lit fire ~20 s; two players 3 m apart (one waves); side-view walk (hop); a couple of jumps (squash/stretch)
Stills requests from Modeling (painted-1 rocks, painted-2 bushes) were forwarded to Render B.

## Progress log
- 2026-10-17 03:45: took over from Render A; scenes already existed (tools/render/scenes), never rendered.
- 2026-10-17 05:20: first hold-repeat attempt unusable (see lessons); tooling fixed and pushed.
- 2026-10-17 05:55: water scenes re-aimed at the pond edge after a test showed the camera faced away; queue restarted.

## Open questions / blockers
- Water: ponds are only 0.4 m deep, so the "float and swim" part cannot be shown on main; reported to Atmosphere/PM in the clip message.

## Next step
Watch /tmp/render/cq.log (the queue loop); check each saved clip's frames, make a gif with ffmpeg palettegen (640 px, 10 fps), commit renders/, push, and send the PM the links.
