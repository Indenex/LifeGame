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

## Queue (PM order 2026-10-17 07:35; the rest was dropped by the PM)
1. water BEFORE on main 2d9e6e27 (AFTER done: renders/water-after-d02f740-water-after.mp4); both go to the PM and Atmosphere (session_01KvVF5vpkYEHTKNRpHC18WN)
2. hold-repeat dig check (PR #495 0f4ecc5d): "one dig then Still working, no repeat" may be a real bug; reshoot cleanly, if it reproduces tell the PM and Crafting (session_01N9APthjRDfjknUvCweqzzN)
3. anim-11 equip/hold in hand (main): scenes/anim-11-equip.txt; copy Animation (session_019roNRWQBPw4ozNLDhzseig)
4. anim-12 sit/lie + anim-13 idle fidgets and hop in one clip (branch claude/anim-13 e2a67c28): scenes/anim-12-13-rest-idle.txt; copy Animation
5. smoke signal #524 (c1fa9240): near, far; copy Atmosphere
Dropped: painted trees, tree fall, painted items, chibi, anim-10, anim-9 idle, water rain/no-reflect/stream, crafting-29 equip, painted-2 walk-past. Scenes for some exist in tools/render/scenes if they come back.

## Progress log
- 2026-10-17 03:45: took over from Render A; scenes already existed (tools/render/scenes), never rendered.
- 2026-10-17 05:20: first hold-repeat attempt unusable (see lessons); tooling fixed and pushed.
- 2026-10-17 05:55: water scenes re-aimed at the pond edge after a test showed the camera faced away; queue restarted.

## Open questions / blockers
- Water: ponds are only 0.4 m deep, so the "float and swim" part cannot be shown on main; reported to Atmosphere/PM in the clip message.

## Next step
Watch /tmp/render/cq.log (the queue loop); check each saved clip's frames, make a gif with ffmpeg palettegen (640 px, 10 fps), commit renders/, push, and send the PM the links.
