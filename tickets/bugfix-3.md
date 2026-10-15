# bugfix-3: CI upgrade check (publish over the old world without a wipe)
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_progress
Branch: claude/bugfix-lyle6o (restarted from main e147e09)
PR: (not yet)

## Goal
Justin wiped onto 5adb2c1 and now publishes every merge over his world without `--delete-data`. CI on each pull request publishes the base branch's module, seeds it with a short load test, publishes the PR's module over it without `--delete-data`, and fails with "needs a world wipe" if that is refused. Then it plays briefly on the upgraded world. Script: `tools/upgrade-check.sh <base-ref>`; step in `.github/workflows/build.yml` (Crafting owns it; told via the PM).

## Progress log
- 2026-10-15 23:05: script written. Locally: main over main passes; adding a column to `player` fails with the world-wipe error.

## Open questions / blockers
- (none)

## Next step
Push, open the PR, check the CI step runs on it.
