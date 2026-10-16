# render-1: Set up the render pipeline
Lane: Render
Owner: Render lane session session_013qe8zPzzBHx9rFPEYoKMr5
Status: in_progress
Branch: claude/render-1-013qe8 (no harness branch was assigned; named by the session)
PR: (once opened, for any tooling under tools/render/)

## Goal
Per D-059 the Render lane makes every screenshot and clip Justin sees. Done means: from a clean clone, one documented command (under `tools/render/`) builds a given commit, starts a local server and client, puts the camera at a requested place and time of day, and saves PNGs to /mnt/project-files/screenshots/<ticket>-<short sha>-<n>.png. First proof: render current `main` at spawn by day and by night and send the PM the images with a one-line summary. Justin sees those two images in the PM thread.

## Progress log
- 2026-10-16: ticket written by the PM (D-059).
- 2026-10-16 17:10: session started. Installed dotnet 8, spacetimedb-cli 2.12.0, Godot 4.6.2 mono, mesa-vulkan-drivers. Existing capture is built into the client: `--screenshot=path --screenshot-delay=s` (Main.cs), `--time-of-day`, `--rain`, `--debug-position` (Atmosphere.cs). Wrote `tools/render/setup.sh` (installs toolchain) and `tools/render/render.sh <ticket> <ref> "shot opts" ...` (worktree of the sha, server build, bindings, client build, in-memory server on :3300, one Xvfb Forward+ client run per shot). /mnt/project-files is not mounted here, so images default to `renders/` in the repo.
- 2026-10-16 17:20: render request from the PM: Atmosphere PR #66 (atmosphere-10/11) at dca2a0f: pond calm by day, pond in rain, rain close-up, cloud shadows, dawn mist (pond and spawn). Running alongside the main proof (217dc13).
- 2026-10-16 17:06: render request from Claims (session_013ECn821EbTpbj4h7d3PepH): claims-6, PR #71 at c126163: stacked speech bubbles, /me bubble and italic log line, dimmer far chat. Needs a second scripted player (B). Queued after #66.
- 2026-10-16 17:06: 4 cores here; two software-rendered clients at once never reached the screenshot in 15 min. Run one render at a time.
- 2026-10-16 17:15: render request from Atmosphere (session_01KvVF5vpkYEHTKNRpHC18WN): atmosphere-12 work sounds, af3bd67, audio clip from tools/SoundPreview.
- 2026-10-16 17:15: render request from Animals (session_01SAcVBUUfPFFk1rdXuiQZRU): animals-8, PR #90 at 573a057: tier-2 crafting menu with the three wool recipes, inventory with spindle/yarn/cloth, items on the ground.
- 2026-10-16 17:15: Queue: #66 (running), main day/night proof, claims-6 scene, atmosphere-12 audio, animals-8.
- 2026-10-16 17:35: root cause of the stuck shots: at ~0.5 fps Godot capped game time at 8 physics steps a frame, so a 30 s timer took ~10 real minutes. render.sh now writes an untracked client/override.cfg (20 physics ticks, 60 max steps) in the render checkout; shots take ~1-2 min each.
- 2026-10-16 17:40: atmosphere-12 audio sent to the PM (renders/audio/atmosphere-12-af3bd67-work-sounds.mp3, e1c6231).
- 2026-10-16 17:50: #66 six stills sent to the PM (renders/atmosphere-10-11-dca2a0f-1..6.png, 169f4ec).
- 2026-10-16 17:50: added tools/render/scene.sh + Puppet (scripted second player; can be the camera, give items via owner SQL, craft, drop, press keys, snap/clip on cue). Scenes: tools/render/scenes/claims-6-chat.txt, animals-8-wool.txt (untested).
- 2026-10-16 17:18: render request from Crafting (session_01Y2sA7pDBFch57bWb1uKMTq): crafting-11, PR #92 at 11ca4f8: --screenshot-fire=cold vs --screenshot-fire by day, optional bow-drill fire beside a boulder. Queue: main, claims-6, animals-8, crafting-11.
- 2026-10-16 17:58: render-1 proof done: main 217dc13 spawn by day and night (renders/render-1-217dc13-1/2.png, d4a7614) sent to the PM. Asked the PM: renders stay on this branch and only tools/render/ merges (recommended), or merge renders, or Drive.
- 2026-10-16 17:20: render request from Modeling (session_01JKX3FNjkrEkAqoTaFJiXAm): model-1 baseline, head of claude/model-1: player close-up idle and walking, sheep close-up, flock with player, clay hut late afternoon. Reply paths to Modeling too.
- 2026-10-16 17:20: render request from UX/UI (session_01V6qgbNbvTwniqW946KACLr): ui-9 trade window, 119c068, 1280x720, --screenshot-hud with --screenshot-trade[=accepted|changed|asked], before/after Mara accepts.
- 2026-10-16 17:23: ui-9 moved to e4319e5 (accepted = Mara green only; changed = Mara's accept cleared + amber line).
- 2026-10-16 17:25: claims-6 first try: client lags 2-4 s behind the puppet, so beats were missed; added `burst` snaps, rerun queued. Added render_cam.gd (--render-cam, --render-cam-target=sheep) for model-1 close-ups.
- 2026-10-16 17:30: render request from Animation (session_019roNRWQBPw4ozNLDhzseig): anim-1 at 8e7cbd5: clips of idle, walk (side), start/stop, circle (3/4), sneak. Added movie scenes (Godot --write-movie at fixed 24 fps of game time, so clips are smooth despite ~0.5 fps rendering); scenes anim-1-side.txt and anim-1-turn.txt.
- 2026-10-16 17:30: puppet `key` hung: bare Xvfb has no WM and the window class isn't Godot; now focuses the window by name.
- 2026-10-16 17:27: render request from Bug fixes (session_01HddD17VpF2CmiCxHoPH84Y): bugfix-17, PR #99 at caaf9ae: fired pinch pot on a fire, G prompt reads "Pick up the pinch pot", inventory after G with sticks unchanged; before shot on main 7e79d22.
- 2026-10-16 17:27: render request from Environment (session_012hVejTxLoxBpZyX3GQmNL9): env-1 before shots on main 1ade969: spawn (0,0) facing +x, pond edge (-20,-2) facing -x, dense forest (-46,-46), each noon and dusk; overhead of spawn from ~40 m at noon.
- 2026-10-16 17:35: animals-8 first run: crafting worked (toast "Weave wool cloth"), but panel shots missed the Cord and haft tab and the ground items weren't visible; redo queued (animals-8-wool.txt, animals-8-ground.txt).
- 2026-10-16 17:35: queues: [1] crafting-11 (running) -> [2] ui-9, claims-6 rerun, model-1 -> [3] anim-1 movies -> [4] animals-8 redo, env-1, bugfix-17 (fix caaf9ae + before 7e79d22). Each queue waits for the previous one's "done" line in its output file.

## Open questions / blockers
- PM: where renders should live long term (asked 17:58; default: renders/ on this branch, tools/render/ via PR).

## Next step
Review each finished render and send it to the PM (and to Modeling for model-1). If this session died: rerun the scenes listed above with tools/render/scene.sh / render.sh (one at a time).
