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

## Queue (PM order 2026-10-17 08:27; the rest was dropped by the PM)
Done and sent to the PM: water before/after #517 (renders/water-before-2d9e6e2-*, water-after-d02f740-*), hold E at the spawn berry bush (hold-bush-0f4ecc5-bush).
1. hold-repeat dig check (PR #495 0f4ecc5d): "one dig then Still working, no repeat" may be a real bug; reshoot cleanly, if it reproduces tell the PM and Crafting (session_01N9APthjRDfjknUvCweqzzN)
2. Animation's three clips from ONE build, branch claude/anim-render (e76b914c, anim-12 to 16 merged; render-only, never push to it): anim-render-b (emotes 24-27 + eat, for Justin's morning), anim-render-c (fire sit, lie, stand), anim-render-a (idle fidgets, walk hop, jumps). Scenes in tools/render/scenes/anim-render-*.txt. Send to the PM and copy Animation (session_019roNRWQBPw4ozNLDhzseig)
3. anim-11 equip/hold in hand (main): scenes/anim-11-equip.txt; copy Animation
4. smoke signal #524 (c1fa9240): near, far; copy Atmosphere
5. atmos-shadows-1 (PR #667 claude/atmos-shadows-1 b1f013f6 (not 4e571073); before = main efc00f3b), request from session_014MSnDGYpnGPmnSQ4CjhF7c (Atmosphere): 4 clips (main noon, branch noon, main dusk ~0.725, branch dusk), ~10 s walking past trees in the default follow camera, same path and camera, --shadows=medium on the branch; look at shadow edges near the player and under trees; send to the PM. Queued last; asked them to ask the PM for priority.
5b. Atmosphere (session_014MSnDGYpnGPmnSQ4CjhF7c) adds: average frame time for both builds noon and dusk --shadows=medium (software rendering numbers are not GPU numbers, say so); main is now cbc623f with #667 merged, so use before = efc00f3b, after = cbc623f. Their tip: render.sh hung on later shots; a small autoload that saves the viewport after 260 s with --fixed-fps 30 worked (recipe in tickets/atmos-shadows-1.md).
6. LOW priority (D-142), Animation: anim-26 station crafts (branch claude/anim-26, PR #746): short clip or three stills, 3/4 front: pinch pot at the potter's bench (clay.pinch_pot), spin wool at the spindle (cord.spin_wool), scrape a hide at the drying rack (hide.scrape); check hands on/just above the station top, not inside or floating; tell Animation directly if hands clip. PM summary: "anim-26: at a station, the player now stands with the hands doing the craft's own work (pressing a pot, rolling fibre, scraping a hide) instead of one generic pose."
7. SOON, Justin's design pick (Animation): anim-29 jump wind-up prototype, branch claude/anim-jump-windup: side-view clip, real time + 0.25x slow motion, two jumps (Space), one standing, one walking, vs main's current jump side by side if possible. PM text: "anim-29 prototype: jump with a 0.1 s knee dip before take-off and a push-off with legs straight for a moment in the air (left), vs today's jump (right). Options: (a) wind-up + push-off [rec], (b) push-off only, no delay, (c) keep today's." Tell Animation directly if feet/toes look wrong in the push-off.
8. PM verification via Audio (session_01SXcxtoyeqDmypiHxkHedU6): PR #757 branch claude/audio-34-selfreg 2b98d793: run connected, report the exact client log line starting 'Audio: ' (expect 'Audio: 10 sound schedulers hooked up: DeerGrazing, EmoteSounds, LampSounds, ...') and any 'Audio:' error lines, plus whether footsteps or any cue plays (event counts fine). Send result to Audio and the PM. Job: audio-34.
Dropped: painted trees, tree fall, painted items, chibi, anim-10, anim-9 idle, water rain/no-reflect/stream, crafting-29 equip, painted-2 walk-past, anim-14 face close-up (anim-render-b shows faces 3/4). env-30 ambient life went to Render A. Scenes for dropped items exist in tools/render/scenes.

## Progress log
- 2026-10-17 03:45: took over from Render A; scenes already existed (tools/render/scenes), never rendered.
- 2026-10-17 05:20: first hold-repeat attempt unusable (see lessons); tooling fixed and pushed.
- 2026-10-17 05:55: water scenes re-aimed at the pond edge after a test showed the camera faced away; queue restarted.

## Open questions / blockers
- Water: ponds are only 0.4 m deep, so the "float and swim" part cannot be shown on main; reported to Atmosphere/PM in the clip message.

## Next step
Watch /tmp/render/cq.log (the queue loop); check each saved clip's frames, make a gif with ffmpeg palettegen (640 px, 10 fps), commit renders/, push, and send the PM the links.
