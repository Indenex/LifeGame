# bugfix-64: F8 reports open a GitHub issue by themselves
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-5
PR: (opening)

## Goal
Justin picked fully automatic: every F8 report becomes an issue on Indenex/LifeGame with its screenshot and log tail, using his token. The token never ships in the client, the repo or any table a client can read: the publisher sets it with `set_bug_report_token` into the private `BugReportGitHub` table, and only the `SendBugReports` procedure (outbound HTTP) reads it. Screenshots go on the `bug-reports` branch (made from main on first use; CI only runs on main and PRs). Rate limits: 3 per player per hour, 10 per player per day, 40 per server per day; past that, and with no token, the report is kept for `/bugs`. The F8 window shows "Sending it to GitHub...", then "It's on GitHub as issue #N" or why it was kept.

## Progress log
- 2026-10-17: server `BugReportsGitHub.cs` (outbox, status view `my_bug_report_sends`, rate log, scheduled procedure, up to 3 attempts), hook in `SubmitBugReport`, client sends the log tail and a 1280-wide JPEG in 60 KB parts (`attach_bug_report`), no longer opens the zip folder. Setup clicks for Justin in `/mnt/project-files/bugfix/github-token-setup.md`, admin line in `docs/ADMIN.md`. No local toolchain here; CI is the build.

## Open questions / blockers
- (none) Untested against the real GitHub API until Justin sets the token; the first live report will show it.

## Next step
Wait for CI on the PR; once green send the sha to the PM. After the token is set, file one F8 report and check the issue.
