# Render workers: setup notes (from Render A, 2026-10-17)

Three workers share the lane: A = look checks, B = UI and gameplay stills, C = clips. Each runs in its own
container (4 cores each; one render at a time per container). Use your own branch for images (for example
`claude/render-b` / `claude/render-c`, folder `renders/`) so pushes don't collide; start from
`claude/render-1-013qe8` (all tooling and scenes are here).

## Setup (once per container)
1. `git checkout -b claude/render-<b|c> origin/claude/render-1-013qe8`
2. `tools/render/setup.sh` (dotnet 8, SpacetimeDB 2.12.0, Godot 4.6.2 mono, mesa-vulkan, xvfb); `apt-get install -y xdotool imagemagick ffmpeg`.
3. `mkdir -p /tmp/render && cp tools/render/queue.done.snapshot /tmp/render/queue.done` (jobs already finished are skipped).
4. Start your queue detached:
   `RENDER_WORK=/tmp/render RENDER_OUT=$PWD/renders QUEUE_JOBS=tools/render/queue.<b|c>.jobs nohup tools/render/queue.sh > /tmp/render/queue.log 2>&1 &`
   queue.sh rereads the jobs file between jobs: add/reorder lines freely. A failed job is still marked done; remove its name from `/tmp/render/queue.done` to re-run it.
5. Watch: `tail -F /tmp/render/queue.log | grep -E '^== |saved|no image|fatal'`.

## Scripts
- `tools/render/render.sh <ticket> <ref> "<shot opts>" ...` builds the ref in a worktree, starts a throwaway server, runs one client per shot (software Forward+, ~0.5 fps). Shot options: `--offline` (no server), `--engine=gl_compatibility`, `--screenshot-delay=N`, `--debug-position=x,z`, `--screenshot-yaw=N` (whole 45 degree steps), `--render-cam=dist,height,yaw,look` (yaw is added to the game camera's 45 degrees; camera sits at (sin a, cos a)*dist from the player with a = 45 + yaw), `--camera-zoom=N` (negative = closer), `--time-of-day=0..1` (a FRACTION of the day, 0.5 noon), `--season=0..1` (0 spring, 0.25 summer, 0.5 autumn), plus the game's own flags.
- `tools/render/scene.sh <ticket> <ref> <scene.txt>`: a real client is the camera and a puppet player acts the scene. Steps are listed in the header of `tools/render/Puppet/Program.cs` (`give`, `craft`, `craftloop`, `key`, `keydown/keyup`, `click`, `snap`, `burst`, `sql`, `act <Reducer> ...` (any reducer by name, `sheep|bush|carcass|cellx|cellz` arguments), `walkto bush`, `frames N`, `mark x-start/x-end`). `client-as-puppet` makes the camera the puppet; `client-movie 12` records fixed-fps movies (clips are cut between `mark` pairs). Scenes live in `tools/render/scenes/`.
- `tools/render/scenes/model-1-style.sh` renders Modeling's Showcase views (`VIEWS="a --flag;b --flag"`, `;` when an argument holds commas).
- `tools/render/pair.sh ours.png <reference name> [out.png]` puts an art-look image beside `docs/art-reference/<name>.webp` (PM rule: every art-look image goes to the PM beside its reference).
- Render-tree-only patches (never committed to the game): `RENDER_PATCH=tools/render/patches/<x>.sh` (pitch.sh with RENDER_PITCH, buildtab.sh with RENDER_TAB). `nolook.sh` is applied automatically: it stops the first-join look picker covering stills.

## Pitfalls
- Movie rendering is slow (about 2-4 s per frame at 1280x720): 12 fps, short clips, `RENDER_SIZE=960x540` if needed. Do not press Return in scenes (it opens chat).
- Terrain loads chunk by chunk: `--screenshot-delay=90` for land shots (more with `--view-radius=8`).
- `{target}` is 0 in `client-as-puppet` scenes; SQL needs no WHERE for the single player.
- `build` in a scene adds ONE unit of material per call (a hut needs about 50 calls).
- Moving the player: `sql UPDATE player_pos SET x=..., z=..., chunk_x=floor(x/16), chunk_z=floor(z/16)`.
- Never edit a script while it runs; write a copy and `mv` it. Never `pkill -f` a pattern that matches your own shell.
- Check every image yourself before sending; say plainly what you see (report, don't judge design). Send the PM (session_01CvoSQGRe3vDvDXWQu2qruU) raw links `https://github.com/Indenex/LifeGame/raw/<your branch>/renders/<file>` and the requester's work summary.
- Tree to chop: (14.2,-2.0), stand at (13,-2). Clay ground everywhere near spawn. Water is at most 0.4 m deep near spawn (pond at (30,76)). Heath at (-4,-54) (dense heather patches). Giant trees: (-72,-155), (-159,56), (7,116), (30,12); deer trails (-68,38), (-22,-38), (32,-66).
