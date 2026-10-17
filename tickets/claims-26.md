# claims-26: A banned player is told why
Lane: Claims
Owner: Claims thread
Status: in_review
Branch: claude/claims-26-ban-message
PR: (see PR list)

## Goal
A player under a Marshal's ban who tries to build, dig, take or lay a station on the town's claims is told "You are kept off <town>'s claims for about N more (min or h)" instead of the generic "This land is claimed by <owner>".

## Progress log
- 2026-10-17: `BanRefusal(ctx, claim, who)` in `TownPowers.cs` (null when no live ban applies); used by the three claim refusals (`RequireClaimAccess` in Claims.cs, `CheckClaimOnly` in Building.cs, the station check in Stations.cs). The time left is real time (minutes under an hour, else rounded hours). SocialTest's banned-A refusal now expects "kept off" and passes locally on a fresh database ("about 10 h more" for the 7-day cap). No schema change.

## Open questions / blockers
- Wording is the PM's/Justin's call (offered 3 options to the PM; built the recommended one).

## Next step
Wait for CI, send the green sha to the PM.
