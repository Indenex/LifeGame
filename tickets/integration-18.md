# integration-18: Nightly ClayCheck and the follow-ups of the flake sweep
Lane: Integration
Owner: Integration thread (Sonnet)
Status: in_review
Branch: claude/integration-wrapup (this ticket); the nightly itself is on main (#814, e5c6a43)
PR: #814 merged; this ticket in the PR opened from claude/integration-wrapup

## Goal
Long checks that don't belong on every PR run on a schedule. `.github/workflows/nightly.yml` runs once a day (03:17 UTC) and on the manual button: it skips when main is unchanged since the last green nightly, builds the module, starts a server and runs `tools/ClayCheck` (about 13 minutes, 40-minute cap). It never blocks a PR or a merge. A red nightly is reported to the PM and to Crafting/Ceramics. Runner minutes are free (public repo, PM).

## Progress log
- 2026-10-17 14:33: #814 merged (e5c6a43); manual run 1 started: https://github.com/Indenex/LifeGame/actions/runs/39041224283 (in progress at the wrap-up; its result is NOT known yet).
- Same session, all merged: #527 subscriptions per lane, #679 Showcase views per lane, #591 SurvivalCheck/CharacterCheck/JournalCheck/ctx-sender in CI, #688 + #708 BuildTest gather robustness, #722 CraftCheck/strict-walk waits, #748 bots walk at 4 m/s, #762/#778/#783 Building check parts (coop+hearth+sundial, wooden chest, straw bed), #774 SocialTest town claims, #782 + #790 AnimalCheck wired in (cap 240 s), #703 WORKER_LOOP 5c. #787 closed unmerged (refused calls don't count toward pacing).

## Open questions / blockers
- None waiting on Justin.

## Next step
Read the first nightly run (https://github.com/Indenex/LifeGame/actions/runs/39041224283): if the `clay` job is green, note its time here; if red, send the failing log line to the PM and to Crafting/Ceramics. Then: main's Build on the newest sha (a run is started by every merge; the one for e5c6a43 was in progress), the forced-stall check that BotWalker's unstuck step fires (#556 is on main), and the after-numbers in tickets/integration-10.md. When a lane adds a BuildTest part, add its name to the `parts:` list or a new entry in the build-test matrix in build.yml.
