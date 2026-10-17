# animals-30: AnimalCheck covers the claim egg and leather rules
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-claim-check-5gkntn
PR: (to open)

## Goal
Stage 3 of animals-29 (PM: separate PR). AnimalCheck proves the animals-28 rules with two characters: A raises a claim marker 42 m or more out, B is no member. Eggs from hens inside the claim are refused to B ("These hens belong to A's yard") and taken by A; hide-rack leather someone else hung is refused to B, taken by A; whoever hung a hide takes it even inside another's claim; anyone takes eggs outside a claim; once A adds B as a member, B takes eggs and leather in the claim. New admin hook `AdminHangHide(x, z, madeBy)` in `server/spacetimedb/AdminTest.cs`, gated like AdminGive.

## Progress log
- 2026-10-17: built and run locally (dotnet in /tmp/dn, server from /root/stdb): all ok, about 57 s. Removing the egg claim check makes it fail; leather mutation run in progress.

## Open questions / blockers
- The workflow step has `timeout 120` and the tool now runs about 57 s locally; ask Integration to raise it to 240 for CI slack.

## Next step
Open the PR, ask Integration for the timeout, send the green sha to the PM.
