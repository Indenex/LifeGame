# claims-18: Tell a player how to get a place to build
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_review
Branch: claude/claims-18-house-hint
PR: (see below)

## Goal
House-type buildings and stations are claim-only now (building-13, #362). The refusal must tell a new player what to do, and say whose land it is when it is someone else's.

## Progress log
- 2026-10-17: before, both refused with "A mud-brick house has to stand inside your claim", whether or not the land was claimed by someone else, and without saying how to claim. Now: on unclaimed land "... has to stand inside your claim. Raise a claim marker first (build menu), away from the waking clearing"; on someone else's claim "This land is claimed by <owner>. Ask them to add you, or build a <thing> on your own claim". Same wording in `Building.CheckClaimOnly` and `Stations.CheckStationSite` (House building's files; message text only). The unclaimed wording keeps "inside your claim", which BuildTest and CraftCheck look for. SocialTest has a check for the claimed-by-another case. Server builds, SocialTest passes locally; BuildTest and CraftCheck run in CI.

## Open questions / blockers
- (none)

## Next step
Wait for CI; send the PM the green sha. Tell House building (via the PM) that two message strings in their files changed.
