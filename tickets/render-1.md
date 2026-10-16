# render-1: Set up the render pipeline
Lane: Render
Owner: unassigned (PM starts an Opus session for it)
Status: todo
Branch: (record the harness-assigned branch here)
PR: (once opened, for any tooling under tools/render/)

## Goal
Per D-059 the Render lane makes every screenshot and clip Justin sees. Done means: from a clean clone, one documented command (under `tools/render/`) builds a given commit, starts a local server and client, puts the camera at a requested place and time of day, and saves PNGs to /mnt/project-files/screenshots/<ticket>-<short sha>-<n>.png. First proof: render current `main` at spawn by day and by night and send the PM the images with a one-line summary. Justin sees those two images in the PM thread.

## Progress log
- 2026-10-16: ticket written by the PM (D-059).

## Open questions / blockers
- (none)

## Next step
Clone, run `tools/gen-bindings.sh`, build server and client, and find how existing screenshots were captured (`git log -S screenshots`, `client/scripts/ui/PauseMenu.cs`).
