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

## Open questions / blockers
- (none)

## Next step
Run `tools/render/setup.sh`, then `tools/render/render.sh render-1 origin/main "--time-of-day=0.45" "--time-of-day=0.02"` and send the two PNGs to the PM.
