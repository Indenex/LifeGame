# integration-18: Nightly ClayCheck and the follow-ups of the flake sweep
Lane: Integration
Owner: Integration thread (session_01XqJiErC6c8P4DJBSQNi1Br, this account since 2026-10-17)
Status: in_review
Branch: claude/integration-vflv1x (this ticket, from 2026-10-17 15:00); before that claude/integration-wrapup; the nightly itself is on main (#814, e5c6a43)
PR: #814 merged; this ticket in the PR opened from claude/integration-wrapup

## Goal
Long checks that don't belong on every PR run on a schedule. `.github/workflows/nightly.yml` runs once a day (03:17 UTC) and on the manual button: it skips when main is unchanged since the last green nightly, builds the module, starts a server and runs `tools/ClayCheck` (about 13 minutes, 40-minute cap). It never blocks a PR or a merge. A red nightly is reported to the PM and to Crafting/Ceramics. Runner minutes are free (public repo, PM).

## Progress log
- 2026-10-17 15:00: picked up on this account. Nightly run 39041224283 green: clay job 12 min 24 s (Clay check step 14:34:46 to 14:47:10). main's Build green on a21753d (run 39042327866) and on 5064a25 (#820, run 39042588662). #820 already merged. #819 (green at 50fc658, 38 behind main) branch updated with main as head of the merge queue; #826 next (CI on fdd4060 still running, 129 behind main). PR subscriptions refused for this session, so check PRs by hand.
- 2026-10-17 14:33: #814 merged (e5c6a43); manual run 1 started: https://github.com/Indenex/LifeGame/actions/runs/39041224283 (in progress at the wrap-up; its result is NOT known yet).
- Same session, all merged: #527 subscriptions per lane, #679 Showcase views per lane, #591 SurvivalCheck/CharacterCheck/JournalCheck/ctx-sender in CI, #688 + #708 BuildTest gather robustness, #722 CraftCheck/strict-walk waits, #748 bots walk at 4 m/s, #762/#778/#783 Building check parts (coop+hearth+sundial, wooden chest, straw bed), #774 SocialTest town claims, #782 + #790 AnimalCheck wired in (cap 240 s), #703 WORKER_LOOP 5c. #787 closed unmerged (refused calls don't count toward pacing).

## Open questions / blockers
- None waiting on Justin.

## Next step
When #819's run on its updated head is green, send the sha to the PM; then update #826's branch with main and do the same. After that: the forced-stall check that BotWalker's unstuck step fires (#556 is on main) and the after-numbers in tickets/integration-10.md. When a lane adds a BuildTest part, add its name to the `parts:` list or a new entry in the build-test matrix in build.yml.
