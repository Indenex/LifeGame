# release-6: Patch notes in the launcher
Lane: Release & Launcher
Owner: Release & Launcher thread
Status: in_review
Branch: claude/release-9
PR: (see below)

## Goal
Justin: "on every release, can the launcher show the latest patch notes?" Release notes are player-facing (`Player note:` line in PR bodies, else the title without ticket prefix; scribe/docs/ticket-only/ci-only skipped), written to `release.json` `notes` and the release body between `<!-- notes -->` markers. Both launchers show a "What's new" box (all builds between installed and newest, newest first) when an update is ready and after it lands, and a "What's new" button reopens it.

## Progress log
- 2026-10-17 05:30: merged-prs.sh picks up notes/skip; make-release.sh writes notes; launcher panel + button; tested headless against the fake API with three releases (shows 1500 then 1480 grouped); no release.yml change needed.

## Open questions / blockers
- None. Lanes should add a `Player note:` line to their PR bodies (PM to announce; Integration may add it to a PR template).

## Next step
Wait for CI on the PR, send the green sha to the PM, then verify the next release shows notes.
