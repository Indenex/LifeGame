# release-7: Resizable launcher windows
Lane: Release & Launcher
Owner: Release & Launcher thread
Status: in_review
Branch: claude/release-10
PR: (see below)

## Goal
Justin: "the launcher needs to be resizeable". Both launchers' windows resize, with a sensible minimum, layouts that stretch (the What's new box grows, buttons stay at the bottom and wrap when narrow), the last size and position remembered in LifeGame/window.json, and DPI scaling respected on Windows.

## Progress log
- 2026-10-17 07:20: project.godot resizable, 860x560 default; window setup in launcher.gd (min size, wrap_controls, content scale from screen scale, restore/save window.json); button row is an HFlowContainer; notes box expands. Screenshots at 640x540, 800x600, 1280x720, 1920x1080 (player and host) look right; window.json round-trips (900x620 restored and re-saved).

## Open questions / blockers
- Windows DPI scaling and multi-monitor restore can't be tested here (Linux xvfb only).

## Next step
Wait for CI on the PR, send the green sha to the PM and the screenshots to Render B.
