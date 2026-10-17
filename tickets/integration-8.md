# integration-8: Merge queue and auto-merge
Lane: Integration
Owner: Integration thread
Status: in_review
Branch: claude/integration-merge-queue
PR: (see GitHub)

## Goal
GitHub merges green PRs itself through a merge queue, so merges no longer wait on a person. `build.yml` runs the three required checks on `merge_group`; docs/MERGE_QUEUE.md has Justin's exact clicks.

## Progress log
- 2026-10-17: added `merge_group` to build.yml. Removed paths-ignore from `pull_request` (a required check that never reports blocks a doc-only PR); a 'Doc-only change?' step now skips all other steps in the three jobs. Push to main keeps paths-ignore. Release already only picks `event=push` Builds of main, so queue branches are not released. Wrote docs/MERGE_QUEUE.md.

## Open questions / blockers
- Justin must do the settings clicks in docs/MERGE_QUEUE.md (the queue cannot be enabled from the repo).

## Next step
Wait for CI on the PR, send the PM the green sha, then start integration-9 (tickets/integration-9.md plan).
