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

## Open questions / blockers
- (none)

## Next step
When main 217dc13 day/night finishes (running), send them to the PM; then `tools/render/scene.sh claims-6 origin/claude/claims-chat-pvp-vk2qak tools/render/scenes/claims-6-chat.txt`, then the animals-8 scene at 573a057.
