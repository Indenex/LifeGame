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

## Queue (state at 2026-10-17 14:35; PM wrap-up received)
DONE and sent to the PM (all on claude/render-c-clips, renders/): water before/after #517 (water-before-2d9e6e2-*, water-after-d02f740-*), hold E at the spawn bush (hold-bush-0f4ecc5-bush), hold F dig check (hold-dig-0f4ecc5-dig: one dig then 'Still working', unconfirmed because of fixed-step recording), emotes back view (anim-render-b-92f9141-emotes-back), audio checks for #757 (log line, audio-34-e892c49-1.png) and #794 (audio-stats-8d4b405-1.png; log: 'Audio: 11 sound schedulers hooked up: ...' and 'Audio cues (since last report): hen.cluck=3').
IN PROGRESS: jump wind-up pair (Animation anim-29, Justin's pick). NEW branch clip DONE: renders/jump-new-d96b811-jump.mp4 (claude/anim-jump-windup d96b811, profile view, 8.5 s). MAIN clip (job jump-main, origin/main) was recording at 14:34 (frame 127 of ~170); when it lands as renders/jump-main-<sha>-jump.mp4 compose: ffmpeg hstack of crop=640:540:160:0 of each (new left, main right; labels via drawtext DejaVuSans), plus slow 0.25x (setpts=4*PTS,fps=24); then send the PM (session_01CvoSQGRe3vDvDXWQu2qruU) with the text "anim-29 prototype: jump with a 0.1 s knee dip before take-off and a push-off with legs straight for a moment in the air (left), vs today's jump (right). Options: (a) wind-up + push-off [rec], (b) push-off only, no delay, (c) keep today's." and tell Animation (session_019roNRWQBPw4ozNLDhzseig) directly if feet/toes look wrong. Caveat to state: the toast 'No player for this connection' is on screen in both (the server refuses the camera client's reducers in my setup; the jump plays locally).
QUEUED, in /tmp/render/c.jobs order (the file is lost if the container is lost; recreate from this list): emotes front view (anim-render-b scene, claude/anim-render head; figure first steps toward camera) -> sit/lie via `act Rest 1`/`act Rest 2`/`act StandUp` (anim-render-c) -> idle fidgets/hop/jumps + spade (`act EquipSlot 0`, `act Dig cellx cellz`; anim-render-a) -> clay count for the hold-F dig check (hold-dig-count, PR #495 0f4ecc5d, prints clay count via puppet `sqlout`) -> anim-11 equip clips (main, scenes/anim-11-equip.txt) -> smoke signal #524 (c1fa9240, near/far). Low priority/unqueued requests: atmos-shadows-1 (#667/cbc623f, before = main efc00f3b, 4 clips + frame time; use b1f013f6 if still the head), anim-26 station hands (claude/anim-26), anim-14 faces (#647), crafting-29 equip clips (#582 merged to main), painted-items #584, painted-2 bushes walk-past (#561); Crafting's #780 minigame play-test cannot be done by me (told them/PM).
Dropped by the PM: painted trees, tree fall, chibi, anim-10, anim-9 idle, water extras. env-30 ambient life went to Render A. Stills requests from Modeling went to Render B (session_01AkKLF7VETNCk9bZ7SvfjkG).

## Progress log
- 2026-10-17 03:45: took over from Render A; scenes already existed (tools/render/scenes), never rendered.
- 2026-10-17 05:20 to 14:34: tooling faults found and fixed (see lessons): scene timeout, viewport size, ffmpeg -nostdin, inventory_item columns (give tries the 9-column shape then 8), forage_bush subscription, nolook patch, per-server token file, puppet sqlout, camera-client reducers refused ('No player for this connection', use `act` for server actions).
- 2026-10-17 14:35: wrap-up for PM; pushed; clips listed above.

## Open questions / blockers
- Waiting on nothing from Justin. PM to decide queue order for the remaining clips (above).
- Fire for anim-15 sit-by-the-fire: Animation says fire.hand_drill needs StartCraft score >= 0.35; the puppet's `craft` step uses the default score and lit no fire. Fix idea: `act StartCraft fire.hand_drill 1.0` (string arg support in the puppet's `act` is untested) then wait 4.5 s.

## Next step
Check /tmp/render/cq.log (the queue loop `bash /tmp/render/cq.sh`; restart it with `nohup /tmp/render/cq.sh &` if dead; it needs /tmp/render/c.jobs, c.done); when renders/jump-main-*-jump.mp4 exists, compose the jump pair as described above and send it to the PM; then continue the QUEUED list in order.
