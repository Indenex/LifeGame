# claims-14: Stash moves take a count
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_review
Branch: claude/claims-14-stash-count
PR: (see below)

## Goal
`StashItem` and `TakeFromStash` take a `uint count`, so UX/UI's drag and drop (ui-18, PR #324) can move a whole stack in one call. The panel's Leave and Take buttons pass 1 and look the same.

## Progress log
- 2026-10-16: UX/UI asked (options 1 and 2); picked option 1. Server validates count >= 1 and no more than held or stashed; one log line per move (LogStash folds repeats within a minute). Callers updated: ClaimPanel buttons, SocialTest, CraftCheck. SocialTest has new checks for count 0 and over-count both ways.
- 2026-10-16: local (dotnet 8 + spacetimedb-cli in /tmp): server builds, bindings generated, SocialTest passes all checks on a freshly wiped database. A second run on the same database can fail "an empty /s says nothing" because chat from the previous run is still in the two-minute window; wipe or wait. The client was not built here (no Godot).

## Open questions / blockers
- UX/UI will drop a BagGrid into the stash section of ClaimPanel on their branch (#324) and keep the stash grid in a container that is not rebuilt on refresh.

## Next step
Wait for CI on the PR; send the PM the green sha, then tell UX/UI it is merged.
