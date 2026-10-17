# bugfix-69: SurvivalCheck honest-run step no longer fails on one short pair
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-11
PR: #863

## Goal
"Load, spawn and social checks" failed on #862 (job 117450636001) in SurvivalCheck: "an honest run, rest, run
was slowed: 46.5 m, expected at least 47.4 m", 2% short, on a PR that does not touch SurvivalCheck. The bot
paces an 8 m stretch and turns at anything in the way, so one pair of 5 s runs can lose a stretch to a
trunk or the turn timing. A short pair now moves 3 m over and tries again (3 lanes at most); a real
slow-down from the server's stamina bar would show on every lane, so the check still catches it.

## Progress log
- 2026-10-17 17:05: fix written; no local dotnet here, CI is the compile check.
- 2026-10-17 17:10: #862's re-run failed the next step instead ("the bot got stuck while winded, 28.6 m in 20 s"); the winded step now retries on a new lane the same way (a too-high result, the cap failing, still fails at once).

## Open questions / blockers
- (none)

## Next step
Wait for this PR's "Load, spawn and social checks" to pass, then send the green sha to the PM.
