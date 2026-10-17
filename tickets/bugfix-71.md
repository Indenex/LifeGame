# bugfix-71: Mark merged bugfix tickets done
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-13
PR: (opening)

## Goal
Tickets bugfix-29, 42, 55, 56 and 57 still said in_progress although their PRs are merged (#406, #541, #831, #846), so a fresh session would redo them. They now say done with the PR number. Tickets only; no code. Left alone on purpose: bugfix-53 (later hidden-value families may still be open), bugfix-40 (needs Justin's answers), and bugfix-3, 6, 7, 34 (their merge state is not clear from the PR list; the PM should say).

## Progress log
- 2026-10-17 17:55: edited and pushed.

## Open questions / blockers
- (none)

## Next step
Wait for CI, then send the PM the green sha.
