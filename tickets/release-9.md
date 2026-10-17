# release-9: Say why when a build's file list is missing or damaged
Lane: Release & Launcher
Owner: Release & Launcher thread
Status: in_review
Branch: claude/release-12
PR: (see below)

## Goal
When the newest release has no `release.json` (still publishing) or a damaged one, the launcher said nothing and just stopped. It now says so ("Build N has no file list. It may still be publishing. Try Refresh in a minute.") and never crashes on a manifest with missing keys.

## Progress log
- 2026-10-17 08:10: `_fetch` in launcher.gd validates the manifest and sets a status for each failure; tested headless against the fake API with a release without release.json and one with a damaged one.

## Open questions / blockers
- None.

## Next step
Wait for CI, send the green sha to the PM.
