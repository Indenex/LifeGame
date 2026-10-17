# bugfix-42: In-game bug report (F8)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: claude/bugfix-42
PR: (not yet)

## Goal
F8 (and later an Esc-menu entry) takes a screenshot, asks "What went wrong?", then writes
`user://bug-reports/<timestamp>.zip` (report.md, screenshot.png, log.txt of the last 200 lines),
opens that folder, puts a one-line summary on the clipboard and sends summary + report text to the
server (`submit_bug_report`, private `bug_report` table, `my_bug_reports` view: own reports, or all
for an admin). Admin `/bugs` lists the newest five. The auth token is never included.

## Progress log
- 2026-10-17: server `BugReports.cs`, client `bugreport/BugLog.cs`, `BugReportPanel.cs`, `Main.BugReport.cs`; hooks: Hud.Note and the unhandled-reducer-error handler feed BugLog, Panels.Open counts the panel, SocialLayer chat commands try `/bugs`, Controls lists F8. Tested headless on a scratch server: F8, text, send produced the zip (report.md, log.txt; screenshot only with a real display) and a server row.

## Open questions / blockers
- UX: style the window (Ui.Window/Title already used) and add an Esc-menu entry that calls `BugReportPanel.Open()`.
- Held item is not in the report yet (no hotbar "selected" accessor); biome name not in the report (no client biome lookup found).
- Launcher version comes from env LIFEGAME_LAUNCHER, build from `--build=` / LIFEGAME_BUILD; Release lane must set them.

## Next step
Push, wait for CI, send the green sha to the PM, ping UX, request a Render B still (F8 window over the world).
